using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using PrescriptionRefill.Application.Abstractions;
using PrescriptionRefill.Application.Localization;

namespace PrescriptionRefill.Application.Mapping;

/// <summary>
/// The display read (research R3): every medication name and interaction description in a
/// response comes from here, selected in SQL for one language. Rule reads load entities as before
/// and never supply response text. Only text comes from this read; every safety value comes from
/// the rule read (research R3 merge rule).
/// </summary>
public sealed class LocalizedReferenceData(IRefillDbContext db, LocalizedProjections projections)
{
    public IQueryable<LocalizedNameDto> MedicationNames(IReadOnlyCollection<int> medicationIds, Language language) =>
        db.Medications.AsNoTracking()
            .Where(m => medicationIds.Contains(m.Id))
            .ProjectTo<LocalizedNameDto>(projections.For(language));

    public IQueryable<LocalizedTextDto> InteractionDescriptions(IReadOnlyCollection<int> interactionIds, Language language) =>
        db.DrugInteractions.AsNoTracking()
            .Where(i => interactionIds.Contains(i.Id))
            .ProjectTo<LocalizedTextDto>(projections.For(language));

    /// <summary>Names and descriptions for the given ids, in one language.</summary>
    public async Task<ReferenceText> ReadAsync(
        IEnumerable<int> medicationIds,
        IEnumerable<int> interactionIds,
        Language language,
        CancellationToken cancellationToken)
    {
        var medications = medicationIds.Distinct().ToList();
        var interactions = interactionIds.Distinct().ToList();

        var names = medications.Count == 0
            ? []
            : await MedicationNames(medications, language).ToDictionaryAsync(n => n.Id, n => n.Name, cancellationToken);
        var descriptions = interactions.Count == 0
            ? []
            : await InteractionDescriptions(interactions, language).ToDictionaryAsync(d => d.Id, d => d.Description, cancellationToken);

        return new ReferenceText(names, descriptions);
    }
}

/// <summary>Localized names and descriptions by id, from one display read.</summary>
public sealed record ReferenceText(
    IReadOnlyDictionary<int, string> MedicationNames,
    IReadOnlyDictionary<int, string> InteractionDescriptions)
{
    public string Name(int medicationId) => MedicationNames[medicationId];

    public string Description(int interactionId) => InteractionDescriptions[interactionId];
}
