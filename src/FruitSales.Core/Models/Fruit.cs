using FruitSales.Core.Pricing;

namespace FruitSales.Core.Models;

/// <summary>
/// A fruit always has exactly one base pricing strategy (per-kg or
/// per-item), which can be swapped at any time via SetBaseStrategy, plus
/// zero or more active discounts that can be added or removed
/// independently. Strategy is rebuilt from BaseStrategy + active discounts
/// on every access, so mutating either is reflected immediately.
/// </summary>
public class Fruit
{
    private readonly List<IDiscountSpec> _discounts = new();

    public string Name { get; }
    public IPricingStrategy BaseStrategy { get; private set; }
    public IReadOnlyList<IDiscountSpec> Discounts => _discounts;

    public IPricingStrategy Strategy =>
        _discounts.Aggregate(BaseStrategy, (strategy, spec) => spec.Apply(strategy));

    public decimal BasePrice => BaseStrategy.BasePrice;

    public Fruit(string name, IPricingStrategy baseStrategy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Fruit name must not be empty.", nameof(name));

        Name = name;
        BaseStrategy = baseStrategy ?? throw new ArgumentNullException(nameof(baseStrategy));
    }

    /// <summary>
    /// Swaps the base pricing strategy (e.g. per-kg to per-item). Active
    /// discounts are untouched - they're reapplied on top of whatever the
    /// base strategy is the next time Strategy is read.
    /// </summary>
    public void SetBaseStrategy(IPricingStrategy newBaseStrategy)
    {
        BaseStrategy = newBaseStrategy ?? throw new ArgumentNullException(nameof(newBaseStrategy));
    }

    public void AddDiscount(IDiscountSpec discount)
    {
        if (discount is null)
            throw new ArgumentNullException(nameof(discount));

        _discounts.Add(discount);
    }

    /// <summary>
    /// Removes a specific discount spec. Returns false if it isn't
    /// currently active, rather than throwing, so the console layer can
    /// report a clean message instead of crashing.
    /// </summary>
    public bool RemoveDiscount(IDiscountSpec discount) => _discounts.Remove(discount);
}