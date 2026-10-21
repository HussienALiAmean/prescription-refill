using AutoMapper;
using Microsoft.Extensions.Logging;
using PrescriptionRefill.Application.Localization;

namespace PrescriptionRefill.Application.Mapping;

/// <summary>
/// One AutoMapper configuration per language, built and validated once at startup (research R2).
/// The API passes the Community License key from configuration <see cref="LicenseKeySetting"/> (user
/// secrets or environment); it is never committed. Without it AutoMapper works and logs a warning under
/// <c>LuckyPennySoftware.AutoMapper.License</c> (research R1).
/// </summary>
public sealed class LocalizedProjections
{
    public const string LicenseKeySetting = "AutoMapper:LicenseKey";

    private readonly IConfigurationProvider arabic;
    private readonly IConfigurationProvider english;

    public LocalizedProjections(string? licenseKey, ILoggerFactory loggerFactory)
    {
        arabic = Build(Language.Arabic, licenseKey, loggerFactory);
        english = Build(Language.English, licenseKey, loggerFactory);
    }

    public IConfigurationProvider For(Language language) => language switch
    {
        Language.Arabic => arabic,
        Language.English => english,
        _ => throw new ArgumentOutOfRangeException(nameof(language), language, null),
    };

    private static MapperConfiguration Build(Language language, string? licenseKey, ILoggerFactory loggerFactory)
    {
        var configuration = new MapperConfiguration(
            cfg =>
            {
                if (!string.IsNullOrWhiteSpace(licenseKey))
                {
                    cfg.LicenseKey = licenseKey;
                }

                cfg.AddProfile(new RefillRequestProfile(language));
            },
            loggerFactory);
        configuration.AssertConfigurationIsValid();
        return configuration;
    }
}
