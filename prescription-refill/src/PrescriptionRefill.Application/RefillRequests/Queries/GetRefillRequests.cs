using Microsoft.EntityFrameworkCore;
using PrescriptionRefill.Application.Abstractions;
using PrescriptionRefill.Application.Localization;
using PrescriptionRefill.Application.Mapping;
using PrescriptionRefill.Application.RefillRequests.Dtos;
using PrescriptionRefill.Domain.Evaluation;
using PrescriptionRefill.Domain.RefillRequests;

namespace PrescriptionRefill.Application.RefillRequests.Queries;

/// <summary>
/// Requests of one status (FR-018): Pending oldest submission first; Approved and Denied most
/// recently decided first. Pending items are evaluated at one instant, returned as the list's
/// <c>evaluatedAt</c>; decided items carry no evaluation flags. Unpaged unless the query asks for
/// a page (003 research R1). Status, flags, and order come from the rule read; each item's
/// medication name is the <paramref name="language"/> name, selected in SQL (spec 005 research R3).
/// </summary>
public sealed class GetRefillRequests(IRefillDbContext db, LocalizedReferenceData referenceData, TimeProvider clock)
{
    public Task<RefillRequestListDto> ExecuteAsync(RefillRequestListQuery query, Language language, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().ToUniversalTime();

        return query.Paging is null
            ? ExecuteUnpagedAsync(query.Status, now, language, cancellationToken)
            : ExecutePagedAsync(query.Status, query.Paging, now, language, cancellationToken);
    }

    private async Task<RefillRequestListDto> ExecuteUnpagedAsync(RefillRequestStatus status, DateTimeOffset now, Language language, CancellationToken cancellationToken)
    {
        if (status == RefillRequestStatus.Pending)
        {
            var evaluated = await EvaluatePendingAsync(now, cancellationToken);
            var pendingItems = await SummariesAsync(evaluated.Select(e => (e.Request, (RefillEvaluation?)e.Evaluation)).ToList(), language, cancellationToken);
            return new RefillRequestListDto(now, pendingItems);
        }

        var requests = await Ordered(WithDetails().Where(r => r.Status == status), status).ToListAsync(cancellationToken);
        return new RefillRequestListDto(now, await SummariesAsync(Unevaluated(requests), language, cancellationToken));
    }

    /// <summary>
    /// One page (003 research R3). Pending: every Pending request is evaluated once at
    /// <paramref name="now"/> (the summary needs them all), and SQL selects the page's ids so
    /// order is defined in one place. The two reads can disagree briefly if a decision commits in
    /// between; the live update that follows the commit makes clients reload. Approved/Denied:
    /// counts and one page only.
    /// </summary>
    private async Task<RefillRequestListDto> ExecutePagedAsync(
        RefillRequestStatus status, PageRequest paging, DateTimeOffset now, Language language, CancellationToken cancellationToken)
    {
        var ofStatus = db.RefillRequests.Where(r => r.Status == status);
        var matching = Matching(ofStatus, paging.Search, language);
        var matchingCount = await matching.CountAsync(cancellationToken);
        var skip = (long)(paging.Page - 1) * paging.PageSize;
        var beyondEnd = skip >= matchingCount;

        if (status == RefillRequestStatus.Pending)
        {
            var evaluated = await EvaluatePendingAsync(now, cancellationToken);
            var pageIds = beyondEnd
                ? new List<int>()
                : await Ordered(matching, status).Skip((int)skip).Take(paging.PageSize).Select(r => r.Id).ToListAsync(cancellationToken);
            var byId = evaluated.ToDictionary(e => e.Request.Id);

            // A request created after the evaluation read is left out until the next reload.
            var items = await SummariesAsync(
                pageIds.Where(byId.ContainsKey).Select(id => (byId[id].Request, (RefillEvaluation?)byId[id].Evaluation)).ToList(),
                language,
                cancellationToken);
            return new RefillRequestListDto(now, items, paging.Page, paging.PageSize, matchingCount, evaluated.Count, SummaryOf(evaluated));
        }

        var statusTotal = await ofStatus.CountAsync(cancellationToken);
        var requests = beyondEnd
            ? new List<RefillRequest>()
            : await Ordered(Matching(WithDetails().Where(r => r.Status == status), paging.Search, language), status)
                .Skip((int)skip).Take(paging.PageSize).ToListAsync(cancellationToken);
        return new RefillRequestListDto(
            now, await SummariesAsync(Unevaluated(requests), language, cancellationToken), paging.Page, paging.PageSize, matchingCount, statusTotal);
    }

    /// <summary>
    /// Whole-status counts over every Pending evaluation, ignoring search (003 FR-010). Only counts
    /// the Domain's results; the same split as the items' <c>canApprove</c>/<c>hasWarnings</c> flags.
    /// </summary>
    private static ListSummaryDto SummaryOf(List<(RefillRequest Request, RefillEvaluation Evaluation)> evaluated) => new(
        evaluated.Count(e => !e.Evaluation.CanApprove),
        evaluated.Count(e => e.Evaluation.Warnings.Count > 0),
        evaluated.Count(e => e.Evaluation.CanApprove && e.Evaluation.Warnings.Count == 0));

    /// <summary>Every Pending request, in list order, evaluated at <paramref name="now"/>.</summary>
    private async Task<List<(RefillRequest Request, RefillEvaluation Evaluation)>> EvaluatePendingAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var requests = await Ordered(WithDetails().Where(r => r.Status == RefillRequestStatus.Pending), RefillRequestStatus.Pending)
            .ToListAsync(cancellationToken);

        var patientIds = requests.Select(r => r.Prescription.PatientId).Distinct().ToList();
        var prescriptionsByPatient = (await db.Prescriptions
                .AsNoTrackingWithIdentityResolution()
                .Include(p => p.Medication)
                .Where(p => patientIds.Contains(p.PatientId))
                .ToListAsync(cancellationToken))
            .ToLookup(p => p.PatientId);

        // The interaction table is small seed data; one query serves the whole list.
        var interactions = await db.DrugInteractions
            .AsNoTrackingWithIdentityResolution()
            .Include(i => i.MedicationA)
            .Include(i => i.MedicationB)
            .ToListAsync(cancellationToken);

        return requests.Select(request =>
        {
            var prescription = request.Prescription;
            var facts = new EvaluationFacts(
                prescription.Patient.Allergies.ToList(),
                prescriptionsByPatient[prescription.PatientId].ToList(),
                interactions);
            return (request, RefillEvaluator.Evaluate(prescription, facts, now));
        }).ToList();
    }

    /// <summary>
    /// Requests whose patient name, or medication name in <paramref name="language"/> only,
    /// contains <paramref name="term"/> (003 FR-007, amended by spec 005 FR-025). Both sides are
    /// lower-cased, so the match ignores case whatever the database collation (research R4); that
    /// is a no-op for Arabic script. The column is chosen outside the query, so the SQL names one
    /// language's column, never a CASE over both (005 research R8). The provider escapes LIKE
    /// wildcards in the parameter, so they match literally. The term is never logged (FR-009).
    /// </summary>
    internal static IQueryable<RefillRequest> Matching(IQueryable<RefillRequest> source, string? term, Language language)
    {
        if (term is null)
        {
            return source;
        }

        var lowered = term.ToLowerInvariant();
        return language == Language.Arabic
            ? source.Where(r =>
                r.Prescription.Patient.FullName.ToLower().Contains(lowered)
                || r.Prescription.Medication.NameAr.ToLower().Contains(lowered))
            : source.Where(r =>
                r.Prescription.Patient.FullName.ToLower().Contains(lowered)
                || r.Prescription.Medication.NameEn.ToLower().Contains(lowered));
    }

    private IQueryable<RefillRequest> WithDetails() => db.RefillRequests
        .AsNoTrackingWithIdentityResolution()
        .Include(r => r.Prescription).ThenInclude(p => p.Medication)
        .Include(r => r.Prescription).ThenInclude(p => p.Patient).ThenInclude(p => p.Allergies).ThenInclude(a => a.Medication);

    /// <summary>The list order for a status, with an id tie-break so pages never overlap (003 FR-006).</summary>
    private static IOrderedQueryable<RefillRequest> Ordered(IQueryable<RefillRequest> source, RefillRequestStatus status) =>
        status == RefillRequestStatus.Pending
            ? source.OrderBy(r => r.RequestedAt).ThenBy(r => r.Id)
            : source.OrderByDescending(r => r.DecidedAt).ThenByDescending(r => r.Id);

    private static List<(RefillRequest Request, RefillEvaluation? Evaluation)> Unevaluated(List<RefillRequest> requests) =>
        requests.Select(r => (r, (RefillEvaluation?)null)).ToList();

    /// <summary>Items in the given order, with one display read for their medication names.</summary>
    private async Task<List<RefillRequestSummaryDto>> SummariesAsync(
        IReadOnlyList<(RefillRequest Request, RefillEvaluation? Evaluation)> rows, Language language, CancellationToken cancellationToken)
    {
        var reference = await referenceData.ReadAsync(rows.Select(r => r.Request.Prescription.MedicationId), [], language, cancellationToken);
        return rows.Select(r => Summary(r.Request, r.Evaluation, reference)).ToList();
    }

    private static RefillRequestSummaryDto Summary(RefillRequest request, RefillEvaluation? evaluation, ReferenceText reference) => new(
        request.Id,
        request.Status,
        request.RequestedAt,
        request.DecidedAt,
        request.Prescription.Patient.FullName,
        reference.Name(request.Prescription.MedicationId),
        evaluation?.CanApprove,
        evaluation is null ? null : evaluation.Warnings.Count > 0);
}
