using FruitSales.Core.Models;

namespace FruitSales.Core.Catalog;

/// <summary>
/// Provides access to the fruit catalog. Abstracting this behind an
/// interface means the in-memory implementation used in this project can
/// later be swapped for a database-backed implementation (e.g. EF Core or
/// Dapper against SQL Server or Oracle) without any change to the code
/// that consumes the repository.
/// </summary>
public interface IFruitRepository
{
    Fruit? GetByName(string name);
    IReadOnlyCollection<Fruit> GetAll();
    void Add(Fruit fruit);
}

