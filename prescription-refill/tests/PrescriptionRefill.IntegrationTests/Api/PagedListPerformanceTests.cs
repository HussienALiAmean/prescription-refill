using System.Diagnostics;
using System.Net;
using PrescriptionRefill.Domain.Medications;
using PrescriptionRefill.Domain.Patients;
using PrescriptionRefill.Domain.Prescriptions;
using PrescriptionRefill.Domain.RefillRequests;
using PrescriptionRefill.IntegrationTests.Infrastructure;
using Xunit.Abstractions;
using static PrescriptionRefill.IntegrationTests.Infrastructure.Json;

namespace PrescriptionRefill.IntegrationTests.Api;

/// <summary>
/// 1,000 Pending requests (003 SC-001, SC-005): a page or search delivers at most the page size
/// and answers within 1 second. Every Pending request is still evaluated per load (003 research
/// R3), so this is the realistic upper bound of the design. Timings go to the test output.
/// </summary>
public sealed class PagedListPerformanceTests(SqlServerDatabaseFixture database, ITestOutputHelper output)
    : IClassFixture<SqlServerDatabaseFixture>, IDisposable
{
    private const int Patients = 100;
    private const int PrescriptionsPerPatient = 10;

    private readonly RefillApiFactory _api = new(database);
    private readonly TestData _data = new(database);

    public void Dispose() => _api.Dispose();

    private async Task ArrangeAsync(string medicationPrefix)
    {
        await using var db = _data.NewContext();
        var now = _api.Clock.GetUtcNow();
        var medications = Enumerable.Range(0, PrescriptionsPerPatient)
            .Select(i => new Medication(TestData.ArabicNameFor($"{medicationPrefix}{i:00}"), $"{medicationPrefix}{i:00}"))
            .ToList();
        db.Medications.AddRange(medications);

        for (var p = 0; p < Patients; p++)
        {
            var patient = new Patient($"Load Patient {p:000}", new DateOnly(1970, 1, 1));
            db.Patients.Add(patient);
            for (var m = 0; m < PrescriptionsPerPatient; m++)
            {
                var prescription = new Prescription(patient, medications[m], "1 tablet daily", 30, 2, now.AddDays(-25 - (m % 10)), isActive: true);
                db.Prescriptions.Add(prescription);
                db.RefillRequests.Add(RefillRequest.Create(prescription, now.AddMinutes(-(p * PrescriptionsPerPatient + m) - 1)));
            }
        }

        await db.SaveChangesAsync();
    }

    private async Task<TimeSpan> TimeAsync(HttpClient client, string query)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = await client.GetAsync($"/api/refill-requests{query}");
        stopwatch.Stop();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadAsync(response);
        Assert.True(body["items"]!.AsArray().Count <= 10);
        output.WriteLine($"{query}: {stopwatch.ElapsedMilliseconds} ms, matching {body["matchingCount"]}, total {body["statusTotal"]}");
        return stopwatch.Elapsed;
    }

    [Fact]
    public async Task A_page_or_search_over_1000_pending_requests_answers_within_one_second()
    {
        var prefix = $"Perf{Guid.NewGuid():N}"[..14];
        await ArrangeAsync(prefix);
        var client = _api.CreateClient();
        await client.GetAsync("/api/refill-requests?page=1&pageSize=10"); // warm-up

        var timings = new[]
        {
            await TimeAsync(client, "?page=1&pageSize=10"),
            await TimeAsync(client, "?page=100&pageSize=10"),
            await TimeAsync(client, $"?page=1&pageSize=10&search={prefix}07"),
        };

        Assert.All(timings, elapsed => Assert.True(elapsed < TimeSpan.FromSeconds(1), $"Took {elapsed.TotalMilliseconds} ms."));
    }
}
