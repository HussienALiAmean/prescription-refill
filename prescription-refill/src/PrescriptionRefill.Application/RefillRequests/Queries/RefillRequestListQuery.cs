using PrescriptionRefill.Domain.RefillRequests;

namespace PrescriptionRefill.Application.RefillRequests.Queries;

/// <summary>
/// One page of a list (003 spec FR-001, FR-007). <paramref name="Search"/> is already trimmed and
/// null when empty; it can contain patient names and is never logged (FR-009).
/// </summary>
public sealed record PageRequest(int Page, int PageSize, string? Search);

/// <summary>
/// Input to <see cref="GetRefillRequests"/>. A null <paramref name="Paging"/> means unpaged: the
/// complete list exactly as in 001 (003 spec FR-002, research R1).
/// </summary>
public sealed record RefillRequestListQuery(RefillRequestStatus Status, PageRequest? Paging)
{
    /// <summary>Page size used when only a page number or search is given (FR-002).</summary>
    public const int DefaultPageSize = 10;

    /// <summary>Largest allowed page size; bounds response size (FR-003).</summary>
    public const int MaxPageSize = 100;

    /// <summary>Longest allowed search text after trimming (FR-008).</summary>
    public const int MaxSearchLength = 100;
}
