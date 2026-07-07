using FruitSales.Core.Pricing;

namespace FruitSales.Core.Models;

/// <summary>
/// Represents a fruit product available for sale, along with the pricing
/// strategy used to calculate its cost. Fruit itself carries no pricing
/// logic - that responsibility belongs entirely to its IPricingStrategy.
/// </summary>
public class Fruit
{
    public string Name { get; }
    public IPricingStrategy Strategy { get; }
    public decimal BasePrice => Strategy.BasePrice;

    public Fruit(string name, IPricingStrategy strategy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Fruit name must not be empty.", nameof(name));

        Name = name;
        Strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
    }
}
