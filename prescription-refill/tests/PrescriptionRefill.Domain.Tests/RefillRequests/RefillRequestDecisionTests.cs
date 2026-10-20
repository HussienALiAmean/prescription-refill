using PrescriptionRefill.Domain.Evaluation;
using PrescriptionRefill.Domain.Prescriptions;
using PrescriptionRefill.Domain.RefillRequests;
using static PrescriptionRefill.Domain.Tests.TestSupport.Builders;

namespace PrescriptionRefill.Domain.Tests.RefillRequests;

public sealed class RefillRequestDecisionTests
{
    private static (RefillRequest Request, Prescription Rx, EvaluationFacts Facts) Pending(int remainingRefills = 2, DateTimeOffset? lastFilledAt = null)
    {
        var rx = Prescription(remainingRefills: remainingRefills, lastFilledAt: lastFilledAt ?? Now.AddDays(-30));
        return (RefillRequest.Create(rx, Now.AddHours(-1)), rx, new EvaluationFacts([], [rx], []));
    }

    [Fact]
    public void Approve_from_pending_consumes_one_refill_and_records_the_decision()
    {
        var (request, rx, facts) = Pending();
        var lastFilled = rx.LastFilledAt;

        var result = request.Approve("  Looks fine.  ", facts, Now);

        Assert.Equal(new DecisionResult.Decided(RefillRequestStatus.Approved), result);
        Assert.Equal(RefillRequestStatus.Approved, request.Status);
        Assert.Equal(Now, request.DecidedAt);
        Assert.Equal("Looks fine.", request.DecisionNote);
        Assert.Equal(1, rx.RemainingRefills);
        Assert.Equal(lastFilled, rx.LastFilledAt);
    }

    [Fact]
    public void Deny_from_pending_does_not_change_the_prescription()
    {
        var (request, rx, _) = Pending();
        var lastFilled = rx.LastFilledAt;

        var result = request.Deny("Not needed.", Now);

        Assert.Equal(new DecisionResult.Decided(RefillRequestStatus.Denied), result);
        Assert.Equal(RefillRequestStatus.Denied, request.Status);
        Assert.Equal(Now, request.DecidedAt);
        Assert.Equal(2, rx.RemainingRefills);
        Assert.Equal(lastFilled, rx.LastFilledAt);
    }

    [Fact]
    public void Deny_is_allowed_while_approval_is_blocked()
    {
        var (request, _, _) = Pending(remainingRefills: 0);

        Assert.Equal(new DecisionResult.Decided(RefillRequestStatus.Denied), request.Deny("No refills.", Now));
    }

    [Fact]
    public void Blocked_approval_changes_nothing_and_returns_the_evaluation()
    {
        var (request, rx, facts) = Pending(remainingRefills: 0);

        var result = request.Approve("Approve please.", facts, Now);

        var blocked = Assert.IsType<DecisionResult.Blocked>(result);
        Assert.Equal(BlockingReasonCodes.NoRefillsRemaining, Assert.Single(blocked.Evaluation.BlockingReasons).Code);
        Assert.Equal(RefillRequestStatus.Pending, request.Status);
        Assert.Null(request.DecidedAt);
        Assert.Null(request.DecisionNote);
        Assert.Equal(0, rx.RemainingRefills);
    }

    [Fact]
    public void Approval_re_evaluates_at_decision_time()
    {
        // Too early one tick before the threshold, eligible exactly at it.
        var (request, _, facts) = Pending(lastFilledAt: Now - TimeSpan.FromSeconds(30 * 69_120L));

        Assert.IsType<DecisionResult.Blocked>(request.Approve("Early.", facts, Now.AddTicks(-1)));
        Assert.IsType<DecisionResult.Decided>(request.Approve("On time.", facts, Now));
    }

    [Theory]
    [InlineData(RefillRequestStatus.Approved)]
    [InlineData(RefillRequestStatus.Denied)]
    public void Decided_requests_reject_further_decisions_without_changes(RefillRequestStatus first)
    {
        var (request, rx, facts) = Pending();
        _ = first == RefillRequestStatus.Approved ? request.Approve("First.", facts, Now) : request.Deny("First.", Now);
        var decidedAt = request.DecidedAt;
        var refills = rx.RemainingRefills;

        var again = request.Approve("Second.", facts, Now.AddMinutes(5));
        var deny = request.Deny("Second.", Now.AddMinutes(5));

        Assert.Equal(new DecisionResult.AlreadyDecided(first), again);
        Assert.Equal(new DecisionResult.AlreadyDecided(first), deny);
        Assert.Equal(first, request.Status);
        Assert.Equal(decidedAt, request.DecidedAt);
        Assert.Equal("First.", request.DecisionNote);
        Assert.Equal(refills, rx.RemainingRefills);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void Blank_notes_are_rejected_for_both_decisions(string? note)
    {
        var (request, rx, facts) = Pending();

        Assert.IsType<DecisionResult.InvalidNote>(request.Approve(note, facts, Now));
        Assert.IsType<DecisionResult.InvalidNote>(request.Deny(note, Now));
        Assert.Equal(RefillRequestStatus.Pending, request.Status);
        Assert.Equal(2, rx.RemainingRefills);
    }

    [Fact]
    public void Note_of_maximum_length_is_accepted_and_one_more_is_rejected()
    {
        var (accepted, _, facts) = Pending();
        var (rejected, _, rejectedFacts) = Pending();

        Assert.IsType<DecisionResult.Decided>(accepted.Approve(new string('a', 1000), facts, Now));
        Assert.IsType<DecisionResult.InvalidNote>(rejected.Approve(new string('a', 1001), rejectedFacts, Now));
    }

    [Fact]
    public void Note_is_validated_before_the_status_check()
    {
        var (request, _, _) = Pending();
        request.Deny("First.", Now);

        Assert.IsType<DecisionResult.InvalidNote>(request.Deny(" ", Now));
    }
}
