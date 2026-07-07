using FruitSales.Core.Catalog;
using FruitSales.Core.Models;
using FruitSales.Core.Pricing;
using Xunit;

namespace FruitSales.Tests;

public class InMemoryFruitRepositoryTests
{
    [Fact]
    public void GetByName_ReturnsSeededFruit_CaseInsensitive()
    {
        // Arrange
        var repository = new InMemoryFruitRepository();

        // Act
        var fruit = repository.GetByName("apple");

        // Assert
        Assert.NotNull(fruit);
        Assert.Equal("Apple", fruit!.Name);
    }

    [Fact]
    public void GetByName_ReturnsNull_WhenFruitNotFound()
    {
        // Arrange
        var repository = new InMemoryFruitRepository();

        // Act
        var fruit = repository.GetByName("Dragonfruit");

        // Assert
        Assert.Null(fruit);
    }

    [Fact]
    public void GetAll_ReturnsAllFourSeededFruit()
    {
        // Arrange
        var repository = new InMemoryFruitRepository();

        // Act
        var fruits = repository.GetAll();

        // Assert
        Assert.Equal(4, fruits.Count);
    }

    [Fact]
    public void GetByName_Apple_ChargesTwoDollarsPerKg()
    {
        // Arrange
        var repository = new InMemoryFruitRepository();
        var apple = repository.GetByName("Apple")!;

        // Act
        var price = apple.Strategy.CalculatePrice(2m);

        // Assert
        Assert.Equal(4.00m, price);
    }

    [Fact]
    public void GetByName_Banana_ChargesThirtyCentsPerItem()
    {
        // Arrange
        var repository = new InMemoryFruitRepository();
        var banana = repository.GetByName("Banana")!;

        // Act
        var price = banana.Strategy.CalculatePrice(4m);

        // Assert
        Assert.Equal(1.20m, price);
    }

    [Fact]
    public void GetByName_Cherry_AppliesDiscount_WhenOverTwoKg()
    {
        // Arrange
        var repository = new InMemoryFruitRepository();
        var cherry = repository.GetByName("Cherry")!;

        // Act
        var price = cherry.Strategy.CalculatePrice(3m);

        // Assert
        Assert.Equal(13.50m, price);
    }

    [Fact]
    public void GetByName_Cherry_NoDiscount_WhenTwoKgOrLess()
    {
        // Arrange
        var repository = new InMemoryFruitRepository();
        var cherry = repository.GetByName("Cherry")!;

        // Act
        var price = cherry.Strategy.CalculatePrice(2m);

        // Assert
        Assert.Equal(10.00m, price);
    }

    [Fact]
    public void GetByName_Strawberry_IsSeededIntoCatalog()
    {
        // Arrange
        var repository = new InMemoryFruitRepository();

        // Act
        var strawberry = repository.GetByName("strawberry");

        // Assert
        Assert.NotNull(strawberry);
        Assert.Equal("Strawberry", strawberry!.Name);
    }

    [Fact]
    public void Add_MakesNewFruitRetrievableByName()
    {
        // Arrange
        var repository = new InMemoryFruitRepository();
        var newFruit = new Fruit("Mango", new PerWeightPricingStrategy(pricePerKg: 3.50m));

        // Act
        repository.Add(newFruit);
        var retrieved = repository.GetByName("Mango");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(3.50m, retrieved!.BasePrice);
    }
}
