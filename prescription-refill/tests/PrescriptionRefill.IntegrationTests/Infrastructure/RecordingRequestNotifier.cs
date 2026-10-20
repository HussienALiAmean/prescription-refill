using Microsoft.EntityFrameworkCore;
using PrescriptionRefill.Application.Abstractions;
using PrescriptionRefill.Domain.RefillRequests;

namespace PrescriptionRefill.IntegrationTests.Infrastructure;

/// <param name="StatusInDatabase">The request's status read from SQL Server through a separate
/// context at the moment of the call, or null if the row was not visible (not committed).</param>
public sealed record RecordedNotification(int RequestId, RequestChange Change, RefillRequestStatus? StatusInDatabase);

/// <summary>
/// Test double for the notifier port (FR-029–FR-031). It reads the database when called, which
/// proves a notification is only sent after the change was committed.
/// </summary>
public sealed class RecordingRequestNotifier(SqlServerDatabaseFixture database) : IRequestNotifier
{
    private readonly List<RecordedNotification> _calls = [];

    public bool Throw { get; set; }

    public IReadOnlyList<RecordedNotification> Calls
    {
        get
        {
            lock (_calls)
            {
                return _calls.ToList();
            }
        }
    }

    public async Task RequestUpdatedAsync(int requestId, RequestChange change, CancellationToken cancellationToken)
    {
        await using var db = database.CreateContext();
        var status = await db.RefillRequests
            .Where(r => r.Id == requestId)
            .Select(r => (RefillRequestStatus?)r.Status)
            .SingleOrDefaultAsync(CancellationToken.None);

        lock (_calls)
        {
            _calls.Add(new RecordedNotification(requestId, change, status));
        }

        if (Throw)
        {
            throw new InvalidOperationException("Simulated notification delivery failure.");
        }
    }
}
