namespace CourierKata;

/// <summary>Collects parcels to be sent and prices them.</summary>
public sealed class Cart
{
    private readonly List<Parcel> _parcels = [];

    /// <summary>Adds a parcel to the cart.</summary>
    public void AddParcel(Parcel parcel)
    {
        ArgumentNullException.ThrowIfNull(parcel);
        _parcels.Add(parcel);
    }

    /// <summary>Prices every parcel in the cart.</summary>
    public Quote GetQuote() =>
        new(_parcels.Select(parcel => new LineItem(parcel, ParcelPricing.Classify(parcel))).ToList());
}
