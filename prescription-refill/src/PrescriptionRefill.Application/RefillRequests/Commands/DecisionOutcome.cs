using PrescriptionRefill.Application.RefillRequests.Dtos;
using PrescriptionRefill.Domain.RefillRequests;

namespace PrescriptionRefill.Application.RefillRequests.Commands;

/// <summary>Application result of approve/deny, mapped once to HTTP by the API.</summary>
public abstract record DecisionOutcome
{
    private DecisionOutcome()
    {
    }

    public sealed record Succeeded(RefillRequestDetailDto Request) : DecisionOutcome;

    public sealed record NotFound : DecisionOutcome;

    public sealed record InvalidNote(string Message) : DecisionOutcome;

    public sealed record AlreadyDecided(RefillRequestStatus Status) : DecisionOutcome;

    public sealed record Blocked(EvaluationDto Evaluation) : DecisionOutcome;
}
