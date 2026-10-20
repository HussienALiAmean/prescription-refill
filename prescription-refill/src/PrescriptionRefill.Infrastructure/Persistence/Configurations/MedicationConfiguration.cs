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

        builder.Property(m => m.Name).HasMaxLength(Medication.MaxNameLength).IsRequired();
        builder.HasIndex(m => m.Name).IsUnique();
    }
}
