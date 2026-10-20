using PrescriptionRefill.Domain.RefillRequests;
using static PrescriptionRefill.Domain.Tests.TestSupport.Builders;

namespace PrescriptionRefill.Domain.Tests.RefillRequests;

public sealed class CreateRefillRequestTests
{
    [Fact]
    public void Active_prescription_yields_a_pending_request_at_now_in_utc()
    {
        var rx = Prescription();
        var now = Now.ToOffset(TimeSpan.FromHours(3));

        var result = RefillRequest.TryCreate(rx, now);

        var request = Assert.IsType<CreateRequestResult.Created>(result).Request;
        Assert.Equal(RefillRequestStatus.Pending, request.Status);
        Assert.Same(rx, request.Prescription);
        Assert.Equal(Now, request.RequestedAt);
        Assert.Equal(TimeSpan.Zero, request.RequestedAt.Offset);
        Assert.Null(request.DecidedAt);
    }

    [Fact]
    public void Blocked_but_active_prescription_can_still_be_requested()
    {
        var rx = Prescription(remainingRefills: 0, lastFilledAt: Now.AddDays(-1));

        Assert.IsType<CreateRequestResult.Created>(RefillRequest.TryCreate(rx, Now));
    }

    [Fact]
    public void Inactive_prescription_is_rejected_with_a_validation_error()
    {
        var rx = Prescription(isActive: false);

        var result = RefillRequest.TryCreate(rx, Now);

        Assert.Equal("Prescription is inactive.", Assert.IsType<CreateRequestResult.PrescriptionInactive>(result).Message);
        Assert.Throws<InvalidOperationException>(() => RefillRequest.Create(rx, Now));
    }
}
