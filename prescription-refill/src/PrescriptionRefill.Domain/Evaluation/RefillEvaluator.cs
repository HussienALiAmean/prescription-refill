using PrescriptionRefill.Domain.Prescriptions;

namespace PrescriptionRefill.Domain.Evaluation;

/// <summary>
/// Deterministic evaluation of every refill rule at one instant (FR-005–FR-009, FR-027). The same
/// method serves displayed evaluations and decision processing (FR-006).
/// </summary>
public static class RefillEvaluator
{
    /// <summary>80% of one 24-hour supply day in ticks (19.2 h); exact, no floating point.</summary>
    public const long ThresholdTicksPerSupplyDay = TimeSpan.TicksPerDay * 4 / 5;

    public static RefillEvaluation Evaluate(Prescription requested, EvaluationFacts facts, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(requested);
        ArgumentNullException.ThrowIfNull(facts);

        var evaluatedAt = now.ToUniversalTime();
        var blocking = new List<BlockingReason>();
        DateTimeOffset? eligibleAt = null;

        var invalidReason = InvalidTimingData(requested, evaluatedAt);
        if (invalidReason is not null)
        {
            blocking.Add(new BlockingReason(BlockingReasonCodes.InvalidPrescriptionData, invalidReason));
        }
        else
        {
            var threshold = requested.DaysSupply * ThresholdTicksPerSupplyDay;
            var lastFilledAt = requested.LastFilledAt.ToUniversalTime();
            eligibleAt = lastFilledAt.AddTicks(threshold);

            // Strictly less than the threshold blocks; exact equality does not (FR-006).
            if ((evaluatedAt - lastFilledAt).Ticks < threshold)
            {
                blocking.Add(new BlockingReason(
                    BlockingReasonCodes.TooEarly,
                    $"Less than 80% of the {requested.DaysSupply}-day supply has elapsed since the last fill.",
                    eligibleAt));
            }
        }

        if (requested.RemainingRefills <= 0)
        {
            blocking.Add(new BlockingReason(BlockingReasonCodes.NoRefillsRemaining, "No refills remain on this prescription."));
        }

        return new RefillEvaluation(evaluatedAt, eligibleAt, blocking, Warnings(requested, facts));
    }

    private static string? InvalidTimingData(Prescription requested, DateTimeOffset evaluatedAt)
    {
        if (requested.DaysSupply <= 0)
        {
            return "The prescription's days supply is not a positive number.";
        }

        if (requested.LastFilledAt == default)
        {
            return "The prescription has no last filled time.";
        }

        if (requested.LastFilledAt > evaluatedAt)
        {
            return "The prescription's last filled time is later than the evaluation time.";
        }

        return null;
    }

    private static List<Warning> Warnings(Prescription requested, EvaluationFacts facts)
    {
        var warnings = new List<Warning>();
        var medication = requested.Medication;

        if (facts.Allergies.Any(a => a.Medication.IsSameAs(medication)))
        {
            warnings.Add(new Warning(
                WarningCodes.AllergyConflict,
                $"The patient has a recorded allergy to {medication.Name}.",
                medication.Name));
        }

        var seen = new List<Medications.Medication>();
        foreach (var active in ActiveMedications.For(requested, facts.PatientPrescriptions))
        {
            var other = active.Medication;
            if (other.IsSameAs(medication) || seen.Any(m => m.IsSameAs(other)))
            {
                continue;
            }

            var interaction = facts.Interactions.FirstOrDefault(i => i.Joins(medication, other));
            if (interaction is not null)
            {
                seen.Add(other);
                warnings.Add(new Warning(
                    WarningCodes.DrugInteraction,
                    $"{medication.Name} has a synthetic demo interaction with active medication {other.Name}.",
                    other.Name,
                    interaction.Description));
            }
        }

        return warnings;
    }
}
