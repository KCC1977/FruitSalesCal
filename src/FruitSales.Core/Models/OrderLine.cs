namespace FruitSales.Core.Models;

/// <summary>
/// A single line in an order: a fruit and the quantity or weight being
/// purchased. Whether the value represents item count or kilograms is
/// implied by the fruit's own pricing strategy.
/// </summary>
public record OrderLine(Fruit Fruit, decimal QuantityOrWeight);
