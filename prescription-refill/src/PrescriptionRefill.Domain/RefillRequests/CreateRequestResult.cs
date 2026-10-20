namespace PrescriptionRefill.Domain.RefillRequests;

/// <summary>Outcome of creating a refill request (FR-001, FR-021).</summary>
public abstract record CreateRequestResult
{
    private CreateRequestResult()
    {
    }

    public sealed record Created(RefillRequest Request) : CreateRequestResult;

    /// <summary>The prescription is inactive; nothing was created.</summary>
    public sealed record PrescriptionInactive(string Message) : CreateRequestResult;
}
