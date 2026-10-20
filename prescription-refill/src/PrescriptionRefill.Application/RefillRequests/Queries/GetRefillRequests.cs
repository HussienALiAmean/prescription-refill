using Microsoft.EntityFrameworkCore;
using PrescriptionRefill.Application.Abstractions;
using PrescriptionRefill.Application.RefillRequests.Dtos;
using PrescriptionRefill.Domain.Evaluation;
using PrescriptionRefill.Domain.RefillRequests;

namespace PrescriptionRefill.Application.RefillRequests.Queries;

/// <summary>
/// Requests of one status (FR-018): Pending oldest submission first; Approved and Denied most
/// recently decided first. Pending items are evaluated at one instant, returned as the list's
/// <c>evaluatedAt</c>; decided items carry no evaluation flags. Unpaged unless the query asks for
/// a page (003 research R1).
/// </summary>
public sealed class GetRefillRequests(IRefillDbContext db, TimeProvider clock)
{
    public Task<RefillRequestListDto> ExecuteAsync(RefillRequestListQuery query, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().ToUniversalTime();

        return query.Paging is null
            ? ExecuteUnpagedAsync(query.Status, now, cancellationToken)
            : ExecutePagedAsync(query.Status, query.Paging, now, cancellationToken);
    }

    private async Task<RefillRequestListDto> ExecuteUnpagedAsync(RefillRequestStatus status, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (status == RefillRequestStatus.Pending)
        {
            var evaluated = await EvaluatePendingAsync(now, cancellationToken);
            return new RefillRequestListDto(now, evaluated.Select(e => Summary(e.Request, e.Evaluation)).ToList());
        }

        var requests = await Ordered(WithDetails().Where(r => r.Status == status), status).ToListAsync(cancellationToken);
        return new RefillRequestListDto(now, requests.Select(r => Summary(r, evaluation: null)).ToList());
    }

    /// <summary>
    /// One page (003 research R3). Pending: every Pending request is evaluated once at
    /// <paramref name="now"/> (the summary needs them all), and SQL selects the page's ids so
    /// order is defined in one place. The two reads can disagree briefly if a decision commits in
    /// between; the live update that follows the commit makes clients reload. Approved/Denied:
    /// counts and one page only.
    /// </summary>
    private async Task<RefillRequestListDto> ExecutePagedAsync(RefillRequestStatus status, PageRequest paging, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var ofStatus = db.RefillRequests.Where(r => r.Status == status);
        var matching = Matching(ofStatus, paging.Search);
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
            var items = pageIds.Where(byId.ContainsKey).Select(id => Summary(byId[id].Request, byId[id].Evaluation)).ToList();
            return new RefillRequestListDto(now, items, paging.Page, paging.PageSize, matchingCount, evaluated.Count, SummaryOf(evaluated));
        }

        var statusTotal = await ofStatus.CountAsync(cancellationToken);
        var requests = beyondEnd
            ? new List<RefillRequest>()
            : await Ordered(Matching(WithDetails().Where(r => r.Status == status), paging.Search), status)
                .Skip((int)skip).Take(paging.PageSize).ToListAsync(cancellationToken);
        return new RefillRequestListDto(
            now, requests.Select(r => Summary(r, evaluation: null)).ToList(), paging.Page, paging.PageSize, matchingCount, statusTotal);
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
    /// Requests whose patient or medication name contains <paramref name="term"/> (003 FR-007).
    /// Both sides are lower-cased, so the match ignores case whatever the database collation
    /// (research R4). The provider escapes LIKE wildcards in the parameter, so they match
    /// literally. The term is never logged (FR-009).
    /// </summary>
    private static IQueryable<RefillRequest> Matching(IQueryable<RefillRequest> source, string? term)
    {
        if (term is null)
        {
            return source;
        }

        var lowered = term.ToLowerInvariant();
        return source.Where(r =>
            r.Prescription.Patient.FullName.ToLower().Contains(lowered)
            || r.Prescription.Medication.Name.ToLower().Contains(lowered));
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

    private static RefillRequestSummaryDto Summary(RefillRequest request, RefillEvaluation? evaluation) => new(
        request.Id,
        request.Status,
        request.RequestedAt,
        request.DecidedAt,
        request.Prescription.Patient.FullName,
        request.Prescription.Medication.Name,
        evaluation?.CanApprove,
        evaluation is null ? null : evaluation.Warnings.Count > 0);
}
