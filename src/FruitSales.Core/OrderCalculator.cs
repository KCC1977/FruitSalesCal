using FruitSales.Core.Models;

namespace FruitSales.Core;

/// <summary>
/// Calculates the total cost of an order by delegating to each fruit's own
/// pricing strategy. The calculator has no knowledge of how any individual
/// fruit is priced - that is entirely encapsulated in IPricingStrategy.
/// </summary>
public class OrderCalculator
{
    public decimal CalculateTotal(IEnumerable<OrderLine> lines)
    {
        if (lines is null)
            throw new ArgumentNullException(nameof(lines));

        return lines.Sum(line => line.Fruit.Strategy.CalculatePrice(line.QuantityOrWeight));
    }

    /// <summary>
    /// Returns a per-line breakdown (fruit name, quantity/weight, line total),
    /// useful for printing an itemised receipt.
    /// </summary>
    public IReadOnlyList<(string FruitName, decimal BasePrice, string StrategyName, decimal QuantityOrWeight, decimal LineTotal)> Breakdown(
    IEnumerable<OrderLine> lines)
{
    if (lines is null)
        throw new ArgumentNullException(nameof(lines));

    return lines
        .Select(line => (
            line.Fruit.Name,
            line.Fruit.Strategy.BasePrice,
            line.Fruit.Strategy.StrategyName,
            line.QuantityOrWeight,
            line.Fruit.Strategy.CalculatePrice(line.QuantityOrWeight)))
        .ToList();
}
}
