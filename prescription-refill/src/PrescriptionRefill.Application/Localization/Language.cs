namespace PrescriptionRefill.Application.Localization;

/// <summary>
/// The one supported language a request resolved to (spec 005 FR-010). The API resolves it from
/// the request culture and passes it explicitly; Application never reads ambient culture, so the
/// same input always produces the same output (research R4).
/// </summary>
public enum Language
{
    Arabic,
    English,
}
