using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrescriptionRefill.Domain.Prescriptions;

namespace PrescriptionRefill.Infrastructure.Persistence.Configurations;

internal sealed class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
{
    public void Configure(EntityTypeBuilder<Prescription> builder)
    {
        builder.ToTable("Prescriptions", table =>
        {
            table.HasCheckConstraint("CK_Prescriptions_DaysSupply", "[DaysSupply] > 0");
            table.HasCheckConstraint("CK_Prescriptions_RemainingRefills", "[RemainingRefills] >= 0");
        });
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Dosage).HasMaxLength(Prescription.MaxDosageLength).IsRequired();
        builder.Property(p => p.LastFilledAt).IsRequired();

        builder.HasOne(p => p.Patient)
            .WithMany()
            .HasForeignKey(p => p.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Medication)
            .WithMany()
            .HasForeignKey(p => p.MedicationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Concurrency token (constitution Principle IV): protects the refill count.
        builder.Property<byte[]>(RefillDbContext.RowVersionProperty).IsRowVersion();
    }
}
