using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace PrescriptionRefill.Infrastructure.Persistence;

/// <summary>
/// Normalizes every <see cref="DateTimeOffset"/> to offset +00:00 when saved and when read, so
/// stored instants are always UTC (data-model.md "Time Storage").
/// </summary>
public sealed class UtcDateTimeOffsetConverter() : ValueConverter<DateTimeOffset, DateTimeOffset>(
    value => value.ToUniversalTime(),
    value => value.ToUniversalTime());
