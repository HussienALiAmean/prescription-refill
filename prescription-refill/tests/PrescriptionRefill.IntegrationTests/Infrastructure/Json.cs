using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace PrescriptionRefill.IntegrationTests.Infrastructure;

/// <summary>Small helpers for asserting on raw JSON, so tests check the wire contract itself.</summary>
public static class Json
{
    public static async Task<JsonNode> ReadAsync(HttpResponseMessage response) =>
        JsonNode.Parse(await response.Content.ReadAsStringAsync())
        ?? throw new InvalidOperationException("Empty response body.");

    public static Task<HttpResponseMessage> ApproveAsync(this HttpClient client, int id, string? note) =>
        client.PostAsJsonAsync($"/api/refill-requests/{id}/approve", new { note });

    public static Task<HttpResponseMessage> DenyAsync(this HttpClient client, int id, string? note) =>
        client.PostAsJsonAsync($"/api/refill-requests/{id}/deny", new { note });

    /// <summary>Asserts a contract instant: offset +00:00 (UTC) and the expected value to the tick.</summary>
    public static void AssertUtcInstant(JsonNode? node, DateTimeOffset expected)
    {
        var text = node?.GetValue<string>() ?? throw new Xunit.Sdk.XunitException("Instant is missing.");
        Assert.EndsWith("+00:00", text);
        Assert.Equal(expected.UtcTicks, DateTimeOffset.Parse(text, System.Globalization.CultureInfo.InvariantCulture).UtcTicks);
    }

    public static IEnumerable<string> PropertyNames(this JsonNode node) =>
        node.AsObject().Select(p => p.Key);
}
