namespace CourierKata;

/// <summary>The rules that decide what size a parcel is charged at and what it costs.</summary>
internal static class ParcelPricing
{
    private const decimal OverweightChargePerKilogram = 2m;

    public static ParcelLineItem Price(Parcel parcel)
    {
        var size = Classify(parcel);
        return new ParcelLineItem(parcel, size, ParcelRate.BySize, PriceOf(size), OverweightCharge(parcel.Weight, size));
    }

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

    /// <summary>The weight in kilograms a parcel of this size can be before it pays an overweight charge.</summary>
    private static decimal WeightLimitOf(ParcelSize size) => size switch
    {
        ParcelSize.Small => 1m,
        ParcelSize.Medium => 3m,
        ParcelSize.Large => 6m,
        ParcelSize.ExtraLarge => 10m,
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, "Unknown parcel size."),
    };

    /// <summary>Charges for each kilogram over the limit, counting any part of a kilogram as a whole one.</summary>
    private static decimal OverweightCharge(decimal weight, ParcelSize size)
    {
        var over = weight - WeightLimitOf(size);
        return over > 0m ? OverweightChargePerKilogram * Math.Ceiling(over) : 0m;
    }
}
