namespace FruitSales.Core.Pricing;

/// <summary>
/// Decorates any IPricingStrategy with a percentage discount that applies
/// once the quantity/weight strictly exceeds a given threshold
/// (e.g. Cherry: $5.00/kg, 10% off orders over 2kg).
///
/// Using the Decorator pattern here - rather than baking the discount
/// logic into a dedicated strategy class - means discounts compose.
/// A second, different discount can be layered on top of this one
/// (or on top of any other strategy) without writing a new strategy
/// class or touching any existing pricing code.
/// </summary>
public class ThresholdDiscountDecorator : IPricingStrategy
{
    private readonly IPricingStrategy _inner;
    private readonly decimal _threshold;
    private readonly decimal _discountRate;
    public decimal BasePrice => _inner.BasePrice;
    public string StrategyName =>
    $"{_inner.StrategyName} + Threshold Discount ({_discountRate:P0} over {_threshold})";
    public PricingUnit Unit => _inner.Unit;
    public ThresholdDiscountDecorator(IPricingStrategy inner, decimal threshold, decimal discountRate)
    {
        if (discountRate < 0 || discountRate > 1)
            throw new ArgumentOutOfRangeException(nameof(discountRate), "Discount rate must be between 0 and 1.");

        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _threshold = threshold;
        _discountRate = discountRate;
    }

    public decimal CalculatePrice(decimal quantityOrWeight)
    {
        var basePrice = _inner.CalculatePrice(quantityOrWeight);

        return quantityOrWeight > _threshold
            ? basePrice * (1 - _discountRate)
            : basePrice;
    }
}
