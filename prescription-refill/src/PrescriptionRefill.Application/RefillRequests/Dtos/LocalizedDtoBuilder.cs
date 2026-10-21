using PrescriptionRefill.Application.Localization;
using PrescriptionRefill.Application.Mapping;
using PrescriptionRefill.Domain.Evaluation;
using PrescriptionRefill.Domain.Prescriptions;

namespace PrescriptionRefill.Application.RefillRequests.Dtos;

/// <summary>
/// Pairs a rule read with its one display read: collects the ids a response needs, reads their
/// localized text in one language, and builds the DTO (spec 005 research R3).
/// </summary>
public sealed class LocalizedDtoBuilder(LocalizedReferenceData referenceData)
{
    /// <param name="evaluation">Evaluation to include; pass only while the request is Pending.</param>
    public async Task<RefillRequestDetailDto> DetailAsync(
        RefillDecisionContext context,
        RefillEvaluation? evaluation,
        Language language,
        CancellationToken cancellationToken)
    {
        var prescription = context.Request.Prescription;

        // Same Domain definition as the interaction check (FR-022).
        var active = ActiveMedications.For(prescription, context.Facts.PatientPrescriptions);

        var medicationIds = context.Facts.Allergies.Select(a => a.MedicationId)
            .Concat(active.Select(p => p.MedicationId))
            .Append(prescription.MedicationId)
            .Concat(evaluation?.MedicationIds(prescription) ?? []);
        var reference = await referenceData.ReadAsync(medicationIds, evaluation?.InteractionIds() ?? [], language, cancellationToken);

        return context.ToDetailDto(active, evaluation, reference, AppText.For(language));
    }

    public async Task<EvaluationDto> EvaluationAsync(
        RefillEvaluation evaluation,
        Prescription requested,
        Language language,
        CancellationToken cancellationToken)
    {
        var reference = await referenceData.ReadAsync(
            evaluation.MedicationIds(requested), evaluation.InteractionIds(), language, cancellationToken);
        return evaluation.ToDto(requested, reference, AppText.For(language));
    }
}
