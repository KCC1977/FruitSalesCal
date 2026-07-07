using FruitSales.Core;
using FruitSales.Core.Models;
using FruitSales.Core.Pricing;
using Xunit;

namespace FruitSales.Tests;

public class OrderCalculatorTests
{
    [Fact]
    public void CalculateTotal_SumsAllOrderLines()
    {
        // Arrange
        var apple = new Fruit("Apple", new PerWeightPricingStrategy(2.00m));
        var banana = new Fruit("Banana", new PerItemPricingStrategy(0.30m));
        var lines = new[]
        {
            new OrderLine(apple, 2m),   // $4.00
            new OrderLine(banana, 5m),  // $1.50
        };
        var calculator = new OrderCalculator();

        // Act
        var total = calculator.CalculateTotal(lines);

        // Assert
        Assert.Equal(5.50m, total);
    }

    [Fact]
    public void CalculateTotal_ReturnsZero_ForEmptyOrder()
    {
        // Arrange
        var calculator = new OrderCalculator();

        // Act
        var total = calculator.CalculateTotal(Array.Empty<OrderLine>());

        // Assert
        Assert.Equal(0m, total);
    }

    [Fact]
    public void CalculateTotal_Throws_WhenLinesIsNull()
    {
        // Arrange
        var calculator = new OrderCalculator();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => calculator.CalculateTotal(null!));
    }

    [Fact]
    public void CalculateTotal_UsesEachFruitsOwnStrategy_IncludingDiscounts()
    {
        // Arrange
        var cherry = new Fruit(
            "Cherry",
            new ThresholdDiscountDecorator(new PerWeightPricingStrategy(5.00m), threshold: 2m, discountRate: 0.10m));
        var lines = new[] { new OrderLine(cherry, 3m) };
        var calculator = new OrderCalculator();

        // Act
        var total = calculator.CalculateTotal(lines);

        // Assert
        Assert.Equal(13.50m, total);
    }

    [Fact]
    public void Breakdown_ReturnsOneEntryPerLine_WithCorrectLineTotals()
    {
        // Arrange
        var apple = new Fruit("Apple", new PerWeightPricingStrategy(2.00m));
        var lines = new[] { new OrderLine(apple, 3m) };
        var calculator = new OrderCalculator();

        // Act
        var breakdown = calculator.Breakdown(lines);

        // Assert
        Assert.Single(breakdown);
        Assert.Equal("Apple", breakdown[0].FruitName);
        Assert.Equal(2.00m, breakdown[0].BasePrice);
        Assert.Equal("Per Kg", breakdown[0].StrategyName);
        Assert.Equal(3m, breakdown[0].QuantityOrWeight);
        Assert.Equal(6.00m, breakdown[0].LineTotal);
    }

    [Fact]
    public void ThresholdDiscountDecorator_StrategyName_DescribesInnerAndDiscount()
    {
        // Arrange
        var inner = new PerWeightPricingStrategy(5.00m);
        var decorator = new ThresholdDiscountDecorator(inner, threshold: 2m, discountRate: 0.10m);

        // Act
        var name = decorator.StrategyName;

        // Assert
        Assert.Equal("Per Kg + Threshold Discount (10% over 2)", name);
    }
}
