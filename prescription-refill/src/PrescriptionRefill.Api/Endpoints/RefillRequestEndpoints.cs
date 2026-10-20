using System.Globalization;
using Microsoft.Extensions.Primitives;
using PrescriptionRefill.Api.Errors;
using PrescriptionRefill.Application.Common;
using PrescriptionRefill.Application.RefillRequests.Commands;
using PrescriptionRefill.Application.RefillRequests.Queries;
using PrescriptionRefill.Domain.RefillRequests;

namespace PrescriptionRefill.Api.Endpoints;

/// <summary>Refill request endpoints (contracts/http-api.md). There is no update or delete.</summary>
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
            // Absent means Pending; anything else must be one of the status names exactly.
            var parsed = RefillRequestStatus.Pending;
            if (status is not null && !StatusNames.TryGetValue(status, out parsed))
            {
                return ProblemResults.Validation("status", "Status must be Pending, Approved, or Denied.");
            }

            var (paging, error) = ParsePaging(request.Query);
            if (error is not null)
            {
                return ProblemResults.Validation(error.Value.Field, error.Value.Message);
            }

            return TypedResults.Ok(await query.ExecuteAsync(new RefillRequestListQuery(parsed, paging), cancellationToken));
        });

        // API only; no UI calls it (FR-001).
        group.MapPost("/", async Task<IResult> (CreateBody? body, CreateRefillRequest command, CancellationToken cancellationToken) =>
        {
            if (body?.PrescriptionId is not { } prescriptionId || prescriptionId <= 0)
            {
                return ProblemResults.Validation("prescriptionId", "A prescription id is required.");
            }

            try
            {
                return await command.ExecuteAsync(prescriptionId, cancellationToken) switch
                {
                    CreateOutcome.Created created => TypedResults.Created($"/api/refill-requests/{created.Request.Id}", created.Request),
                    CreateOutcome.PrescriptionNotFound => ProblemResults.NotFound("prescription"),
                    CreateOutcome.Invalid invalid => ProblemResults.Validation(invalid.Field, invalid.Message),
                    var other => throw new InvalidOperationException($"Unhandled outcome {other}."),
                };
            }
            catch (PendingRequestExistsException)
            {
                return ProblemResults.PendingRequestExists(prescriptionId);
            }
        });

        group.MapGet("/{id:int}", async Task<IResult> (int id, GetRefillRequestDetail query, CancellationToken cancellationToken) =>
            await query.ExecuteAsync(id, cancellationToken) is { } detail
                ? TypedResults.Ok(detail)
                : ProblemResults.NotFound("refill request"));

        group.MapPost("/{id:int}/approve", (int id, DecisionBody? body, ApproveRefillRequest command, CancellationToken cancellationToken) =>
            DecideAsync(id, () => command.ExecuteAsync(id, body?.Note, cancellationToken)));

        group.MapPost("/{id:int}/deny", (int id, DecisionBody? body, DenyRefillRequest command, CancellationToken cancellationToken) =>
            DecideAsync(id, () => command.ExecuteAsync(id, body?.Note, cancellationToken)));

        return app;
    }

    /// <summary>
    /// Reads <c>page</c>, <c>pageSize</c>, and <c>search</c> (003 contracts/http-api.md). Paged when
    /// any of them is present, even empty (research R1). Read from the raw query so presence and
    /// exact text are known. The search text is never logged (003 FR-009).
    /// </summary>
    private static (PageRequest? Paging, (string Field, string Message)? Error) ParsePaging(IQueryCollection query)
    {
        if (!query.ContainsKey("page") && !query.ContainsKey("pageSize") && !query.ContainsKey("search"))
        {
            return (null, null);
        }

        var page = 1;
        if (query.TryGetValue("page", out var pageText) && (!TryParseWholeNumber(pageText, out page) || page < 1))
        {
            return (null, ("page", "Page must be a whole number of at least 1."));
        }

        var pageSize = RefillRequestListQuery.DefaultPageSize;
        if (query.TryGetValue("pageSize", out var pageSizeText)
            && (!TryParseWholeNumber(pageSizeText, out pageSize) || pageSize < 1 || pageSize > RefillRequestListQuery.MaxPageSize))
        {
            return (null, ("pageSize", $"Page size must be a whole number from 1 to {RefillRequestListQuery.MaxPageSize}."));
        }

        var search = query.TryGetValue("search", out var searchText) ? searchText.ToString().Trim() : string.Empty;
        if (search.Length > RefillRequestListQuery.MaxSearchLength)
        {
            return (null, ("search", $"Search must be at most {RefillRequestListQuery.MaxSearchLength} characters."));
        }

        return (new PageRequest(page, pageSize, search.Length == 0 ? null : search), null);
    }

    // Digits only: no sign, spaces, or decimals; repeated parameters fail (joined with a comma).
    private static bool TryParseWholeNumber(StringValues text, out int value) =>
        int.TryParse(text.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out value);

    private static async Task<IResult> DecideAsync(int id, Func<Task<DecisionOutcome>> decide)
    {
        try
        {
            return await decide() switch
            {
                DecisionOutcome.Succeeded success => TypedResults.Ok(success.Request),
                DecisionOutcome.NotFound => ProblemResults.NotFound("refill request"),
                DecisionOutcome.InvalidNote invalid => ProblemResults.Validation("note", invalid.Message),
                DecisionOutcome.AlreadyDecided decided => ProblemResults.AlreadyDecided(id, decided.Status),
                DecisionOutcome.Blocked blocked => ProblemResults.ApprovalBlocked(id, blocked.Evaluation),
                var other => throw new InvalidOperationException($"Unhandled outcome {other}."),
            };
        }
        catch (ConcurrencyConflictException)
        {
            // Raised only after the transaction was rolled back (FR-013); never retried (FR-016).
            return ProblemResults.ConcurrencyConflict(id);
        }
    }
}
