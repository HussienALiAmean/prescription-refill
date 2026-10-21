using System.Globalization;
using Microsoft.Extensions.Primitives;
using PrescriptionRefill.Api.Errors;
using PrescriptionRefill.Api.Localization;
using PrescriptionRefill.Application.Localization;
using PrescriptionRefill.Application.Common;
using PrescriptionRefill.Application.RefillRequests.Commands;
using PrescriptionRefill.Application.RefillRequests.Queries;
using PrescriptionRefill.Domain.RefillRequests;

namespace PrescriptionRefill.Api.Endpoints;

/// <summary>
/// Refill request endpoints (contracts/http-api.md). There is no update or delete. Each handler
/// resolves the request language once and passes it on; parsing stays language-independent
/// (spec 005 FR-011).
/// </summary>
public static class RefillRequestEndpoints
{
    public sealed record DecisionBody(string? Note);

    public sealed record CreateBody(int? PrescriptionId);

    private static readonly Dictionary<string, RefillRequestStatus> StatusNames =
        Enum.GetValues<RefillRequestStatus>().ToDictionary(s => s.ToString(), StringComparer.OrdinalIgnoreCase);

    public static IEndpointRouteBuilder MapRefillRequestEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/refill-requests");

        group.MapGet("/", async Task<IResult> (string? status, HttpRequest request, GetRefillRequests query, CancellationToken cancellationToken) =>
        {
            var language = RequestLanguage.From(request.HttpContext);
            var text = AppText.For(language);

            // Absent means Pending; anything else must be one of the status names exactly.
            var parsed = RefillRequestStatus.Pending;
            if (status is not null && !StatusNames.TryGetValue(status, out parsed))
            {
                return ProblemResults.Validation(text, "status", text.StatusInvalid);
            }

            var (paging, error) = ParsePaging(request.Query, text);
            if (error is not null)
            {
                return ProblemResults.Validation(text, error.Value.Field, error.Value.Message);
            }

            return TypedResults.Ok(await query.ExecuteAsync(new RefillRequestListQuery(parsed, paging), language, cancellationToken));
        });

        // API only; no UI calls it (FR-001).
        group.MapPost("/", async Task<IResult> (CreateBody? body, HttpContext http, CreateRefillRequest command, CancellationToken cancellationToken) =>
        {
            var language = RequestLanguage.From(http);
            var text = AppText.For(language);
            if (body?.PrescriptionId is not { } prescriptionId || prescriptionId <= 0)
            {
                return ProblemResults.Validation(text, "prescriptionId", text.PrescriptionIdRequired);
            }

            try
            {
                return await command.ExecuteAsync(prescriptionId, language, cancellationToken) switch
                {
                    CreateOutcome.Created created => TypedResults.Created($"/api/refill-requests/{created.Request.Id}", created.Request),
                    CreateOutcome.PrescriptionNotFound => ProblemResults.NotFound(text.PrescriptionNotFound),
                    CreateOutcome.PrescriptionInactive => ProblemResults.Validation(text, "prescriptionId", text.PrescriptionInactive),
                    var other => throw new InvalidOperationException($"Unhandled outcome {other}."),
                };
            }
            catch (PendingRequestExistsException)
            {
                return ProblemResults.PendingRequestExists(text, prescriptionId);
            }
        });

        group.MapGet("/{id:int}", async Task<IResult> (int id, HttpContext http, GetRefillRequestDetail query, CancellationToken cancellationToken) =>
        {
            var language = RequestLanguage.From(http);
            return await query.ExecuteAsync(id, language, cancellationToken) is { } detail
                ? TypedResults.Ok(detail)
                : ProblemResults.NotFound(AppText.For(language).RefillRequestNotFound);
        });

        group.MapPost("/{id:int}/approve", (int id, DecisionBody? body, HttpContext http, ApproveRefillRequest command, CancellationToken cancellationToken) =>
            DecideAsync(id, RequestLanguage.From(http), language => command.ExecuteAsync(id, body?.Note, language, cancellationToken)));

        group.MapPost("/{id:int}/deny", (int id, DecisionBody? body, HttpContext http, DenyRefillRequest command, CancellationToken cancellationToken) =>
            DecideAsync(id, RequestLanguage.From(http), language => command.ExecuteAsync(id, body?.Note, language, cancellationToken)));

        return app;
    }

    /// <summary>
    /// Reads <c>page</c>, <c>pageSize</c>, and <c>search</c> (003 contracts/http-api.md). Paged when
    /// any of them is present, even empty (research R1). Read from the raw query so presence and
    /// exact text are known. The search text is never logged (003 FR-009).
    /// </summary>
    private static (PageRequest? Paging, (string Field, string Message)? Error) ParsePaging(IQueryCollection query, IAppText text)
    {
        if (!query.ContainsKey("page") && !query.ContainsKey("pageSize") && !query.ContainsKey("search"))
        {
            return (null, null);
        }

        var page = 1;
        if (query.TryGetValue("page", out var pageText) && (!TryParseWholeNumber(pageText, out page) || page < 1))
        {
            return (null, ("page", text.PageInvalid));
        }

        var pageSize = RefillRequestListQuery.DefaultPageSize;
        if (query.TryGetValue("pageSize", out var pageSizeText)
            && (!TryParseWholeNumber(pageSizeText, out pageSize) || pageSize < 1 || pageSize > RefillRequestListQuery.MaxPageSize))
        {
            return (null, ("pageSize", text.PageSizeInvalid(RefillRequestListQuery.MaxPageSize)));
        }

        var search = query.TryGetValue("search", out var searchText) ? searchText.ToString().Trim() : string.Empty;
        if (search.Length > RefillRequestListQuery.MaxSearchLength)
        {
            return (null, ("search", text.SearchTooLong(RefillRequestListQuery.MaxSearchLength)));
        }

        return (new PageRequest(page, pageSize, search.Length == 0 ? null : search), null);
    }

    // Digits only: no sign, spaces, or decimals; repeated parameters fail (joined with a comma).
    private static bool TryParseWholeNumber(StringValues text, out int value) =>
        int.TryParse(text.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out value);

    private static async Task<IResult> DecideAsync(int id, Language language, Func<Language, Task<DecisionOutcome>> decide)
    {
        var text = AppText.For(language);
        try
        {
            return await decide(language) switch
            {
                DecisionOutcome.Succeeded success => TypedResults.Ok(success.Request),
                DecisionOutcome.NotFound => ProblemResults.NotFound(text.RefillRequestNotFound),
                DecisionOutcome.InvalidNote invalid => ProblemResults.Validation(text, "note", invalid.Error switch
                {
                    NoteError.Required => text.NoteRequired,
                    NoteError.TooLong => text.NoteTooLong(RefillRequest.MaxNoteLength),
                    var error => throw new InvalidOperationException($"Unhandled note error {error}."),
                }),
                DecisionOutcome.AlreadyDecided decided => ProblemResults.AlreadyDecided(text, id, decided.Status),
                DecisionOutcome.Blocked blocked => ProblemResults.ApprovalBlocked(text, id, blocked.Evaluation),
                var other => throw new InvalidOperationException($"Unhandled outcome {other}."),
            };
        }
        catch (ConcurrencyConflictException)
        {
            // Raised only after the transaction was rolled back (FR-013); never retried (FR-016).
            return ProblemResults.ConcurrencyConflict(text, id);
        }
    }
}
