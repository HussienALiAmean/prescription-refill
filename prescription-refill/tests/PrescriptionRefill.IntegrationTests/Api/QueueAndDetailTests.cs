using System.Net;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PrescriptionRefill.Application.RefillRequests;
using PrescriptionRefill.Infrastructure.Persistence;
using PrescriptionRefill.IntegrationTests.Infrastructure;
using static PrescriptionRefill.IntegrationTests.Infrastructure.Json;

namespace PrescriptionRefill.IntegrationTests.Api;

public sealed partial class QueueAndDetailTests(SqlServerDatabaseFixture database)
    : IClassFixture<SqlServerDatabaseFixture>, IDisposable
{
    private readonly RefillApiFactory _api = new(database);
    private readonly TestData _data = new(database);
    private DateTimeOffset Now => _api.Clock.GetUtcNow();

    public void Dispose() => _api.Dispose();

    [Fact]
    public async Task Pending_queue_lists_requests_oldest_first_with_one_evaluation_time()
    {
        var newer = await _data.ArrangeAsync(Now, requestedAgo: TimeSpan.FromHours(1));
        var older = await _data.ArrangeAsync(Now, remainingRefills: 0, interactingActive: true, requestedAgo: TimeSpan.FromHours(5));
        var client = _api.CreateClient();

        var response = await client.GetAsync("/api/refill-requests");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadAsync(response);
        AssertUtcInstant(body["evaluatedAt"], Now);
        var items = body["items"]!.AsArray();
        var ids = items.Select(i => i!["id"]!.GetValue<int>()).ToList();
        Assert.True(ids.IndexOf(older.RequestId!.Value) < ids.IndexOf(newer.RequestId!.Value));

        var olderItem = items.Single(i => i!["id"]!.GetValue<int>() == older.RequestId)!;
        Assert.Equal(
            ["id", "status", "requestedAt", "decidedAt", "patientName", "medicationName", "canApprove", "hasWarnings"],
            olderItem.PropertyNames());
        Assert.Equal("Pending", olderItem["status"]!.GetValue<string>());
        Assert.False(olderItem["canApprove"]!.GetValue<bool>());
        Assert.True(olderItem["hasWarnings"]!.GetValue<bool>());
        Assert.Equal(older.MedicationName, olderItem["medicationName"]!.GetValue<string>());

        var newerItem = items.Single(i => i!["id"]!.GetValue<int>() == newer.RequestId)!;
        Assert.True(newerItem["canApprove"]!.GetValue<bool>());
        Assert.False(newerItem["hasWarnings"]!.GetValue<bool>());
    }

    [Fact]
    public async Task Detail_shows_profile_allergies_active_prescriptions_and_evaluation()
    {
        var arranged = await _data.ArrangeAsync(Now, allergicToRequested: true, interactingActive: true);
        var client = _api.CreateClient();

        var response = await client.GetAsync($"/api/refill-requests/{arranged.RequestId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadAsync(response);
        Assert.Equal(
            ["id", "status", "requestedAt", "decidedAt", "decisionNote", "patient", "prescription", "activePrescriptions", "evaluation"],
            body.PropertyNames());
        Assert.Null(body["decidedAt"]);
        Assert.Null(body["decisionNote"]);

        var patient = body["patient"]!;
        Assert.Equal(arranged.PatientId, patient["id"]!.GetValue<int>());
        Assert.Equal("1980-01-01", patient["dateOfBirth"]!.GetValue<string>());
        Assert.Equal([arranged.MedicationName], patient["allergies"]!.AsArray().Select(a => a!.GetValue<string>()));

        var prescription = body["prescription"]!;
        Assert.Equal(
            ["id", "medicationName", "dosage", "daysSupply", "remainingRefills", "lastFilledAt", "isActive"],
            prescription.PropertyNames());
        Assert.Equal(arranged.PrescriptionId, prescription["id"]!.GetValue<int>());
        AssertUtcInstant(prescription["lastFilledAt"], Now.AddDays(-30));

        var active = Assert.Single(body["activePrescriptions"]!.AsArray())!;
        Assert.Equal(["id", "medicationName", "dosage", "remainingRefills", "lastFilledAt"], active.PropertyNames());
        Assert.NotEqual(arranged.PrescriptionId, active["id"]!.GetValue<int>());

        var evaluation = body["evaluation"]!;
        Assert.Equal(["evaluatedAt", "canApprove", "eligibleAt", "blockingReasons", "warnings"], evaluation.PropertyNames());
        AssertUtcInstant(evaluation["evaluatedAt"], Now);
        AssertUtcInstant(evaluation["eligibleAt"], Now.AddDays(-30).AddDays(24));
        Assert.True(evaluation["canApprove"]!.GetValue<bool>());
        Assert.Equal(
            ["ALLERGY_CONFLICT", "DRUG_INTERACTION"],
            evaluation["warnings"]!.AsArray().Select(w => w!["code"]!.GetValue<string>()));
        Assert.Equal(
            ["code", "message", "medicationName", "description"],
            evaluation["warnings"]![0]!.PropertyNames());
    }

    [Fact]
    public async Task Inactive_other_prescriptions_are_not_listed_as_active()
    {
        var arranged = await _data.ArrangeAsync(Now, interactingInactive: true);
        var body = await ReadAsync(await _api.CreateClient().GetAsync($"/api/refill-requests/{arranged.RequestId}"));

        Assert.Empty(body["activePrescriptions"]!.AsArray());
        Assert.Empty(body["evaluation"]!["warnings"]!.AsArray());
    }

    [Fact]
    public async Task Unknown_request_returns_not_found_problem()
    {
        var response = await _api.CreateClient().GetAsync("/api/refill-requests/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("not_found", (await ReadAsync(response))["code"]!.GetValue<string>());
    }

    [Fact]
    public async Task Responses_never_expose_persistence_fields_and_all_instants_are_utc()
    {
        var arranged = await _data.ArrangeAsync(Now, interactingActive: true);
        var client = _api.CreateClient();

        var detail = await (await client.GetAsync($"/api/refill-requests/{arranged.RequestId}")).Content.ReadAsStringAsync();
        var list = await (await client.GetAsync("/api/refill-requests")).Content.ReadAsStringAsync();

        foreach (var json in new[] { detail, list })
        {
            Assert.DoesNotContain("rowVersion", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("patientId", json, StringComparison.OrdinalIgnoreCase);
            foreach (Match match in InstantPattern().Matches(json))
            {
                Assert.EndsWith("+00:00", match.Groups[1].Value);
            }
        }
    }

    /// <summary>
    /// Spec 005 T037 and research R3 (merge rule): every safety value in a detail response comes
    /// from the one rule read its evaluation used, even when another change commits before the
    /// display read that supplies the localized names.
    /// </summary>
    [Theory]
    [InlineData("ar")]
    [InlineData("en")]
    public async Task Detail_facts_and_evaluation_come_from_the_same_read(string language)
    {
        var between = new BetweenReadsInterceptor(database.ConnectionString, RefillDecisionContextLoader.RuleReadTag);
        using var api = new RefillApiFactory(database)
        {
            DefaultLanguage = language,
            ConfigureServices = services =>
                services.ConfigureDbContext<RefillDbContext>(options => options.AddInterceptors(between)),
        };
        var arranged = await _data.ArrangeAsync(api.Clock.GetUtcNow(), remainingRefills: 2);
        between.RunOnceAfterTaggedRead($"UPDATE Prescriptions SET RemainingRefills = 0 WHERE Id = {arranged.PrescriptionId}");
        var client = api.CreateClient();

        var first = await ReadAsync(await client.GetAsync($"/api/refill-requests/{arranged.RequestId}"));

        Assert.Equal(2, first["prescription"]!["remainingRefills"]!.GetValue<int>());
        Assert.DoesNotContain(first["evaluation"]!["blockingReasons"]!.AsArray(), r => r!["code"]!.GetValue<string>() == "NO_REFILLS_REMAINING");
        Assert.True(first["evaluation"]!["canApprove"]!.GetValue<bool>());

        // The competing change did commit between the reads: the next detail shows it.
        var second = await ReadAsync(await client.GetAsync($"/api/refill-requests/{arranged.RequestId}"));
        Assert.Equal(0, second["prescription"]!["remainingRefills"]!.GetValue<int>());
        Assert.Contains(second["evaluation"]!["blockingReasons"]!.AsArray(), r => r!["code"]!.GetValue<string>() == "NO_REFILLS_REMAINING");
    }

    [GeneratedRegex("\"\\w+At\":\"([^\"]+)\"")]
    private static partial Regex InstantPattern();
}
