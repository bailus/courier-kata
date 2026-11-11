namespace CourierKata;

/// <summary>The extra charge for speedy shipping, in dollars.</summary>
public sealed record SpeedyShippingLineItem(decimal Cost) : LineItem
{
    /// <inheritdoc />
    public override decimal Cost { get; } = Cost;

    internal override void SealHierarchy()
    {
    }
}
