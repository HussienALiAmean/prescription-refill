using PrescriptionRefill.Application.Localization;
using PrescriptionRefill.Application.RefillRequests.Dtos;
using PrescriptionRefill.Domain.Evaluation;
using PrescriptionRefill.Domain.RefillRequests;

namespace PrescriptionRefill.Application.RefillRequests.Queries;

/// <summary>
/// Request detail with a fresh server evaluation while Pending (FR-005, FR-018). The rule read and
/// the evaluation are unchanged; the response's names and texts are in <paramref name="language"/>.
/// </summary>
public sealed class GetRefillRequestDetail(RefillDecisionContextLoader loader, LocalizedDtoBuilder dtos, TimeProvider clock)
{
    public async Task<RefillRequestDetailDto?> ExecuteAsync(int requestId, Language language, CancellationToken cancellationToken)
    {
        var context = await loader.LoadAsync(requestId, cancellationToken);
        if (context is null)
        {
            return null;
        }

        var evaluation = context.Request.Status == RefillRequestStatus.Pending
            ? RefillEvaluator.Evaluate(context.Request.Prescription, context.Facts, clock.GetUtcNow())
            : null;

        return await dtos.DetailAsync(context, evaluation, language, cancellationToken);
    }
}
