namespace CourierKata;

/// <summary>Collects parcels to be sent, with how fast to ship them, and prices them.</summary>
public sealed class Cart
{
    private readonly List<Parcel> _parcels = [];

    /// <summary>How fast the order is shipped. Defaults to <see cref="Shipping.Standard"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not a defined <see cref="CourierKata.Shipping"/>.</exception>
    public Shipping Shipping
    {
        get;
        set
        {
            if (!Enum.IsDefined(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown shipping option.");
            }

            field = value;
        }
    } = Shipping.Standard;

    /// <summary>Adds a parcel to the cart.</summary>
    public void AddParcel(Parcel parcel)
    {
        ArgumentNullException.ThrowIfNull(parcel);
        _parcels.Add(parcel);
    }

    /// <summary>Prices every parcel in the cart, followed by speedy shipping when it is selected.</summary>
    public Quote GetQuote()
    {
        var parcelItems = _parcels.Select(ParcelPricing.Price).ToList();
        var items = new List<LineItem>(parcelItems);

        if (Shipping == Shipping.Speedy)
        {
            items.Add(new SpeedyShippingLineItem(parcelItems.Sum(item => item.Cost)));
        }

        return new Quote(items.AsReadOnly(), items.Sum(item => item.Cost));
    }
}
