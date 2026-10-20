using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrescriptionRefill.Domain.Patients;

namespace PrescriptionRefill.Infrastructure.Persistence.Configurations;

internal sealed class AllergyConfiguration : IEntityTypeConfiguration<Allergy>
{
    public void Configure(EntityTypeBuilder<Allergy> builder)
    {
        builder.ToTable("Allergies");
        builder.HasKey(a => a.Id);

        builder.HasOne(a => a.Medication)
            .WithMany()
            .HasForeignKey(a => a.MedicationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.PatientId, a.MedicationId }).IsUnique();
    }
}
