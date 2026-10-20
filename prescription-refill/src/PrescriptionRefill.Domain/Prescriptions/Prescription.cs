using PrescriptionRefill.Domain.Medications;
using PrescriptionRefill.Domain.Patients;

namespace PrescriptionRefill.Domain.Prescriptions;

/// <summary>
/// A patient's prescription. Values loaded from storage are not re-validated here; the
/// evaluator treats invalid stored timing data as blocking (FR-027).
/// </summary>
public sealed class Prescription
{
    public const int MaxDosageLength = 200;

    private Prescription()
    {
        Patient = null!;
        Medication = null!;
        Dosage = null!;
    }

    public Prescription(
        Patient patient,
        Medication medication,
        string dosage,
        int daysSupply,
        int remainingRefills,
        DateTimeOffset lastFilledAt,
        bool isActive)
    {
        ArgumentNullException.ThrowIfNull(patient);
        ArgumentNullException.ThrowIfNull(medication);

        if (string.IsNullOrWhiteSpace(dosage) || dosage.Trim().Length > MaxDosageLength)
        {
            throw new ArgumentException($"Dosage is required and at most {MaxDosageLength} characters.", nameof(dosage));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(daysSupply);
        ArgumentOutOfRangeException.ThrowIfNegative(remainingRefills);

        Patient = patient;
        PatientId = patient.Id;
        Medication = medication;
        MedicationId = medication.Id;
        Dosage = dosage.Trim();
        DaysSupply = daysSupply;
        RemainingRefills = remainingRefills;
        LastFilledAt = lastFilledAt.ToUniversalTime();
        IsActive = isActive;
    }

    public int Id { get; private set; }

    public int PatientId { get; private set; }

    public Patient Patient { get; private set; }

    public int MedicationId { get; private set; }

    public Medication Medication { get; private set; }

    public string Dosage { get; private set; }

    public int DaysSupply { get; private set; }

    public int RemainingRefills { get; private set; }

    public DateTimeOffset LastFilledAt { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>Uses exactly one refill (FR-012); never goes below zero (FR-025).</summary>
    public void ConsumeRefill()
    {
        if (RemainingRefills <= 0)
        {
            throw new InvalidOperationException("No refills remain on this prescription.");
        }

        RemainingRefills--;
    }
}
