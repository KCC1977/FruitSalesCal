namespace FruitSales.Core.Pricing;

/// <summary>
/// Distinguishes whether a pricing strategy charges by weight (kg) or by
/// item count. Kept separate from StrategyName so calling code can branch
/// on it directly, rather than parsing a human-readable description string.
/// </summary>
public enum PricingUnit
{
    Weight,
    Item
}