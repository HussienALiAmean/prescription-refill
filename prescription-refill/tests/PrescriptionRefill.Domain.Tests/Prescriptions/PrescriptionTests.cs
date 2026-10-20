using static PrescriptionRefill.Domain.Tests.TestSupport.Builders;

namespace PrescriptionRefill.Domain.Tests.Prescriptions;

public sealed class PrescriptionTests
{
    [Fact]
    public void Consume_refill_at_one_leaves_zero()
    {
        var rx = Prescription(remainingRefills: 1);

        rx.ConsumeRefill();

        Assert.Equal(0, rx.RemainingRefills);
    }

    [Fact]
    public void Consume_refill_at_zero_is_rejected_and_never_goes_negative()
    {
        var rx = Prescription(remainingRefills: 0);

        Assert.Throws<InvalidOperationException>(rx.ConsumeRefill);
        Assert.Equal(0, rx.RemainingRefills);
    }

    [Fact]
    public void Constructor_normalizes_the_last_filled_time_to_utc()
    {
        var local = new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.FromHours(3));

        var rx = Prescription(lastFilledAt: local);

        Assert.Equal(TimeSpan.Zero, rx.LastFilledAt.Offset);
        Assert.Equal(local.UtcTicks, rx.LastFilledAt.UtcTicks);
    }
}
