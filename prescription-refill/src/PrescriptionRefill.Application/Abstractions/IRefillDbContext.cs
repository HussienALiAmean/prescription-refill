using Microsoft.EntityFrameworkCore;
using PrescriptionRefill.Domain.Medications;
using PrescriptionRefill.Domain.Patients;
using PrescriptionRefill.Domain.Prescriptions;
using PrescriptionRefill.Domain.RefillRequests;

namespace PrescriptionRefill.Application.Abstractions;

/// <summary>
/// The Application layer's view of the database. One <see cref="SaveChangesAsync"/> call is one
/// transaction (research R5). No generic repository is layered on top (constitution Principle I).
/// </summary>
public interface IRefillDbContext
{
    DbSet<Patient> Patients { get; }

    DbSet<Allergy> Allergies { get; }

    DbSet<Medication> Medications { get; }

    DbSet<DrugInteraction> DrugInteractions { get; }

    DbSet<Prescription> Prescriptions { get; }

    DbSet<RefillRequest> RefillRequests { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
