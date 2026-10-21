using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PrescriptionRefill.Application.Localization;
using PrescriptionRefill.Application.Mapping;
using PrescriptionRefill.Application.RefillRequests.Queries;
using PrescriptionRefill.Domain.RefillRequests;
using PrescriptionRefill.Infrastructure.Persistence;
using PrescriptionRefill.IntegrationTests.Infrastructure;

namespace PrescriptionRefill.IntegrationTests.Persistence;

/// <summary>
/// DG-1 condition 2 and spec 005 FR-022 (quickstart V3): each localized projection translates to
/// SQL on SQL Server, names only the resolved language's column, and runs with no client
/// evaluation.
/// </summary>
public sealed class ProjectionSqlTests(SqlServerDatabaseFixture database) : IClassFixture<SqlServerDatabaseFixture>
{
    private readonly TestData _data = new(database);

    public static TheoryData<Language, string, string> Columns => new()
    {
        { Language.Arabic, "Ar", "En" },
        { Language.English, "En", "Ar" },
    };

    [Theory]
    [MemberData(nameof(Columns))]
    public async Task Medication_name_projection_selects_only_the_language_column(Language language, string own, string other)
    {
        var arranged = await _data.ArrangeAsync(RefillApiFactory.StartTime);
        var (reference, db, log) = Create();
        await using var _ = db;

        var query = reference.MedicationNames([arranged.MedicationId], language);
        var sql = query.ToQueryString();

        Assert.Contains($"[Name{own}]", sql);
        Assert.DoesNotContain($"Name{other}", sql);
        Assert.DoesNotContain("CASE", sql, StringComparison.OrdinalIgnoreCase);

        var row = Assert.Single(await query.ToListAsync());
        Assert.Equal(language == Language.Arabic ? arranged.MedicationNameAr : arranged.MedicationName, row.Name);
        AssertNoClientEvaluation(log);
    }

    [Theory]
    [MemberData(nameof(Columns))]
    public async Task Interaction_description_projection_selects_only_the_language_column(Language language, string own, string other)
    {
        var arranged = await _data.ArrangeAsync(RefillApiFactory.StartTime);
        var (reference, db, log) = Create();
        await using var _ = db;
        var interactionId = await db.DrugInteractions
            .Where(i => i.MedicationAId == arranged.MedicationId || i.MedicationBId == arranged.MedicationId)
            .Select(i => i.Id)
            .SingleAsync();

        var query = reference.InteractionDescriptions([interactionId], language);
        var sql = query.ToQueryString();

        Assert.Contains($"[Description{own}]", sql);
        Assert.DoesNotContain($"Description{other}", sql);
        Assert.DoesNotContain("CASE", sql, StringComparison.OrdinalIgnoreCase);

        var row = Assert.Single(await query.ToListAsync());
        Assert.Equal(language == Language.Arabic ? TestData.ArabicInteractionDescription : "Synthetic test interaction.", row.Description);
        AssertNoClientEvaluation(log);
    }

    [Theory]
    [MemberData(nameof(Columns))]
    public async Task Search_matches_only_the_language_column(Language language, string own, string other)
    {
        var (_, db, log) = Create();
        await using var __ = db;

        var query = GetRefillRequests.Matching(db.RefillRequests.Where(r => r.Status == RefillRequestStatus.Pending), "term", language);
        var sql = query.ToQueryString();

        Assert.Contains($"[Name{own}]", sql);
        Assert.DoesNotContain($"Name{other}", sql);
        Assert.DoesNotContain("CASE", sql, StringComparison.OrdinalIgnoreCase);
        await query.CountAsync();
        AssertNoClientEvaluation(log);
    }

    private (LocalizedReferenceData Reference, RefillDbContext Db, CapturingLoggerProvider Log) Create()
    {
        var log = new CapturingLoggerProvider();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(log).SetMinimumLevel(LogLevel.Debug));
        var db = new RefillDbContext(new DbContextOptionsBuilder<RefillDbContext>()
            .UseSqlServer(database.ConnectionString)
            .UseLoggerFactory(loggerFactory)
            .Options);
        var projections = new LocalizedProjections(licenseKey: null, NullLoggerFactory.Instance);
        return (new LocalizedReferenceData(db, projections), db, log);
    }

    /// <summary>
    /// EF Core 10 throws for untranslatable queries. This also rules out any query warning, such as
    /// a client-side projection or a split-query fallback.
    /// </summary>
    private static void AssertNoClientEvaluation(CapturingLoggerProvider log) =>
        Assert.DoesNotContain(log.Entries, e => e.StartsWith("Warning Microsoft.EntityFrameworkCore.Query", StringComparison.Ordinal));
}
