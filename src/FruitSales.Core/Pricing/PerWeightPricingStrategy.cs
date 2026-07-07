namespace FruitSales.Core.Pricing;

/// <summary>
/// Prices a fruit at a fixed amount per kilogram (e.g. Apple: $2.00 per kg).
/// </summary>
public class PerWeightPricingStrategy : IPricingStrategy
{
    private readonly decimal _pricePerKg;
    public decimal BasePrice => _pricePerKg;
    public string StrategyName => "Per Kg";
    public PricingUnit Unit => PricingUnit.Weight;
    public PerWeightPricingStrategy(decimal pricePerKg)
    {
        if (pricePerKg < 0)
            throw new ArgumentOutOfRangeException(nameof(pricePerKg), "Price per kg cannot be negative.");

        _pricePerKg = pricePerKg;
    }

    public decimal CalculatePrice(decimal quantityOrWeight) => _pricePerKg * quantityOrWeight;
}
