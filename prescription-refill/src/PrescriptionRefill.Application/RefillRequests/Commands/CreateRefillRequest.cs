using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PrescriptionRefill.Application.Abstractions;
using PrescriptionRefill.Application.Localization;
using PrescriptionRefill.Application.RefillRequests.Dtos;
using PrescriptionRefill.Application.RefillRequests.Queries;
using PrescriptionRefill.Domain.RefillRequests;

namespace PrescriptionRefill.Application.RefillRequests.Commands;

public abstract record CreateOutcome
{
    private CreateOutcome()
    {
    }

    public sealed record Created(RefillRequestDetailDto Request) : CreateOutcome;

    public sealed record PrescriptionNotFound : CreateOutcome;

    /// <summary>The prescription is inactive (FR-021); the API words the validation error.</summary>
    public sealed record PrescriptionInactive : CreateOutcome;
}

/// <summary>
/// API-only request creation (FR-001–FR-004, FR-021). Order: not found → inactive → insert; a
/// second Pending request surfaces as <c>PendingRequestExistsException</c> from the database.
/// </summary>
public sealed class CreateRefillRequest(IRefillDbContext db,
    GetRefillRequestDetail detail,
    IRequestNotifier notifier,
    TimeProvider clock,
    ILogger<CreateRefillRequest> logger)
{
    public async Task<CreateOutcome> ExecuteAsync(int prescriptionId, Language language, CancellationToken cancellationToken)
    {
        var prescription = await db.Prescriptions.SingleOrDefaultAsync(p => p.Id == prescriptionId, cancellationToken);
        if (prescription is null)
        {
            return new CreateOutcome.PrescriptionNotFound();
        }

        switch (RefillRequest.TryCreate(prescription, clock.GetUtcNow()))
        {
            case CreateRequestResult.PrescriptionInactive:
                return new CreateOutcome.PrescriptionInactive();
            case CreateRequestResult.Created created:
                db.RefillRequests.Add(created.Request);
                await db.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Refill request {RequestId} created for prescription {PrescriptionId}", created.Request.Id, prescriptionId);
                await notifier.NotifyAfterCommitAsync(logger, created.Request.Id, RequestChange.Created);
                return new CreateOutcome.Created((await detail.ExecuteAsync(created.Request.Id, language, cancellationToken))!);
            default:
                throw new InvalidOperationException("Unexpected creation result.");
        }
    }
}
