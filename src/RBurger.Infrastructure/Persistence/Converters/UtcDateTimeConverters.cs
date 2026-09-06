using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace RBurger.Infrastructure.Persistence.Converters;

// Ensures all DateTime values are persisted and rehydrated as UTC, consistent with §7.0's
// "Dates are ISO-8601 UTC" rule. Approved Day 2 decision #7 - not a literal EF Core
// requirement stated in the documentation, but applied by explicit approval.
internal static class UtcDateTimeConverters
{
    public static readonly ValueConverter<DateTime, DateTime> NonNullable = new(
        toProvider => toProvider.Kind == DateTimeKind.Utc ? toProvider : toProvider.ToUniversalTime(),
        fromProvider => DateTime.SpecifyKind(fromProvider, DateTimeKind.Utc));

    public static readonly ValueConverter<DateTime?, DateTime?> Nullable = new(
        toProvider => toProvider.HasValue
            ? (toProvider.Value.Kind == DateTimeKind.Utc ? toProvider.Value : toProvider.Value.ToUniversalTime())
            : toProvider,
        fromProvider => fromProvider.HasValue
            ? DateTime.SpecifyKind(fromProvider.Value, DateTimeKind.Utc)
            : fromProvider);
}
