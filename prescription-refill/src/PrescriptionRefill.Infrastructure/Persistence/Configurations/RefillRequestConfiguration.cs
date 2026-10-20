using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrescriptionRefill.Domain.RefillRequests;

namespace PrescriptionRefill.Infrastructure.Persistence.Configurations;

internal sealed class RefillRequestConfiguration : IEntityTypeConfiguration<RefillRequest>
{
    public const string PendingIndexName = "UX_RefillRequests_Pending";

    public void Configure(EntityTypeBuilder<RefillRequest> builder)
    {
        builder.ToTable("RefillRequests");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(r => r.RequestedAt).IsRequired();
        builder.Property(r => r.DecisionNote).HasMaxLength(RefillRequest.MaxNoteLength);

        builder.HasOne(r => r.Prescription)
            .WithMany()
            .HasForeignKey(r => r.PrescriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        // At most one Pending request per prescription (FR-003), enforced by SQL Server.
        builder.HasIndex(r => r.PrescriptionId)
            .IsUnique()
            .HasFilter("[Status] = N'Pending'")
            .HasDatabaseName(PendingIndexName);

        builder.HasIndex(r => new { r.Status, r.RequestedAt });

        // Concurrency token (constitution Principle IV): protects the decision.
        builder.Property<byte[]>(RefillDbContext.RowVersionProperty).IsRowVersion();
    }
}
