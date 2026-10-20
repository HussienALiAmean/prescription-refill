using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrescriptionRefill.Domain.Patients;

namespace PrescriptionRefill.Infrastructure.Persistence.Configurations;

internal sealed class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.FullName).HasMaxLength(Patient.MaxNameLength).IsRequired();
        builder.Property(p => p.DateOfBirth).HasColumnType("date").IsRequired();

        builder.HasMany(p => p.Allergies)
            .WithOne(a => a.Patient)
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(p => p.Allergies).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
