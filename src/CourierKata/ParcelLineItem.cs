namespace CourierKata;

/// <summary>
/// One parcel in a quote: the size it is charged at, the base cost of that size, and any
/// charge for being over the size's weight limit, all in dollars.
/// </summary>
public sealed record ParcelLineItem(Parcel Parcel, ParcelSize Size, decimal BaseCost, decimal OverweightCharge) : LineItem
{
    /// <summary>The base cost plus the overweight charge, in dollars.</summary>
    public override decimal Cost => BaseCost + OverweightCharge;

    internal override void SealHierarchy()
    {
    }
}
