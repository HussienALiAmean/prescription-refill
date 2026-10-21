using AutoMapper;
using PrescriptionRefill.Application.Localization;
using PrescriptionRefill.Domain.Medications;

namespace PrescriptionRefill.Application.Mapping;

/// <summary>
/// Projections for one language. Each map names only that language's column, so the SQL that
/// <c>ProjectTo</c> generates selects <c>NameAr</c> or <c>NameEn</c>, never both (spec 005 FR-022,
/// DG-1 condition 2, research R2). A conditional inside one map would put a CASE over both columns
/// into the query instead.
/// </summary>
public sealed class RefillRequestProfile : Profile
{
    public RefillRequestProfile(Language language)
    {
        var names = CreateMap<Medication, LocalizedNameDto>();
        var descriptions = CreateMap<DrugInteraction, LocalizedTextDto>();

        if (language == Language.Arabic)
        {
            names.ForMember(d => d.Name, o => o.MapFrom(m => m.NameAr));
            descriptions.ForMember(d => d.Description, o => o.MapFrom(i => i.DescriptionAr));
        }
        else
        {
            names.ForMember(d => d.Name, o => o.MapFrom(m => m.NameEn));
            descriptions.ForMember(d => d.Description, o => o.MapFrom(i => i.DescriptionEn));
        }
    }
}
