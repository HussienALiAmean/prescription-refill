namespace PrescriptionRefill.Application.Mapping;

// Internal lookup shapes for localized reference text (spec 005 research R3). They are never
// serialized; response DTOs keep their shapes and receive these values (FR-021).

/// <summary>A medication's name in one language.</summary>
public sealed record LocalizedNameDto
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;
}

/// <summary>A drug interaction's description in one language.</summary>
public sealed record LocalizedTextDto
{
    public int Id { get; init; }

    public string Description { get; init; } = string.Empty;
}
