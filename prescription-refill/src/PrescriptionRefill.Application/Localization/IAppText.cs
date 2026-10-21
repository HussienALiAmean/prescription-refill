using PrescriptionRefill.Domain.Evaluation;

namespace PrescriptionRefill.Application.Localization;

/// <summary>
/// Every human-readable server message, one typed member each (spec 005 FR-011, research R5). Both
/// languages implement it, so a message missing in either fails to compile. The texts are in
/// specs/005-bilingual-localization-themes/contracts/http-api.md. Codes stay separate (FR-002).
/// </summary>
public interface IAppText
{
    // Evaluation messages

    string TooEarly(int daysSupply);

    string NoRefillsRemaining { get; }

    string InvalidData(InvalidPrescriptionData kind);

    string AllergyConflict(string medicationName);

    string DrugInteraction(string medicationName, string otherMedicationName);

    // Validation (400 validation_failed)

    string ValidationTitle { get; }

    string NoteRequired { get; }

    string NoteTooLong(int maxLength);

    string PrescriptionInactive { get; }

    string PrescriptionIdRequired { get; }

    string StatusInvalid { get; }

    string PageInvalid { get; }

    string PageSizeInvalid(int maxPageSize);

    string SearchTooLong(int maxLength);

    // Other errors

    string RefillRequestNotFound { get; }

    string PrescriptionNotFound { get; }

    string AlreadyDecidedTitle { get; }

    string AlreadyDecidedDetail { get; }

    string PendingRequestExistsTitle { get; }

    string PendingRequestExistsDetail { get; }

    string ConcurrencyConflictTitle { get; }

    string ConcurrencyConflictDetail { get; }

    string ApprovalBlockedTitle { get; }

    string ApprovalBlockedDetail { get; }

    string UnexpectedErrorTitle { get; }

    /// <summary>
    /// Title for a problem the framework produced (malformed body, unknown route), or null to keep
    /// the framework's own title, which is today's English output.
    /// </summary>
    string? FrameworkProblemTitle(int statusCode);
}
