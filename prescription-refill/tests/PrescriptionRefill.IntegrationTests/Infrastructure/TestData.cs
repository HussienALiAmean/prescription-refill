using Microsoft.EntityFrameworkCore;
using PrescriptionRefill.Domain.Medications;
using PrescriptionRefill.Domain.Patients;
using PrescriptionRefill.Domain.Prescriptions;
using PrescriptionRefill.Domain.RefillRequests;
using PrescriptionRefill.Infrastructure.Persistence;

namespace PrescriptionRefill.IntegrationTests.Infrastructure;

/// <summary>IDs of one arranged patient, prescription, and (optionally) refill request.</summary>
/// <param name="MedicationName">The English name (what English-pinned clients see).</param>
/// <param name="MedicationNameAr">The Arabic name (spec 005 FR-019).</param>
public sealed record Arranged(int PatientId, int MedicationId, int PrescriptionId, int? RequestId, string MedicationName, string MedicationNameAr);

/// <summary>
/// Arranges rows directly in SQL Server. Names are unique per call so tests sharing one database
/// do not collide.
/// </summary>
public sealed class TestData(SqlServerDatabaseFixture database)
{
    public RefillDbContext NewContext() => database.CreateContext();

    public async Task<Arranged> ArrangeAsync(
        DateTimeOffset now,
        int daysSupply = 30,
        int remainingRefills = 2,
        TimeSpan? sinceLastFill = null,
        bool isActive = true,
        bool withPendingRequest = true,
        bool allergicToRequested = false,
        bool interactingActive = false,
        bool interactingInactive = false,
        TimeSpan? requestedAgo = null,
        string? patientName = null,
        string? medicationName = null,
        string? medicationNameAr = null)
    {
        await using var db = NewContext();

        var requestedName = medicationName ?? UniqueName("Req");
        var requested = new Medication(medicationNameAr ?? ArabicNameFor(requestedName), requestedName);
        var interactingName = UniqueName("Int");
        var interacting = new Medication(ArabicNameFor(interactingName), interactingName);
        db.Medications.AddRange(requested, interacting);
        await db.SaveChangesAsync();

        db.DrugInteractions.Add(new DrugInteraction(requested, interacting, ArabicInteractionDescription, "Synthetic test interaction."));

        var patient = new Patient(patientName ?? $"Test Patient {Guid.NewGuid():N}"[..24], new DateOnly(1980, 1, 1));
        db.Patients.Add(patient);
        if (allergicToRequested)
        {
            patient.AddAllergy(requested);
        }

        var lastFilledAt = now - (sinceLastFill ?? TimeSpan.FromDays(daysSupply));
        var prescription = new Prescription(patient, requested, "1 tablet by mouth once daily", daysSupply, remainingRefills, lastFilledAt, isActive);
        db.Prescriptions.Add(prescription);

        if (interactingActive || interactingInactive)
        {
            db.Prescriptions.Add(new Prescription(patient, interacting, "1 tablet daily", 30, 1, now.AddDays(-5), isActive: interactingActive));
        }

        RefillRequest? request = null;
        if (withPendingRequest)
        {
            request = RefillRequest.Create(prescription, now - (requestedAgo ?? TimeSpan.FromHours(1)));
            db.RefillRequests.Add(request);
        }

        await db.SaveChangesAsync();
        return new Arranged(patient.Id, requested.Id, prescription.Id, request?.Id, requested.NameEn, requested.NameAr);
    }

    /// <summary>Adds a new Pending request for an existing prescription (Domain factory).</summary>
    public async Task<int> AddPendingRequestAsync(int prescriptionId, DateTimeOffset requestedAt)
    {
        await using var db = NewContext();
        var prescription = await db.Prescriptions.SingleAsync(p => p.Id == prescriptionId);
        var request = RefillRequest.Create(prescription, requestedAt);
        db.RefillRequests.Add(request);
        await db.SaveChangesAsync();
        return request.Id;
    }

    public async Task<Prescription> GetPrescriptionAsync(int id)
    {
        await using var db = NewContext();
        return await db.Prescriptions.AsNoTracking().SingleAsync(p => p.Id == id);
    }

    public async Task<RefillRequest> GetRequestAsync(int id)
    {
        await using var db = NewContext();
        return await db.RefillRequests.AsNoTracking().SingleAsync(r => r.Id == id);
    }

    public async Task<byte[]> GetRowVersionAsync<TEntity>(int id)
        where TEntity : class
    {
        await using var db = NewContext();
        var entity = await db.Set<TEntity>().FindAsync(id) ?? throw new InvalidOperationException("Row not found.");
        return (byte[])db.Entry(entity).Property(RefillDbContext.RowVersionProperty).CurrentValue!;
    }

    public async Task ExecuteSqlAsync(FormattableString sql)
    {
        await using var db = NewContext();
        await db.Database.ExecuteSqlInterpolatedAsync(sql);
    }

    private static string UniqueName(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..24];

    public const string ArabicInteractionDescription = "تداخل اختباري مُصطنع.";

    /// <summary>
    /// A distinct Arabic name for an English test name: Arabic prefix plus the unique English
    /// suffix, so names stay unique in both languages and no Arabic name equals an English one.
    /// </summary>
    public static string ArabicNameFor(string englishName) => $"دواء {englishName}";

    /// <summary>A unique token in Arabic letters only, for names that must share no text with English.</summary>
    public static string ArabicToken() =>
        string.Concat(Guid.NewGuid().ToString("N")[..10].Select(c => "ابتثجحخدذرسشصضطظ"[Convert.ToInt32(c.ToString(), 16)]));
}
