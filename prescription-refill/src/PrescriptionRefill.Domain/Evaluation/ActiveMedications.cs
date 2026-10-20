using PrescriptionRefill.Domain.Prescriptions;

namespace PrescriptionRefill.Domain.Evaluation;

/// <summary>
/// The single definition of a patient's active medications (FR-022): the medications of the
/// patient's other prescriptions marked active. Used for interaction checks and for the active
/// prescriptions shown in the patient profile.
/// </summary>
public static class ActiveMedications
{
    public static IReadOnlyList<Prescription> For(Prescription requested, IEnumerable<Prescription> patientPrescriptions)
    {
        ArgumentNullException.ThrowIfNull(requested);

        return patientPrescriptions
            .Where(p => p.IsActive && !IsSame(p, requested) && SamePatient(p, requested))
            .ToList();
    }

    private static bool IsSame(Prescription a, Prescription b) =>
        ReferenceEquals(a, b) || (a.Id != 0 && a.Id == b.Id);

    private static bool SamePatient(Prescription a, Prescription b) =>
        ReferenceEquals(a.Patient, b.Patient) || (a.PatientId != 0 && a.PatientId == b.PatientId);
}
