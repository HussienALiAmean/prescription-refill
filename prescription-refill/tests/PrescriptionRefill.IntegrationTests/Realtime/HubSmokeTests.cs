using System.Text.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using PrescriptionRefill.IntegrationTests.Infrastructure;

namespace PrescriptionRefill.IntegrationTests.Realtime;

/// <summary>End-to-end check of the real SignalR hub and payload shape (FR-029).</summary>
public sealed class HubSmokeTests(SqlServerDatabaseFixture database)
    : IClassFixture<SqlServerDatabaseFixture>, IAsyncLifetime
{
    private readonly RefillApiFactory _api = new(database);
    private readonly TestData _data = new(database);
    private HubConnection? _connection;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        await _api.DisposeAsync();
    }

    [Fact]
    public async Task Connected_client_receives_a_minimal_request_updated_event_after_approval()
    {
        var client = _api.CreateClient(); // starts the in-process test server
        var received = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);

        // The in-process test server supports long polling without extra WebSocket setup.
        _connection = new HubConnectionBuilder()
            .WithUrl(new Uri(_api.Server.BaseAddress, "/hubs/refill-requests"), options =>
            {
                options.HttpMessageHandlerFactory = _ => _api.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            })
            .Build();
        _connection.On<JsonElement>("RequestUpdated", payload => received.TrySetResult(payload));
        await _connection.StartAsync();

        var arranged = await _data.ArrangeAsync(_api.Clock.GetUtcNow());
        await client.ApproveAsync(arranged.RequestId!.Value, "Fine.");

        var payload = await received.Task.WaitAsync(TimeSpan.FromSeconds(15));
        Assert.Equal(["requestId", "change"], payload.EnumerateObject().Select(p => p.Name));
        Assert.Equal(arranged.RequestId.Value, payload.GetProperty("requestId").GetInt32());
        Assert.Equal("decided", payload.GetProperty("change").GetString());
    }
}
