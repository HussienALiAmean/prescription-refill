using Microsoft.Extensions.Logging;
using PrescriptionRefill.Application.Abstractions;
using PrescriptionRefill.Application.Common;
using PrescriptionRefill.Application.RefillRequests.Dtos;
using PrescriptionRefill.Domain.RefillRequests;

namespace PrescriptionRefill.Application.RefillRequests.Commands;

/// <summary>
/// Shared orchestration for approve and deny: load current stored data, let the Domain decide at
/// <c>now</c>, and commit with one <c>SaveChangesAsync</c> (one transaction, both row versions
/// checked). Logs identifiers only (constitution Principle VI).
/// </summary>
public abstract class DecisionCommand(
    RefillDecisionContextLoader loader,
    IRefillDbContext db,
    IRequestNotifier notifier,
    TimeProvider clock,
    ILogger logger)
{
    protected abstract string Action { get; }

    protected abstract DecisionResult Decide(RefillDecisionContext context, string? note, DateTimeOffset now);

    public async Task<DecisionOutcome> ExecuteAsync(int requestId, string? note, CancellationToken cancellationToken)
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
                return new DecisionOutcome.InvalidNote(invalid.Message);
            case DecisionResult.AlreadyDecided decided:
                return new DecisionOutcome.AlreadyDecided(decided.Status);
            case DecisionResult.Blocked blocked:
                logger.LogInformation("Refill request {RequestId} {Action} blocked by rules", requestId, Action);
                return new DecisionOutcome.Blocked(blocked.Evaluation.ToDto());
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
        return new DecisionOutcome.Succeeded(context.ToDetailDto(evaluation: null));
    }
}

public sealed class ApproveRefillRequest(
    RefillDecisionContextLoader loader,
    IRefillDbContext db,
    IRequestNotifier notifier,
    TimeProvider clock,
    ILogger<ApproveRefillRequest> logger) : DecisionCommand(loader, db, notifier, clock, logger)
{
    protected override string Action => "approval";

    protected override DecisionResult Decide(RefillDecisionContext context, string? note, DateTimeOffset now) =>
        context.Request.Approve(note, context.Facts, now);
}

public sealed class DenyRefillRequest(
    RefillDecisionContextLoader loader,
    IRefillDbContext db,
    IRequestNotifier notifier,
    TimeProvider clock,
    ILogger<DenyRefillRequest> logger) : DecisionCommand(loader, db, notifier, clock, logger)
{
    protected override string Action => "denial";

    protected override DecisionResult Decide(RefillDecisionContext context, string? note, DateTimeOffset now) =>
        context.Request.Deny(note, now);
}
