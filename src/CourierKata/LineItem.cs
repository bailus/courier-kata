namespace CourierKata;

/// <summary>One parcel in a quote, with the size it is charged at.</summary>
public sealed record LineItem(Parcel Parcel, ParcelSize Size);
