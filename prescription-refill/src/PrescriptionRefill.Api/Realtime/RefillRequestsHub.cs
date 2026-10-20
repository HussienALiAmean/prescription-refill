using Microsoft.AspNetCore.SignalR;

namespace PrescriptionRefill.Api.Realtime;

/// <summary>
/// Live-update hub at <c>/hubs/refill-requests</c> (FR-029). Server-to-client only: it exposes no
/// client-callable methods, so it cannot change data. Unauthenticated demo channel; not
/// production-secure (FR-020).
/// </summary>
public sealed class RefillRequestsHub : Hub
{
    public const string Path = "/hubs/refill-requests";
    public const string RequestUpdatedEvent = "RequestUpdated";
}
