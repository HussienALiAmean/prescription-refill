using PrescriptionRefill.Application.Localization;
using PrescriptionRefill.Application.Mapping;
using PrescriptionRefill.Domain.Evaluation;
using PrescriptionRefill.Domain.Prescriptions;
using PrescriptionRefill.Domain.RefillRequests;

namespace PrescriptionRefill.Application.RefillRequests.Dtos;

/// <summary>
/// Builds response DTOs from a rule read plus localized reference text (spec 005 research R3).
/// Every safety value (refills, last filled, days supply, active flag, status, the evaluation)
/// comes from the loaded entities; names and descriptions come only from <see cref="ReferenceText"/>,
/// which was selected in SQL for one language. Messages come from the catalog by code.
/// </summary>
internal static class DtoMapper
{
    /// <summary>Medication ids whose names an evaluation's messages need.</summary>
    public static IEnumerable<int> MedicationIds(this RefillEvaluation evaluation, Prescription requested) =>
        evaluation.Warnings.Select(w => w.Medication.Id).Append(requested.MedicationId);

    public static IEnumerable<int> InteractionIds(this RefillEvaluation evaluation) =>
        evaluation.Warnings.Where(w => w.Interaction is not null).Select(w => w.Interaction!.Id);

    public static EvaluationDto ToDto(this RefillEvaluation evaluation, Prescription requested, ReferenceText reference, IAppText text) => new(
        evaluation.EvaluatedAt,
        evaluation.CanApprove,
        evaluation.EligibleAt,
        evaluation.BlockingReasons.Select(r => new BlockingReasonDto(r.Code, Message(r, requested, text), r.EligibleAt)).ToList(),
        evaluation.Warnings.Select(w => ToDto(w, requested, reference, text)).ToList());

    /// <param name="activePrescriptions">The Domain's selection (FR-022), in the Domain's order.</param>
    /// <param name="evaluation">Evaluation to include; pass only while the request is Pending.</param>
    public static RefillRequestDetailDto ToDetailDto(
        this RefillDecisionContext context,
        IReadOnlyList<Prescription> activePrescriptions,
        RefillEvaluation? evaluation,
        ReferenceText reference,
        IAppText text)
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
                // Ordered by the localized name, ordinal (spec 005 FR-023).
                context.Facts.Allergies.Select(a => reference.Name(a.MedicationId)).OrderBy(n => n, StringComparer.Ordinal).ToList()),
            ToDto(prescription, reference),
            activePrescriptions
                .Select(p => new ActivePrescriptionDto(p.Id, reference.Name(p.MedicationId), p.Dosage, p.RemainingRefills, p.LastFilledAt))
                .ToList(),
            request.Status == RefillRequestStatus.Pending ? evaluation?.ToDto(prescription, reference, text) : null);
    }

    private static string Message(BlockingReason reason, Prescription requested, IAppText text) => reason.Code switch
    {
        BlockingReasonCodes.TooEarly => text.TooEarly(requested.DaysSupply),
        BlockingReasonCodes.NoRefillsRemaining => text.NoRefillsRemaining,
        BlockingReasonCodes.InvalidPrescriptionData => text.InvalidData(reason.InvalidData
            ?? throw new InvalidOperationException("Invalid-data reason without a kind.")),
        _ => throw new InvalidOperationException($"Unknown blocking reason {reason.Code}."),
    };

    private static WarningDto ToDto(Warning warning, Prescription requested, ReferenceText reference, IAppText text)
    {
        var name = reference.Name(warning.Medication.Id);
        return warning.Code switch
        {
            WarningCodes.AllergyConflict => new WarningDto(warning.Code, text.AllergyConflict(name), name, null),
            WarningCodes.DrugInteraction => new WarningDto(
                warning.Code,
                text.DrugInteraction(reference.Name(requested.MedicationId), name),
                name,
                reference.Description((warning.Interaction ?? throw new InvalidOperationException("Interaction warning without an interaction.")).Id)),
            _ => throw new InvalidOperationException($"Unknown warning {warning.Code}."),
        };
    }

    private static PrescriptionDto ToDto(Prescription prescription, ReferenceText reference) => new(
        prescription.Id,
        reference.Name(prescription.MedicationId),
        prescription.Dosage,
        prescription.DaysSupply,
        prescription.RemainingRefills,
        prescription.LastFilledAt,
        prescription.IsActive);
}
