namespace PrescriptionRefill.Api.Errors;

/// <summary>Stable ProblemDetails <c>code</c> values (contracts/http-api.md "Errors").</summary>
public static class ErrorCodes
{
    public const string ValidationFailed = "validation_failed";
    public const string NotFound = "not_found";
    public const string RequestAlreadyDecided = "request_already_decided";
    public const string PendingRequestExists = "pending_request_exists";
    public const string ConcurrencyConflict = "concurrency_conflict";
    public const string ApprovalBlocked = "approval_blocked";
    public const string UnexpectedError = "unexpected_error";
}
