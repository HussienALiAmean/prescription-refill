using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PrescriptionRefill.Application.Abstractions;
using PrescriptionRefill.Domain.RefillRequests;
using PrescriptionRefill.Infrastructure.Persistence;
using PrescriptionRefill.IntegrationTests.Infrastructure;

namespace PrescriptionRefill.IntegrationTests.Realtime;

/// <summary>Notifications are sent after commit only, never on failure (FR-029–FR-031).</summary>
public sealed class NotificationTests : IClassFixture<SqlServerDatabaseFixture>, IDisposable
{
    private readonly RecordingRequestNotifier _notifier;
    private readonly BeforeSaveInterceptor _interceptor = new();
    private readonly RefillApiFactory _api;
    private readonly TestData _data;

    public NotificationTests(SqlServerDatabaseFixture database)
    {
        _data = new TestData(database);
        _notifier = new RecordingRequestNotifier(database);
        _api = new RefillApiFactory(database)
        {
            ConfigureServices = services =>
            {
                services.RemoveAll<IRequestNotifier>();
                services.AddSingleton<IRequestNotifier>(_notifier);
                services.ConfigureDbContext<RefillDbContext>(options => options.AddInterceptors(_interceptor));
            },
        };
    }

    private DateTimeOffset Now => _api.Clock.GetUtcNow();

    public void Dispose() => _api.Dispose();

    [Fact]
    public async Task Approval_notifies_once_after_the_decision_is_committed()
    {
        var arranged = await _data.ArrangeAsync(Now);

        Assert.Equal(HttpStatusCode.OK, (await _api.CreateClient().ApproveAsync(arranged.RequestId!.Value, "Fine.")).StatusCode);

        var call = Assert.Single(_notifier.Calls);
        Assert.Equal(new RecordedNotification(arranged.RequestId.Value, RequestChange.Decided, RefillRequestStatus.Approved), call);
    }

    [Fact]
    public async Task Denial_notifies_once_after_commit()
    {
        var arranged = await _data.ArrangeAsync(Now);

        await _api.CreateClient().DenyAsync(arranged.RequestId!.Value, "No.");

        Assert.Equal(
            new RecordedNotification(arranged.RequestId.Value, RequestChange.Decided, RefillRequestStatus.Denied),
            Assert.Single(_notifier.Calls));
    }

    [Fact]
    public async Task Creation_notifies_once_after_commit()
    {
        var arranged = await _data.ArrangeAsync(Now, withPendingRequest: false);

        var response = await _api.CreateClient().PostAsJsonAsync("/api/refill-requests", new { prescriptionId = arranged.PrescriptionId });

        var id = (await Json.ReadAsync(response))["id"]!.GetValue<int>();
        Assert.Equal(new RecordedNotification(id, RequestChange.Created, RefillRequestStatus.Pending), Assert.Single(_notifier.Calls));
    }

    [Fact]
    public async Task Rejected_operations_never_notify()
    {
        var client = _api.CreateClient();
        var pending = await _data.ArrangeAsync(Now);
        var blocked = await _data.ArrangeAsync(Now, remainingRefills: 0);
        var inactive = await _data.ArrangeAsync(Now, isActive: false, withPendingRequest: false);

        Assert.Equal(HttpStatusCode.BadRequest, (await client.ApproveAsync(pending.RequestId!.Value, " ")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DenyAsync(999999, "No.")).StatusCode);
        Assert.Equal((HttpStatusCode)422, (await client.ApproveAsync(blocked.RequestId!.Value, "Approve.")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/refill-requests", new { prescriptionId = pending.PrescriptionId })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/refill-requests", new { prescriptionId = inactive.PrescriptionId })).StatusCode);

        Assert.Empty(_notifier.Calls);
    }

    [Fact]
    public async Task Repeated_decision_notifies_only_for_the_committed_one()
    {
        var arranged = await _data.ArrangeAsync(Now);
        var client = _api.CreateClient();

        await client.ApproveAsync(arranged.RequestId!.Value, "First.");
        Assert.Equal(HttpStatusCode.Conflict, (await client.DenyAsync(arranged.RequestId.Value, "Second.")).StatusCode);

        Assert.Single(_notifier.Calls);
    }

    [Fact]
    public async Task Rolled_back_approval_never_notifies()
    {
        var arranged = await _data.ArrangeAsync(Now);
        _interceptor.RunOnceBeforeNextSave(() => _data.ExecuteSqlAsync(
            $"UPDATE Prescriptions SET RemainingRefills = 4 WHERE Id = {arranged.PrescriptionId}"));

        Assert.Equal(HttpStatusCode.Conflict, (await _api.CreateClient().ApproveAsync(arranged.RequestId!.Value, "Approve.")).StatusCode);

        Assert.Empty(_notifier.Calls);
    }

    [Fact]
    public async Task Delivery_failure_does_not_undo_or_fail_the_committed_decision()
    {
        var arranged = await _data.ArrangeAsync(Now);
        _notifier.Throw = true;

        var response = await _api.CreateClient().ApproveAsync(arranged.RequestId!.Value, "Fine.");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(_notifier.Calls);
        Assert.Equal(RefillRequestStatus.Approved, (await _data.GetRequestAsync(arranged.RequestId.Value)).Status);
    }
}
