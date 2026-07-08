namespace FruitSales.Core.Pricing;

public class PricingStrategyFactory
{
    public IPricingStrategy CreatePerWeight(decimal pricePerKg) =>
        new PerWeightPricingStrategy(pricePerKg);

    public IPricingStrategy CreatePerItem(decimal pricePerItem) =>
        new PerItemPricingStrategy(pricePerItem);

    public IDiscountSpec CreateThresholdDiscount(decimal threshold, decimal discountRate) =>
        new ThresholdDiscountSpec(threshold, discountRate);

    public IDiscountSpec CreateSeasonalDiscount(DateOnly startDate, DateOnly endDate, decimal discountRate, IDateTimeProvider clock) =>
        new SeasonalDiscountSpec(startDate, endDate, discountRate, clock);
}