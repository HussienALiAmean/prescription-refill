using Microsoft.EntityFrameworkCore;
using PrescriptionRefill.Domain.Evaluation;
using PrescriptionRefill.Domain.Medications;
using PrescriptionRefill.Domain.Patients;
using PrescriptionRefill.Domain.Prescriptions;
using PrescriptionRefill.Domain.RefillRequests;
using PrescriptionRefill.Infrastructure.Persistence;

namespace PrescriptionRefill.Infrastructure.Seeding;

/// <summary>
/// Seeds fictional demo data (data-model.md "Seed Scenarios"). All medication names and
/// interaction pairs are synthetic, not clinical guidance (FR-019). Times are relative to the
/// injected clock so the 80% scenarios stay meaningful whenever the seed runs.
/// </summary>
public sealed class DemoDataSeeder(RefillDbContext db, TimeProvider clock)
{
    public const string SyntheticInteractionDescription =
        "Synthetic demo interaction (fictional, not clinical guidance).";

    /// <returns><c>true</c> when data was added; <c>false</c> when the database already had data.</returns>
    public async Task<bool> SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await db.Patients.AnyAsync(cancellationToken))
        {
            return false;
        }

        var utcNow = clock.GetUtcNow();
        var now = new DateTimeOffset(utcNow.UtcTicks - (utcNow.UtcTicks % TimeSpan.TicksPerMinute), TimeSpan.Zero);
        DateTimeOffset DaysAgo(double days) => now.AddDays(-days);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var medications = new[]
        {
            "Avelorin", "Brimexa", "Corvantil", "Dexolune", "Elvadine", "Fenquira",
            "Glimora", "Halvexin", "Jurvane", "Kestrolin", "Lumavex",
        }.ToDictionary(name => name, name => new Medication(name));
        db.Medications.AddRange(medications.Values);
        await db.SaveChangesAsync(cancellationToken);

        // Pairs need persisted keys so they are stored in ascending order.
        DrugInteraction[] interactions =
        [
            new(medications["Avelorin"], medications["Corvantil"], SyntheticInteractionDescription),
            new(medications["Fenquira"], medications["Glimora"], SyntheticInteractionDescription),
            new(medications["Halvexin"], medications["Jurvane"], SyntheticInteractionDescription),
            new(medications["Brimexa"], medications["Kestrolin"], SyntheticInteractionDescription),
        ];
        db.DrugInteractions.AddRange(interactions);

        var avery = new Patient("Avery Lindqvist", new DateOnly(1984, 3, 12));
        var jordan = new Patient("Jordan Okafor", new DateOnly(1991, 7, 23));
        var morgan = new Patient("Morgan Reyes", new DateOnly(1958, 1, 30));
        var riley = new Patient("Riley Chen", new DateOnly(2001, 9, 5));
        var sam = new Patient("Sam Patel", new DateOnly(1979, 11, 2));
        var taylor = new Patient("Taylor Nguyen", new DateOnly(1966, 4, 17));
        db.Patients.AddRange(avery, jordan, morgan, riley, sam, taylor);

        riley.AddAllergy(medications["Elvadine"]);

        Prescription Rx(Patient patient, string medication, string dosage, int daysSupply, int refills, double filledDaysAgo, bool active = true)
        {
            var prescription = new Prescription(patient, medications[medication], dosage, daysSupply, refills, DaysAgo(filledDaysAgo), active);
            db.Prescriptions.Add(prescription);
            return prescription;
        }

        // Scenario 1: approvable, no warnings; the interacting Corvantil prescription is inactive.
        // Starts with 4 refills; the decided scenario 7 below uses one, leaving 3.
        var averyAvelorin = Rx(avery, "Avelorin", "10 mg by mouth once daily", 30, 4, 26);
        var averyCorvantil = Rx(avery, "Corvantil", "25 mg by mouth once daily", 30, 0, 60, active: false);

        // Scenario 2: too early (eligible 24 days after the fill). Inactive Corvantil for FR-021 API checks.
        var jordanBrimexa = Rx(jordan, "Brimexa", "20 mg by mouth at bedtime", 30, 2, 10);
        Rx(jordan, "Corvantil", "25 mg by mouth once daily", 30, 1, 45, active: false);

        // Scenario 3: no refills remaining (timing is eligible).
        var morganDexolune = Rx(morgan, "Dexolune", "500 mg by mouth twice daily", 90, 0, 80);
        Rx(morgan, "Lumavex", "1 tablet by mouth once daily", 30, 2, 15);

        // Scenario 4: allergy conflict warning, approvable.
        var rileyElvadine = Rx(riley, "Elvadine", "250 mg by mouth three times daily", 10, 1, 9);

        // Scenario 5: drug interaction warning with active Glimora, approvable.
        var samFenquira = Rx(sam, "Fenquira", "10 mg by mouth once daily", 30, 5, 27);
        Rx(sam, "Glimora", "5 mg by mouth once daily", 30, 2, 19);

        // Scenario 6: too early and an interaction warning with active Jurvane.
        var taylorHalvexin = Rx(taylor, "Halvexin", "50 mg by mouth once daily", 30, 2, 5);
        Rx(taylor, "Jurvane", "100 mg by mouth once daily", 30, 1, 12);

        // Decided history (scenarios 7–8), made through the Domain decisions so they follow every
        // rule. Scenario 7 is approved exactly at the 80% point (filled 26 days ago, eligible after 24).
        var earlierApproval = RefillRequest.Create(averyAvelorin, now.AddHours(-50));
        Expect(earlierApproval.Approve(
            "Refill approved; patient stable on current dose.",
            new EvaluationFacts([], [averyAvelorin, averyCorvantil], interactions),
            now.AddHours(-48)));

        var earlierDenial = RefillRequest.Create(morganDexolune, now.AddHours(-60));
        Expect(earlierDenial.Deny("No refills remain; patient asked to book a review.", now.AddHours(-40)));

        // Pending requests, oldest first (scenarios 1–6), created after the decisions above so
        // each prescription has at most one Pending request (FR-003).
        db.RefillRequests.AddRange(earlierApproval, earlierDenial);
        db.RefillRequests.AddRange(
            RefillRequest.Create(averyAvelorin, now.AddHours(-46)),
            RefillRequest.Create(jordanBrimexa, now.AddHours(-44)),
            RefillRequest.Create(morganDexolune, now.AddHours(-36)),
            RefillRequest.Create(rileyElvadine, now.AddHours(-24)),
            RefillRequest.Create(samFenquira, now.AddHours(-18)),
            RefillRequest.Create(taylorHalvexin, now.AddHours(-6)));

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private static void Expect(DecisionResult result)
    {
        if (result is not DecisionResult.Decided)
        {
            throw new InvalidOperationException($"Seed decision was rejected: {result}.");
        }
    }
}
