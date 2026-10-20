using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PrescriptionRefill.Application.Abstractions;
using PrescriptionRefill.Application.Common;
using PrescriptionRefill.Domain.Medications;
using PrescriptionRefill.Domain.Patients;
using PrescriptionRefill.Domain.Prescriptions;
using PrescriptionRefill.Domain.RefillRequests;
using PrescriptionRefill.Infrastructure.Persistence.Configurations;

namespace PrescriptionRefill.Infrastructure.Persistence;

public sealed class RefillDbContext(DbContextOptions<RefillDbContext> options)
    : DbContext(options), IRefillDbContext
{
    /// <summary>Shadow <c>rowversion</c> column on Prescriptions and RefillRequests.</summary>
    public const string RowVersionProperty = "RowVersion";

    public DbSet<Patient> Patients => Set<Patient>();

    public DbSet<Allergy> Allergies => Set<Allergy>();

    public DbSet<Medication> Medications => Set<Medication>();

    public DbSet<DrugInteraction> DrugInteractions => Set<DrugInteraction>();

    public DbSet<Prescription> Prescriptions => Set<Prescription>();

    public DbSet<RefillRequest> RefillRequests => Set<RefillRequest>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Applies to DateTimeOffset and DateTimeOffset? properties alike.
        configurationBuilder.Properties<DateTimeOffset>()
            .HaveConversion<UtcDateTimeOffsetConverter>()
            .HaveColumnType("datetimeoffset(7)");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RefillDbContext).Assembly);
    }

    /// <summary>
    /// Saves in one transaction. A rowversion mismatch on any row makes EF Core roll the
    /// transaction back and then throw; it surfaces as <see cref="ConcurrencyConflictException"/>
    /// and is never retried (research R5).
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException(ex);
        }
        catch (DbUpdateException ex) when (IsPendingIndexViolation(ex))
        {
            throw new PendingRequestExistsException(ex);
        }
    }

    // 2601: duplicate key in a unique index; 2627: unique constraint violation.
    private static bool IsPendingIndexViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: 2601 or 2627 } sql
        && sql.Message.Contains(RefillRequestConfiguration.PendingIndexName, StringComparison.Ordinal);
}
