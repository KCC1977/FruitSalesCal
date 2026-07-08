using FruitSales.Core.Models;
using FruitSales.Core.Pricing;
using FruitSales.Tests.TestSupport;
using Xunit;

namespace FruitSales.Tests;

public class FruitTests
{
    [Fact]
    public void Constructor_Throws_WhenBaseStrategyIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new Fruit("Apple", null!));
    }

    [Fact]
    public void Constructor_Throws_WhenNameIsEmpty()
    {
        Assert.Throws<ArgumentException>(() => new Fruit("", new PerWeightPricingStrategy(2.00m)));
    }

    [Fact]
    public void CalculatePrice_UsesBaseStrategy_WhenNoDiscountsActive()
    {
        // Arrange
        var fruit = new Fruit("Apple", new PerWeightPricingStrategy(2.00m));

        // Act
        var price = fruit.Strategy.CalculatePrice(3m);

        // Assert
        Assert.Equal(6.00m, price);
    }

    [Fact]
    public void SetBaseStrategy_ChangesPricingMethod()
    {
        // Arrange
        var fruit = new Fruit("Apple", new PerWeightPricingStrategy(2.00m));

        // Act
        fruit.SetBaseStrategy(new PerItemPricingStrategy(0.50m));

        // Assert
        Assert.Equal(PricingUnit.Item, fruit.Strategy.Unit);
        Assert.Equal(2.00m, fruit.Strategy.CalculatePrice(4m));
    }

    [Fact]
    public void SetBaseStrategy_Throws_WhenNewStrategyIsNull()
    {
        // Arrange
        var fruit = new Fruit("Apple", new PerWeightPricingStrategy(2.00m));

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => fruit.SetBaseStrategy(null!));
    }

    [Fact]
    public void SetBaseStrategy_KeepsActiveDiscounts()
    {
        // Arrange
        var fruit = new Fruit("Cherry", new PerWeightPricingStrategy(5.00m));
        fruit.AddDiscount(new ThresholdDiscountSpec(threshold: 2m, discountRate: 0.10m));

        // Act
        fruit.SetBaseStrategy(new PerWeightPricingStrategy(10.00m));

        // Assert
        // 3kg * $10.00 = $30.00, minus 10% = $27.00
        Assert.Equal(27.00m, fruit.Strategy.CalculatePrice(3m));
    }

    [Fact]
    public void AddDiscount_AppliesDiscountToCalculatedPrice()
    {
        // Arrange
        var fruit = new Fruit("Cherry", new PerWeightPricingStrategy(5.00m));

        // Act
        fruit.AddDiscount(new ThresholdDiscountSpec(threshold: 2m, discountRate: 0.10m));

        // Assert
        Assert.Equal(13.50m, fruit.Strategy.CalculatePrice(3m));
    }

    [Fact]
    public void AddDiscount_StacksMultipleDiscounts()
    {
        // Arrange
        var fruit = new Fruit("Cherry", new PerWeightPricingStrategy(5.00m));
        var clock = new FakeDateTimeProvider { Today = new DateOnly(2026, 12, 15) };

        // Act
        fruit.AddDiscount(new ThresholdDiscountSpec(threshold: 2m, discountRate: 0.10m));
        fruit.AddDiscount(new SeasonalDiscountSpec(
            new DateOnly(2026, 10, 1), new DateOnly(2027, 3, 31), discountRate: 0.20m, clock));

        // Assert
        // 3kg * $5.00 = $15.00 -> 10% off = $13.50 -> 20% off = $10.80
        Assert.Equal(10.80m, fruit.Strategy.CalculatePrice(3m));
    }

    [Fact]
    public void RemoveDiscount_RemovesOnlyThatDiscount()
    {
        // Arrange
        var fruit = new Fruit("Cherry", new PerWeightPricingStrategy(5.00m));
        var thresholdSpec = new ThresholdDiscountSpec(threshold: 2m, discountRate: 0.10m);
        fruit.AddDiscount(thresholdSpec);

        // Act
        var result = fruit.RemoveDiscount(thresholdSpec);

        // Assert
        Assert.True(result);
        Assert.Equal(15.00m, fruit.Strategy.CalculatePrice(3m));
    }

    [Fact]
    public void RemoveDiscount_ReturnsFalse_WhenSpecNotActive()
    {
        // Arrange
        var fruit = new Fruit("Cherry", new PerWeightPricingStrategy(5.00m));
        var unrelatedSpec = new ThresholdDiscountSpec(threshold: 2m, discountRate: 0.10m);

        // Act
        var result = fruit.RemoveDiscount(unrelatedSpec);

        // Assert
        Assert.False(result);
    }
}