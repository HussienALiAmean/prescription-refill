using Microsoft.Extensions.Logging;
using PrescriptionRefill.Application.Abstractions;
using PrescriptionRefill.Application.Common;
using PrescriptionRefill.Application.Localization;
using PrescriptionRefill.Application.RefillRequests.Dtos;
using PrescriptionRefill.Domain.RefillRequests;

namespace PrescriptionRefill.Application.RefillRequests.Commands;

/// <summary>
/// Shared orchestration for approve and deny: load current stored data, let the Domain decide at
/// <c>now</c>, and commit with one <c>SaveChangesAsync</c> (one transaction, both row versions
/// checked). Logs identifiers only (constitution Principle VI). Approval re-evaluates every rule
/// against the data loaded here, inside this unit of work, as before (constitution Principle III);
/// <paramref name="language"/> only selects response text.
/// </summary>
public abstract class DecisionCommand(
    RefillDecisionContextLoader loader,
    LocalizedDtoBuilder dtos,
    IRefillDbContext db,
    IRequestNotifier notifier,
    TimeProvider clock,
    ILogger logger)
{
    protected abstract string Action { get; }

    protected abstract DecisionResult Decide(RefillDecisionContext context, string? note, DateTimeOffset now);

    /// <remarks>
    /// The success response is built from the entities this command committed (the same values as
    /// before 005), with names and texts read for <paramref name="language"/> after the commit and
    /// notification. That read changes neither the transaction nor the notification order.
    /// </remarks>
    public async Task<DecisionOutcome> ExecuteAsync(int requestId, string? note, Language language, CancellationToken cancellationToken)
    {
        var context = await loader.LoadAsync(requestId, cancellationToken);
        if (context is null)
        {
            return new DecisionOutcome.NotFound();
        }

        var result = Decide(context, note, clock.GetUtcNow());
        switch (result)
        {
            case DecisionResult.InvalidNote invalid:
                return new DecisionOutcome.InvalidNote(invalid.Error);
            case DecisionResult.AlreadyDecided decided:
                return new DecisionOutcome.AlreadyDecided(decided.Status);
            case DecisionResult.Blocked blocked:
                logger.LogInformation("Refill request {RequestId} {Action} blocked by rules", requestId, Action);
                return new DecisionOutcome.Blocked(
                    await dtos.EvaluationAsync(blocked.Evaluation, context.Request.Prescription, language, cancellationToken));
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            logger.LogWarning("Refill request {RequestId} {Action} rolled back after a concurrency conflict", requestId, Action);
            throw;
        }

        logger.LogInformation("Refill request {RequestId} {Action} committed", requestId, Action);
        await notifier.NotifyAfterCommitAsync(logger, requestId, RequestChange.Decided);
        return new DecisionOutcome.Succeeded(await dtos.DetailAsync(context, evaluation: null, language, cancellationToken));
    }
}

public sealed class ApproveRefillRequest(
    RefillDecisionContextLoader loader,
    LocalizedDtoBuilder dtos,
    IRefillDbContext db,
    IRequestNotifier notifier,
    TimeProvider clock,
    ILogger<ApproveRefillRequest> logger) : DecisionCommand(loader, dtos, db, notifier, clock, logger)
{
    protected override string Action => "approval";

    protected override DecisionResult Decide(RefillDecisionContext context, string? note, DateTimeOffset now) =>
        context.Request.Approve(note, context.Facts, now);
}

public sealed class DenyRefillRequest(
    RefillDecisionContextLoader loader,
    LocalizedDtoBuilder dtos,
    IRefillDbContext db,
    IRequestNotifier notifier,
    TimeProvider clock,
    ILogger<DenyRefillRequest> logger) : DecisionCommand(loader, dtos, db, notifier, clock, logger)
{
    protected override string Action => "denial";

    protected override DecisionResult Decide(RefillDecisionContext context, string? note, DateTimeOffset now) =>
        context.Request.Deny(note, now);
}
