using PrescriptionRefill.Domain.Medications;
using PrescriptionRefill.Domain.Patients;
using PrescriptionRefill.Domain.Prescriptions;

namespace PrescriptionRefill.Domain.Evaluation;

/// <summary>Blocking reason codes (contracts/http-api.md "Evaluation object").</summary>
public static class BlockingReasonCodes
{
    public const string TooEarly = "TOO_EARLY";
    public const string NoRefillsRemaining = "NO_REFILLS_REMAINING";
    public const string InvalidPrescriptionData = "INVALID_PRESCRIPTION_DATA";
}

/// <summary>Warning codes; warnings never block approval (FR-008).</summary>
public static class WarningCodes
{
    public const string AllergyConflict = "ALLERGY_CONFLICT";
    public const string DrugInteraction = "DRUG_INTERACTION";
}

/// <param name="EligibleAt">Set only for <see cref="BlockingReasonCodes.TooEarly"/>.</param>
public sealed record BlockingReason(string Code, string Message, DateTimeOffset? EligibleAt = null);

public sealed record Warning(string Code, string Message, string MedicationName, string? Description = null);

/// <summary>The outcome of evaluating every refill rule at one instant (FR-005).</summary>
public sealed record RefillEvaluation(
    DateTimeOffset EvaluatedAt,
    DateTimeOffset? EligibleAt,
    IReadOnlyList<BlockingReason> BlockingReasons,
    IReadOnlyList<Warning> Warnings)
{
    public bool CanApprove => BlockingReasons.Count == 0;
}

/// <summary>
/// Stored data the rules read besides the requested prescription. Supplied as loaded; selecting
/// what counts (for example active medications) is the Domain's job, not the loader's.
/// </summary>
public sealed record EvaluationFacts(
    IReadOnlyCollection<Allergy> Allergies,
    IReadOnlyCollection<Prescription> PatientPrescriptions,
    IReadOnlyCollection<DrugInteraction> Interactions);
