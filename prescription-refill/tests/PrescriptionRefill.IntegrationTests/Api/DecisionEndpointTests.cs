using System.Net;
using System.Net.Http.Json;
using PrescriptionRefill.Domain.Prescriptions;
using PrescriptionRefill.Domain.RefillRequests;
using PrescriptionRefill.IntegrationTests.Infrastructure;
using static PrescriptionRefill.IntegrationTests.Infrastructure.Json;

namespace PrescriptionRefill.IntegrationTests.Api;

public sealed class DecisionEndpointTests(SqlServerDatabaseFixture database)
    : IClassFixture<SqlServerDatabaseFixture>, IDisposable
{
    private static readonly TimeSpan Threshold30Days = TimeSpan.FromSeconds(30 * 69_120L);

    private readonly RefillApiFactory _api = new(database);
    private readonly TestData _data = new(database);
    private DateTimeOffset Now => _api.Clock.GetUtcNow();

    public void Dispose() => _api.Dispose();

    [Fact]
    public async Task Approve_consumes_one_refill_and_records_the_decision()
    {
        var arranged = await _data.ArrangeAsync(Now, remainingRefills: 2);
        var before = await _data.GetPrescriptionAsync(arranged.PrescriptionId);
        var requestVersion = await _data.GetRowVersionAsync<RefillRequest>(arranged.RequestId!.Value);
        var prescriptionVersion = await _data.GetRowVersionAsync<Prescription>(arranged.PrescriptionId);

        var response = await _api.CreateClient().ApproveAsync(arranged.RequestId.Value, "  Approved, stable.  ");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadAsync(response);
        Assert.Equal("Approved", body["status"]!.GetValue<string>());
        Assert.Equal("Approved, stable.", body["decisionNote"]!.GetValue<string>());
        AssertUtcInstant(body["decidedAt"], Now);
        Assert.DoesNotContain("evaluation", body.PropertyNames());
        Assert.Equal(1, body["prescription"]!["remainingRefills"]!.GetValue<int>());

        var after = await _data.GetPrescriptionAsync(arranged.PrescriptionId);
        Assert.Equal(1, after.RemainingRefills);
        Assert.Equal(before.LastFilledAt, after.LastFilledAt);
        Assert.NotEqual(requestVersion, await _data.GetRowVersionAsync<RefillRequest>(arranged.RequestId.Value));
        Assert.NotEqual(prescriptionVersion, await _data.GetRowVersionAsync<Prescription>(arranged.PrescriptionId));
    }

    [Fact]
    public async Task Deny_is_allowed_when_blocked_and_leaves_the_prescription_untouched()
    {
        var arranged = await _data.ArrangeAsync(Now, remainingRefills: 0);
        var prescriptionVersion = await _data.GetRowVersionAsync<Prescription>(arranged.PrescriptionId);

        var response = await _api.CreateClient().DenyAsync(arranged.RequestId!.Value, "No refills remain.");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Denied", (await ReadAsync(response))["status"]!.GetValue<string>());
        Assert.Equal(0, (await _data.GetPrescriptionAsync(arranged.PrescriptionId)).RemainingRefills);
        Assert.Equal(prescriptionVersion, await _data.GetRowVersionAsync<Prescription>(arranged.PrescriptionId));
    }

    [Fact]
    public async Task One_tick_too_early_returns_approval_blocked_with_the_current_evaluation()
    {
        var arranged = await _data.ArrangeAsync(Now, sinceLastFill: Threshold30Days - TimeSpan.FromTicks(1), interactingActive: true);
        var requestVersion = await _data.GetRowVersionAsync<RefillRequest>(arranged.RequestId!.Value);
        var prescriptionVersion = await _data.GetRowVersionAsync<Prescription>(arranged.PrescriptionId);

        var response = await _api.CreateClient().ApproveAsync(arranged.RequestId.Value, "Approve.");

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
        var body = await ReadAsync(response);
        Assert.Equal(["title", "status", "detail", "code", "requestId", "evaluation"], body.PropertyNames().Where(n => n != "type" && n != "traceId"));
        Assert.Equal("approval_blocked", body["code"]!.GetValue<string>());
        Assert.Equal(422, body["status"]!.GetValue<int>());
        Assert.Equal(arranged.RequestId, body["requestId"]!.GetValue<int>());

        var evaluation = body["evaluation"]!;
        AssertUtcInstant(evaluation["evaluatedAt"], Now);
        Assert.False(evaluation["canApprove"]!.GetValue<bool>());
        AssertUtcInstant(evaluation["eligibleAt"], Now.AddTicks(1));
        var reason = Assert.Single(evaluation["blockingReasons"]!.AsArray())!;
        Assert.Equal(["code", "message", "eligibleAt"], reason.PropertyNames());
        Assert.Equal("TOO_EARLY", reason["code"]!.GetValue<string>());
        AssertUtcInstant(reason["eligibleAt"], Now.AddTicks(1));
        Assert.Equal("DRUG_INTERACTION", Assert.Single(evaluation["warnings"]!.AsArray())!["code"]!.GetValue<string>());

        Assert.Equal(RefillRequestStatus.Pending, (await _data.GetRequestAsync(arranged.RequestId.Value)).Status);
        Assert.Equal(2, (await _data.GetPrescriptionAsync(arranged.PrescriptionId)).RemainingRefills);
        Assert.Equal(requestVersion, await _data.GetRowVersionAsync<RefillRequest>(arranged.RequestId.Value));
        Assert.Equal(prescriptionVersion, await _data.GetRowVersionAsync<Prescription>(arranged.PrescriptionId));
    }

    [Fact]
    public async Task Zero_refills_returns_approval_blocked()
    {
        var arranged = await _data.ArrangeAsync(Now, remainingRefills: 0);

        var response = await _api.CreateClient().ApproveAsync(arranged.RequestId!.Value, "Approve.");

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
        var reason = Assert.Single((await ReadAsync(response))["evaluation"]!["blockingReasons"]!.AsArray())!;
        Assert.Equal("NO_REFILLS_REMAINING", reason["code"]!.GetValue<string>());
        Assert.Null(reason["eligibleAt"]);
    }

    [Fact]
    public async Task Stale_displayed_evaluation_is_ignored_and_the_current_reasons_are_returned()
    {
        var arranged = await _data.ArrangeAsync(Now, remainingRefills: 1);
        var client = _api.CreateClient();
        var shown = await ReadAsync(await client.GetAsync($"/api/refill-requests/{arranged.RequestId}"));
        Assert.True(shown["evaluation"]!["canApprove"]!.GetValue<bool>());

        await _data.ExecuteSqlAsync($"UPDATE Prescriptions SET RemainingRefills = 0 WHERE Id = {arranged.PrescriptionId}");
        var response = await client.ApproveAsync(arranged.RequestId!.Value, "Approve.");

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
        Assert.Equal(
            "NO_REFILLS_REMAINING",
            (await ReadAsync(response))["evaluation"]!["blockingReasons"]![0]!["code"]!.GetValue<string>());
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    public async Task Displayed_evaluation_and_approval_use_the_same_calculation(long tickOffset, bool approvable)
    {
        var arranged = await _data.ArrangeAsync(Now, sinceLastFill: Threshold30Days + TimeSpan.FromTicks(tickOffset));
        var client = _api.CreateClient();

        var shown = await ReadAsync(await client.GetAsync($"/api/refill-requests/{arranged.RequestId}"));
        var response = await client.ApproveAsync(arranged.RequestId!.Value, "Approve.");

        Assert.Equal(approvable, shown["evaluation"]!["canApprove"]!.GetValue<bool>());
        Assert.Equal(approvable ? HttpStatusCode.OK : (HttpStatusCode)422, response.StatusCode);
    }

    [Fact]
    public async Task Repeated_decisions_are_rejected_and_refills_are_consumed_once()
    {
        var arranged = await _data.ArrangeAsync(Now, remainingRefills: 3);
        var client = _api.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.ApproveAsync(arranged.RequestId!.Value, "First.")).StatusCode);

        var again = await client.ApproveAsync(arranged.RequestId.Value, "Second.");
        var deny = await client.DenyAsync(arranged.RequestId.Value, "Second.");

        foreach (var response in new[] { again, deny })
        {
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var body = await ReadAsync(response);
            Assert.Equal("request_already_decided", body["code"]!.GetValue<string>());
            Assert.Equal("Approved", body["requestStatus"]!.GetValue<string>());
            Assert.Equal(arranged.RequestId, body["requestId"]!.GetValue<int>());
        }

        Assert.Equal(2, (await _data.GetPrescriptionAsync(arranged.PrescriptionId)).RemainingRefills);
        Assert.Equal("First.", (await _data.GetRequestAsync(arranged.RequestId.Value)).DecisionNote);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Blank_note_returns_validation_failed_and_changes_nothing(string? note)
    {
        var arranged = await _data.ArrangeAsync(Now);
        var client = _api.CreateClient();

        foreach (var response in new[] { await client.ApproveAsync(arranged.RequestId!.Value, note), await client.DenyAsync(arranged.RequestId.Value, note) })
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await ReadAsync(response);
            Assert.Equal("validation_failed", body["code"]!.GetValue<string>());
            Assert.NotEmpty(body["errors"]!["note"]!.AsArray());
        }

        Assert.Equal(RefillRequestStatus.Pending, (await _data.GetRequestAsync(arranged.RequestId.Value)).Status);
    }

    [Fact]
    public async Task Oversized_note_and_malformed_body_return_validation_failed()
    {
        var arranged = await _data.ArrangeAsync(Now);
        var client = _api.CreateClient();

        var oversized = await client.ApproveAsync(arranged.RequestId!.Value, new string('a', 1001));
        var malformed = await client.PostAsync(
            $"/api/refill-requests/{arranged.RequestId}/approve",
            new StringContent("{ not json", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, oversized.StatusCode);
        Assert.Equal("validation_failed", (await ReadAsync(oversized))["code"]!.GetValue<string>());
        Assert.Equal(HttpStatusCode.BadRequest, malformed.StatusCode);
        Assert.Equal("validation_failed", (await ReadAsync(malformed))["code"]!.GetValue<string>());
    }

    [Fact]
    public async Task Decision_on_unknown_request_returns_not_found()
    {
        var response = await _api.CreateClient().ApproveAsync(999999, "Approve.");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("not_found", (await ReadAsync(response))["code"]!.GetValue<string>());
    }

    [Fact]
    public async Task Requests_cannot_be_edited_or_deleted()
    {
        var arranged = await _data.ArrangeAsync(Now);
        var client = _api.CreateClient();

        var put = await client.PutAsJsonAsync($"/api/refill-requests/{arranged.RequestId}", new { status = "Approved" });
        var delete = await client.DeleteAsync($"/api/refill-requests/{arranged.RequestId}");

        Assert.Contains(put.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed });
        Assert.Contains(delete.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed });
        Assert.Equal(RefillRequestStatus.Pending, (await _data.GetRequestAsync(arranged.RequestId!.Value)).Status);
    }
}
