using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PrescriptionRefill.Domain.RefillRequests;
using PrescriptionRefill.Infrastructure.Persistence;
using PrescriptionRefill.IntegrationTests.Infrastructure;
using static PrescriptionRefill.IntegrationTests.Infrastructure.Json;

namespace PrescriptionRefill.IntegrationTests.Concurrency;

/// <summary>
/// SQL Server rowversion behavior (FR-013, FR-016, FR-025). The competing change is committed by a
/// separate context after the API loaded its rows and before it saves, so each conflict is
/// deterministic (constitution Principle V).
/// </summary>
public sealed class DecisionConcurrencyTests : IClassFixture<SqlServerDatabaseFixture>, IDisposable
{
    private readonly BeforeSaveInterceptor _interceptor = new();
    private readonly RefillApiFactory _api;
    private readonly TestData _data;

    public DecisionConcurrencyTests(SqlServerDatabaseFixture database)
    {
        _data = new TestData(database);
        _api = new RefillApiFactory(database)
        {
            ConfigureServices = services =>
                services.ConfigureDbContext<RefillDbContext>(options => options.AddInterceptors(_interceptor)),
        };
    }

    private DateTimeOffset Now => _api.Clock.GetUtcNow();

    public void Dispose() => _api.Dispose();

    [Fact]
    public async Task Request_row_conflict_rolls_back_the_whole_approval()
    {
        var arranged = await _data.ArrangeAsync(Now, remainingRefills: 2);
        var requestId = arranged.RequestId!.Value;
        _interceptor.RunOnceBeforeNextSave(() => _data.ExecuteSqlAsync(
            $"UPDATE RefillRequests SET Status = 'Denied', DecisionNote = 'Decided elsewhere.', DecidedAt = SYSUTCDATETIME() WHERE Id = {requestId}"));

        var response = await _api.CreateClient().ApproveAsync(requestId, "Approve.");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await ReadAsync(response);
        Assert.Equal("concurrency_conflict", body["code"]!.GetValue<string>());
        Assert.Equal(requestId, body["requestId"]!.GetValue<int>());

        // Neither part of the attempted approval persisted.
        Assert.Equal(2, (await _data.GetPrescriptionAsync(arranged.PrescriptionId)).RemainingRefills);
        var request = await _data.GetRequestAsync(requestId);
        Assert.Equal(RefillRequestStatus.Denied, request.Status);
        Assert.Equal("Decided elsewhere.", request.DecisionNote);
    }

    [Fact]
    public async Task Prescription_row_conflict_rolls_back_the_whole_approval()
    {
        var arranged = await _data.ArrangeAsync(Now, remainingRefills: 2);
        _interceptor.RunOnceBeforeNextSave(() => _data.ExecuteSqlAsync(
            $"UPDATE Prescriptions SET RemainingRefills = 5 WHERE Id = {arranged.PrescriptionId}"));

        var response = await _api.CreateClient().ApproveAsync(arranged.RequestId!.Value, "Approve.");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("concurrency_conflict", (await ReadAsync(response))["code"]!.GetValue<string>());

        var request = await _data.GetRequestAsync(arranged.RequestId.Value);
        Assert.Equal(RefillRequestStatus.Pending, request.Status);
        Assert.Null(request.DecidedAt);
        Assert.Equal(5, (await _data.GetPrescriptionAsync(arranged.PrescriptionId)).RemainingRefills);
    }

    [Fact]
    public async Task Request_row_conflict_on_deny_is_also_detected()
    {
        var arranged = await _data.ArrangeAsync(Now);
        var requestId = arranged.RequestId!.Value;
        _interceptor.RunOnceBeforeNextSave(() => _data.ExecuteSqlAsync(
            $"UPDATE RefillRequests SET Status = 'Approved', DecisionNote = 'Elsewhere.', DecidedAt = SYSUTCDATETIME() WHERE Id = {requestId}"));

        var response = await _api.CreateClient().DenyAsync(requestId, "Deny.");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("concurrency_conflict", (await ReadAsync(response))["code"]!.GetValue<string>());
        Assert.Equal(RefillRequestStatus.Approved, (await _data.GetRequestAsync(requestId)).Status);
    }

    /// <summary>
    /// Supplementary check (not the authoritative conflict coverage above): which path each
    /// losing call takes depends on timing, but the asserted outcome holds for any interleaving.
    /// </summary>
    [Fact]
    public async Task Simultaneous_approvals_never_consume_more_refills_than_available()
    {
        var arranged = await _data.ArrangeAsync(Now, remainingRefills: 1);
        var client = _api.CreateClient();

        var responses = await Task.WhenAll(Enumerable.Range(0, 5)
            .Select(i => client.ApproveAsync(arranged.RequestId!.Value, $"Approve {i}.")));

        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.OK));
        Assert.All(responses.Where(r => r.StatusCode != HttpStatusCode.OK), r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode));
        Assert.Equal(0, (await _data.GetPrescriptionAsync(arranged.PrescriptionId)).RemainingRefills);
    }

    [Fact]
    public async Task Last_refill_cannot_be_consumed_twice_by_sequential_requests()
    {
        var arranged = await _data.ArrangeAsync(Now, remainingRefills: 1);
        var client = _api.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.ApproveAsync(arranged.RequestId!.Value, "First.")).StatusCode);

        var second = await _data.AddPendingRequestAsync(arranged.PrescriptionId, Now);
        var response = await client.ApproveAsync(second, "Second.");

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
        Assert.Equal(
            "NO_REFILLS_REMAINING",
            (await ReadAsync(response))["evaluation"]!["blockingReasons"]![0]!["code"]!.GetValue<string>());
        Assert.Equal(0, (await _data.GetPrescriptionAsync(arranged.PrescriptionId)).RemainingRefills);
    }
}
