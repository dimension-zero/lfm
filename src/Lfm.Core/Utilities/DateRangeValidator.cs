using Lfm.Shared.Models.Results;

namespace Lfm.Core.Utilities;

/// <summary>
/// Helper for validating date range parameters
/// </summary>
public static class DateRangeValidator
{
    /// <summary>
    /// Validates a date range ensuring 'from' is before 'to'
    /// </summary>
    /// <param name="from">Start date (optional)</param>
    /// <param name="to">End date (optional)</param>
    /// <returns>Validation result with date range or error</returns>
    public static Result<DateRange> Validate(DateTime? from, DateTime? to)
    {
        // Both null = all time (valid)
        if (!from.HasValue && !to.HasValue)
            return Result<DateRange>.Ok(new DateRange(null, null));

        // Only 'from' specified (valid)
        if (from.HasValue && !to.HasValue)
            return Result<DateRange>.Ok(new DateRange(from, null));

        // Only 'to' specified (valid)
        if (!from.HasValue && to.HasValue)
            return Result<DateRange>.Ok(new DateRange(null, to));

        // Both specified - validate order
        if (from.Value > to.Value)
            return Result<DateRange>.ValidationError(
                "'from' date must be before 'to' date",
                $"from: {from.Value:yyyy-MM-dd}, to: {to.Value:yyyy-MM-dd}");

        return Result<DateRange>.Ok(new DateRange(from, to));
    }

    /// <summary>
    /// Validates that a date is not in the future
    /// </summary>
    public static Result<DateTime> ValidateNotFuture(DateTime date, string parameterName = "date")
    {
        if (date > DateTime.UtcNow)
            return Result<DateTime>.ValidationError(
                $"{parameterName} cannot be in the future",
                $"Provided: {date:yyyy-MM-dd HH:mm:ss}, Current: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");

        return Result<DateTime>.Ok(date);
    }
}

/// <summary>
/// Represents a date range for queries
/// </summary>
public record DateRange(DateTime? From, DateTime? To)
{
    public static DateRange AllTime => new(null, null);

    public bool IsAllTime => !From.HasValue && !To.HasValue;

    public bool HasStart => From.HasValue;

    public bool HasEnd => To.HasValue;

    public override string ToString()
    {
        if (IsAllTime)
            return "All time";

        if (From.HasValue && To.HasValue)
            return $"{From.Value:yyyy-MM-dd} to {To.Value:yyyy-MM-dd}";

        if (From.HasValue)
            return $"From {From.Value:yyyy-MM-dd}";

        return $"Until {To!.Value:yyyy-MM-dd}";
    }
}
