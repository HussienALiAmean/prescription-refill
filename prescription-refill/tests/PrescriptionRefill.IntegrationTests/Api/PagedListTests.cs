using System.Net;
using System.Text.Json.Nodes;
using PrescriptionRefill.IntegrationTests.Infrastructure;
using static PrescriptionRefill.IntegrationTests.Infrastructure.Json;

namespace PrescriptionRefill.IntegrationTests.Api;

/// <summary>
/// Server-side paging (003 spec FR-001, FR-002, FR-005, FR-006; SC-002). The class shares one
/// database, so expectations come from the unpaged list fetched in the same test, never from
/// hard-coded totals.
/// </summary>
public sealed class PagedListTests(SqlServerDatabaseFixture database)
    : IClassFixture<SqlServerDatabaseFixture>, IDisposable
{
    private readonly RefillApiFactory _api = new(database);
    private readonly TestData _data = new(database);
    private DateTimeOffset Now => _api.Clock.GetUtcNow();

    public void Dispose() => _api.Dispose();

    private async Task<JsonNode> GetAsync(string query)
    {
        var response = await _api.CreateClient().GetAsync($"/api/refill-requests{query}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadAsync(response);
    }

    private static List<int> Ids(JsonNode body) =>
        body["items"]!.AsArray().Select(i => i!["id"]!.GetValue<int>()).ToList();

    private async Task ArrangePendingAsync(int count)
    {
        for (var i = 0; i < count; i++)
        {
            await _data.ArrangeAsync(Now, requestedAgo: TimeSpan.FromMinutes(10 + i));
        }
    }

    /// <summary>Concatenates every page of <paramref name="query"/> until an empty page.</summary>
    private async Task<List<int>> AllPagesAsync(string query, int pageSize)
    {
        var ids = new List<int>();
        for (var page = 1; ; page++)
        {
            var body = await GetAsync($"?{query}page={page}&pageSize={pageSize}");
            var pageIds = Ids(body);
            if (pageIds.Count == 0)
            {
                return ids;
            }

            Assert.True(pageIds.Count <= pageSize);
            ids.AddRange(pageIds);
        }
    }

    [Fact]
    public async Task Paging_through_pending_returns_every_request_once_in_unpaged_order()
    {
        await ArrangePendingAsync(23);

        var unpaged = Ids(await GetAsync(string.Empty));
        var paged = await AllPagesAsync(string.Empty, pageSize: 10);

        Assert.True(unpaged.Count >= 23);
        Assert.Equal(unpaged, paged);
    }

    [Fact]
    public async Task Page_metadata_echoes_the_request_and_totals_match_the_unpaged_list()
    {
        await ArrangePendingAsync(12);
        var unpagedCount = Ids(await GetAsync(string.Empty)).Count;

        var body = await GetAsync("?page=2&pageSize=5");

        Assert.Equal(2, body["page"]!.GetValue<int>());
        Assert.Equal(5, body["pageSize"]!.GetValue<int>());
        Assert.Equal(unpagedCount, body["matchingCount"]!.GetValue<int>());
        Assert.Equal(unpagedCount, body["statusTotal"]!.GetValue<int>());
        Assert.Equal(5, Ids(body).Count);
    }

    [Fact]
    public async Task Only_page_or_only_page_size_applies_the_default_for_the_other()
    {
        await ArrangePendingAsync(12);

        var onlyPage = await GetAsync("?page=2");
        var onlySize = await GetAsync("?pageSize=5");

        Assert.Equal(2, onlyPage["page"]!.GetValue<int>());
        Assert.Equal(10, onlyPage["pageSize"]!.GetValue<int>());
        Assert.Equal(1, onlySize["page"]!.GetValue<int>());
        Assert.Equal(5, onlySize["pageSize"]!.GetValue<int>());
        Assert.Equal(5, Ids(onlySize).Count);
    }

    [Theory]
    [InlineData(50)]
    [InlineData(999999)]
    public async Task Page_beyond_the_last_returns_no_items_with_correct_totals(int page)
    {
        await ArrangePendingAsync(3);
        var unpagedCount = Ids(await GetAsync(string.Empty)).Count;

        var body = await GetAsync($"?page={page}&pageSize=100");

        Assert.Empty(Ids(body));
        Assert.Equal(page, body["page"]!.GetValue<int>());
        Assert.Equal(unpagedCount, body["matchingCount"]!.GetValue<int>());
        Assert.Equal(unpagedCount, body["statusTotal"]!.GetValue<int>());
    }

    [Fact]
    public async Task Equal_request_times_keep_id_order_across_a_page_boundary()
    {
        // Older than every other request in this class, so these lead the queue.
        var tied = new List<int>();
        for (var i = 0; i < 3; i++)
        {
            tied.Add((await _data.ArrangeAsync(Now, requestedAgo: TimeSpan.FromDays(400))).RequestId!.Value);
        }

        var first = Ids(await GetAsync("?page=1&pageSize=2"));
        var second = Ids(await GetAsync("?page=2&pageSize=2"));

        Assert.Equal(tied.Order().ToList(), first.Concat(second).Take(3).ToList());
    }

    [Fact]
    public async Task Pending_page_flags_equal_the_unpaged_flags()
    {
        await _data.ArrangeAsync(Now, sinceLastFill: TimeSpan.FromDays(1));
        await _data.ArrangeAsync(Now, allergicToRequested: true);
        await _data.ArrangeAsync(Now, remainingRefills: 0, interactingActive: true);

        var unpaged = await GetAsync(string.Empty);
        var paged = await GetAsync($"?page=1&pageSize=100");

        Assert.Equal(unpaged["evaluatedAt"]!.GetValue<string>(), paged["evaluatedAt"]!.GetValue<string>());
        Assert.Equal(unpaged["items"]!.ToJsonString(), paged["items"]!.ToJsonString());
    }

    [Fact]
    public async Task Decided_lists_page_newest_decision_first()
    {
        var client = _api.CreateClient();
        for (var i = 0; i < 12; i++)
        {
            var arranged = await _data.ArrangeAsync(Now);
            Assert.Equal(HttpStatusCode.OK, (await client.DenyAsync(arranged.RequestId!.Value, "No.")).StatusCode);
            _api.Clock.Advance(TimeSpan.FromMinutes(1));
        }

        var unpaged = Ids(await GetAsync("?status=Denied"));
        var paged = await AllPagesAsync("status=Denied&", pageSize: 5);
        var firstPage = await GetAsync("?status=Denied&page=1&pageSize=5");

        Assert.Equal(unpaged, paged);
        Assert.Equal(unpaged.Count, firstPage["statusTotal"]!.GetValue<int>());
        Assert.Null(firstPage["summary"]);
        Assert.All(firstPage["items"]!.AsArray(), item => Assert.Null(item!["canApprove"]));
    }

    [Fact]
    public async Task Unpaged_response_has_only_evaluatedAt_and_items()
    {
        await ArrangePendingAsync(1);

        Assert.Equal(["evaluatedAt", "items"], (await GetAsync(string.Empty)).PropertyNames());
        Assert.Equal(["evaluatedAt", "items"], (await GetAsync("?status=Denied")).PropertyNames());
    }
}
