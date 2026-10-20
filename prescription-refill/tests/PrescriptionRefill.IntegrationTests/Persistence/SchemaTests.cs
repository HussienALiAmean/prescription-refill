using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PrescriptionRefill.IntegrationTests.Infrastructure;

namespace PrescriptionRefill.IntegrationTests.Persistence;

public sealed class SchemaTests(SqlServerDatabaseFixture database) : IClassFixture<SqlServerDatabaseFixture>
{
    private readonly TestData _data = new(database);

    [Fact]
    public async Task Migrations_are_fully_applied_to_the_new_database()
    {
        await using var db = database.CreateContext();

        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.Contains("20261020110824_InitialCreate", await db.Database.GetAppliedMigrationsAsync());
    }

    [Fact]
    public async Task Remaining_refills_cannot_be_negative()
    {
        var arranged = await _data.ArrangeAsync(RefillApiFactory.StartTime);

        var error = await Assert.ThrowsAsync<SqlException>(() =>
            _data.ExecuteSqlAsync($"UPDATE Prescriptions SET RemainingRefills = -1 WHERE Id = {arranged.PrescriptionId}"));

        Assert.Equal(547, error.Number); // CHECK constraint violation
        Assert.Contains("CK_Prescriptions_RemainingRefills", error.Message);
    }

    [Fact]
    public async Task Days_supply_must_be_positive()
    {
        var arranged = await _data.ArrangeAsync(RefillApiFactory.StartTime);

        var error = await Assert.ThrowsAsync<SqlException>(() =>
            _data.ExecuteSqlAsync($"UPDATE Prescriptions SET DaysSupply = 0 WHERE Id = {arranged.PrescriptionId}"));

        Assert.Equal(547, error.Number);
        Assert.Contains("CK_Prescriptions_DaysSupply", error.Message);
    }

    [Fact]
    public async Task Last_filled_time_cannot_be_null()
    {
        var arranged = await _data.ArrangeAsync(RefillApiFactory.StartTime);

        var error = await Assert.ThrowsAsync<SqlException>(() =>
            _data.ExecuteSqlAsync($"UPDATE Prescriptions SET LastFilledAt = NULL WHERE Id = {arranged.PrescriptionId}"));

        Assert.Equal(515, error.Number); // cannot insert NULL
    }

    [Fact]
    public async Task Interaction_pairs_must_be_stored_in_ascending_order()
    {
        var arranged = await _data.ArrangeAsync(RefillApiFactory.StartTime);

        var error = await Assert.ThrowsAsync<SqlException>(() =>
            _data.ExecuteSqlAsync(
                $"INSERT INTO DrugInteractions (MedicationAId, MedicationBId, Description) VALUES ({arranged.MedicationId + 1}, {arranged.MedicationId}, 'reversed')"));

        Assert.Equal(547, error.Number);
        Assert.Contains("CK_DrugInteractions_PairOrder", error.Message);
    }

    [Fact]
    public async Task Pending_index_is_unique_and_filtered_to_pending_requests()
    {
        await using var db = database.CreateContext();

        var index = await db.Database
            .SqlQuery<IndexInfo>($"""
                SELECT i.is_unique AS IsUnique, i.has_filter AS HasFilter, i.filter_definition AS FilterDefinition
                FROM sys.indexes i
                WHERE i.name = 'UX_RefillRequests_Pending' AND i.object_id = OBJECT_ID('RefillRequests')
                """)
            .SingleAsync();

        Assert.True(index.IsUnique);
        Assert.True(index.HasFilter);
        Assert.Equal("([Status]=N'Pending')", index.FilterDefinition);
    }

    [Fact]
    public async Task Both_concurrency_tokens_are_rowversion_columns()
    {
        await using var db = database.CreateContext();

        var tables = await db.Database
            .SqlQuery<string>($"""
                SELECT t.name AS Value
                FROM sys.columns c
                JOIN sys.tables t ON t.object_id = c.object_id
                JOIN sys.types ty ON ty.user_type_id = c.user_type_id
                WHERE c.name = 'RowVersion' AND ty.name = 'timestamp'
                ORDER BY t.name
                """)
            .ToListAsync();

        Assert.Equal(["Prescriptions", "RefillRequests"], tables);
    }

    private sealed record IndexInfo(bool IsUnique, bool HasFilter, string? FilterDefinition);
}
