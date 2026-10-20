using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PrescriptionRefill.Application.Common;
using PrescriptionRefill.Domain.RefillRequests;
using PrescriptionRefill.Infrastructure.Persistence;
using PrescriptionRefill.IntegrationTests.Infrastructure;
using static PrescriptionRefill.IntegrationTests.Infrastructure.Json;

namespace PrescriptionRefill.IntegrationTests.Concurrency;

/// <summary>
/// One Pending request per prescription under concurrency (FR-003), enforced by the SQL Server
/// filtered unique index. Competing operations are injected deterministically before a save.
/// </summary>
public sealed class CreateConcurrencyTests : IClassFixture<SqlServerDatabaseFixture>, IDisposable
{
    private readonly SqlServerDatabaseFixture _database;
    private readonly BeforeSaveInterceptor _interceptor = new();
    private readonly RefillApiFactory _api;
    private readonly TestData _data;

    public CreateConcurrencyTests(SqlServerDatabaseFixture database)
    {
        _database = database;
        _data = new TestData(database);
        _api = new RefillApiFactory(database)
        {
            ConfigureServices = services =>
                services.ConfigureDbContext<RefillDbContext>(options => options.AddInterceptors(_interceptor)),
        };
    }

    private DateTimeOffset Now => _api.Clock.GetUtcNow();

    public void Dispose() => _api.Dispose();

    private Task<HttpResponseMessage> CreateAsync(int prescriptionId) =>
        _api.CreateClient().PostAsJsonAsync("/api/refill-requests", new { prescriptionId });

    private async Task<int> PendingCountAsync(int prescriptionId)
    {
        await using var db = _database.CreateContext();
        return await db.RefillRequests.CountAsync(r => r.PrescriptionId == prescriptionId && r.Status == RefillRequestStatus.Pending);
    }

    [Fact]
    public async Task Two_contexts_inserting_pending_requests_for_one_prescription_commit_only_one()
    {
        var arranged = await _data.ArrangeAsync(Now, withPendingRequest: false);
        await using var first = _database.CreateContext();
        await using var second = _database.CreateContext();
        first.RefillRequests.Add(RefillRequest.Create(await first.Prescriptions.SingleAsync(p => p.Id == arranged.PrescriptionId), Now));
        second.RefillRequests.Add(RefillRequest.Create(await second.Prescriptions.SingleAsync(p => p.Id == arranged.PrescriptionId), Now));

        await first.SaveChangesAsync();
        await Assert.ThrowsAsync<PendingRequestExistsException>(() => second.SaveChangesAsync());

        Assert.Equal(1, await PendingCountAsync(arranged.PrescriptionId));
    }

    [Fact]
    public async Task Create_racing_another_create_returns_pending_request_exists()
    {
        var arranged = await _data.ArrangeAsync(Now, withPendingRequest: false);
        _interceptor.RunOnceBeforeNextSave(() => _data.AddPendingRequestAsync(arranged.PrescriptionId, Now));

        var response = await CreateAsync(arranged.PrescriptionId);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("pending_request_exists", (await ReadAsync(response))["code"]!.GetValue<string>());
        Assert.Equal(1, await PendingCountAsync(arranged.PrescriptionId));
    }

    [Fact]
    public async Task Create_while_the_previous_request_is_approved_first_ends_with_one_pending_and_one_refill_used()
    {
        var arranged = await _data.ArrangeAsync(Now, remainingRefills: 2);
        var client = _api.CreateClient();
        HttpResponseMessage? approval = null;
        // The approval commits after the create loaded its data and before the create saves.
        _interceptor.RunOnceBeforeNextSave(async () => approval = await client.ApproveAsync(arranged.RequestId!.Value, "Approve A."));

        var create = await CreateAsync(arranged.PrescriptionId);

        Assert.Equal(HttpStatusCode.OK, approval!.StatusCode);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Assert.Equal(1, await PendingCountAsync(arranged.PrescriptionId));
        Assert.Equal(1, (await _data.GetPrescriptionAsync(arranged.PrescriptionId)).RemainingRefills);
    }

    [Fact]
    public async Task Create_during_an_approval_of_the_pending_request_is_rejected_and_the_refill_is_used_once()
    {
        var arranged = await _data.ArrangeAsync(Now, remainingRefills: 2);
        var client = _api.CreateClient();
        HttpResponseMessage? create = null;
        // The create runs while A is still Pending in the database.
        _interceptor.RunOnceBeforeNextSave(async () => create = await client.PostAsJsonAsync("/api/refill-requests", new { prescriptionId = arranged.PrescriptionId }));

        var approval = await client.ApproveAsync(arranged.RequestId!.Value, "Approve A.");

        Assert.Equal(HttpStatusCode.Conflict, create!.StatusCode);
        Assert.Equal("pending_request_exists", (await ReadAsync(create))["code"]!.GetValue<string>());
        Assert.Equal(HttpStatusCode.OK, approval.StatusCode);
        Assert.Equal(0, await PendingCountAsync(arranged.PrescriptionId));
        Assert.Equal(1, (await _data.GetPrescriptionAsync(arranged.PrescriptionId)).RemainingRefills);
    }
}
