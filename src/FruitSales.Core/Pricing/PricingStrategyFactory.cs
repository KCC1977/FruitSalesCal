namespace FruitSales.Core.Pricing;

/// <summary>
/// Builds pricing strategies from their configuration. This factory knows
/// about strategy TYPES (per-weight, per-item, threshold discount, seasonal
/// discount) - it has no knowledge of which fruit uses which strategy.
/// That mapping lives entirely in the catalog seed data, so adding a new
/// fruit never requires a change here.
/// </summary>
public class PricingStrategyFactory
{
    public IPricingStrategy CreatePerWeight(decimal pricePerKg) =>
        new PerWeightPricingStrategy(pricePerKg);

    public IPricingStrategy CreatePerItem(decimal pricePerItem) =>
        new PerItemPricingStrategy(pricePerItem);

    public IPricingStrategy WithThresholdDiscount(IPricingStrategy inner, decimal threshold, decimal discountRate) =>
        new ThresholdDiscountDecorator(inner, threshold, discountRate);

    public IPricingStrategy WithSeasonalDiscount(
        IPricingStrategy inner, DateOnly startDate, DateOnly endDate, decimal discountRate, IDateTimeProvider clock) =>
        new SeasonalDiscountDecorator(inner, startDate, endDate, discountRate, clock);
}