using FruitSales.Core.Pricing;

namespace FruitSales.Tests.TestSupport;

/// <summary>
/// Test double for IDateTimeProvider that lets tests control "today"
/// directly, instead of depending on the real system clock.
/// </summary>
public class FakeDateTimeProvider : IDateTimeProvider
{
    public DateOnly Today { get; set; }
}