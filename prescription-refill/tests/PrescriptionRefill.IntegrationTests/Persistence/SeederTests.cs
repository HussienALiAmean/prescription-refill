using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using PrescriptionRefill.Domain.RefillRequests;
using PrescriptionRefill.Infrastructure.Seeding;
using PrescriptionRefill.IntegrationTests.Infrastructure;

namespace PrescriptionRefill.IntegrationTests.Persistence;

/// <summary>Uses its own database (class fixture) so the seed starts empty.</summary>
public sealed class SeederTests(SqlServerDatabaseFixture database) : IClassFixture<SqlServerDatabaseFixture>
{
    [Fact]
    public async Task Seeds_the_demo_scenarios_once()
    {
        var clock = new FakeTimeProvider(RefillApiFactory.StartTime);

        await using (var db = database.CreateContext())
        {
            Assert.True(await new DemoDataSeeder(db, clock).SeedAsync());
        }

        await using (var db = database.CreateContext())
        {
            Assert.False(await new DemoDataSeeder(db, clock).SeedAsync());
        }

        await using var check = database.CreateContext();
        Assert.Equal(11, await check.Medications.CountAsync());
        Assert.Equal(4, await check.DrugInteractions.CountAsync());
        Assert.Equal(6, await check.Patients.CountAsync());
        Assert.Equal(1, await check.Allergies.CountAsync());
        Assert.Equal(11, await check.Prescriptions.CountAsync());
        Assert.Equal(2, await check.Prescriptions.CountAsync(p => !p.IsActive));
        Assert.Equal(6, await check.RefillRequests.CountAsync(r => r.Status == RefillRequestStatus.Pending));
        Assert.Equal(1, await check.RefillRequests.CountAsync(r => r.Status == RefillRequestStatus.Approved));
        Assert.Equal(1, await check.RefillRequests.CountAsync(r => r.Status == RefillRequestStatus.Denied));
        Assert.True(await check.DrugInteractions.AllAsync(i => i.DescriptionEn.Contains("Synthetic")));
        Assert.True(await check.DrugInteractions.AllAsync(i => i.DescriptionAr == SeedNames.InteractionDescriptionAr));

        // Every seeded medication has its authored Arabic name (spec 005 FR-019).
        var names = await check.Medications.ToDictionaryAsync(m => m.NameEn, m => m.NameAr);
        Assert.Equal(SeedNames.Medications.ToDictionary(n => n.En, n => n.Ar), names);

        // Scenario 7's approval used one of Avery's four refills (data-model "Seed Scenarios").
        var avery = await check.Prescriptions.SingleAsync(p => p.Patient.FullName == "Avery Lindqvist" && p.IsActive);
        Assert.Equal(3, avery.RemainingRefills);
    }
}
