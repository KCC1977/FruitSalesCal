using FruitSales.Core;
using FruitSales.Core.Catalog;
using FruitSales.Core.Models;
using FruitSales.Core.Pricing;

var repository = new InMemoryFruitRepository();
var calculator = new OrderCalculator();
var factory = new PricingStrategyFactory();
var currentOrder = new Order();

var running = true;

while (running)
{
    PrintMenu();
    var choice = (Console.ReadLine() ?? "").Trim();

    switch (choice)
    {
        case "1": ViewCatalog(repository); break;
        case "2": AddFruit(repository, factory); break;
        case "3": ChangeBaseStrategy(repository, factory); break;
        case "4": ManageDiscounts(repository, factory); break;
        case "5": AddFruitToOrder(repository, currentOrder); break;
        case "6": RemoveFruitFromOrder(currentOrder); break;
        case "7": PrintOrder(currentOrder, calculator); break;
        case "8": LoadSampleOrder(repository, currentOrder); break;
        case "9": running = false; break;
        default: Console.WriteLine("Not a valid option, try again."); break;
    }
    Console.WriteLine();
}

Console.WriteLine("Goodbye!");

static void PrintMenu()
{
    Console.WriteLine("Fruit Sales Calculator");
    Console.WriteLine("=======================");
    Console.WriteLine("1. View catalog");
    Console.WriteLine("2. Add a new fruit to the catalog");
    Console.WriteLine("3. Change a fruit's base pricing method");
    Console.WriteLine("4. Add or remove a discount on a fruit");
    Console.WriteLine("5. Add fruit to the current order");
    Console.WriteLine("6. Remove fruit from the current order");
    Console.WriteLine("7. View current order");
    Console.WriteLine("8. Load a sample order");
    Console.WriteLine("9. Exit");
    Console.Write("Choose an option: ");
}

static void LoadSampleOrder(IFruitRepository repository, Order order)
{
    Console.WriteLine();

    var apple = repository.GetByName("Apple");
    var banana = repository.GetByName("Banana");
    var cherry = repository.GetByName("Cherry");
    var strawberry = repository.GetByName("Strawberry");

    if (apple is null || banana is null || cherry is null || strawberry is null)
    {
        Console.WriteLine("Sample order needs Apple, Banana, Cherry and Strawberry in the catalog - one or more is missing.");
        return;
    }

    order.Clear();
    order.AddLine(apple, 2m);         // 2kg of apples6
    order.AddLine(banana, 5m);        // 5 bananas
    order.AddLine(cherry, 3m);        // 3kg of cherries -> over the 2kg discount threshold
    order.AddLine(strawberry, 1.5m);  // 1.5kg of strawberries -> seasonal discount if in-season today

    Console.WriteLine("Sample order loaded. Choose option 7 to view it.");
}

static void ViewCatalog(IFruitRepository repository)
{
    Console.WriteLine();
    Console.WriteLine("Catalog:");

    foreach (var fruit in repository.GetAll())
    {
        var unitLabel = fruit.BaseStrategy.Unit == PricingUnit.Weight ? "kg" : "item";
        Console.WriteLine($"  {fruit.Name,-12} base price: ${fruit.BasePrice,-6:F2} per {unitLabel} ({fruit.BaseStrategy.StrategyName})");

        if (fruit.Discounts.Count == 0)
        {
            Console.WriteLine("    (no active discounts)");
        }
        else
        {
            foreach (var discount in fruit.Discounts)
            {
                Console.WriteLine($"    + {discount.Description}");
            }
        }
    }
}

static void AddFruit(IFruitRepository repository, PricingStrategyFactory factory)
{
    Console.WriteLine();
    Console.Write("Fruit name: ");
    var name = (Console.ReadLine() ?? "").Trim();

    if (string.IsNullOrWhiteSpace(name))
    {
        Console.WriteLine("Name cannot be empty - cancelled.");
        return;
    }

    var baseStrategy = PromptForBaseStrategy(factory);
    if (baseStrategy is null)
        return;

    var fruit = new Fruit(name, baseStrategy);

    Console.Write("Add a quantity/weight threshold discount? (y/n): ");
    if (IsYes(Console.ReadLine()))
    {
        var spec = PromptForThresholdDiscount(factory);
        if (spec is not null) fruit.AddDiscount(spec);
    }

    Console.Write("Add a seasonal discount? (y/n): ");
    if (IsYes(Console.ReadLine()))
    {
        var spec = PromptForSeasonalDiscount(factory);
        if (spec is not null) fruit.AddDiscount(spec);
    }

    repository.Add(fruit);
    Console.WriteLine($"Added {name} to the catalog.");
}

static IPricingStrategy? PromptForBaseStrategy(PricingStrategyFactory factory)
{
    Console.Write("Pricing method - (1) per kg or (2) per item: ");
    var methodChoice = (Console.ReadLine() ?? "").Trim();

    Console.Write("Base price: ");
    if (!decimal.TryParse(Console.ReadLine(), out var basePrice))
    {
        Console.WriteLine("Invalid price - cancelled.");
        return null;
    }

    try
    {
        return methodChoice == "2"
            ? factory.CreatePerItem(basePrice)
            : factory.CreatePerWeight(basePrice);
    }
    catch (ArgumentOutOfRangeException ex)
    {
        Console.WriteLine($"Could not create pricing strategy: {ex.Message}");
        return null;
    }
}

static IDiscountSpec? PromptForThresholdDiscount(PricingStrategyFactory factory)
{
    Console.Write("  Threshold (kg or items): ");
    decimal.TryParse(Console.ReadLine(), out var threshold);

    Console.Write("  Discount rate (e.g. 0.10 for 10%): ");
    decimal.TryParse(Console.ReadLine(), out var rate);

    try
    {
        return factory.CreateThresholdDiscount(threshold, rate);
    }
    catch (ArgumentOutOfRangeException ex)
    {
        Console.WriteLine($"  Could not create threshold discount: {ex.Message}");
        return null;
    }
}

static IDiscountSpec? PromptForSeasonalDiscount(PricingStrategyFactory factory)
{
    Console.Write("  Season start date (yyyy-MM-dd): ");
    DateOnly.TryParse(Console.ReadLine(), out var startDate);

    Console.Write("  Season end date (yyyy-MM-dd): ");
    DateOnly.TryParse(Console.ReadLine(), out var endDate);

    Console.Write("  Discount rate (e.g. 0.20 for 20%): ");
    decimal.TryParse(Console.ReadLine(), out var rate);

    try
    {
        return factory.CreateSeasonalDiscount(startDate, endDate, rate, new SystemDateTimeProvider());
    }
    catch (ArgumentOutOfRangeException ex)
    {
        Console.WriteLine($"  Could not create seasonal discount: {ex.Message}");
        return null;
    }
}

static void AddFruitToOrder(IFruitRepository repository, Order order)
{
    Console.WriteLine();
    Console.Write("Fruit name to add to order: ");
    var name = (Console.ReadLine() ?? "").Trim();

    var fruit = repository.GetByName(name);
    if (fruit is null)
    {
        Console.WriteLine($"No fruit named '{name}' found in the catalog.");
        return;
    }

    var unitLabel = fruit.Strategy.Unit == PricingUnit.Weight ? "kg" : "items";
    Console.WriteLine($"{fruit.Name} - base price: ${fruit.BasePrice:F2} per {(fruit.Strategy.Unit == PricingUnit.Weight ? "kg" : "item")} ({fruit.Strategy.StrategyName})");

    Console.Write($"Quantity ({unitLabel}) of {fruit.Name}: ");
    if (!decimal.TryParse(Console.ReadLine(), out var quantityOrWeight))
    {
        Console.WriteLine("Invalid quantity - cancelled.");
        return;
    }

    order.AddLine(fruit, quantityOrWeight);

    var updatedLine = order.Lines.First(line =>
        string.Equals(line.Fruit.Name, fruit.Name, StringComparison.OrdinalIgnoreCase));

    Console.WriteLine($"{fruit.Name} is now at {updatedLine.QuantityOrWeight} in your order.");
}

static void ChangeBaseStrategy(IFruitRepository repository, PricingStrategyFactory factory)
{
    Console.WriteLine();
    Console.Write("Fruit name to change: ");
    var name = (Console.ReadLine() ?? "").Trim();

    var fruit = repository.GetByName(name);
    if (fruit is null)
    {
        Console.WriteLine($"No fruit named '{name}' found in the catalog.");
        return;
    }

    Console.WriteLine($"Current base: ${fruit.BasePrice:F2} ({fruit.BaseStrategy.StrategyName})");

    var newBaseStrategy = PromptForBaseStrategy(factory);
    if (newBaseStrategy is null)
        return;

    fruit.SetBaseStrategy(newBaseStrategy);
    Console.WriteLine($"{fruit.Name}'s base pricing is now ${fruit.BasePrice:F2} ({fruit.BaseStrategy.StrategyName}). Active discounts are unchanged.");
}

static void ManageDiscounts(IFruitRepository repository, PricingStrategyFactory factory)
{
    Console.WriteLine();
    Console.Write("Fruit name: ");
    var name = (Console.ReadLine() ?? "").Trim();

    var fruit = repository.GetByName(name);
    if (fruit is null)
    {
        Console.WriteLine($"No fruit named '{name}' found in the catalog.");
        return;
    }

    Console.WriteLine($"{fruit.Name} - current pricing: {fruit.Strategy.StrategyName}");
    Console.WriteLine("1. Add a threshold discount");
    Console.WriteLine("2. Add a seasonal discount");
    Console.WriteLine("3. Remove a discount");
    Console.Write("Choose an option: ");
    var choice = (Console.ReadLine() ?? "").Trim();

    switch (choice)
    {
        case "1":
            var thresholdSpec = PromptForThresholdDiscount(factory);
            if (thresholdSpec is not null)
            {
                fruit.AddDiscount(thresholdSpec);
                Console.WriteLine("Threshold discount added.");
            }
            break;
        case "2":
            var seasonalSpec = PromptForSeasonalDiscount(factory);
            if (seasonalSpec is not null)
            {
                fruit.AddDiscount(seasonalSpec);
                Console.WriteLine("Seasonal discount added.");
            }
            break;
        case "3":
            RemoveDiscount(fruit);
            break;
        default:
            Console.WriteLine("Not a valid option.");
            break;
    }
}

static void RemoveDiscount(Fruit fruit)
{
    if (fruit.Discounts.Count == 0)
    {
        Console.WriteLine($"{fruit.Name} has no active discounts.");
        return;
    }

    Console.WriteLine("Active discounts:");
    for (var i = 0; i < fruit.Discounts.Count; i++)
    {
        Console.WriteLine($"  {i + 1}. {fruit.Discounts[i].Description}");
    }

    Console.Write("Enter the number of the discount to remove: ");
    if (!int.TryParse(Console.ReadLine(), out var choice) || choice < 1 || choice > fruit.Discounts.Count)
    {
        Console.WriteLine("Invalid selection - cancelled.");
        return;
    }

    fruit.RemoveDiscount(fruit.Discounts[choice - 1]);
    Console.WriteLine("Discount removed.");
}

static void RemoveFruitFromOrder(Order order)
{
    Console.WriteLine();

    if (order.Lines.Count == 0)
    {
        Console.WriteLine("Your order is empty - nothing to remove.");
        return;
    }

    Console.WriteLine("Current order:");
    for (var i = 0; i < order.Lines.Count; i++)
    {
        var line = order.Lines[i];
        Console.WriteLine($"  {i + 1}. {line.Fruit.Name} x {line.QuantityOrWeight}");
    }

    Console.Write("Enter the fruit name to remove: ");
    var name = (Console.ReadLine() ?? "").Trim();

    if (order.RemoveLineByName(name))
    {
        Console.WriteLine($"Removed {name} from the order.");
    }
    else
    {
        Console.WriteLine($"No line for '{name}' found in the order.");
    }
}

static void PrintOrder(Order order, OrderCalculator calculator)
{
    Console.WriteLine();
    if (order.Lines.Count == 0)
    {
        Console.WriteLine("Your order is empty.");
        return;
    }

    Console.WriteLine("Order breakdown:");
    foreach (var (name, basePrice, strategyName, quantityOrWeight, lineTotal) in calculator.Breakdown(order.Lines))
    {
        Console.WriteLine($"  {name,-10} base: ${basePrice,-6:F2} ({strategyName})");
        Console.WriteLine($"    x {quantityOrWeight,-6} => ${lineTotal:F2}");
    }

    var total = calculator.CalculateTotal(order.Lines);
    Console.WriteLine("-----------------------");
    Console.WriteLine($"Total: ${total:F2}");
}

static bool IsYes(string? input) =>
    (input ?? "").Trim().Equals("y", StringComparison.OrdinalIgnoreCase);