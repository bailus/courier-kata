namespace CourierKata;

/// <summary>
/// One parcel in a quote: its size (from its dimensions), the rate it is charged at, and that
/// rate's base cost and charge for being over the rate's weight limit, both in dollars.
/// </summary>
public sealed record ParcelLineItem(Parcel Parcel, ParcelSize Size, ParcelRate Rate, decimal BaseCost, decimal OverweightCharge) : LineItem
{
    /// <summary>The base cost plus the overweight charge, in dollars.</summary>
    public override decimal Cost => BaseCost + OverweightCharge;

    internal override void SealHierarchy()
    {
    }
}
