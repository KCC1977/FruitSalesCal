using FruitSales.Core.Pricing;
using FruitSales.Tests.TestSupport;
using Xunit;

namespace FruitSales.Tests;

public class SeasonalDiscountDecoratorTests
{
    private static SeasonalDiscountDecorator CreateStrategy(FakeDateTimeProvider clock, decimal discountRate = 0.20m) =>
        new SeasonalDiscountDecorator(
            inner: new PerWeightPricingStrategy(pricePerKg: 6.00m),
            startDate: new DateOnly(2026, 10, 1),
            endDate: new DateOnly(2027, 3, 31),
            discountRate: discountRate,
            clock: clock);

    [Fact]
    public void CalculatePrice_AppliesDiscount_WhenWithinDateRange()
    {
        // Arrange
        var clock = new FakeDateTimeProvider { Today = new DateOnly(2026, 12, 15) };
        var strategy = CreateStrategy(clock);

        // Act
        var price = strategy.CalculatePrice(2m);

        // Assert
        // 2kg * $6.00 = $12.00, minus 20% = $9.60
        Assert.Equal(9.60m, price);
    }

    [Fact]
    public void CalculatePrice_AppliesDiscount_OnStartDate()
    {
        // Arrange
        var clock = new FakeDateTimeProvider { Today = new DateOnly(2026, 10, 1) };
        var strategy = CreateStrategy(clock);

        // Act
        var price = strategy.CalculatePrice(2m);

        // Assert
        Assert.Equal(9.60m, price);
    }

    [Fact]
    public void CalculatePrice_AppliesDiscount_OnEndDate()
    {
        // Arrange
        var clock = new FakeDateTimeProvider { Today = new DateOnly(2027, 3, 31) };
        var strategy = CreateStrategy(clock);

        // Act
        var price = strategy.CalculatePrice(2m);

        // Assert
        Assert.Equal(9.60m, price);
    }

    [Fact]
    public void CalculatePrice_NoDiscount_BeforeStartDate()
    {
        // Arrange
        var clock = new FakeDateTimeProvider { Today = new DateOnly(2026, 9, 30) };
        var strategy = CreateStrategy(clock);

        // Act
        var price = strategy.CalculatePrice(2m);

        // Assert
        Assert.Equal(12.00m, price);
    }

    [Fact]
    public void CalculatePrice_NoDiscount_AfterEndDate()
    {
        // Arrange
        var clock = new FakeDateTimeProvider { Today = new DateOnly(2027, 4, 1) };
        var strategy = CreateStrategy(clock);

        // Act
        var price = strategy.CalculatePrice(2m);

        // Assert
        Assert.Equal(12.00m, price);
    }

    [Fact]
    public void Constructor_Throws_WhenEndDateBeforeStartDate()
    {
        // Arrange
        var clock = new FakeDateTimeProvider();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SeasonalDiscountDecorator(
                inner: new PerWeightPricingStrategy(6.00m),
                startDate: new DateOnly(2027, 1, 1),
                endDate: new DateOnly(2026, 1, 1),
                discountRate: 0.20m,
                clock: clock));
    }

    [Fact]
    public void Constructor_Throws_WhenDiscountRateOutOfRange()
    {
        // Arrange
        var clock = new FakeDateTimeProvider();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SeasonalDiscountDecorator(
                inner: new PerWeightPricingStrategy(6.00m),
                startDate: new DateOnly(2026, 10, 1),
                endDate: new DateOnly(2027, 3, 31),
                discountRate: 1.5m,
                clock: clock));
    }

    [Fact]
    public void Constructor_Throws_WhenClockIsNull()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SeasonalDiscountDecorator(
                inner: new PerWeightPricingStrategy(6.00m),
                startDate: new DateOnly(2026, 10, 1),
                endDate: new DateOnly(2027, 3, 31),
                discountRate: 0.20m,
                clock: null!));
    }
}