using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrescriptionRefill.Domain.Medications;

namespace PrescriptionRefill.Infrastructure.Persistence.Configurations;

internal sealed class DrugInteractionConfiguration : IEntityTypeConfiguration<DrugInteraction>
{
    public void Configure(EntityTypeBuilder<DrugInteraction> builder)
    {
        builder.ToTable("DrugInteractions", table =>
            table.HasCheckConstraint("CK_DrugInteractions_PairOrder", "[MedicationAId] < [MedicationBId]"));
        builder.HasKey(i => i.Id);

        builder.Property(i => i.DescriptionAr).HasMaxLength(DrugInteraction.MaxDescriptionLength).IsRequired();
        builder.Property(i => i.DescriptionEn).HasMaxLength(DrugInteraction.MaxDescriptionLength).IsRequired();

        builder.HasOne(i => i.MedicationA)
            .WithMany()
            .HasForeignKey(i => i.MedicationAId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.MedicationB)
            .WithMany()
            .HasForeignKey(i => i.MedicationBId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => new { i.MedicationAId, i.MedicationBId }).IsUnique();
    }
}
