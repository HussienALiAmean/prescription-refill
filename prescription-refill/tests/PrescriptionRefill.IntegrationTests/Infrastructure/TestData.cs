using Microsoft.EntityFrameworkCore;
using PrescriptionRefill.Domain.Medications;
using PrescriptionRefill.Domain.Patients;
using PrescriptionRefill.Domain.Prescriptions;
using PrescriptionRefill.Domain.RefillRequests;
using PrescriptionRefill.Infrastructure.Persistence;

namespace PrescriptionRefill.IntegrationTests.Infrastructure;

/// <summary>IDs of one arranged patient, prescription, and (optionally) refill request.</summary>
public sealed record Arranged(int PatientId, int MedicationId, int PrescriptionId, int? RequestId, string MedicationName);

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
        string? medicationName = null)
    {
        await using var db = NewContext();

        var requested = new Medication(medicationName ?? UniqueName("Req"));
        var interacting = new Medication(UniqueName("Int"));
        db.Medications.AddRange(requested, interacting);
        await db.SaveChangesAsync();

        db.DrugInteractions.Add(new DrugInteraction(requested, interacting, "Synthetic test interaction."));

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
        return new Arranged(patient.Id, requested.Id, prescription.Id, request?.Id, requested.Name);
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
}
