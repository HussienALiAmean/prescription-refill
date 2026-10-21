using Microsoft.Extensions.Time.Testing;
using PrescriptionRefill.Domain.Medications;
using PrescriptionRefill.Domain.Patients;
using PrescriptionRefill.Domain.Prescriptions;

namespace PrescriptionRefill.Domain.Tests.TestSupport;

internal static class Builders
{
    public static readonly DateTimeOffset Now = new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);

    public static FakeTimeProvider Clock(DateTimeOffset? at = null) => new(at ?? Now);

    /// <summary>English name as given; the Arabic name is derived so both stay unique per test.</summary>
    public static Medication Medication(string name = "Testmed") => new($"دواء {name}", name);

    public static Patient Patient() => new("Test Patient", new DateOnly(1980, 1, 1));

    public static Prescription Prescription(
        Patient? patient = null,
        Medication? medication = null,
        int daysSupply = 30,
        int remainingRefills = 2,
        DateTimeOffset? lastFilledAt = null,
        bool isActive = true) =>
        new(
            patient ?? Patient(),
            medication ?? Medication(),
            "1 tablet by mouth once daily",
            daysSupply,
            remainingRefills,
            lastFilledAt ?? Now.AddDays(-30),
            isActive);

    /// <summary>
    /// Simulates a value that was persisted outside the Domain's constructors (for example
    /// invalid stored data, FR-027) by setting a private setter.
    /// </summary>
    public static T WithStoredValue<T>(this T entity, string propertyName, object? value)
        where T : class
    {
        var property = typeof(T).GetProperty(propertyName)
            ?? throw new ArgumentException($"No property {propertyName} on {typeof(T).Name}.");
        property.SetValue(entity, value);
        return entity;
    }
}
