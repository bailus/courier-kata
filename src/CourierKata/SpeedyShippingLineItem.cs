namespace CourierKata;

/// <summary>The extra charge for speedy shipping, in dollars.</summary>
public sealed record SpeedyShippingLineItem(decimal Cost) : LineItem(Cost)
{
    internal override void SealHierarchy()
    {
    }
}
