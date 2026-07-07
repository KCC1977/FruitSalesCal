namespace FruitSales.Core.Pricing;

/// <summary>
/// Abstracts "what is today's date" so that time-dependent pricing logic
/// (like seasonal discounts) can be tested deterministically, instead of
/// depending on DateTime.Now/Today directly.
/// </summary>
public interface IDateTimeProvider
{
    DateOnly Today { get; }
}