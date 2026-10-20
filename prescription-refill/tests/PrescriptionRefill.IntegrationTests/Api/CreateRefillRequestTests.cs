using System.Net;
using System.Net.Http.Json;
using PrescriptionRefill.IntegrationTests.Infrastructure;
using static PrescriptionRefill.IntegrationTests.Infrastructure.Json;

namespace PrescriptionRefill.IntegrationTests.Api;

public sealed class CreateRefillRequestTests(SqlServerDatabaseFixture database)
    : IClassFixture<SqlServerDatabaseFixture>, IDisposable
{
    private readonly RefillApiFactory _api = new(database);
    private readonly TestData _data = new(database);
    private DateTimeOffset Now => _api.Clock.GetUtcNow();

    public void Dispose() => _api.Dispose();

    private Task<HttpResponseMessage> CreateAsync(object body) =>
        _api.CreateClient().PostAsJsonAsync("/api/refill-requests", body);

    [Fact]
    public async Task Creates_a_pending_request_with_location()
    {
        var arranged = await _data.ArrangeAsync(Now, withPendingRequest: false);

        var response = await CreateAsync(new { prescriptionId = arranged.PrescriptionId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await ReadAsync(response);
        var id = body["id"]!.GetValue<int>();
        Assert.Equal($"/api/refill-requests/{id}", response.Headers.Location?.OriginalString);
        Assert.Equal("Pending", body["status"]!.GetValue<string>());
        AssertUtcInstant(body["requestedAt"], Now);
        Assert.NotNull(body["evaluation"]);
    }

    [Fact]
    public async Task Blocked_prescription_is_accepted_and_the_review_shows_why()
    {
        var arranged = await _data.ArrangeAsync(Now, remainingRefills: 0, withPendingRequest: false);

        var response = await CreateAsync(new { prescriptionId = arranged.PrescriptionId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var evaluation = (await ReadAsync(response))["evaluation"]!;
        Assert.False(evaluation["canApprove"]!.GetValue<bool>());
        Assert.Equal("NO_REFILLS_REMAINING", evaluation["blockingReasons"]![0]!["code"]!.GetValue<string>());
    }

    [Fact]
    public async Task Malformed_or_missing_prescription_id_returns_validation_failed()
    {
        var malformed = await _api.CreateClient().PostAsync(
            "/api/refill-requests",
            new StringContent("{ nope", System.Text.Encoding.UTF8, "application/json"));
        var missing = await CreateAsync(new { });

        Assert.Equal(HttpStatusCode.BadRequest, malformed.StatusCode);
        Assert.Equal("validation_failed", (await ReadAsync(malformed))["code"]!.GetValue<string>());
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        var body = await ReadAsync(missing);
        Assert.Equal("validation_failed", body["code"]!.GetValue<string>());
        Assert.NotEmpty(body["errors"]!["prescriptionId"]!.AsArray());
    }

    [Fact]
    public async Task Unknown_prescription_returns_not_found()
    {
        var response = await CreateAsync(new { prescriptionId = 999999 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("not_found", (await ReadAsync(response))["code"]!.GetValue<string>());
    }

    [Fact]
    public async Task Inactive_prescription_returns_the_documented_validation_error()
    {
        var arranged = await _data.ArrangeAsync(Now, isActive: false, withPendingRequest: false);

        var response = await CreateAsync(new { prescriptionId = arranged.PrescriptionId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await ReadAsync(response);
        Assert.Equal("validation_failed", body["code"]!.GetValue<string>());
        Assert.Equal("Prescription is inactive.", body["errors"]!["prescriptionId"]![0]!.GetValue<string>());
    }

    [Fact]
    public async Task Second_pending_request_returns_pending_request_exists()
    {
        var arranged = await _data.ArrangeAsync(Now);

        var response = await CreateAsync(new { prescriptionId = arranged.PrescriptionId });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await ReadAsync(response);
        Assert.Equal("pending_request_exists", body["code"]!.GetValue<string>());
        Assert.Equal(arranged.PrescriptionId, body["prescriptionId"]!.GetValue<int>());
    }

    [Fact]
    public async Task New_request_is_accepted_after_the_previous_one_is_decided()
    {
        var arranged = await _data.ArrangeAsync(Now);
        Assert.Equal(HttpStatusCode.OK, (await _api.CreateClient().DenyAsync(arranged.RequestId!.Value, "Not now.")).StatusCode);

        var response = await CreateAsync(new { prescriptionId = arranged.PrescriptionId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
