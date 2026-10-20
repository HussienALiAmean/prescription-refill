using PrescriptionRefill.Domain.Evaluation;
using PrescriptionRefill.Domain.Prescriptions;

namespace PrescriptionRefill.Domain.RefillRequests;

/// <summary>A refill request for one prescription (FR-001, FR-010, FR-015).</summary>
public sealed class RefillRequest
{
    /// <summary>Maximum decision note length after trimming (FR-010).</summary>
    public const int MaxNoteLength = 1000;

    private RefillRequest()
    {
        Prescription = null!;
    }

    private RefillRequest(Prescription prescription, DateTimeOffset requestedAt)
    {
        Prescription = prescription;
        PrescriptionId = prescription.Id;
        Status = RefillRequestStatus.Pending;
        RequestedAt = requestedAt.ToUniversalTime();
    }

    public int Id { get; private set; }

    public int PrescriptionId { get; private set; }

    public Prescription Prescription { get; private set; }

    public RefillRequestStatus Status { get; private set; }

    public DateTimeOffset RequestedAt { get; private set; }

    public DateTimeOffset? DecidedAt { get; private set; }

    public string? DecisionNote { get; private set; }

    /// <summary>
    /// The single creation path for refill requests (FR-001). Inactive prescriptions are rejected
    /// (FR-021); blocking rules do not prevent creation (FR-002). One Pending request per
    /// prescription is enforced by the database (FR-003).
    /// </summary>
    public static CreateRequestResult TryCreate(Prescription prescription, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(prescription);

        return prescription.IsActive
            ? new CreateRequestResult.Created(new RefillRequest(prescription, now))
            : new CreateRequestResult.PrescriptionInactive("Prescription is inactive.");
    }

    /// <summary><see cref="TryCreate"/> for callers that already know the prescription is active.</summary>
    public static RefillRequest Create(Prescription prescription, DateTimeOffset now) =>
        TryCreate(prescription, now) switch
        {
            CreateRequestResult.Created created => created.Request,
            CreateRequestResult.PrescriptionInactive inactive => throw new InvalidOperationException(inactive.Message),
            var other => throw new InvalidOperationException($"Unexpected result {other}."),
        };

    /// <summary>
    /// Approves a Pending request after re-evaluating every rule at <paramref name="now"/> against
    /// the supplied stored data (FR-011), consuming exactly one refill (FR-012). Checks run in the
    /// contract's order: note, status, rules.
    /// </summary>
    public DecisionResult Approve(string? note, EvaluationFacts facts, DateTimeOffset now)
    {
        if (ValidateNote(note) is { } noteError)
        {
            return noteError;
        }

        if (Status != RefillRequestStatus.Pending)
        {
            return new DecisionResult.AlreadyDecided(Status);
        }

        var evaluation = RefillEvaluator.Evaluate(Prescription, facts, now);
        if (!evaluation.CanApprove)
        {
            return new DecisionResult.Blocked(evaluation);
        }

        Prescription.ConsumeRefill();
        Decide(RefillRequestStatus.Approved, note!, now);
        return new DecisionResult.Decided(Status);
    }

    /// <summary>Denies a Pending request; allowed regardless of rules (FR-014).</summary>
    public DecisionResult Deny(string? note, DateTimeOffset now)
    {
        if (ValidateNote(note) is { } noteError)
        {
            return noteError;
        }

        if (Status != RefillRequestStatus.Pending)
        {
            return new DecisionResult.AlreadyDecided(Status);
        }

        Decide(RefillRequestStatus.Denied, note!, now);
        return new DecisionResult.Decided(Status);
    }

    private static DecisionResult.InvalidNote? ValidateNote(string? note)
    {
        var trimmed = note?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return new DecisionResult.InvalidNote("A decision note is required.");
        }

        return trimmed.Length > MaxNoteLength
            ? new DecisionResult.InvalidNote($"The decision note must be at most {MaxNoteLength} characters.")
            : null;
    }

    private void Decide(RefillRequestStatus status, string note, DateTimeOffset now)
    {
        Status = status;
        DecidedAt = now.ToUniversalTime();
        DecisionNote = note.Trim();
    }
}
