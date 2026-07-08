namespace FruitSales.Core.Pricing;

public class SeasonalDiscountSpec : IDiscountSpec
{
    private readonly DateOnly _startDate;
    private readonly DateOnly _endDate;
    private readonly decimal _discountRate;
    private readonly IDateTimeProvider _clock;

    public SeasonalDiscountSpec(DateOnly startDate, DateOnly endDate, decimal discountRate, IDateTimeProvider clock)
    {
        _startDate = startDate;
        _endDate = endDate;
        _discountRate = discountRate;
        _clock = clock;
    }

    public string Description =>
        $"Seasonal Discount ({_discountRate:P0}, {_startDate:yyyy-MM-dd} to {_endDate:yyyy-MM-dd})";

    public IPricingStrategy Apply(IPricingStrategy inner) =>
        new SeasonalDiscountDecorator(inner, _startDate, _endDate, _discountRate, _clock);
}