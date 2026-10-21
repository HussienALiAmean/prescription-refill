using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PrescriptionRefill.Api.Localization;

namespace PrescriptionRefill.Api.Errors;

/// <summary>
/// Last-resort handler: returns <c>500 unexpected_error</c> without internal details (contract
/// error table). The exception is logged with the trace identifier only.
/// </summary>
internal sealed class UnexpectedErrorHandler(
    IProblemDetailsService problemDetails,
    ILogger<UnexpectedErrorHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception for request {TraceId}", httpContext.TraceIdentifier);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Title = RequestLanguage.Text(httpContext).UnexpectedErrorTitle,
                Status = StatusCodes.Status500InternalServerError,
                Extensions = { ["code"] = ErrorCodes.UnexpectedError },
            },
        });
    }
}
