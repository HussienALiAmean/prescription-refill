namespace PrescriptionRefill.Domain.Patients;

/// <summary>A fictional patient (FR-019).</summary>
public sealed class Patient
{
    public const int MaxNameLength = 200;

    private readonly List<Allergy> _allergies = [];

    private Patient()
    {
        FullName = null!;
    }

    public Patient(string fullName, DateOnly dateOfBirth)
    {
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Trim().Length > MaxNameLength)
        {
            throw new ArgumentException($"Full name is required and at most {MaxNameLength} characters.", nameof(fullName));
        }

        FullName = fullName.Trim();
        DateOfBirth = dateOfBirth;
    }

    public int Id { get; private set; }

    public string FullName { get; private set; }

    public DateOnly DateOfBirth { get; private set; }

    public IReadOnlyCollection<Allergy> Allergies => _allergies;

    public Allergy AddAllergy(Medications.Medication medication)
    {
        if (_allergies.Any(a => a.Medication.IsSameAs(medication)))
        {
            throw new InvalidOperationException("The allergy is already recorded.");
        }

        var allergy = new Allergy(this, medication);
        _allergies.Add(allergy);
        return allergy;
    }
}
