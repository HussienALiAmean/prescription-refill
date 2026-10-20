using PrescriptionRefill.Domain.Evaluation;
using PrescriptionRefill.Domain.Prescriptions;
using PrescriptionRefill.Domain.RefillRequests;

namespace PrescriptionRefill.Application.RefillRequests.Dtos;

internal static class DtoMapper
{
    public static EvaluationDto ToDto(this RefillEvaluation evaluation) => new(
        evaluation.EvaluatedAt,
        evaluation.CanApprove,
        evaluation.EligibleAt,
        evaluation.BlockingReasons.Select(r => new BlockingReasonDto(r.Code, r.Message, r.EligibleAt)).ToList(),
        evaluation.Warnings.Select(w => new WarningDto(w.Code, w.Message, w.MedicationName, w.Description)).ToList());

    /// <param name="evaluation">Evaluation to include; pass only while the request is Pending.</param>
    public static RefillRequestDetailDto ToDetailDto(this RefillDecisionContext context, RefillEvaluation? evaluation)
    {
        var request = context.Request;
        var prescription = request.Prescription;
        var patient = prescription.Patient;

        return new RefillRequestDetailDto(
            request.Id,
            request.Status,
            request.RequestedAt,
            request.DecidedAt,
            request.DecisionNote,
            new PatientDto(
                patient.Id,
                patient.FullName,
                patient.DateOfBirth,
                context.Facts.Allergies.Select(a => a.Medication.Name).OrderBy(n => n).ToList()),
            ToDto(prescription),
            // Same Domain definition as the interaction check (FR-022).
            ActiveMedications.For(prescription, context.Facts.PatientPrescriptions)
                .Select(p => new ActivePrescriptionDto(p.Id, p.Medication.Name, p.Dosage, p.RemainingRefills, p.LastFilledAt))
                .ToList(),
            request.Status == RefillRequestStatus.Pending ? evaluation?.ToDto() : null);
    }

    private static PrescriptionDto ToDto(Prescription prescription) => new(
        prescription.Id,
        prescription.Medication.Name,
        prescription.Dosage,
        prescription.DaysSupply,
        prescription.RemainingRefills,
        prescription.LastFilledAt,
        prescription.IsActive);
}
