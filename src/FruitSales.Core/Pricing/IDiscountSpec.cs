namespace FruitSales.Core.Pricing;

/// <summary>
/// Describes a discount that can be applied on top of a pricing strategy.
/// Unlike a plain IPricingStrategy decorator (fixed once constructed), a
/// spec is a lightweight, removable handle: Fruit keeps a list of active
/// specs and rebuilds the decorated strategy from its base strategy every
/// time it's needed, so any spec can be added or removed independently
/// without unwrapping a fixed nested chain.
/// </summary>
public interface IDiscountSpec
{
    string Description { get; }
    IPricingStrategy Apply(IPricingStrategy inner);
}