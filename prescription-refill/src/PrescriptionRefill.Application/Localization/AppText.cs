namespace PrescriptionRefill.Application.Localization;

/// <summary>Selects the message catalog for a resolved language. Both catalogs are stateless.</summary>
public static class AppText
{
    private static readonly IAppText Arabic = new ArabicText();
    private static readonly IAppText English = new EnglishText();

    public static IAppText For(Language language) => language switch
    {
        Language.Arabic => Arabic,
        Language.English => English,
        _ => throw new ArgumentOutOfRangeException(nameof(language), language, null),
    };
}
