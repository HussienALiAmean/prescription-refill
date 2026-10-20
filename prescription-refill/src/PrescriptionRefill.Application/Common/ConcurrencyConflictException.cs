namespace PrescriptionRefill.Application.Common;

/// <summary>
/// A row changed after it was loaded (rowversion mismatch). Thrown only after the database
/// transaction has been rolled back, so nothing from the attempted change was saved (FR-013).
/// </summary>
public sealed class ConcurrencyConflictException(Exception innerException)
    : Exception("The data changed after it was loaded; no changes were saved.", innerException);
