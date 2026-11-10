namespace CourierKata;

/// <summary>One parcel in a quote, with the size it is charged at and its cost in dollars.</summary>
public sealed record ParcelLineItem(Parcel Parcel, ParcelSize Size, decimal Cost) : LineItem(Cost)
{
    internal override void SealHierarchy()
    {
    }
}
