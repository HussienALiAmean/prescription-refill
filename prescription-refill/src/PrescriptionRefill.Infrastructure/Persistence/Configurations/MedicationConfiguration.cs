using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrescriptionRefill.Domain.Medications;

namespace PrescriptionRefill.Infrastructure.Persistence.Configurations;

internal sealed class MedicationConfiguration : IEntityTypeConfiguration<Medication>
{
    public void Configure(EntityTypeBuilder<Medication> builder)
    {
        builder.ToTable("Medications");
        builder.HasKey(m => m.Id);

        // One Arabic and one English name on the row itself, each unique in its language; no
        // translation table (spec 005 FR-019, FR-020).
        builder.Property(m => m.NameAr).HasMaxLength(Medication.MaxNameLength).IsRequired();
        builder.Property(m => m.NameEn).HasMaxLength(Medication.MaxNameLength).IsRequired();
        builder.HasIndex(m => m.NameAr).IsUnique();
        builder.HasIndex(m => m.NameEn).IsUnique();
    }
}
