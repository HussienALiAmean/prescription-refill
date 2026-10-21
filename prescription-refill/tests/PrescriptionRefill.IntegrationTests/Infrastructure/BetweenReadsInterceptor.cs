using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace PrescriptionRefill.IntegrationTests.Infrastructure;

/// <summary>
/// Commits a competing change once, right after the API's tagged rule read has finished reading
/// and before any later read of the same request (spec 005 T037). It runs on a separate
/// connection when the reader closes, so it never waits on the rule read's shared locks.
/// </summary>
public sealed class BetweenReadsInterceptor(string connectionString, string tag) : DbCommandInterceptor
{
    private string? _once;

    public void RunOnceAfterTaggedRead(string sql) => _once = sql;

    public override async ValueTask<InterceptionResult> DataReaderClosingAsync(
        DbCommand command,
        DataReaderClosingEventData eventData,
        InterceptionResult result)
    {
        if (command.CommandText.Contains(tag, StringComparison.Ordinal) && Interlocked.Exchange(ref _once, null) is { } sql)
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            await using var competing = new SqlCommand(sql, connection);
            await competing.ExecuteNonQueryAsync();
        }

        return result;
    }
}
