using System.Net;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrescriptionRefill.IntegrationTests.Infrastructure;
using static PrescriptionRefill.IntegrationTests.Infrastructure.Json;

namespace PrescriptionRefill.IntegrationTests.Api;

/// <summary>
/// Server-side search (003 spec FR-007–FR-010, SC-003). Names carry a per-test token so matches
/// are known regardless of other data in the class's shared database.
/// </summary>
public sealed class ListSearchTests(SqlServerDatabaseFixture database)
    : IClassFixture<SqlServerDatabaseFixture>, IDisposable
{
    private readonly RefillApiFactory _api = new(database);
    private readonly TestData _data = new(database);
    private readonly string _token = Guid.NewGuid().ToString("N")[..12];
    private DateTimeOffset Now => _api.Clock.GetUtcNow();

    public void Dispose() => _api.Dispose();

    private async Task<JsonNode> GetAsync(string query, RefillApiFactory? api = null)
    {
        var response = await (api ?? _api).CreateClient().GetAsync($"/api/refill-requests{query}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadAsync(response);
    }

    private static List<int> Ids(JsonNode body) =>
        body["items"]!.AsArray().Select(i => i!["id"]!.GetValue<int>()).ToList();

    private static string Search(string text) => $"search={Uri.EscapeDataString(text)}";

    [Fact]
    public async Task Search_finds_a_match_that_is_not_on_the_first_unfiltered_page()
    {
        for (var i = 0; i < 15; i++)
        {
            await _data.ArrangeAsync(Now, requestedAgo: TimeSpan.FromHours(20 + i));
        }

        // Requested most recently, so it is last in the unfiltered Pending order.
        var target = await _data.ArrangeAsync(Now, requestedAgo: TimeSpan.FromMinutes(1), patientName: $"Target {_token}");

        Assert.DoesNotContain(target.RequestId!.Value, Ids(await GetAsync("?page=1&pageSize=10")));
        var body = await GetAsync($"?page=1&pageSize=10&{Search(_token)}");

        Assert.Equal([target.RequestId.Value], Ids(body));
        Assert.Equal(1, body["matchingCount"]!.GetValue<int>());
    }

    [Fact]
    public async Task Search_matches_patient_or_medication_ignoring_case_and_surrounding_spaces()
    {
        var byPatient = await _data.ArrangeAsync(Now, patientName: $"Casey {_token}");
        var byMedication = await _data.ArrangeAsync(Now, medicationName: $"Med{_token}");
        int[] expected = [byPatient.RequestId!.Value, byMedication.RequestId!.Value];

        foreach (var text in new[] { _token, _token.ToUpperInvariant(), $"  {_token}  " })
        {
            Assert.Equal(expected.Order(), Ids(await GetAsync($"?{Search(text)}")).Order());
        }
    }

    [Fact]
    public async Task Search_results_are_paged_in_list_order()
    {
        var arranged = new HashSet<int>();
        for (var i = 0; i < 12; i++)
        {
            arranged.Add((await _data.ArrangeAsync(Now, requestedAgo: TimeSpan.FromMinutes(30 + i), patientName: $"Paged {_token} {i:00}")).RequestId!.Value);
        }

        var unpagedOrder = Ids(await GetAsync(string.Empty)).Where(arranged.Contains).ToList();
        var first = await GetAsync($"?page=1&pageSize=10&{Search(_token)}");
        var second = await GetAsync($"?page=2&pageSize=10&{Search(_token)}");

        Assert.Equal(10, Ids(first).Count);
        Assert.Equal(2, Ids(second).Count);
        Assert.Equal(unpagedOrder, Ids(first).Concat(Ids(second)).ToList());
        Assert.Equal(12, first["matchingCount"]!.GetValue<int>());
    }

    [Fact]
    public async Task Wildcard_characters_match_literally()
    {
        var literal = await _data.ArrangeAsync(Now, patientName: $"Lit_{_token} 50%");
        await _data.ArrangeAsync(Now, patientName: $"LitX{_token} 50X");

        Assert.Equal([literal.RequestId!.Value], Ids(await GetAsync($"?{Search($"_{_token}")}")));
        Assert.Equal([literal.RequestId.Value], Ids(await GetAsync($"?{Search($"{_token} 50%")}")));
        Assert.Equal(0, (await GetAsync($"?{Search($"[{_token}")}"))["matchingCount"]!.GetValue<int>());
    }

    [Fact]
    public async Task Status_total_ignores_search()
    {
        await _data.ArrangeAsync(Now, patientName: $"Total {_token}");
        await _data.ArrangeAsync(Now);
        var unpagedCount = Ids(await GetAsync(string.Empty)).Count;

        var body = await GetAsync($"?{Search(_token)}");

        Assert.Equal(1, body["matchingCount"]!.GetValue<int>());
        Assert.Equal(unpagedCount, body["statusTotal"]!.GetValue<int>());
    }

    [Theory]
    [InlineData("search=")]
    [InlineData("search=%20%20")]
    public async Task Empty_or_whitespace_search_is_paged_without_a_filter(string query)
    {
        await _data.ArrangeAsync(Now);

        var body = await GetAsync($"?{query}");

        Assert.Equal(1, body["page"]!.GetValue<int>());
        Assert.Equal(10, body["pageSize"]!.GetValue<int>());
        Assert.Equal(body["statusTotal"]!.GetValue<int>(), body["matchingCount"]!.GetValue<int>());
    }

    [Fact]
    public async Task Search_text_is_never_written_to_logs()
    {
        var logs = new CapturingLoggerProvider();
        using var api = new RefillApiFactory(database)
        {
            ConfigureServices = services => services.AddSingleton<ILoggerProvider>(logs),
        };
        var arranged = await _data.ArrangeAsync(Now, patientName: $"Private {_token}");

        await GetAsync($"?{Search(_token)}", api);
        await GetAsync($"?page=2&pageSize=5&{Search(_token)}", api);
        await GetAsync($"?status=Denied&{Search(_token)}", api);
        // A committed decision logs at Information, proving the capture sees application logs.
        Assert.Equal(HttpStatusCode.OK, (await api.CreateClient().DenyAsync(arranged.RequestId!.Value, "No.")).StatusCode);

        Assert.Contains(logs.Entries, entry => entry.Contains($"Refill request {arranged.RequestId} denial committed"));
        Assert.DoesNotContain(logs.Entries, entry => entry.Contains(_token, StringComparison.OrdinalIgnoreCase));
    }
}
