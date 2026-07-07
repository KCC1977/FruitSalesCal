using FruitSales.Core.Pricing;
using Xunit;

namespace FruitSales.Tests;

public class PerWeightPricingStrategyTests
{
    [Fact]
    public void CalculatePrice_ReturnsPricePerKgTimesWeight()
    {
        // Arrange
        var strategy = new PerWeightPricingStrategy(pricePerKg: 2.00m);

        // Act
        var price = strategy.CalculatePrice(quantityOrWeight: 3m);

        // Assert
        Assert.Equal(6.00m, price);
    }

    [Fact]
    public void CalculatePrice_ReturnsZero_WhenWeightIsZero()
    {
        // Arrange
        var strategy = new PerWeightPricingStrategy(pricePerKg: 2.00m);

        // Act
        var price = strategy.CalculatePrice(quantityOrWeight: 0m);

        // Assert
        Assert.Equal(0m, price);
    }
    [Fact]
    public void PerWeightPricingStrategy_Unit_IsWeight()
    {
        // Arrange
        var strategy = new PerWeightPricingStrategy(2.00m);

        // Act & Assert
        Assert.Equal(PricingUnit.Weight, strategy.Unit);
    }

    [Fact]
    public void Constructor_Throws_WhenPriceIsNegative()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new PerWeightPricingStrategy(pricePerKg: -1m));
    }
}
