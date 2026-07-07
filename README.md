# Fruit Sales Calculator

A small system for calculating the price of fruit orders, built for the
Software Engineer coding assessment. It supports weight- and item-based
pricing, optional stackable discounts (quantity-threshold and seasonal),
an in-memory but DB-ready catalog, and a menu-driven console app for
building and reviewing an order interactively.

## Running it

```bash
dotnet restore
dotnet build
dotnet run --project src/FruitSales.Console
dotnet test
```

Everything is in-memory — no database or external service is required to
run or test this solution.

## Project layout

```
src/FruitSales.Core/       Domain logic: models, pricing strategies, catalog
src/FruitSales.Console/    Menu-driven console app demonstrating the library
tests/FruitSales.Tests/    XUnit tests (Arrange / Act / Assert)
```

## Design decisions

### Strategy pattern — pricing rules

The core requirement is "different fruit can be priced in different ways,
and new pricing approaches should be easy to add." `IPricingStrategy`
captures that as a single contract:

```csharp
public interface IPricingStrategy
{
    decimal BasePrice { get; }
    string StrategyName { get; }
    PricingUnit Unit { get; }
    decimal CalculatePrice(decimal quantityOrWeight);
}
```

- `BasePrice` — the underlying rate, always present regardless of any
  discounts layered on top (see Decorator, below).
- `StrategyName` — a human-readable description, used by the console app
  so a user can see exactly how a fruit is priced (e.g.
  "Per Kg + Seasonal Discount (20%, 2026-10-01 to 2027-03-31)").
- `Unit` — a `PricingUnit` enum (`Weight` or `Item`), so calling code can
  tell what kind of quantity to ask for without parsing `StrategyName`.
- `CalculatePrice` — the actual calculation.

Two concrete base strategies implement it:
- `PerItemPricingStrategy` — Banana, $0.30/item
- `PerWeightPricingStrategy` — Apple, $2.00/kg

`Fruit` holds a *reference* to whichever strategy applies to it, and
exposes `BasePrice` itself by delegating to the strategy:

```csharp
public class Fruit
{
    public IPricingStrategy Strategy { get; }
    public decimal BasePrice => Strategy.BasePrice;
}
```

`OrderCalculator` never knows which concrete strategy it's dealing with —
it calls `.CalculatePrice()` polymorphically. Adding a genuinely new
pricing behaviour later means adding one class that implements
`IPricingStrategy`; nothing else needs to change.

### Decorator pattern — optional, stackable discounts

Cherry needed "$5.00/kg, with 10% off orders over 2kg" — a discount
layered conditionally on top of ordinary per-kg pricing. Rather than a
one-off `CherryPricingStrategy` with the discount hard-coded inside,
discounts are decorators: each one wraps another `IPricingStrategy` and
modifies its result.

```csharp
public class ThresholdDiscountDecorator : IPricingStrategy
{
    public decimal BasePrice => _inner.BasePrice;
    public PricingUnit Unit => _inner.Unit;
    public string StrategyName =>
        $"{_inner.StrategyName} + Threshold Discount ({_discountRate:P0} over {_threshold})";

    public decimal CalculatePrice(decimal quantityOrWeight)
    {
        var basePrice = _inner.CalculatePrice(quantityOrWeight);
        return quantityOrWeight > _threshold
            ? basePrice * (1 - _discountRate)
            : basePrice;
    }
}
```

`SeasonalDiscountDecorator` follows the same shape, but keys off a date
range instead of a quantity threshold (see below for how "today" is
determined).

Two things fall out of this design that are worth calling out directly:

- A base rate is always required; discounts are always optional layers
  on top of it. You can never have a discount with nothing underneath —
  the type system enforces that every decorator wraps an
  `IPricingStrategy`, and the innermost one is always a plain
  `PerWeightPricingStrategy` or `PerItemPricingStrategy`.
- Discounts stack. Because each decorator implements the same interface
  it wraps, a `SeasonalDiscountDecorator` can wrap a
  `ThresholdDiscountDecorator` (or vice versa) with no new code. For a
  fruit priced at $5.00/kg with both a 10% threshold discount and a 20%
  seasonal discount active, 3kg works out to
  $15.00, then 10% off to $13.50, then 20% off to $10.80. `BasePrice`,
  `Unit`, and the full `StrategyName` narrative all still correctly
  report through the whole chain, because every decorator delegates
  those properties down to `_inner`.

One caveat worth knowing if asked: stacking order doesn't currently
affect the final price, because percentage discounts are multiplicative
and multiplication is commutative. It would matter if a future discount
were a flat amount rather than a percentage — a known limitation of the
current model rather than something addressed yet.

A design trade-off left deliberately unresolved: `BasePrice`, `Unit`, and
part of `StrategyName` are duplicated (as identical one-line delegations
to `_inner`) across both decorators. An abstract base decorator class
could remove that duplication. With only two decorators, the duplication
is small and easy to keep correct by inspection, so I chose not to add
that abstraction yet — I'd introduce it as soon as a third decorator
appeared, or if a delegation were ever missed in review.

### Seasonal discounts and testability — IDateTimeProvider

A seasonal discount needs to know "what is today's date," but calling
`DateTime.Now` directly inside `SeasonalDiscountDecorator` would make it
untestable — a test can't deterministically assert "is Dec 15 inside the
window" if the answer depends on when the test happens to run.

`IDateTimeProvider` abstracts that away:

```csharp
public interface IDateTimeProvider
{
    DateOnly Today { get; }
}
```

`SystemDateTimeProvider` is the production implementation, backed by the
real clock. Tests instead use a small fake (`FakeDateTimeProvider`) with a
settable `Today`, letting every boundary (before / on start date / during
/ on end date / after) be asserted deterministically.

### Factory pattern — building strategies, not fruit

The factory went through a real design iteration worth describing
directly, because it's a stronger interview answer than a clean history
would be: the first version had one method per fruit
(`CreateApplePricing()`, `CreateCherryPricing()`, and so on), which meant
the factory would grow by one method for every new fruit added — an
unbounded, ever-growing class. That's a smell, not just a style
preference.

The fix was to make the factory build strategy types, with no knowledge
of which specific fruit uses which strategy:

```csharp
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
```

This is a Simple Factory (a class with methods that build objects) — not
Factory Method or Abstract Factory. The fruit-to-strategy mapping now
lives entirely in the catalog's seed data (see below), which is the only
place that changes when a new fruit is added — the factory itself is
stable regardless of how many fruit exist.

### Repository — a seam for a real database, without needing one

`IFruitRepository` abstracts "where does the fruit catalog come from?":

```csharp
public interface IFruitRepository
{
    Fruit? GetByName(string name);
    IReadOnlyCollection<Fruit> GetAll();
    void Add(Fruit fruit);
}
```

`InMemoryFruitRepository` is the only implementation, seeded via
`PricingStrategyFactory`:

```csharp
var seedFruits = new[]
{
    new Fruit("Apple", factory.CreatePerWeight(2.00m)),
    new Fruit("Banana", factory.CreatePerItem(0.30m)),
    new Fruit("Cherry", factory.WithThresholdDiscount(
        factory.CreatePerWeight(5.00m), threshold: 2m, discountRate: 0.10m)),
    new Fruit("Strawberry", factory.WithSeasonalDiscount(
        factory.CreatePerWeight(6.00m),
        startDate: new DateOnly(2026, 10, 1),
        endDate: new DateOnly(2027, 3, 31),
        discountRate: 0.20m,
        clock: new SystemDateTimeProvider())),
};
```

The brief doesn't actually require persistence, so this seemed like the
right amount of "database-ready" without introducing infrastructure (or a
SQL instance) the exercise doesn't call for. Swapping to a real data store
means adding an `EfFruitRepository` or `DapperFruitRepository`
implementing the same interface — `OrderCalculator`, `Program.cs`, and
every test written against `IFruitRepository` need no changes at all.

`Add` upserts (replaces on a name collision) rather than throwing, which
keeps the console's "add a new fruit" flow simple — re-entering a fruit
with corrected pricing just replaces it rather than requiring a separate
update path.

### Order — combining, removing, and clearing lines

`Order` wraps a list of `OrderLine`s (a fruit plus a quantity or weight).
Three behaviours worth calling out:

- `AddLine` auto-combines. Adding the same fruit twice merges the
  quantity into the existing line (matched by name, case-insensitive)
  rather than creating a second line. This matches how a real order
  works — "2kg of apples" is one line on a receipt, however many times it
  was added to. It also means `RemoveLineByName` never has to pick which
  of several same-fruit lines to remove, since there's only ever one.
- `RemoveLineAt` / `RemoveLineByName` — removal by index or by
  case-insensitive name; both return false on no match rather than
  throwing, so the console layer can report a clean message instead of
  crashing on bad input.
- `Clear` — empties the order; used before loading a sample order, so
  loading it twice (or after manually adding fruit) doesn't produce a
  mixed result.

`OrderCalculator` still doesn't know or care how any individual fruit is
priced — it asks each line's fruit for its strategy and sums the results,
and separately offers a `Breakdown` for itemised output:

```csharp
public IReadOnlyList<(string FruitName, decimal BasePrice, string StrategyName, decimal QuantityOrWeight, decimal LineTotal)> Breakdown(
    IEnumerable<OrderLine> lines);
```

### Console app — menu-driven

`Program.cs` runs a loop rather than a single fixed demo, so the patterns
above can actually be exercised interactively:

1. View catalog — lists every fruit with its base price, unit (kg/item),
   and full strategy description.
2. Add a new fruit — prompts for name, base price, and pricing method
   (per kg / per item), then optionally layers on a threshold discount
   and/or a seasonal discount, in any combination.
3. Add a fruit to the current order — shows the fruit's pricing before
   asking for a quantity, so the unit (kg vs item) is unambiguous.
4. View order total — itemised breakdown plus grand total.
5. Load a sample order — clears the current order first, then loads a
   fixed order of Apple, Banana, Cherry, and Strawberry, demonstrating
   every pricing type and discount type in one action.
6. Remove a fruit from the current order — by name, case-insensitive.
7. Exit

## How I'd extend this further

Add a new fruit with existing pricing behaviour: either via the console's
"Add a new fruit" option, or by adding one line to
`InMemoryFruitRepository`'s seed list. No changes to the factory, the
strategies, or the decorators.

Add a new discount type (for example, loyalty pricing — a flat discount
for returning customers, independent of quantity or date): add a new
`IPricingStrategy` decorator implementing the same delegation pattern as
the existing two, and a corresponding `With...Discount` method on the
factory. Existing strategies, decorators, and tests are untouched.

Add a genuinely new pricing model (for example, "buy 3 get 1 free"): add
a new class implementing `IPricingStrategy` directly, or as a decorator
if it's naturally a modifier on top of an existing strategy, and a
corresponding factory method.

Move the catalog to a real database: implement `IFruitRepository` against
SQL Server or Oracle (EF Core or Dapper), and swap it in wherever
`InMemoryFruitRepository` is currently constructed. No changes needed to
`OrderCalculator`, `Program.cs`, or any test written against
`IFruitRepository`.

Remove the decorator duplication (`BasePrice`/`Unit`/part of
`StrategyName` delegation, repeated in both decorators): extract an
abstract base decorator class implementing the delegating properties
once. Deliberately not done yet — see the Decorator section above for why.

## Testing approach

XUnit, Arrange/Act/Assert, one behaviour per test:

- Each `IPricingStrategy` implementation is tested in isolation: correct
  calculation, edge cases (zero quantity), constructor validation, and
  its `BasePrice`/`Unit` values.
- `ThresholdDiscountDecoratorTests` and `SeasonalDiscountDecoratorTests`
  cover above/at/below a threshold and before/on/during/on/after a date
  range respectively, plus decorator-on-decorator stacking and
  constructor validation. `SeasonalDiscountDecoratorTests` uses
  `FakeDateTimeProvider` so every date boundary is deterministic.
- `PricingStrategyFactoryTests` locks in that the generic builder methods
  produce correctly configured strategies.
- `InMemoryFruitRepositoryTests` covers lookup (including case
  insensitivity), a missing fruit, `Add`, and the specific default
  catalog prices/discounts (Apple, Banana, Cherry) — deliberately not
  Strawberry's exact discounted price, since that fruit is seeded with
  the real system clock and asserting an exact price would make the test
  flaky depending on what day it's run.
- `OrderTests` covers auto-combining on repeated `AddLine`, case
  insensitivity, removal by index and by name, and `Clear`.
- `OrderCalculatorTests` covers summing multiple lines, an empty order,
  null input, discounted strategies honoured end-to-end, and the full
  `Breakdown` tuple shape.