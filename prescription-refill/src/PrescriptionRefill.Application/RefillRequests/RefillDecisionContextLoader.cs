using Microsoft.EntityFrameworkCore;
using PrescriptionRefill.Application.Abstractions;
using PrescriptionRefill.Domain.Evaluation;
using PrescriptionRefill.Domain.RefillRequests;

namespace PrescriptionRefill.Application.RefillRequests;

/// <summary>A tracked request plus the stored data its rules read.</summary>
public sealed record RefillDecisionContext(RefillRequest Request, EvaluationFacts Facts);

/// <summary>
/// Loads everything the rules need from SQL Server, the only source of truth (FR-028). It applies
/// no rule filtering: the Domain decides which prescriptions are active and what matches (FR-022).
/// Entities are tracked so a decision saved from this context carries the loaded row versions.
/// </summary>
public sealed class RefillDecisionContextLoader(IRefillDbContext db)
{
    public async Task<RefillDecisionContext?> LoadAsync(int requestId, CancellationToken cancellationToken)
    {
        var request = await db.RefillRequests
            .Include(r => r.Prescription).ThenInclude(p => p.Medication)
            .Include(r => r.Prescription).ThenInclude(p => p.Patient).ThenInclude(p => p.Allergies).ThenInclude(a => a.Medication)
            .SingleOrDefaultAsync(r => r.Id == requestId, cancellationToken);

        if (request is null)
        {
            return null;
        }

        var prescription = request.Prescription;
        var patientPrescriptions = await db.Prescriptions
            .Include(p => p.Medication)
            .Where(p => p.PatientId == prescription.PatientId)
            .ToListAsync(cancellationToken);

        // Narrows the query to pairs that involve the requested medication; matching itself is
        // done by the Domain evaluator.
        var interactions = await db.DrugInteractions
            .Include(i => i.MedicationA)
            .Include(i => i.MedicationB)
            .Where(i => i.MedicationAId == prescription.MedicationId || i.MedicationBId == prescription.MedicationId)
            .ToListAsync(cancellationToken);

        return new RefillDecisionContext(
            request,
            new EvaluationFacts(prescription.Patient.Allergies.ToList(), patientPrescriptions, interactions));
    }
}
