using PrescriptionRefill.Domain.Evaluation;

namespace PrescriptionRefill.Domain.RefillRequests;

/// <summary>Outcome of an approve or deny decision (FR-010, FR-011, FR-015).</summary>
public abstract record DecisionResult
{
    private DecisionResult()
    {
    }

    /// <summary>The request moved to <paramref name="Status"/>.</summary>
    public sealed record Decided(RefillRequestStatus Status) : DecisionResult;

    /// <summary>The note is missing, blank, or too long; nothing changed.</summary>
    public sealed record InvalidNote(string Message) : DecisionResult;

    /// <summary>The request was already Approved or Denied; nothing changed.</summary>
    public sealed record AlreadyDecided(RefillRequestStatus Status) : DecisionResult;

    /// <summary>A blocking rule applies at decision time; nothing changed.</summary>
    public sealed record Blocked(RefillEvaluation Evaluation) : DecisionResult;
}
