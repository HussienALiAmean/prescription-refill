namespace PrescriptionRefill.Application.Common;

/// <summary>
/// The SQL Server filtered unique index rejected a second Pending request for one prescription
/// (FR-003). Raised after the transaction was rolled back; nothing was created.
/// </summary>
public sealed class PendingRequestExistsException(Exception innerException)
    : Exception("The prescription already has a Pending refill request.", innerException);
