using PrescriptionRefill.Application.RefillRequests.Dtos;
using PrescriptionRefill.Domain.Evaluation;
using PrescriptionRefill.Domain.RefillRequests;

namespace PrescriptionRefill.Application.RefillRequests.Queries;

/// <summary>Request detail with a fresh server evaluation while Pending (FR-005, FR-018).</summary>
public sealed class GetRefillRequestDetail(RefillDecisionContextLoader loader, TimeProvider clock)
{
    public async Task<RefillRequestDetailDto?> ExecuteAsync(int requestId, CancellationToken cancellationToken)
    {
        var context = await loader.LoadAsync(requestId, cancellationToken);
        if (context is null)
        {
            return null;
        }

        var evaluation = context.Request.Status == RefillRequestStatus.Pending
            ? RefillEvaluator.Evaluate(context.Request.Prescription, context.Facts, clock.GetUtcNow())
            : null;

        return context.ToDetailDto(evaluation);
    }
}
