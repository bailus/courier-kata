namespace CourierKata;

/// <summary>Which price list a parcel is charged at.</summary>
public enum ParcelRate
{
    /// <summary>The price of the parcel's size, plus $2 per kilogram over that size's weight limit.</summary>
    BySize,

    /// <summary>A flat $50 for any dimensions, plus $1 per kilogram over 50 kg.</summary>
    Heavy,
}
