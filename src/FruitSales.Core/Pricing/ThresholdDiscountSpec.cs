namespace FruitSales.Core.Pricing;

public class ThresholdDiscountSpec : IDiscountSpec
{
    private readonly decimal _threshold;
    private readonly decimal _discountRate;

    public ThresholdDiscountSpec(decimal threshold, decimal discountRate)
    {
        _threshold = threshold;
        _discountRate = discountRate;
    }

    public string Description => $"Threshold Discount ({_discountRate:P0} over {_threshold})";

    public IPricingStrategy Apply(IPricingStrategy inner) =>
        new ThresholdDiscountDecorator(inner, _threshold, _discountRate);
}