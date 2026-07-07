namespace FruitSales.Core.Pricing;

/// <summary>
/// Prices a fruit at a fixed amount per item (e.g. Banana: $0.30 per item).
/// </summary>
public class PerItemPricingStrategy : IPricingStrategy
{
    private readonly decimal _pricePerItem;
    public decimal BasePrice => _pricePerItem;
    public string StrategyName => "Per Item";
    public PricingUnit Unit => PricingUnit.Item;
    public PerItemPricingStrategy(decimal pricePerItem)
    {
        if (pricePerItem < 0)
            throw new ArgumentOutOfRangeException(nameof(pricePerItem), "Price per item cannot be negative.");

        _pricePerItem = pricePerItem;
    }

    public decimal CalculatePrice(decimal quantityOrWeight) => _pricePerItem * quantityOrWeight;
}
