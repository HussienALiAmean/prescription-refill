namespace PrescriptionRefill.Domain.Medications;

/// <summary>
/// A seeded, synthetic interaction between two different medications (FR-008, FR-019).
/// Persisted pairs are stored in ascending key order so a pair has one representation.
/// </summary>
public sealed class DrugInteraction
{
    public const int MaxDescriptionLength = 300;

    private DrugInteraction()
    {
        MedicationA = null!;
        MedicationB = null!;
        DescriptionAr = null!;
        DescriptionEn = null!;
    }

    public DrugInteraction(Medication first, Medication second, string descriptionAr, string descriptionEn)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        if (first.IsSameAs(second))
        {
            throw new ArgumentException("An interaction needs two different medications.", nameof(second));
        }

        DescriptionAr = ValidDescription(descriptionAr, nameof(descriptionAr));
        DescriptionEn = ValidDescription(descriptionEn, nameof(descriptionEn));

        var swap = first.Id != 0 && second.Id != 0 && first.Id > second.Id;
        MedicationA = swap ? second : first;
        MedicationB = swap ? first : second;
        MedicationAId = MedicationA.Id;
        MedicationBId = MedicationB.Id;
    }

    public int Id { get; private set; }

    public int MedicationAId { get; private set; }

    public Medication MedicationA { get; private set; }

    public int MedicationBId { get; private set; }

    public Medication MedicationB { get; private set; }

    public string DescriptionAr { get; private set; }

    public string DescriptionEn { get; private set; }

    /// <summary>True when this pair joins the two medications, in either order.</summary>
    public bool Joins(Medication one, Medication other) =>
        (MedicationA.IsSameAs(one) && MedicationB.IsSameAs(other))
        || (MedicationA.IsSameAs(other) && MedicationB.IsSameAs(one));

    private static string ValidDescription(string description, string parameter)
    {
        if (string.IsNullOrWhiteSpace(description) || description.Trim().Length > MaxDescriptionLength)
        {
            throw new ArgumentException($"Description is required and at most {MaxDescriptionLength} characters.", parameter);
        }

        return description.Trim();
    }
}
