using FruitSales.Core.Pricing;
using FruitSales.Tests.TestSupport;
using Xunit;

namespace FruitSales.Tests;

public class PricingStrategyFactoryTests
{
    private readonly PricingStrategyFactory _factory = new();

    [Fact]
    public void CreatePerWeight_ChargesGivenPricePerKg()
    {
        // Arrange
        var strategy = _factory.CreatePerWeight(pricePerKg: 2.00m);

        // Act
        var price = strategy.CalculatePrice(2m);

        // Assert
        Assert.Equal(4.00m, price);
    }

    [Fact]
    public void CreatePerItem_ChargesGivenPricePerItem()
    {
        // Arrange
        var strategy = _factory.CreatePerItem(pricePerItem: 0.30m);

        // Act
        var price = strategy.CalculatePrice(4m);

        // Assert
        Assert.Equal(1.20m, price);
    }

    [Fact]
    public void WithThresholdDiscount_AppliesDiscount_WhenOverThreshold()
    {
        // Arrange
        var strategy = _factory.WithThresholdDiscount(
            inner: _factory.CreatePerWeight(5.00m),
            threshold: 2m,
            discountRate: 0.10m);

        // Act
        var price = strategy.CalculatePrice(3m);

        // Assert
        Assert.Equal(13.50m, price);
    }

    [Fact]
    public void WithThresholdDiscount_NoDiscount_WhenAtOrBelowThreshold()
    {
        // Arrange
        var strategy = _factory.WithThresholdDiscount(
            inner: _factory.CreatePerWeight(5.00m),
            threshold: 2m,
            discountRate: 0.10m);

        // Act
        var price = strategy.CalculatePrice(2m);

        // Assert
        Assert.Equal(10.00m, price);
    }

    [Fact]
    public void WithSeasonalDiscount_AppliesDiscount_WhenWithinDateRange()
    {
        // Arrange
        var clock = new FakeDateTimeProvider { Today = new DateOnly(2026, 12, 15) };
        var strategy = _factory.WithSeasonalDiscount(
            inner: _factory.CreatePerWeight(6.00m),
            startDate: new DateOnly(2026, 10, 1),
            endDate: new DateOnly(2027, 3, 31),
            discountRate: 0.20m,
            clock: clock);

        // Act
        var price = strategy.CalculatePrice(2m);

        // Assert
        // 2kg * $6.00 = $12.00, minus 20% = $9.60
        Assert.Equal(9.60m, price);
    }

    [Fact]
    public void WithSeasonalDiscount_NoDiscount_WhenOutsideDateRange()
    {
        // Arrange
        var clock = new FakeDateTimeProvider { Today = new DateOnly(2026, 6, 1) };
        var strategy = _factory.WithSeasonalDiscount(
            inner: _factory.CreatePerWeight(6.00m),
            startDate: new DateOnly(2026, 10, 1),
            endDate: new DateOnly(2027, 3, 31),
            discountRate: 0.20m,
            clock: clock);

        // Act
        var price = strategy.CalculatePrice(2m);

        // Assert
        Assert.Equal(12.00m, price);
    }
}