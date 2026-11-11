namespace CourierKata;

/// <summary>
/// A discount applied to a group of parcels: which discount it is, the parcels in its group
/// in the order they were added, and how much it saves in dollars.
/// </summary>
public sealed record DiscountLineItem(Discount Discount, IReadOnlyList<Parcel> Parcels, decimal Saving) : LineItem
{
    /// <summary>Minus the saving, in dollars.</summary>
    public override decimal Cost => -Saving;

    /// <summary>Equal when the discount, the saving and the parcels, in order, are equal.</summary>
    public bool Equals(DiscountLineItem? other) =>
        other is not null
        && Discount == other.Discount
        && Saving == other.Saving
        && Parcels.SequenceEqual(other.Parcels);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Discount);
        hash.Add(Saving);
        foreach (var parcel in Parcels)
        {
            hash.Add(parcel);
        }

        return hash.ToHashCode();
    }

    internal override void SealHierarchy()
    {
    }
}
