using FruitSales.Core.Models;
using FruitSales.Core.Pricing;

namespace FruitSales.Core.Catalog;

/// <summary>
/// In-memory implementation of IFruitRepository, seeded with the default
/// fruit catalog via PricingStrategyFactory. This is the only class that
/// would need to change (or be replaced) if the catalog moved to a real
/// data store.
/// </summary>
public class InMemoryFruitRepository : IFruitRepository
{
    private readonly Dictionary<string, Fruit> _fruits;

    public InMemoryFruitRepository(PricingStrategyFactory? factory = null)
    {
        factory ??= new PricingStrategyFactory();

        var seedFruits = new[]
        {
            new Fruit("Apple", factory.CreatePerWeight(2.00m)),

            new Fruit("Banana", factory.CreatePerItem(0.30m)),

            new Fruit("Cherry", factory.WithThresholdDiscount(
                inner: factory.CreatePerWeight(5.00m),
                threshold: 2m,
                discountRate: 0.10m)),

            new Fruit("Strawberry", factory.WithSeasonalDiscount(
                inner: factory.CreatePerWeight(6.00m),
                startDate: new DateOnly(2026, 10, 1),
                endDate: new DateOnly(2027, 3, 31),
                discountRate: 0.20m,
                clock: new SystemDateTimeProvider())),
        };

        _fruits = seedFruits.ToDictionary(f => f.Name, StringComparer.OrdinalIgnoreCase);
    }

    public void Add(Fruit fruit)
    {
        if (fruit is null)
            throw new ArgumentNullException(nameof(fruit));

        // Upsert: adding a fruit with a name that already exists replaces it,
        // rather than throwing - keeps the console flow simple if someone
        // re-enters a fruit with corrected pricing.
        _fruits[fruit.Name] = fruit;
}

    public Fruit? GetByName(string name) =>
        _fruits.TryGetValue(name, out var fruit) ? fruit : null;

    public IReadOnlyCollection<Fruit> GetAll() => _fruits.Values.ToList();
}
