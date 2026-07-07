namespace FruitSales.Core.Pricing;

/// <summary>
/// Decorates any IPricingStrategy with a percentage discount that applies
/// only while today's date falls within a given date range
/// (e.g. "10% off apples during December").
///
/// Like ThresholdDiscountDecorator, this wraps another IPricingStrategy
/// rather than being baked into a dedicated strategy class, so seasonal
/// discounts compose with quantity-based discounts (or with each other)
/// without any changes to existing pricing code.
/// </summary>
public class SeasonalDiscountDecorator : IPricingStrategy
{
    private readonly IPricingStrategy _inner;
    private readonly DateOnly _startDate;
    private readonly DateOnly _endDate;
    private readonly decimal _discountRate;
    private readonly IDateTimeProvider _clock;
    public decimal BasePrice => _inner.BasePrice;

    public string StrategyName =>
    $"{_inner.StrategyName} + Seasonal Discount ({_discountRate:P0}, {_startDate:yyyy-MM-dd} to {_endDate:yyyy-MM-dd})";
    public PricingUnit Unit => _inner.Unit;
    public SeasonalDiscountDecorator(
        IPricingStrategy inner,
        DateOnly startDate,
        DateOnly endDate,
        decimal discountRate,
        IDateTimeProvider clock)
    {
        if (discountRate < 0 || discountRate > 1)
            throw new ArgumentOutOfRangeException(nameof(discountRate), "Discount rate must be between 0 and 1.");

        if (endDate < startDate)
            throw new ArgumentOutOfRangeException(nameof(endDate), "End date must not be before start date.");

        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _startDate = startDate;
        _endDate = endDate;
        _discountRate = discountRate;
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public decimal CalculatePrice(decimal quantityOrWeight)
    {
        var basePrice = _inner.CalculatePrice(quantityOrWeight);
        var today = _clock.Today;

        return today >= _startDate && today <= _endDate
            ? basePrice * (1 - _discountRate)
            : basePrice;
    }
}