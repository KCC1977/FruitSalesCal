using FruitSales.Core.Pricing;
using Xunit;

namespace FruitSales.Tests;

public class PerItemPricingStrategyTests
{
    [Fact]
    public void CalculatePrice_ReturnsPricePerItemTimesQuantity()
    {
        // Arrange
        var strategy = new PerItemPricingStrategy(pricePerItem: 0.30m);

        // Act
        var price = strategy.CalculatePrice(quantityOrWeight: 5m);

        // Assert
        Assert.Equal(1.50m, price);
    }
    
    [Fact]
    public void PerItemPricingStrategy_Unit_IsItem()
    {
        // Arrange
        var strategy = new PerItemPricingStrategy(0.30m);

        // Act & Assert
        Assert.Equal(PricingUnit.Item, strategy.Unit);
    }

    [Fact]
    public void CalculatePrice_ReturnsZero_WhenQuantityIsZero()
    {
        // Arrange
        var strategy = new PerItemPricingStrategy(pricePerItem: 0.30m);

        // Act
        var price = strategy.CalculatePrice(quantityOrWeight: 0m);

        // Assert
        Assert.Equal(0m, price);
    }

    [Fact]
    public void Constructor_Throws_WhenPriceIsNegative()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new PerItemPricingStrategy(pricePerItem: -0.10m));
    }
}
