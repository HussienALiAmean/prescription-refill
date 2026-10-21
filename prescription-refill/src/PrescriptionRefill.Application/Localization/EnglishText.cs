using System.Globalization;
using PrescriptionRefill.Domain.Evaluation;

namespace PrescriptionRefill.Application.Localization;

/// <summary>English texts, verbatim as before feature 005 so English output is unchanged (FR-017).</summary>
internal sealed class EnglishText : IAppText
{
    public string TooEarly(int daysSupply) =>
        $"Less than 80% of the {Number(daysSupply)}-day supply has elapsed since the last fill.";

    public string NoRefillsRemaining => "No refills remain on this prescription.";

    public string InvalidData(InvalidPrescriptionData kind) => kind switch
    {
        InvalidPrescriptionData.DaysSupplyNotPositive => "The prescription's days supply is not a positive number.",
        InvalidPrescriptionData.LastFilledMissing => "The prescription has no last filled time.",
        InvalidPrescriptionData.LastFilledAfterEvaluation => "The prescription's last filled time is later than the evaluation time.",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    public string AllergyConflict(string medicationName) =>
        $"The patient has a recorded allergy to {medicationName}.";

    public string DrugInteraction(string medicationName, string otherMedicationName) =>
        $"{medicationName} has a synthetic demo interaction with active medication {otherMedicationName}.";

    public string ValidationTitle => "The request is not valid.";

    public string NoteRequired => "A decision note is required.";

    public string NoteTooLong(int maxLength) => $"The decision note must be at most {Number(maxLength)} characters.";

    public string PrescriptionInactive => "Prescription is inactive.";

    public string PrescriptionIdRequired => "A prescription id is required.";

    public string StatusInvalid => "Status must be Pending, Approved, or Denied.";

    public string PageInvalid => "Page must be a whole number of at least 1.";

    public string PageSizeInvalid(int maxPageSize) => $"Page size must be a whole number from 1 to {Number(maxPageSize)}.";

    public string SearchTooLong(int maxLength) => $"Search must be at most {Number(maxLength)} characters.";

    public string RefillRequestNotFound => "The refill request was not found.";

    public string PrescriptionNotFound => "The prescription was not found.";

    public string AlreadyDecidedTitle => "The request has already been decided.";

    public string AlreadyDecidedDetail => "Decided requests cannot be changed.";

    public string PendingRequestExistsTitle => "The prescription already has a pending refill request.";

    public string PendingRequestExistsDetail => "Decide the existing request before creating another.";

    public string ConcurrencyConflictTitle => "The request changed while your decision was being saved.";

    public string ConcurrencyConflictDetail => "No changes were saved. Reload the request and decide again.";

    public string ApprovalBlockedTitle => "Approval is blocked.";

    public string ApprovalBlockedDetail => "The request cannot be approved based on current data. It can still be denied.";

    public string UnexpectedErrorTitle => "An unexpected error occurred.";

    public string? FrameworkProblemTitle(int statusCode) => null;

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
}
