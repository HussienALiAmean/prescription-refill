using System.Net;
using PrescriptionRefill.IntegrationTests.Infrastructure;
using static PrescriptionRefill.IntegrationTests.Infrastructure.Json;

namespace PrescriptionRefill.IntegrationTests.Api;

public sealed class StatusFilterTests(SqlServerDatabaseFixture database)
    : IClassFixture<SqlServerDatabaseFixture>, IDisposable
{
    private readonly RefillApiFactory _api = new(database);
    private readonly TestData _data = new(database);
    private DateTimeOffset Now => _api.Clock.GetUtcNow();

    public void Dispose() => _api.Dispose();

    private async Task<List<int>> IdsAsync(string query)
    {
        var body = await ReadAsync(await _api.CreateClient().GetAsync($"/api/refill-requests{query}"));
        return body["items"]!.AsArray().Select(i => i!["id"]!.GetValue<int>()).ToList();
    }

    [Fact]
    public async Task Each_status_filter_returns_only_that_status_and_decided_lists_are_newest_decision_first()
    {
        var client = _api.CreateClient();
        var pending = await _data.ArrangeAsync(Now);
        var approvedFirst = await _data.ArrangeAsync(Now);
        var approvedSecond = await _data.ArrangeAsync(Now);
        var denied = await _data.ArrangeAsync(Now);

        Assert.Equal(HttpStatusCode.OK, (await client.ApproveAsync(approvedFirst.RequestId!.Value, "First.")).StatusCode);
        _api.Clock.Advance(TimeSpan.FromMinutes(5));
        Assert.Equal(HttpStatusCode.OK, (await client.ApproveAsync(approvedSecond.RequestId!.Value, "Second.")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.DenyAsync(denied.RequestId!.Value, "No.")).StatusCode);

        var approved = await IdsAsync("?status=Approved");
        var deniedIds = await IdsAsync("?status=Denied");
        var pendingIds = await IdsAsync("?status=Pending");
        var defaultIds = await IdsAsync(string.Empty);

        Assert.True(approved.IndexOf(approvedSecond.RequestId.Value) < approved.IndexOf(approvedFirst.RequestId.Value));
        Assert.DoesNotContain(pending.RequestId!.Value, approved);
        Assert.Contains(denied.RequestId.Value, deniedIds);
        Assert.DoesNotContain(denied.RequestId.Value, pendingIds);
        Assert.Contains(pending.RequestId.Value, pendingIds);
        Assert.Equal(pendingIds, defaultIds);
    }

    [Fact]
    public async Task Decided_items_have_no_evaluation_flags_and_show_the_decision_time()
    {
        var arranged = await _data.ArrangeAsync(Now);
        await _api.CreateClient().DenyAsync(arranged.RequestId!.Value, "No.");

        var body = await ReadAsync(await _api.CreateClient().GetAsync("/api/refill-requests?status=Denied"));
        var item = body["items"]!.AsArray().Single(i => i!["id"]!.GetValue<int>() == arranged.RequestId)!;

        Assert.Equal("Denied", item["status"]!.GetValue<string>());
        Assert.Null(item["canApprove"]);
        Assert.Null(item["hasWarnings"]);
        AssertUtcInstant(item["decidedAt"], Now);
    }

    [Fact]
    public async Task Decided_detail_is_read_only_data_without_an_evaluation()
    {
        var arranged = await _data.ArrangeAsync(Now);
        await _api.CreateClient().ApproveAsync(arranged.RequestId!.Value, "Fine.");

        var body = await ReadAsync(await _api.CreateClient().GetAsync($"/api/refill-requests/{arranged.RequestId}"));

        Assert.DoesNotContain("evaluation", body.PropertyNames());
        Assert.Equal("Fine.", body["decisionNote"]!.GetValue<string>());
        AssertUtcInstant(body["decidedAt"], Now);
    }

    [Theory]
    [InlineData("Bogus")]
    [InlineData("1")]
    [InlineData("")]
    public async Task Unknown_status_returns_validation_failed(string status)
    {
        var response = await _api.CreateClient().GetAsync($"/api/refill-requests?status={status}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await ReadAsync(response);
        Assert.Equal("validation_failed", body["code"]!.GetValue<string>());
        Assert.NotEmpty(body["errors"]!["status"]!.AsArray());
    }
}
