using System.Net;
using PrescriptionRefill.IntegrationTests.Infrastructure;
using static PrescriptionRefill.IntegrationTests.Infrastructure.Json;

namespace PrescriptionRefill.IntegrationTests.Api;

/// <summary>
/// Paging and search parameter validation (003 spec FR-003, FR-004, FR-008;
/// 003 contracts/http-api.md "Errors"). The first invalid field is reported, in the order
/// status, page, pageSize, search.
/// </summary>
public sealed class ListValidationTests(SqlServerDatabaseFixture database)
    : IClassFixture<SqlServerDatabaseFixture>, IDisposable
{
    private const string PageMessage = "Page must be a whole number of at least 1.";
    private const string PageSizeMessage = "Page size must be a whole number from 1 to 100.";
    private const string SearchMessage = "Search must be at most 100 characters.";

    private readonly RefillApiFactory _api = new(database);

    public void Dispose() => _api.Dispose();

    private async Task AssertInvalidAsync(string query, string field, string message)
    {
        var response = await _api.CreateClient().GetAsync($"/api/refill-requests?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await ReadAsync(response);
        Assert.Equal("validation_failed", body["code"]!.GetValue<string>());
        var errors = body["errors"]!.AsObject();
        Assert.Equal([field], errors.Select(e => e.Key));
        Assert.Equal(message, errors[field]!.AsArray().Single()!.GetValue<string>());
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("page=-1")]
    [InlineData("page=abc")]
    [InlineData("page=1.5")]
    [InlineData("page=%2B1")]
    [InlineData("page=%201")]
    [InlineData("page=")]
    [InlineData("page=2147483648")]
    public Task Invalid_page_is_rejected(string query) => AssertInvalidAsync(query, "page", PageMessage);

    [Theory]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=101")]
    [InlineData("pageSize=abc")]
    public Task Invalid_page_size_is_rejected(string query) => AssertInvalidAsync(query, "pageSize", PageSizeMessage);

    [Fact]
    public Task Search_longer_than_100_characters_after_trimming_is_rejected() =>
        AssertInvalidAsync($"search={new string('a', 101)}", "search", SearchMessage);

    [Fact]
    public Task Status_is_checked_before_page() =>
        AssertInvalidAsync("status=Closed&page=0", "status", "Status must be Pending, Approved, or Denied.");

    [Fact]
    public Task Page_is_checked_before_page_size() =>
        AssertInvalidAsync("page=0&pageSize=0", "page", PageMessage);

    [Theory]
    [InlineData("pageSize=1")]
    [InlineData("pageSize=100")]
    [InlineData("page=999999")]
    [InlineData("search=aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("search=%20%20aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa%20%20")]
    public async Task Boundary_values_are_accepted(string query)
    {
        var response = await _api.CreateClient().GetAsync($"/api/refill-requests?{query}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
