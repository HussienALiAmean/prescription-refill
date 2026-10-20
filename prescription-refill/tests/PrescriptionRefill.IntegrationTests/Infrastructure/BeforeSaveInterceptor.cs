using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace PrescriptionRefill.IntegrationTests.Infrastructure;

/// <summary>
/// Runs a callback once, after the API's unit of work has loaded its rows and immediately before
/// it saves. The callback commits a competing change through a separate context, which makes
/// concurrency conflicts deterministic instead of timing-dependent (constitution Principle V).
/// </summary>
public sealed class BeforeSaveInterceptor : SaveChangesInterceptor
{
    private Func<Task>? _once;

    public void RunOnceBeforeNextSave(Func<Task> competingChange) => _once = competingChange;

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var callback = Interlocked.Exchange(ref _once, null);
        if (callback is not null)
        {
            await callback();
        }

        return result;
    }
}
