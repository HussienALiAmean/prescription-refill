using Microsoft.AspNetCore.SignalR;
using PrescriptionRefill.Application.Abstractions;

namespace PrescriptionRefill.Api.Realtime;

/// <summary>Minimal hint payload: request ID and kind of change only (FR-029).</summary>
public sealed record RequestUpdatedMessage(int RequestId, string Change);

/// <summary>Broadcasts <c>RequestUpdated</c> to every connected client (no identity, no groups).</summary>
internal sealed class SignalRRequestNotifier(IHubContext<RefillRequestsHub> hub) : IRequestNotifier
{
    public Task RequestUpdatedAsync(int requestId, RequestChange change, CancellationToken cancellationToken) =>
        hub.Clients.All.SendAsync(
            RefillRequestsHub.RequestUpdatedEvent,
            new RequestUpdatedMessage(requestId, change == RequestChange.Created ? "created" : "decided"),
            cancellationToken);
}
