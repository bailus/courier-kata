namespace CourierKata.Tests;

/// <summary>Parcels at known costs, and helpers for quoting carts of them.</summary>
internal static class QuoteParcels
{
    /// <summary>Small, $3.</summary>
    public static readonly Parcel Small = new(5m, 5m, 5m, 0.5m);

    /// <summary>Small weighing 5 kg, $11.</summary>
    public static readonly Parcel Small5Kg = new(5m, 5m, 5m, 5m);

    /// <summary>Small at the Heavy rate, $50.</summary>
    public static readonly Parcel Small40Kg = new(5m, 5m, 5m, 40m);

    /// <summary>Medium weighing 3 kg, $8.</summary>
    public static readonly Parcel Medium3Kg = new(20m, 5m, 5m, 3m);

    /// <summary>Medium weighing 4 kg, $10.</summary>
    public static readonly Parcel Medium4Kg = new(20m, 5m, 5m, 4m);

    /// <summary>Medium weighing 5 kg, $12.</summary>
    public static readonly Parcel Medium5Kg = new(20m, 5m, 5m, 5m);

    /// <summary>Large, $15.</summary>
    public static readonly Parcel Large = new(70m, 5m, 5m, 0.5m);

    /// <summary>XL, $25.</summary>
    public static readonly Parcel ExtraLarge = new(150m, 5m, 5m, 0.5m);

    public static Quote QuoteFor(params Parcel[] parcels) => QuoteFor(Shipping.Standard, parcels);

    public static Quote QuoteFor(Shipping shipping, params Parcel[] parcels)
    {
        var cart = new Cart { Shipping = shipping };
        foreach (var parcel in parcels)
        {
            cart.AddParcel(parcel);
        }

        return cart.GetQuote();
    }

    public static Parcel[] Times(int count, Parcel parcel) => Enumerable.Repeat(parcel, count).ToArray();

    public static Parcel[] Concat(params Parcel[][] groups) => groups.SelectMany(group => group).ToArray();

    public static IReadOnlyList<DiscountLineItem> DiscountsIn(Quote quote) => quote.Items.OfType<DiscountLineItem>().ToList();

    /// <summary>The parcel line item each parcel gets when quoted on its own.</summary>
    public static IEnumerable<LineItem> PricedOnTheirOwn(params Parcel[] parcels) =>
        parcels.Select(parcel => Assert.Single(QuoteFor(parcel).Items));
}
