using FruitSales.Core.Pricing;
using Xunit;

namespace FruitSales.Tests;

public class ThresholdDiscountDecoratorTests
{
    [Fact]
    public void CalculatePrice_AppliesDiscount_WhenAboveThreshold()
    {
        // Arrange
        var inner = new PerWeightPricingStrategy(pricePerKg: 5.00m);
        var decorator = new ThresholdDiscountDecorator(inner, threshold: 2m, discountRate: 0.10m);

        // Act
        var price = decorator.CalculatePrice(quantityOrWeight: 3m);

        // Assert
        // 3kg * $5.00 = $15.00, minus 10% = $13.50
        Assert.Equal(13.50m, price);
    }

    [Fact]
    public void CalculatePrice_DoesNotApplyDiscount_WhenAtThreshold()
    {
        // Arrange
        var inner = new PerWeightPricingStrategy(pricePerKg: 5.00m);
        var decorator = new ThresholdDiscountDecorator(inner, threshold: 2m, discountRate: 0.10m);

        // Act
        var price = decorator.CalculatePrice(quantityOrWeight: 2m);

        // Assert
        // Exactly at the threshold - discount only applies when strictly above it
        Assert.Equal(10.00m, price);
    }

    [Fact]
    public void CalculatePrice_DoesNotApplyDiscount_WhenBelowThreshold()
    {
        // Arrange
        var inner = new PerWeightPricingStrategy(pricePerKg: 5.00m);
        var decorator = new ThresholdDiscountDecorator(inner, threshold: 2m, discountRate: 0.10m);

        // Act
        var price = decorator.CalculatePrice(quantityOrWeight: 1m);

        // Assert
        Assert.Equal(5.00m, price);
    }

    [Fact]
    public void CalculatePrice_CanDecorateAnotherDecorator_ForStackedDiscounts()
    {
        // Arrange - demonstrates how a second discount could be layered on
        // top of the first without writing a new strategy class
        var inner = new PerWeightPricingStrategy(pricePerKg: 5.00m);
        var firstDiscount = new ThresholdDiscountDecorator(inner, threshold: 2m, discountRate: 0.10m);
        var secondDiscount = new ThresholdDiscountDecorator(firstDiscount, threshold: 2m, discountRate: 0.05m);

        // Act
        var price = secondDiscount.CalculatePrice(quantityOrWeight: 3m);

        // Assert
        // 3kg * $5.00 = $15.00 -> 10% off = $13.50 -> 5% off = $12.825
        Assert.Equal(12.825m, price);
    }

    [Fact]
    public void Constructor_Throws_WhenDiscountRateOutOfRange()
    {
        // Arrange
        var inner = new PerWeightPricingStrategy(pricePerKg: 5.00m);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ThresholdDiscountDecorator(inner, threshold: 2m, discountRate: 1.5m));
    }

    [Fact]
    public void ThresholdDiscountDecorator_Unit_DelegatesToInner()
    {
        // Arrange
        var inner = new PerWeightPricingStrategy(5.00m);
        var decorator = new ThresholdDiscountDecorator(inner, threshold: 2m, discountRate: 0.10m);

        // Act & Assert
        Assert.Equal(PricingUnit.Weight, decorator.Unit);
    }

    [Fact]
    public void Constructor_Throws_WhenInnerStrategyIsNull()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => new ThresholdDiscountDecorator(null!, threshold: 2m, discountRate: 0.10m));
    }
}
