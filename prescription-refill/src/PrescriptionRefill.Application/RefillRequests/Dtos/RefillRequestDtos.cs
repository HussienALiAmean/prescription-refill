using System.Text.Json.Serialization;
using PrescriptionRefill.Domain.RefillRequests;

namespace PrescriptionRefill.Application.RefillRequests.Dtos;

// Response shapes from contracts/http-api.md. Property order is the wire order. No persistence
// fields (RowVersion, foreign keys) appear here (constitution Principle VI).

/// <summary>
/// The paging members (003 contracts/http-api.md) are null and not written for an unpaged list,
/// so its JSON stays <c>{ evaluatedAt, items }</c> (003 spec FR-002).
/// </summary>
public sealed record RefillRequestListDto(
    DateTimeOffset EvaluatedAt,
    IReadOnlyList<RefillRequestSummaryDto> Items,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? Page = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? PageSize = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? MatchingCount = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? StatusTotal = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ListSummaryDto? Summary = null);

/// <summary>Whole-status Pending counts from the list's evaluations (003 spec FR-010).</summary>
public sealed record ListSummaryDto(int Blocked, int Warnings, int Clear);

public sealed record RefillRequestSummaryDto(
    int Id,
    RefillRequestStatus Status,
    DateTimeOffset RequestedAt,
    DateTimeOffset? DecidedAt,
    string PatientName,
    string MedicationName,
    bool? CanApprove,
    bool? HasWarnings);

public sealed record RefillRequestDetailDto(
    int Id,
    RefillRequestStatus Status,
    DateTimeOffset RequestedAt,
    DateTimeOffset? DecidedAt,
    string? DecisionNote,
    PatientDto Patient,
    PrescriptionDto Prescription,
    IReadOnlyList<ActivePrescriptionDto> ActivePrescriptions,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] EvaluationDto? Evaluation);

public sealed record PatientDto(int Id, string FullName, DateOnly DateOfBirth, IReadOnlyList<string> Allergies);

public sealed record PrescriptionDto(
    int Id,
    string MedicationName,
    string Dosage,
    int DaysSupply,
    int RemainingRefills,
    DateTimeOffset LastFilledAt,
    bool IsActive);

public sealed record ActivePrescriptionDto(
    int Id,
    string MedicationName,
    string Dosage,
    int RemainingRefills,
    DateTimeOffset LastFilledAt);

public sealed record EvaluationDto(
    DateTimeOffset EvaluatedAt,
    bool CanApprove,
    DateTimeOffset? EligibleAt,
    IReadOnlyList<BlockingReasonDto> BlockingReasons,
    IReadOnlyList<WarningDto> Warnings);

public sealed record BlockingReasonDto(string Code, string Message, DateTimeOffset? EligibleAt);

public sealed record WarningDto(string Code, string Message, string MedicationName, string? Description);
