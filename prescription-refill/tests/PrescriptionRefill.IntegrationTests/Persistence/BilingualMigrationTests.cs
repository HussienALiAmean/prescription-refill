using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrescriptionRefill.Infrastructure.Persistence;
using PrescriptionRefill.IntegrationTests.Infrastructure;

namespace PrescriptionRefill.IntegrationTests.Persistence;

/// <summary>
/// The <c>BilingualReferenceData</c> migration on SQL Server against a database created before
/// feature 005 (quickstart V7, research R7). Expected Arabic values are literals here, not
/// <c>SeedNames</c>, so a later seeder change cannot hide a migration change.
/// </summary>
public sealed class BilingualMigrationTests : IAsyncLifetime
{
    private const string InitialCreate = "20261020110824_InitialCreate";

    private readonly string _connectionString;

    public BilingualMigrationTests()
    {
        var server = Environment.GetEnvironmentVariable(SqlServerDatabaseFixture.ServerVariable);
        _connectionString = new SqlConnectionStringBuilder(string.IsNullOrWhiteSpace(server) ? SqlServerDatabaseFixture.DefaultServer : server)
        {
            InitialCatalog = $"RefillMigrationTests_{Guid.NewGuid():N}",
        }.ConnectionString;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await using var db = Context();
        await db.Database.EnsureDeletedAsync();
    }

    [Fact]
    public async Task Existing_values_become_English_and_seeded_rows_get_their_authored_Arabic_values()
    {
        await MigrateToAsync(InitialCreate);
        await ExecuteAsync("""
            INSERT INTO Medications (Name) VALUES (N'Halvexin'), (N'Jurvane'), (N'Unseeded Local Med');
            INSERT INTO DrugInteractions (MedicationAId, MedicationBId, Description)
            SELECT a.Id, b.Id, N'Synthetic demo interaction (fictional, not clinical guidance).'
            FROM Medications a, Medications b WHERE a.Name = N'Halvexin' AND b.Name = N'Jurvane';
            INSERT INTO DrugInteractions (MedicationAId, MedicationBId, Description)
            SELECT a.Id, b.Id, N'A locally added description.'
            FROM Medications a, Medications b WHERE a.Name = N'Jurvane' AND b.Name = N'Unseeded Local Med';
            """);

        await MigrateToAsync(null);

        await using var db = Context();
        var names = await db.Medications.AsNoTracking().ToDictionaryAsync(m => m.NameEn, m => m.NameAr);
        Assert.Equal("هالفيكسين", names["Halvexin"]);
        Assert.Equal("جورفان", names["Jurvane"]);
        Assert.Equal("Unseeded Local Med", names["Unseeded Local Med"]);

        var descriptions = await db.DrugInteractions.AsNoTracking().ToDictionaryAsync(i => i.DescriptionEn, i => i.DescriptionAr);
        Assert.Equal("تداخل تجريبي مُصطنع (خيالي، وليس إرشادًا سريريًا).", descriptions["Synthetic demo interaction (fictional, not clinical guidance)."]);
        Assert.Equal("A locally added description.", descriptions["A locally added description."]);

        var uniqueIndexes = await db.Database.SqlQuery<string>($"""
            SELECT name AS Value FROM sys.indexes
            WHERE object_id = OBJECT_ID('Medications') AND is_unique = 1 AND name LIKE 'IX_Medications_Name%'
            ORDER BY name
            """).ToListAsync();
        Assert.Equal(["IX_Medications_NameAr", "IX_Medications_NameEn"], uniqueIndexes);
    }

    [Fact]
    public async Task Down_restores_the_single_language_schema_with_the_English_values()
    {
        await MigrateToAsync(InitialCreate);
        await ExecuteAsync("INSERT INTO Medications (Name) VALUES (N'Avelorin');");
        await MigrateToAsync(null);

        await MigrateToAsync(InitialCreate);

        await using var db = Context();
        var columns = await db.Database.SqlQuery<string>($"""
            SELECT t.name + '.' + c.name AS Value
            FROM sys.columns c JOIN sys.tables t ON t.object_id = c.object_id
            WHERE t.name IN ('Medications', 'DrugInteractions') AND c.name NOT IN ('Id', 'MedicationAId', 'MedicationBId')
            ORDER BY t.name, c.name
            """).ToListAsync();
        Assert.Equal(["DrugInteractions.Description", "Medications.Name"], columns);

        var name = await db.Database.SqlQuery<string>($"SELECT Name AS Value FROM Medications").SingleAsync();
        Assert.Equal("Avelorin", name);
        var index = await db.Database.SqlQuery<string>($"""
            SELECT name AS Value FROM sys.indexes WHERE object_id = OBJECT_ID('Medications') AND is_unique = 1 AND name LIKE 'IX_%'
            """).SingleAsync();
        Assert.Equal("IX_Medications_Name", index);
    }

    private RefillDbContext Context() =>
        new(new DbContextOptionsBuilder<RefillDbContext>()
            .UseSqlServer(_connectionString, sql => sql.CommandTimeout(SqlServerDatabaseFixture.CommandTimeoutSeconds))
            .Options);

    /// <param name="target">A migration id, or null for the latest.</param>
    private async Task MigrateToAsync(string? target)
    {
        await using var db = Context();
        await db.GetService<IMigrator>().MigrateAsync(target);
    }

    private async Task ExecuteAsync(string sql)
    {
        await using var db = Context();
        await db.Database.ExecuteSqlRawAsync(sql);
    }
}
