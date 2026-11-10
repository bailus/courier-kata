namespace CourierKata;

/// <summary>The rules that decide what size a parcel is charged at.</summary>
internal static class ParcelPricing
{
    public static ParcelSize Classify(Parcel parcel)
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
}
