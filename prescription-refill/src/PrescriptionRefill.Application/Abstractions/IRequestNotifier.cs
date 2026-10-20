using Microsoft.Extensions.Logging;

namespace PrescriptionRefill.Application.Abstractions;

public enum RequestChange
{
    Created,
    Decided,
}

/// <summary>
/// Port for live-update hints (FR-029). Called only after a change has been committed; the
/// payload is the request ID and kind of change, never patient data.
/// </summary>
public interface IRequestNotifier
{
    Task RequestUpdatedAsync(int requestId, RequestChange change, CancellationToken cancellationToken);
}

internal static class RequestNotifierExtensions
{
    /// <summary>
    /// Best-effort delivery (FR-031): a failure is logged with the request ID and swallowed, so a
    /// committed change and its response are never affected. Not tied to the request's
    /// cancellation, because the change is already committed.
    /// </summary>
    public static async Task NotifyAfterCommitAsync(this IRequestNotifier notifier, ILogger logger, int requestId, RequestChange change)
    {
        try
        {
            await notifier.RequestUpdatedAsync(requestId, change, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Live update for refill request {RequestId} could not be delivered", requestId);
        }
    }
}
