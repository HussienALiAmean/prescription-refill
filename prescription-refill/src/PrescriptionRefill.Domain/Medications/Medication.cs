namespace PrescriptionRefill.Domain.Medications;

/// <summary>
/// A synthetic demo medication (FR-019) with an authored Arabic and English name, each unique
/// within its language (spec 005 FR-019). No rule reads a name.
/// </summary>
public sealed class Medication
{
    public const int MaxNameLength = 100;

    private Medication()
    {
        NameAr = null!;
        NameEn = null!;
    }

    public Medication(string nameAr, string nameEn)
    {
        NameAr = ValidName(nameAr, nameof(nameAr));
        NameEn = ValidName(nameEn, nameof(nameEn));
    }

    public int Id { get; private set; }

    public string NameAr { get; private set; }

    public string NameEn { get; private set; }

    /// <summary>
    /// Identity comparison that works both for persisted rows (by key) and for unsaved objects
    /// (by reference), so the same rule code runs in unit tests and against the database.
    /// </summary>
    public bool IsSameAs(Medication other) =>
        ReferenceEquals(this, other) || (Id != 0 && Id == other.Id);

    private static string ValidName(string name, string parameter)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > MaxNameLength)
        {
            throw new ArgumentException($"Medication name is required and at most {MaxNameLength} characters.", parameter);
        }

        return name.Trim();
    }
}
