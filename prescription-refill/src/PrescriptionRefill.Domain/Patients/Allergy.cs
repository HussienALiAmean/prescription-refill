using PrescriptionRefill.Domain.Medications;

namespace PrescriptionRefill.Domain.Patients;

/// <summary>A patient's recorded allergy to one specific medication (FR-008).</summary>
public sealed class Allergy
{
    private Allergy()
    {
        Patient = null!;
        Medication = null!;
    }

    internal Allergy(Patient patient, Medication medication)
    {
        Patient = patient ?? throw new ArgumentNullException(nameof(patient));
        Medication = medication ?? throw new ArgumentNullException(nameof(medication));
        PatientId = patient.Id;
        MedicationId = medication.Id;
    }

    public int Id { get; private set; }

    public int PatientId { get; private set; }

    public Patient Patient { get; private set; }

    public int MedicationId { get; private set; }

    public Medication Medication { get; private set; }
}
