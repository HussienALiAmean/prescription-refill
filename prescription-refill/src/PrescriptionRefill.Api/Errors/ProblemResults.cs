using Microsoft.AspNetCore.Http.HttpResults;
using PrescriptionRefill.Application.RefillRequests.Dtos;
using PrescriptionRefill.Domain.RefillRequests;

namespace PrescriptionRefill.Api.Errors;

/// <summary>
/// The contract's error table (contracts/http-api.md "Errors"), in one place. Every response is
/// ProblemDetails with a stable <c>code</c> extension.
/// </summary>
internal static class ProblemResults
{
    public static ProblemHttpResult NotFound(string what) =>
        Problem(StatusCodes.Status404NotFound, ErrorCodes.NotFound, $"The {what} was not found.");

    public static ProblemHttpResult Validation(string field, string message) =>
        Problem(
            StatusCodes.Status400BadRequest,
            ErrorCodes.ValidationFailed,
            "The request is not valid.",
            extensions: new() { ["errors"] = new Dictionary<string, string[]> { [field] = [message] } });

    public static ProblemHttpResult AlreadyDecided(int requestId, RefillRequestStatus status) =>
        Problem(
            StatusCodes.Status409Conflict,
            ErrorCodes.RequestAlreadyDecided,
            "The request has already been decided.",
            "Decided requests cannot be changed.",
            new() { ["requestId"] = requestId, ["requestStatus"] = status.ToString() });

    public static ProblemHttpResult PendingRequestExists(int prescriptionId) =>
        Problem(
            StatusCodes.Status409Conflict,
            ErrorCodes.PendingRequestExists,
            "The prescription already has a pending refill request.",
            "Decide the existing request before creating another.",
            new() { ["prescriptionId"] = prescriptionId });

    public static ProblemHttpResult ConcurrencyConflict(int requestId) =>
        Problem(
            StatusCodes.Status409Conflict,
            ErrorCodes.ConcurrencyConflict,
            "The request changed while your decision was being saved.",
            "No changes were saved. Reload the request and decide again.",
            new() { ["requestId"] = requestId });

    public static ProblemHttpResult ApprovalBlocked(int requestId, EvaluationDto evaluation) =>
        Problem(
            StatusCodes.Status422UnprocessableEntity,
            ErrorCodes.ApprovalBlocked,
            "Approval is blocked.",
            "The request cannot be approved based on current data. It can still be denied.",
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
