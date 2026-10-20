using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PrescriptionRefill.Infrastructure.Persistence;

namespace PrescriptionRefill.IntegrationTests.Infrastructure;

/// <summary>
/// Creates a temporary SQL Server database per test class by applying the EF Core migrations
/// (constitution Principle V; research R7). Never uses InMemory, SQLite, or EnsureCreated.
/// </summary>
public sealed class SqlServerDatabaseFixture : IAsyncLifetime
{
    public const string ServerVariable = "REFILL_TEST_SQLSERVER";
    public const string DefaultServer = "Server=.;Trusted_Connection=True;TrustServerCertificate=True";

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var server = Environment.GetEnvironmentVariable(ServerVariable);
        var builder = new SqlConnectionStringBuilder(string.IsNullOrWhiteSpace(server) ? DefaultServer : server)
        {
            InitialCatalog = $"RefillTests_{Guid.NewGuid():N}",
        };
        ConnectionString = builder.ConnectionString;

        try
        {
            await using var db = CreateContext();
            await db.Database.MigrateAsync();
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException(
                $"SQL Server not available at '{builder.DataSource}'. Set {ServerVariable} to a reachable " +
                "server-level connection string. These tests must be reported as not run.", ex);
        }
    }

    public RefillDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<RefillDbContext>().UseSqlServer(ConnectionString).Options);

    public async Task DisposeAsync()
    {
        if (ConnectionString.Length == 0)
        {
            return;
        }

        await using var db = CreateContext();
        await db.Database.EnsureDeletedAsync();
    }
}
