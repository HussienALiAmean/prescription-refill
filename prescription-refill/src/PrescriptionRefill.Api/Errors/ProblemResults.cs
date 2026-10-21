using Microsoft.AspNetCore.Http.HttpResults;
using PrescriptionRefill.Application.Localization;
using PrescriptionRefill.Application.RefillRequests.Dtos;
using PrescriptionRefill.Domain.RefillRequests;

namespace PrescriptionRefill.Api.Errors;

/// <summary>
/// The contract's error table (contracts/http-api.md "Errors"), in one place. Every response is
/// ProblemDetails with a stable <c>code</c> extension. Only <c>title</c>, <c>detail</c>, and the
/// <c>errors</c> texts depend on the request language (spec 005 FR-002, FR-011).
/// </summary>
internal static class ProblemResults
{
    public static ProblemHttpResult NotFound(string title) =>
        Problem(StatusCodes.Status404NotFound, ErrorCodes.NotFound, title);

    public static ProblemHttpResult Validation(IAppText text, string field, string message) =>
        Problem(
            StatusCodes.Status400BadRequest,
            ErrorCodes.ValidationFailed,
            text.ValidationTitle,
            extensions: new() { ["errors"] = new Dictionary<string, string[]> { [field] = [message] } });

    public static ProblemHttpResult AlreadyDecided(IAppText text, int requestId, RefillRequestStatus status) =>
        Problem(
            StatusCodes.Status409Conflict,
            ErrorCodes.RequestAlreadyDecided,
            text.AlreadyDecidedTitle,
            text.AlreadyDecidedDetail,
            new() { ["requestId"] = requestId, ["requestStatus"] = status.ToString() });

    public static ProblemHttpResult PendingRequestExists(IAppText text, int prescriptionId) =>
        Problem(
            StatusCodes.Status409Conflict,
            ErrorCodes.PendingRequestExists,
            text.PendingRequestExistsTitle,
            text.PendingRequestExistsDetail,
            new() { ["prescriptionId"] = prescriptionId });

    public static ProblemHttpResult ConcurrencyConflict(IAppText text, int requestId) =>
        Problem(
            StatusCodes.Status409Conflict,
            ErrorCodes.ConcurrencyConflict,
            text.ConcurrencyConflictTitle,
            text.ConcurrencyConflictDetail,
            new() { ["requestId"] = requestId });

    public static ProblemHttpResult ApprovalBlocked(IAppText text, int requestId, EvaluationDto evaluation) =>
        Problem(
            StatusCodes.Status422UnprocessableEntity,
            ErrorCodes.ApprovalBlocked,
            text.ApprovalBlockedTitle,
            text.ApprovalBlockedDetail,
            new() { ["requestId"] = requestId, ["evaluation"] = evaluation });

    private static ProblemHttpResult Problem(
        int status,
        string code,
        string title,
        string? detail = null,
        Dictionary<string, object?>? extensions = null)
    {
        // `code` first, then the error-specific members (wire order follows insertion order).
        var all = new Dictionary<string, object?> { ["code"] = code };
        foreach (var (key, value) in extensions ?? [])
        {
            all[key] = value;
        }

        return TypedResults.Problem(detail, statusCode: status, title: title, extensions: all);
    }
}
