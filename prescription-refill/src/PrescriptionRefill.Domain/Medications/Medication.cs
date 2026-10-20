namespace PrescriptionRefill.Domain.Medications;

/// <summary>A synthetic demo medication (FR-019).</summary>
public sealed class Medication
{
    public const int MaxNameLength = 100;

    private Medication()
    {
        Name = null!;
    }

    public Medication(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > MaxNameLength)
        {
            throw new ArgumentException($"Medication name is required and at most {MaxNameLength} characters.", nameof(name));
        }

        Name = name.Trim();
    }

    public int Id { get; private set; }

    public string Name { get; private set; }

    /// <summary>
    /// Identity comparison that works both for persisted rows (by key) and for unsaved objects
    /// (by reference), so the same rule code runs in unit tests and against the database.
    /// </summary>
    public bool IsSameAs(Medication other) =>
        ReferenceEquals(this, other) || (Id != 0 && Id == other.Id);
}
