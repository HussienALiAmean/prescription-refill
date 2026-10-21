namespace PrescriptionRefill.Infrastructure.Seeding;

/// <summary>
/// Authored, synthetic bilingual seed names (spec 005 FR-019, FR-024; data-model "Seed names").
/// Used only by the seeder and its tests. The <c>BilingualReferenceData</c> migration holds its
/// own literal copy so it never changes once committed; a later wording fix updates this list and,
/// for already migrated databases, needs a new data migration or a documented reseed.
/// </summary>
public static class SeedNames
{
    /// <summary>English name → Arabic name, in seeding order.</summary>
    public static readonly IReadOnlyList<(string En, string Ar)> Medications =
    [
        ("Avelorin", "أفيلورين"),
        ("Brimexa", "بريميكسا"),
        ("Corvantil", "كورفانتيل"),
        ("Dexolune", "ديكسولون"),
        ("Elvadine", "إلفادين"),
        ("Fenquira", "فينكيرا"),
        ("Glimora", "غليمورا"),
        ("Halvexin", "هالفيكسين"),
        ("Jurvane", "جورفان"),
        ("Kestrolin", "كيسترولين"),
        ("Lumavex", "لومافيكس"),
    ];

    public const string InteractionDescriptionEn = "Synthetic demo interaction (fictional, not clinical guidance).";

    public const string InteractionDescriptionAr = "تداخل تجريبي مُصطنع (خيالي، وليس إرشادًا سريريًا).";
}
