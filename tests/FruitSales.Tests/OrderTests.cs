using FruitSales.Core.Models;
using FruitSales.Core.Pricing;
using Xunit;

namespace FruitSales.Tests;

public class OrderTests
{
    private static Fruit CreateFruit(string name = "Apple") =>
        new(name, new PerWeightPricingStrategy(2.00m));

    [Fact]
    public void AddLine_CreatesNewLine_ForFirstAdditionOfAFruit()
    {
        // Arrange
        var order = new Order();
        var apple = CreateFruit("Apple");

        // Act
        order.AddLine(apple, 2m);

        // Assert
        Assert.Single(order.Lines);
        Assert.Equal(2m, order.Lines[0].QuantityOrWeight);
    }

    [Fact]
    public void AddLine_CombinesQuantity_WhenSameFruitAddedAgain()
    {
        // Arrange
        var order = new Order();
        var apple = CreateFruit("Apple");

        // Act
        order.AddLine(apple, 2m);
        order.AddLine(apple, 3m);

        // Assert
        Assert.Single(order.Lines);
        Assert.Equal(5m, order.Lines[0].QuantityOrWeight);
    }

    [Fact]
    public void AddLine_CombinesQuantity_CaseInsensitively()
    {
        // Arrange
        var order = new Order();

        // Act
        order.AddLine(CreateFruit("Apple"), 2m);
        order.AddLine(CreateFruit("apple"), 1m);

        // Assert
        Assert.Single(order.Lines);
        Assert.Equal(3m, order.Lines[0].QuantityOrWeight);
    }

    [Fact]
    public void AddLine_KeepsDifferentFruitOnSeparateLines()
    {
        // Arrange
        var order = new Order();

        // Act
        order.AddLine(CreateFruit("Apple"), 2m);
        order.AddLine(CreateFruit("Banana"), 5m);

        // Assert
        Assert.Equal(2, order.Lines.Count);
    }

    [Fact]
    public void RemoveLineAt_RemovesTheCorrectLine()
    {
        // Arrange
        var order = new Order()
            .AddLine(CreateFruit("Apple"), 1m)
            .AddLine(CreateFruit("Banana"), 2m)
            .AddLine(CreateFruit("Cherry"), 3m);

        // Act
        var result = order.RemoveLineAt(1); // removes Banana

        // Assert
        Assert.True(result);
        Assert.Equal(2, order.Lines.Count);
        Assert.Equal("Apple", order.Lines[0].Fruit.Name);
        Assert.Equal("Cherry", order.Lines[1].Fruit.Name);
    }

    [Fact]
    public void RemoveLineAt_ReturnsFalse_WhenIndexTooLarge()
    {
        // Arrange
        var order = new Order().AddLine(CreateFruit("Apple"), 1m);

        // Act
        var result = order.RemoveLineAt(5);

        // Assert
        Assert.False(result);
        Assert.Single(order.Lines);
    }

    [Fact]
    public void RemoveLineAt_ReturnsFalse_WhenIndexNegative()
    {
        // Arrange
        var order = new Order().AddLine(CreateFruit("Apple"), 1m);

        // Act
        var result = order.RemoveLineAt(-1);

        // Assert
        Assert.False(result);
        Assert.Single(order.Lines);
    }

    [Fact]
    public void RemoveLineByName_RemovesTheMatchingLine_CaseInsensitive()
    {
        // Arrange
        var order = new Order()
            .AddLine(CreateFruit("Apple"), 1m)
            .AddLine(CreateFruit("Banana"), 2m);

        // Act
        var result = order.RemoveLineByName("APPLE");

        // Assert
        Assert.True(result);
        Assert.Single(order.Lines);
        Assert.Equal("Banana", order.Lines[0].Fruit.Name);
    }

    [Fact]
    public void RemoveLineByName_ReturnsFalse_WhenNameNotFound()
    {
        // Arrange
        var order = new Order().AddLine(CreateFruit("Apple"), 1m);

        // Act
        var result = order.RemoveLineByName("Banana");

        // Assert
        Assert.False(result);
        Assert.Single(order.Lines);
    }
}