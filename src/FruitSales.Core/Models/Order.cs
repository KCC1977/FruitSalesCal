namespace FruitSales.Core.Models;

/// <summary>
/// A customer order made up of one or more order lines.
/// </summary>
public class Order
{
    private readonly List<OrderLine> _lines = new();

    public IReadOnlyList<OrderLine> Lines => _lines;

    /// <summary>
    /// Adds a fruit to the order. If the order already has a line for this
    /// fruit (matched by name, case-insensitive), the quantity is combined
    /// into that existing line rather than creating a second, separate line.
    /// </summary>
    public Order AddLine(Fruit fruit, decimal quantityOrWeight)
    {
        var existingIndex = _lines.FindIndex(line =>
            string.Equals(line.Fruit.Name, fruit.Name, StringComparison.OrdinalIgnoreCase));

        if (existingIndex >= 0)
        {
            var existing = _lines[existingIndex];
            _lines[existingIndex] = existing with { QuantityOrWeight = existing.QuantityOrWeight + quantityOrWeight };
        }
        else
        {
            _lines.Add(new OrderLine(fruit, quantityOrWeight));
        }

        return this;
    }

    public bool RemoveLineAt(int index)
    {
        if (index < 0 || index >= _lines.Count)
            return false;

        _lines.RemoveAt(index);
        return true;
    }

    public bool RemoveLineByName(string name)
    {
        var index = _lines.FindIndex(line =>
            string.Equals(line.Fruit.Name, name, StringComparison.OrdinalIgnoreCase));

        if (index < 0)
            return false;

        _lines.RemoveAt(index);
        return true;
    }

    public void Clear()
    {
        _lines.Clear();
    }
}
