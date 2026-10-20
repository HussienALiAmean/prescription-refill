using PrescriptionRefill.Domain.Evaluation;
using PrescriptionRefill.Domain.Medications;
using PrescriptionRefill.Domain.Patients;
using PrescriptionRefill.Domain.Prescriptions;
using static PrescriptionRefill.Domain.Tests.TestSupport.Builders;
using PrescriptionEntity = PrescriptionRefill.Domain.Prescriptions.Prescription;

namespace PrescriptionRefill.Domain.Tests.Evaluation;

public sealed class RefillEvaluatorTests
{
    // 80% of one 24-hour day, computed independently of the implementation: 0.8 × 86,400 s.
    private static TimeSpan Threshold(int daysSupply) => TimeSpan.FromSeconds(daysSupply * 69_120L);

    private static EvaluationFacts NoFacts(Prescription requested) => new([], [requested], []);

    private static RefillEvaluation Evaluate(Prescription requested, EvaluationFacts? facts = null, DateTimeOffset? now = null) =>
        RefillEvaluator.Evaluate(requested, facts ?? NoFacts(requested), now ?? Now);

    // ---- FR-006: exact 80% threshold -------------------------------------------------------

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(90)]
    public void One_tick_before_the_threshold_is_too_early(int daysSupply)
    {
        var rx = Prescription(daysSupply: daysSupply, lastFilledAt: Now - Threshold(daysSupply) + TimeSpan.FromTicks(1));

        var result = Evaluate(rx);

        var reason = Assert.Single(result.BlockingReasons);
        Assert.Equal(BlockingReasonCodes.TooEarly, reason.Code);
        Assert.Equal(Now.AddTicks(1), reason.EligibleAt);
        Assert.Equal(Now.AddTicks(1), result.EligibleAt);
        Assert.False(result.CanApprove);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(90)]
    public void Exactly_at_the_threshold_is_not_blocked(int daysSupply)
    {
        var rx = Prescription(daysSupply: daysSupply, lastFilledAt: Now - Threshold(daysSupply));

        var result = Evaluate(rx);

        Assert.Empty(result.BlockingReasons);
        Assert.True(result.CanApprove);
        Assert.Equal(Now, result.EligibleAt);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(90)]
    public void One_tick_after_the_threshold_is_not_blocked(int daysSupply)
    {
        var rx = Prescription(daysSupply: daysSupply, lastFilledAt: Now - Threshold(daysSupply) - TimeSpan.FromTicks(1));

        Assert.True(Evaluate(rx).CanApprove);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(180, -300)]
    [InlineData(-300, 330)]
    public void Offsets_do_not_change_the_result(int nowOffsetMinutes, int fillOffsetMinutes)
    {
        var lastFilledUtc = Now - Threshold(30) + TimeSpan.FromTicks(1);
        var rx = Prescription(lastFilledAt: lastFilledUtc.ToOffset(TimeSpan.FromMinutes(fillOffsetMinutes)));
        var now = Now.ToOffset(TimeSpan.FromMinutes(nowOffsetMinutes));

        var result = Evaluate(rx, now: now);

        Assert.Equal(BlockingReasonCodes.TooEarly, Assert.Single(result.BlockingReasons).Code);
        Assert.Equal(TimeSpan.Zero, result.EvaluatedAt.Offset);
        Assert.Equal(Now.UtcTicks, result.EvaluatedAt.UtcTicks);
        Assert.Equal(TimeSpan.Zero, result.EligibleAt!.Value.Offset);
        Assert.Equal(Now.AddTicks(1).UtcTicks, result.EligibleAt.Value.UtcTicks);
    }

    // ---- FR-027: invalid stored timing data fails safely -----------------------------------

    [Fact]
    public void Last_filled_after_now_is_invalid_data_not_too_early()
    {
        var rx = Prescription(lastFilledAt: Now.AddTicks(1));

        var result = Evaluate(rx);

        Assert.Equal(BlockingReasonCodes.InvalidPrescriptionData, Assert.Single(result.BlockingReasons).Code);
        Assert.Null(result.EligibleAt);
        Assert.False(result.CanApprove);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Non_positive_days_supply_is_invalid_data(int daysSupply)
    {
        var rx = Prescription().WithStoredValue(nameof(PrescriptionEntity.DaysSupply), daysSupply);

        var result = Evaluate(rx);

        Assert.Equal(BlockingReasonCodes.InvalidPrescriptionData, Assert.Single(result.BlockingReasons).Code);
        Assert.Null(result.EligibleAt);
        Assert.False(result.CanApprove);
    }

    [Fact]
    public void Missing_last_filled_time_is_invalid_data()
    {
        var rx = Prescription().WithStoredValue(nameof(PrescriptionEntity.LastFilledAt), default(DateTimeOffset));

        var result = Evaluate(rx);

        Assert.Equal(BlockingReasonCodes.InvalidPrescriptionData, Assert.Single(result.BlockingReasons).Code);
        Assert.False(result.CanApprove);
    }

    // ---- FR-007: refills, and reason order ------------------------------------------------

    [Fact]
    public void Zero_refills_blocks_approval()
    {
        var result = Evaluate(Prescription(remainingRefills: 0));

        Assert.Equal(BlockingReasonCodes.NoRefillsRemaining, Assert.Single(result.BlockingReasons).Code);
        Assert.Null(result.BlockingReasons[0].EligibleAt);
    }

    [Fact]
    public void Timing_reason_comes_before_the_refill_reason()
    {
        var tooEarly = Prescription(remainingRefills: 0, lastFilledAt: Now.AddDays(-1));
        var invalid = Prescription(remainingRefills: 0, lastFilledAt: Now.AddDays(1));

        Assert.Equal(
            [BlockingReasonCodes.TooEarly, BlockingReasonCodes.NoRefillsRemaining],
            Evaluate(tooEarly).BlockingReasons.Select(r => r.Code));
        Assert.Equal(
            [BlockingReasonCodes.InvalidPrescriptionData, BlockingReasonCodes.NoRefillsRemaining],
            Evaluate(invalid).BlockingReasons.Select(r => r.Code));
    }

    [Fact]
    public void Evaluation_time_is_now_in_utc()
    {
        Assert.Equal(Now, Evaluate(Prescription()).EvaluatedAt);
    }

    // ---- FR-008, FR-022: warnings ----------------------------------------------------------

    [Fact]
    public void Allergy_to_the_requested_medication_warns_but_does_not_block()
    {
        var patient = Patient();
        var medication = Medication("Elvadine");
        var allergy = patient.AddAllergy(medication);
        var rx = Prescription(patient, medication);

        var result = Evaluate(rx, new EvaluationFacts([allergy], [rx], []));

        var warning = Assert.Single(result.Warnings);
        Assert.Equal(WarningCodes.AllergyConflict, warning.Code);
        Assert.Equal("Elvadine", warning.MedicationName);
        Assert.Null(warning.Description);
        Assert.True(result.CanApprove);
    }

    [Fact]
    public void Allergy_to_another_medication_does_not_warn()
    {
        var patient = Patient();
        var allergy = patient.AddAllergy(Medication("Other"));
        var rx = Prescription(patient, Medication("Requested"));

        Assert.Empty(Evaluate(rx, new EvaluationFacts([allergy], [rx], [])).Warnings);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Interaction_with_another_active_medication_warns_in_either_pair_order(bool requestedFirst)
    {
        var patient = Patient();
        var requested = Medication("Fenquira");
        var other = Medication("Glimora");
        var rx = Prescription(patient, requested);
        var otherRx = Prescription(patient, other);
        var interaction = requestedFirst
            ? new DrugInteraction(requested, other, "Synthetic demo interaction.")
            : new DrugInteraction(other, requested, "Synthetic demo interaction.");

        var result = Evaluate(rx, new EvaluationFacts([], [rx, otherRx], [interaction]));

        var warning = Assert.Single(result.Warnings);
        Assert.Equal(WarningCodes.DrugInteraction, warning.Code);
        Assert.Equal("Glimora", warning.MedicationName);
        Assert.Equal("Synthetic demo interaction.", warning.Description);
        Assert.True(result.CanApprove);
    }

    [Fact]
    public void Interacting_medication_only_on_an_inactive_prescription_does_not_warn()
    {
        var patient = Patient();
        var requested = Medication("Avelorin");
        var other = Medication("Corvantil");
        var rx = Prescription(patient, requested);
        var inactive = Prescription(patient, other, isActive: false);

        var result = Evaluate(rx, new EvaluationFacts([], [rx, inactive], [new DrugInteraction(requested, other, "x")]));

        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Same_medication_on_another_active_prescription_is_not_an_interaction()
    {
        var patient = Patient();
        var requested = Medication("Halvexin");
        var rx = Prescription(patient, requested);
        var duplicate = Prescription(patient, requested);
        var unrelated = new DrugInteraction(requested, Medication("Jurvane"), "x");

        Assert.Empty(Evaluate(rx, new EvaluationFacts([], [rx, duplicate], [unrelated])).Warnings);
    }

    [Fact]
    public void Warnings_never_block_even_together()
    {
        var patient = Patient();
        var requested = Medication("Requested");
        var other = Medication("Other");
        var allergy = patient.AddAllergy(requested);
        var rx = Prescription(patient, requested);
        var otherRx = Prescription(patient, other);

        var result = Evaluate(rx, new EvaluationFacts([allergy], [rx, otherRx], [new DrugInteraction(requested, other, "x")]));

        Assert.Equal(2, result.Warnings.Count);
        Assert.True(result.CanApprove);
    }

    [Fact]
    public void Blockers_and_warnings_are_all_reported()
    {
        var patient = Patient();
        var requested = Medication("Halvexin");
        var other = Medication("Jurvane");
        var rx = Prescription(patient, requested, remainingRefills: 0, lastFilledAt: Now.AddDays(-5));
        var otherRx = Prescription(patient, other);

        var result = Evaluate(rx, new EvaluationFacts([], [rx, otherRx], [new DrugInteraction(requested, other, "x")]));

        Assert.Equal(2, result.BlockingReasons.Count);
        Assert.Single(result.Warnings);
        Assert.False(result.CanApprove);
    }

    // ---- FR-022: one definition of active medications -------------------------------------

    [Fact]
    public void Active_medications_are_the_patients_other_active_prescriptions()
    {
        var patient = Patient();
        var rx = Prescription(patient, Medication("Requested"));
        var active = Prescription(patient, Medication("Active"));
        var inactive = Prescription(patient, Medication("Inactive"), isActive: false);
        var otherPatient = Prescription(Patient(), Medication("Elsewhere"));

        var result = ActiveMedications.For(rx, [rx, active, inactive, otherPatient]);

        Assert.Equal([active], result);
    }
}
