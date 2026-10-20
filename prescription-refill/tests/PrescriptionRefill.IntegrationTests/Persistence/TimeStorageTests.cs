using Microsoft.EntityFrameworkCore;
using PrescriptionRefill.IntegrationTests.Infrastructure;

namespace PrescriptionRefill.IntegrationTests.Persistence;

public sealed class TimeStorageTests(SqlServerDatabaseFixture database) : IClassFixture<SqlServerDatabaseFixture>
{
    private readonly TestData _data = new(database);

    [Fact]
    public async Task Instant_saved_with_an_offset_reads_back_as_the_same_instant_in_UTC()
    {
        var arranged = await _data.ArrangeAsync(RefillApiFactory.StartTime);
        var withOffset = new DateTimeOffset(2026, 10, 1, 15, 30, 0, TimeSpan.FromHours(3));

        await using (var db = database.CreateContext())
        {
            var prescription = await db.Prescriptions.SingleAsync(p => p.Id == arranged.PrescriptionId);
            // Bypass the Domain constructor's normalization to exercise the storage converter itself.
            db.Entry(prescription).Property(p => p.LastFilledAt).CurrentValue = withOffset;
            await db.SaveChangesAsync();
        }

        var stored = await _data.GetPrescriptionAsync(arranged.PrescriptionId);

        Assert.Equal(TimeSpan.Zero, stored.LastFilledAt.Offset);
        Assert.Equal(withOffset.UtcTicks, stored.LastFilledAt.UtcTicks);
    }

    [Fact]
    public async Task Stored_offset_in_the_database_is_zero()
    {
        var arranged = await _data.ArrangeAsync(RefillApiFactory.StartTime);
        await using var db = database.CreateContext();

        var offsetMinutes = await db.Database
            .SqlQuery<int>($"SELECT DATEPART(TZOFFSET, LastFilledAt) AS Value FROM Prescriptions WHERE Id = {arranged.PrescriptionId}")
            .SingleAsync();

        Assert.Equal(0, offsetMinutes);
    }

    [Fact]
    public async Task One_tick_precision_round_trips()
    {
        var oddTicks = RefillApiFactory.StartTime.AddTicks(-1234567);
        var arranged = await _data.ArrangeAsync(RefillApiFactory.StartTime, sinceLastFill: RefillApiFactory.StartTime - oddTicks);

        var stored = await _data.GetPrescriptionAsync(arranged.PrescriptionId);

        Assert.Equal(oddTicks.UtcTicks, stored.LastFilledAt.UtcTicks);
    }
}
