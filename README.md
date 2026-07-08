# Fruit Sales Calculator

A small system for calculating the price of fruit orders, built for the
Software Engineer coding assessment. It supports weight- and item-based
pricing, discounts that can be added and removed independently at
runtime, an in-memory but DB-ready catalog, and a menu-driven console app
for managing the catalog and building an order interactively.

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

Every fruit has exactly one base pricing rule at any given time, captured
by `IPricingStrategy`:

```csharp
public interface IPricingStrategy
{
    decimal BasePrice { get; }
    string StrategyName { get; }
    PricingUnit Unit { get; }
    decimal CalculatePrice(decimal quantityOrWeight);
}
```

- `BasePrice` — the underlying rate.
- `StrategyName` — a human-readable description, shown by the console app.
- `Unit` — a `PricingUnit` enum (`Weight` or `Item`), so calling code can
  tell what kind of quantity to ask for without parsing `StrategyName`.
- `CalculatePrice` — the actual calculation.

Two concrete strategies implement it: `PerItemPricingStrategy` (Banana,
$0.30/item) and `PerWeightPricingStrategy` (Apple, $2.00/kg). Adding a
genuinely new pricing behaviour later means adding one class that
implements `IPricingStrategy`; nothing else needs to change.

### Fruit — a mandatory base strategy, swappable, plus independent discounts

A fruit's pricing has two separate, independently-managed parts:

```csharp
public class Fruit
{
    public IPricingStrategy BaseStrategy { get; private set; }
    public IReadOnlyList<IDiscountSpec> Discounts => _discounts;
    public IPricingStrategy Strategy =>
        _discounts.Aggregate(BaseStrategy, (strategy, spec) => spec.Apply(strategy));

    public void SetBaseStrategy(IPricingStrategy newBaseStrategy) { ... }
    public void AddDiscount(IDiscountSpec discount) { ... }
    public bool RemoveDiscount(IDiscountSpec discount) { ... }
}
```

Three invariants this enforces directly:

- **A fruit always has a base pricing strategy.** It's a required
  constructor argument, `BaseStrategy` can never be set to null (the
  constructor and `SetBaseStrategy` both guard against it), so there's
  never a fruit without pricing.
- **The base strategy can be changed independently of any discounts.**
  `SetBaseStrategy` swaps per-kg for per-item (or vice versa, or just a
  different rate) without touching `Discounts` at all — Cherry can go
  from $5.00/kg to $10.00/kg and its 10%-over-2kg discount is still
  applied on top of the new rate.
- **Discounts can be added and removed independently**, in any
  combination, without disturbing the base strategy or each other.
  `Strategy` is computed fresh each time it's read, by folding
  `BaseStrategy` through every currently-active discount spec — so
  removing one discount doesn't require unwrapping or rebuilding
  anything by hand.

This is a genuine design evolution from an earlier version, worth
describing directly if asked: originally `Fruit` held a single, fixed
`IPricingStrategy` — sometimes a raw base strategy, sometimes a base
strategy nested inside one or two decorators, built once at construction
time. That worked fine for a catalog assembled up front, but broke down
once fruit needed to have their pricing *edited* after creation: you
can't cleanly reach into a nested decorator chain and remove just the
middle layer, or swap out what's at the very centre of it, without
knowing exactly what's wrapped inside what. Separating "the one base
strategy" from "the list of active discounts" on `Fruit` itself resolves
that — each part is now independently addressable.

### Decorator pattern — how discounts are actually applied

`ThresholdDiscountDecorator` and `SeasonalDiscountDecorator` are
unchanged from their original design: each wraps another
`IPricingStrategy` and modifies its result.

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

`SeasonalDiscountDecorator` follows the same shape, keying off a date
range instead of a quantity threshold (see the `IDateTimeProvider`
section below for how "today" is determined).

What changed is *how these get constructed and attached*. Previously,
`Fruit` held one of these directly, nested at construction time -
permanent, and awkward to remove independently once created. Now, a
small `IDiscountSpec` sits in front of each decorator:

```csharp
public interface IDiscountSpec
{
    string Description { get; }
    IPricingStrategy Apply(IPricingStrategy inner);
}
```

`ThresholdDiscountSpec` and `SeasonalDiscountSpec` implement this by
holding the discount's configuration (threshold, rate, date range, clock)
and constructing the corresponding decorator on demand, wrapping whatever
strategy they're handed. `Fruit` keeps a plain list of active specs and
rebuilds the decorated chain by folding through them every time `Strategy`
is read - so a spec can be added to or removed from that list at any
point, and the effective pricing updates immediately, without ever
needing to unwrap an existing chain.

The decorators themselves stay genuinely "Decorator pattern" - real
composition, wrapping `IPricingStrategy` with `IPricingStrategy`. The
spec layer just makes *which* decorators are currently applied a mutable,
inspectable list rather than a fixed structure baked in at construction.

Discounts still stack in whatever combination and order they're added -
for a fruit at $5.00/kg with both a 10% threshold discount and a 20%
seasonal discount active, 3kg works out to $15.00, then 10% off to
$13.50, then 20% off to $10.80. `BasePrice`, `Unit`, and the compound
`StrategyName` on `Strategy` still correctly reflect the full chain,
because each decorator delegates those properties down to `_inner` -
though note `Fruit.BaseStrategy.StrategyName` (undiscounted) and
`Fruit.Strategy.StrategyName` (fully decorated) are now deliberately
different things, and the console app uses whichever is appropriate to
what it's displaying.

One caveat worth knowing if asked: stacking order doesn't currently
affect the final price, because percentage discounts are multiplicative
and multiplication is commutative. It would matter if a future discount
were a flat amount rather than a percentage - a known limitation of the
current model rather than something addressed yet.

A design trade-off left deliberately unresolved: `BasePrice`, `Unit`, and
part of `StrategyName` are still duplicated (as identical one-line
delegations to `_inner`) across both decorators. With only two decorator
types, that duplication is small and easy to keep correct by inspection,
so I chose not to extract a shared base class for it - I'd revisit that
as soon as a third decorator type appeared, or if a delegation were ever
missed in review.

### Seasonal discounts and testability — IDateTimeProvider

A seasonal discount needs to know "what is today's date," but calling
`DateTime.Now` directly inside `SeasonalDiscountDecorator` would make it
untestable - a test can't deterministically assert "is Dec 15 inside the
window" if the answer depends on when the test happens to run.

`IDateTimeProvider` abstracts that away:

```csharp
public interface IDateTimeProvider
{
    DateOnly Today { get; }
}
```

`SystemDateTimeProvider` is the production implementation, backed by the
real clock. Tests use a small fake (`FakeDateTimeProvider`) with a
settable `Today`, letting every boundary (before / on start date / during
/ on end date / after) be asserted deterministically.

### Factory pattern — building strategies and discount specs, not fruit

`PricingStrategyFactory` builds standalone pieces - base strategies and
discount specs - with no knowledge of which fruit they'll end up attached
to:

```csharp
public class PricingStrategyFactory
{
    public IPricingStrategy CreatePerWeight(decimal pricePerKg) =>
        new PerWeightPricingStrategy(pricePerKg);

    public IPricingStrategy CreatePerItem(decimal pricePerItem) =>
        new PerItemPricingStrategy(pricePerItem);

    public IDiscountSpec CreateThresholdDiscount(decimal threshold, decimal discountRate) =>
        new ThresholdDiscountSpec(threshold, discountRate);

    public IDiscountSpec CreateSeasonalDiscount(DateOnly startDate, DateOnly endDate, decimal discountRate, IDateTimeProvider clock) =>
        new SeasonalDiscountSpec(startDate, endDate, discountRate, clock);
}
```

This is a Simple Factory (a class with methods that build objects) - not
Factory Method or Abstract Factory. Note the discount methods no longer
take an `inner: IPricingStrategy` parameter the way they originally did -
a spec is built independent of any strategy, and only wraps one later,
when `Fruit.Strategy` folds it in. This is itself a small further step in
the same direction as the `Fruit` refactor: decoupling "how a discount is
configured" from "what it's currently applied to."

The factory remains stable regardless of how many fruit or discount
instances exist - fruit-to-strategy and fruit-to-discount assignments
live entirely in the catalog seed data and in whatever's called on a
specific `Fruit` at runtime, never in the factory itself.

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
var cherry = new Fruit("Cherry", factory.CreatePerWeight(5.00m));
cherry.AddDiscount(factory.CreateThresholdDiscount(threshold: 2m, discountRate: 0.10m));

var strawberry = new Fruit("Strawberry", factory.CreatePerWeight(6.00m));
strawberry.AddDiscount(factory.CreateSeasonalDiscount(
    startDate: new DateOnly(2026, 10, 1),
    endDate: new DateOnly(2027, 3, 31),
    discountRate: 0.20m,
    clock: new SystemDateTimeProvider()));
```

The brief doesn't actually require persistence, so this seemed like the
right amount of "database-ready" without introducing infrastructure (or a
SQL instance) the exercise doesn't call for. Swapping to a real data store
means adding an `EfFruitRepository` or `DapperFruitRepository`
implementing the same interface - `OrderCalculator`, `Program.cs`, and
every test written against `IFruitRepository` need no changes at all.

`Add` upserts (replaces on a name collision) rather than throwing, which
keeps the console's "add a new fruit" flow simple.

### Order — combining, removing, and clearing lines

`Order` wraps a list of `OrderLine`s (a fruit plus a quantity or weight).

- `AddLine` auto-combines. Adding the same fruit twice merges the
  quantity into the existing line (matched by name, case-insensitive)
  rather than creating a second line - matching how a real order works,
  "2kg of apples" is one line on a receipt however many times it was
  added to.
- `RemoveLineAt` / `RemoveLineByName` - removal by index or by
  case-insensitive name; both return false on no match rather than
  throwing.
- `Clear` - empties the order; used before loading a sample order, so
  loading it twice doesn't produce a mixed result.

`OrderCalculator` doesn't know or care how any individual fruit is priced
- it asks each line's fruit for its (fully computed, discount-aware)
strategy and sums the results, and separately offers a `Breakdown` for
itemised output.

### Console app — menu-driven

`Program.cs` runs a loop so every piece of behaviour above can be
exercised interactively:

1. View catalog - lists every fruit with its base price, unit, and base
   strategy name, followed by each of its currently active discounts
   (or "no active discounts").
2. Add a new fruit - prompts for name and base pricing (per kg / per
   item), then optionally attaches a threshold discount and/or a seasonal
   discount, in any combination.
3. Add a fruit to the current order - shows the fruit's pricing before
   asking for a quantity, so the unit (kg vs item) is unambiguous.
4. View order total - itemised breakdown plus grand total.
5. Load a sample order - clears the current order first, then loads a
   fixed order of Apple, Banana, Cherry, and Strawberry.
6. Remove a fruit from the current order - by name, case-insensitive.
7. Change a fruit's base pricing method - swap per-kg/per-item or the
   rate, independent of that fruit's active discounts.
8. Add or remove a discount on a fruit - attach a new threshold or
   seasonal discount, or remove one of the currently active ones by
   picking it from a list.
9. Exit

## How I'd extend this further

Add a new fruit with existing pricing behaviour: via the console's "Add a
new fruit" option, or by adding a few lines to
`InMemoryFruitRepository`'s seeding. No changes to the factory, the
strategies, or the decorators.

Add a new discount type (for example, loyalty pricing - a flat discount
for returning customers): add a new `IPricingStrategy` decorator
following the existing delegation pattern, a corresponding `IDiscountSpec`
implementation, and a factory method to construct it. `Fruit.AddDiscount`
and `RemoveDiscount` work with it immediately, with no changes needed
there.

Add a genuinely new pricing model (for example, "buy 3 get 1 free"): add
a new class implementing `IPricingStrategy` directly, or as a
decorator/spec pair if it's naturally a modifier on top of an existing
strategy.

Move the catalog to a real database: implement `IFruitRepository` against
SQL Server or Oracle. No changes needed to `OrderCalculator`,
`Program.cs`, or any test written against `IFruitRepository`.

Remove the decorator delegation duplication: extract an abstract base
decorator class implementing `BasePrice`/`Unit` once. Deliberately not
done yet - see the Decorator section above for why.

## Testing approach

XUnit, Arrange/Act/Assert, one behaviour per test:

- Each `IPricingStrategy` implementation is tested in isolation: correct
  calculation, edge cases, constructor validation, and its
  `BasePrice`/`Unit` values.
- `ThresholdDiscountDecoratorTests` and `SeasonalDiscountDecoratorTests`
  cover the decorators' own logic directly - above/at/below a threshold,
  before/on/during/on/after a date range, decorator-on-decorator
  stacking, and constructor validation, using `FakeDateTimeProvider` for
  deterministic dates.
- `PricingStrategyFactoryTests` confirms the factory's builder methods
  produce correctly configured strategies and discount specs.
- `FruitTests` covers the behaviour that's new in this iteration: a null
  base strategy is rejected at construction, `SetBaseStrategy` changes
  pricing while leaving active discounts untouched, `AddDiscount` applies
  and stacks correctly, and `RemoveDiscount` removes only the specific
  spec passed in (returning false if it wasn't active).
- `InMemoryFruitRepositoryTests` covers lookup (including case
  insensitivity), a missing fruit, `Add`, and the specific default
  catalog prices/discounts - deliberately not Strawberry's exact
  discounted price, since that fruit is seeded with the real system clock
  and asserting an exact price would make the test flaky depending on
  what day it's run.
- `OrderTests` covers auto-combining on repeated `AddLine`, case
  insensitivity, removal by index and by name, and `Clear`.
- `OrderCalculatorTests` covers summing multiple lines, an empty order,
  null input, discounted strategies honoured end-to-end, and the full
  `Breakdown` tuple shape.