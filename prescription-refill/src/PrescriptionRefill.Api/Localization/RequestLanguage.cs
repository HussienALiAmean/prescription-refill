using System.Globalization;
using Microsoft.AspNetCore.Localization;
using PrescriptionRefill.Application.Localization;

namespace PrescriptionRefill.Api.Localization;

/// <summary>
/// Request culture resolution (spec 005 FR-010, research R4). Only <c>Accept-Language</c> is read;
/// <c>ar</c>, <c>en</c>, and their regional variants resolve to that language, and anything else
/// (missing, empty, malformed, unsupported) resolves to Arabic. Responses carry
/// <c>Content-Language</c>.
/// </summary>
public static class RequestLanguage
{
    private static readonly CultureInfo[] Supported = [new("ar"), new("en")];

    public static RequestLocalizationOptions Options()
    {
        var options = new RequestLocalizationOptions
        {
            DefaultRequestCulture = new RequestCulture("ar"),
            SupportedCultures = Supported,
            SupportedUICultures = Supported,
            FallBackToParentCultures = true,
            FallBackToParentUICultures = true,
            ApplyCurrentCultureToResponseHeaders = true,
        };

        // One deterministic input: no query-string or cookie provider.
        options.RequestCultureProviders = [new AcceptLanguageHeaderRequestCultureProvider()];
        return options;
    }

    /// <summary>The language the localization middleware resolved for this request.</summary>
    public static Language From(HttpContext context) =>
        context.Features.Get<IRequestCultureFeature>()?.RequestCulture.UICulture.TwoLetterISOLanguageName == "en"
            ? Language.English
            : Language.Arabic;

    public static IAppText Text(HttpContext context) => AppText.For(From(context));
}
