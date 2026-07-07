namespace FruitSales.Core.Pricing;

/// <summary>
/// Encapsulates a single pricing rule for a fruit. Implementations decide
/// how a quantity or weight is converted into a price - this is the
/// Strategy pattern that lets new pricing behaviours be added without
/// changing any existing calculation code.
/// </summary>
public interface IPricingStrategy
{
        
    /// <returns>The base price for that quantity/weight.</returns>
    decimal BasePrice { get; }

    /// <returns>The name of the pricing strategy.</returns>
    string StrategyName { get; }
    /// <param name="quantityOrWeight">
    /// Number of items, or weight in kilograms, depending on the strategy.
    /// </param>
    /// <returns>The calculated price for that quantity/weight.</returns>
    decimal CalculatePrice(decimal quantityOrWeight);
    PricingUnit Unit { get; }
}
