using System.Net;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrescriptionRefill.IntegrationTests.Infrastructure;
using static PrescriptionRefill.IntegrationTests.Infrastructure.Json;

namespace PrescriptionRefill.IntegrationTests.Api;

/// <summary>
/// Search matches the medication name in the resolved language only (spec 005 FR-025, SC-005,
/// quickstart V6). Patient-name matching, paging totals, and the no-logging rule are unchanged.
/// </summary>
public sealed class LocalizedSearchTests(SqlServerDatabaseFixture database)
    : IClassFixture<SqlServerDatabaseFixture>, IDisposable
{
    private readonly RefillApiFactory _api = new(database) { DefaultLanguage = null };
    private readonly TestData _data = new(database);
    private DateTimeOffset Now => _api.Clock.GetUtcNow();

    public void Dispose() => _api.Dispose();

    [Fact]
    public async Task A_medication_is_found_only_by_its_name_in_the_active_language()
    {
        var english = $"Med{Guid.NewGuid():N}"[..20];
        var arabic = $"دواء{TestData.ArabicToken()}";
        var arranged = await _data.ArrangeAsync(Now, medicationName: english, medicationNameAr: arabic);
        var id = arranged.RequestId!.Value;

        Assert.Equal([id], Ids(await GetAsync(arabic, "ar")));
        Assert.Equal([id], Ids(await GetAsync(english.ToUpperInvariant(), "en")));
        Assert.Empty(Ids(await GetAsync(arabic, "en")));
        Assert.Empty(Ids(await GetAsync(english, "ar")));
    }

    [Fact]
    public async Task Totals_follow_the_language_specific_matches()
    {
        var arabic = $"دواء{TestData.ArabicToken()}";
        await _data.ArrangeAsync(Now, medicationNameAr: arabic);

        var inArabic = await GetAsync(arabic, "ar");
        var inEnglish = await GetAsync(arabic, "en");

        Assert.Equal(1, inArabic["matchingCount"]!.GetValue<int>());
        Assert.Equal(0, inEnglish["matchingCount"]!.GetValue<int>());
        Assert.Equal(inArabic["statusTotal"]!.GetValue<int>(), inEnglish["statusTotal"]!.GetValue<int>());
    }

    [Theory]
    [InlineData("ar")]
    [InlineData("en")]
    public async Task Patient_name_matching_is_unchanged_in_both_languages(string language)
    {
        var token = Guid.NewGuid().ToString("N")[..12];
        var arranged = await _data.ArrangeAsync(Now, patientName: $"Patient {token}");

        Assert.Equal([arranged.RequestId!.Value], Ids(await GetAsync(token, language)));
    }

    [Fact]
    public async Task Arabic_search_text_is_never_written_to_logs()
    {
        var logs = new CapturingLoggerProvider();
        using var api = new RefillApiFactory(database)
        {
            DefaultLanguage = null,
            ConfigureServices = services => services.AddSingleton<ILoggerProvider>(logs),
        };
        var arabic = $"دواء{TestData.ArabicToken()}";
        await _data.ArrangeAsync(Now, medicationNameAr: arabic);

        Assert.Single(Ids(await GetAsync(arabic, "ar", api)));
        Assert.DoesNotContain(logs.Entries, e => e.Contains(arabic, StringComparison.Ordinal));
    }

    private async Task<JsonNode> GetAsync(string search, string language, RefillApiFactory? api = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/refill-requests?pageSize=100&search={Uri.EscapeDataString(search)}");
        request.Headers.TryAddWithoutValidation("Accept-Language", language);
        var response = await (api ?? _api).CreateClient().SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadAsync(response);
    }

    private static List<int> Ids(JsonNode body) =>
        body["items"]!.AsArray().Select(i => i!["id"]!.GetValue<int>()).ToList();
}
