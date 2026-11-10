namespace CourierKata;

/// <summary>The rules that decide what size a parcel is charged at and what it costs.</summary>
internal static class ParcelPricing
{
    private static ParcelSize Classify(Parcel parcel)
    {
        var largest = Math.Max(parcel.Length, Math.Max(parcel.Width, parcel.Height));

        return largest switch
        {
            < 10m => ParcelSize.Small,
            < 50m => ParcelSize.Medium,
            < 100m => ParcelSize.Large,
            _ => ParcelSize.ExtraLarge,
        };
    }

    private static decimal PriceOf(ParcelSize size) => size switch
    {
        ParcelSize.Small => 3m,
        ParcelSize.Medium => 8m,
        ParcelSize.Large => 15m,
        ParcelSize.ExtraLarge => 25m,
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, "Unknown parcel size."),
    };

    public static LineItem Price(Parcel parcel)
    {
        var size = Classify(parcel);
        return new LineItem(parcel, size, PriceOf(size));
    }
}
