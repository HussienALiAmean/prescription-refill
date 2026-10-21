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

/// <summary>Which stored timing value made <see cref="BlockingReasonCodes.InvalidPrescriptionData"/> apply (FR-027).</summary>
public enum InvalidPrescriptionData
{
    DaysSupplyNotPositive,
    LastFilledMissing,
    LastFilledAfterEvaluation,
}

/// <summary>
/// A triggered blocking rule as a code plus the facts needed to explain it. Human-readable text is
/// composed outside Domain in the requested language (spec 005 research R6).
/// </summary>
/// <param name="EligibleAt">Set only for <see cref="BlockingReasonCodes.TooEarly"/>.</param>
/// <param name="InvalidData">Set only for <see cref="BlockingReasonCodes.InvalidPrescriptionData"/>.</param>
public sealed record BlockingReason(string Code, DateTimeOffset? EligibleAt = null, InvalidPrescriptionData? InvalidData = null);

/// <summary>A triggered warning and the reference data it is about; no text (spec 005 research R6).</summary>
/// <param name="Medication">The requested medication for an allergy conflict; the other active
/// medication for a drug interaction.</param>
/// <param name="Interaction">Set only for <see cref="WarningCodes.DrugInteraction"/>.</param>
public sealed record Warning(string Code, Medication Medication, DrugInteraction? Interaction = null);

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
