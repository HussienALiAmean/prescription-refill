using System.Net;
using System.Text.Json.Nodes;
using PrescriptionRefill.IntegrationTests.Infrastructure;
using static PrescriptionRefill.IntegrationTests.Infrastructure.Json;

namespace PrescriptionRefill.IntegrationTests.Api;

/// <summary>
/// Whole-status Pending summary (003 spec FR-010, FR-011; SC-004). Expected counts come from the
/// unpaged list's flags in the same database, so the summary is checked against the server's own
/// evaluation rather than hard-coded numbers.
/// </summary>
public sealed class ListSummaryTests(SqlServerDatabaseFixture database)
    : IClassFixture<SqlServerDatabaseFixture>, IDisposable
{
    private readonly RefillApiFactory _api = new(database);
    private readonly TestData _data = new(database);
    private readonly string _token = Guid.NewGuid().ToString("N")[..12];
    private DateTimeOffset Now => _api.Clock.GetUtcNow();

    public void Dispose() => _api.Dispose();

    private async Task<JsonNode> GetAsync(string query)
    {
        var response = await _api.CreateClient().GetAsync($"/api/refill-requests{query}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadAsync(response);
    }

    /// <summary>Too early, no refills, allergy, interaction, too early + interaction, and clear.</summary>
    private async Task ArrangeMixtureAsync()
    {
        await _data.ArrangeAsync(Now, sinceLastFill: TimeSpan.FromDays(1));
        await _data.ArrangeAsync(Now, remainingRefills: 0);
        await _data.ArrangeAsync(Now, allergicToRequested: true, patientName: $"Allergic {_token}");
        await _data.ArrangeAsync(Now, interactingActive: true);
        await _data.ArrangeAsync(Now, sinceLastFill: TimeSpan.FromDays(1), interactingActive: true);
        await _data.ArrangeAsync(Now);
    }

    private async Task<(int Blocked, int Warnings, int Clear)> ExpectedFromUnpagedFlagsAsync()
    {
        var items = (await GetAsync(string.Empty))["items"]!.AsArray();
        bool CanApprove(JsonNode? i) => i!["canApprove"]!.GetValue<bool>();
        bool HasWarnings(JsonNode? i) => i!["hasWarnings"]!.GetValue<bool>();
        return (
            items.Count(i => !CanApprove(i)),
            items.Count(HasWarnings),
            items.Count(i => CanApprove(i) && !HasWarnings(i)));
    }

    private static (int Blocked, int Warnings, int Clear) SummaryOf(JsonNode body)
    {
        var summary = body["summary"]!;
        return (summary["blocked"]!.GetValue<int>(), summary["warnings"]!.GetValue<int>(), summary["clear"]!.GetValue<int>());
    }

    [Fact]
    public async Task Summary_equals_counts_from_the_unpaged_flags()
    {
        await ArrangeMixtureAsync();
        var expected = await ExpectedFromUnpagedFlagsAsync();

        var actual = SummaryOf(await GetAsync("?page=1&pageSize=3"));

        Assert.Equal(expected, actual);
        Assert.True(actual.Blocked >= 3 && actual.Warnings >= 3 && actual.Clear >= 1);
    }

    [Fact]
    public async Task Summary_is_identical_on_every_page_and_with_a_search()
    {
        await ArrangeMixtureAsync();
        var expected = await ExpectedFromUnpagedFlagsAsync();

        foreach (var query in new[] { "?page=1&pageSize=3", "?page=2&pageSize=3", "?page=3&pageSize=3", "?page=999&pageSize=3", $"?search={_token}" })
        {
            Assert.Equal(expected, SummaryOf(await GetAsync(query)));
        }
    }

    [Theory]
    [InlineData("Approved")]
    [InlineData("Denied")]
    public async Task Summary_is_absent_for_approved_and_denied(string status)
    {
        var arranged = await _data.ArrangeAsync(Now);
        var client = _api.CreateClient();
        await (status == "Approved" ? client.ApproveAsync(arranged.RequestId!.Value, "Yes.") : client.DenyAsync(arranged.RequestId!.Value, "No."));

        var body = await GetAsync($"?status={status}&page=1");

        Assert.Null(body["summary"]);
        Assert.True(body["statusTotal"]!.GetValue<int>() >= 1);
    }

    [Fact]
    public async Task Summary_and_items_share_one_evaluation_instant()
    {
        await ArrangeMixtureAsync();

        var body = await GetAsync("?page=1&pageSize=3");

        AssertUtcInstant(body["evaluatedAt"], Now);
        Assert.NotNull(body["summary"]);
    }
}
