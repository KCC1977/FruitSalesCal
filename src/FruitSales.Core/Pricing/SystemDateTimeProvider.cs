namespace FruitSales.Core.Pricing;

/// <summary>
/// Production implementation of IDateTimeProvider, backed by the system clock.
/// </summary>
public class SystemDateTimeProvider : IDateTimeProvider
{
    public DateOnly Today => DateOnly.FromDateTime(DateTime.Now);
}