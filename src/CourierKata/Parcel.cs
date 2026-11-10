namespace CourierKata;

/// <summary>A parcel to deliver, with its dimensions in centimetres.</summary>
/// <exception cref="ArgumentOutOfRangeException">A dimension is zero or negative.</exception>
public sealed record Parcel(decimal Length, decimal Width, decimal Height)
{
    /// <summary>Length in centimetres.</summary>
    public decimal Length { get; } = Positive(Length, nameof(Length));

    /// <summary>Width in centimetres.</summary>
    public decimal Width { get; } = Positive(Width, nameof(Width));

    /// <summary>Height in centimetres.</summary>
    public decimal Height { get; } = Positive(Height, nameof(Height));

    private static decimal Positive(decimal value, string dimension)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value, dimension);
        return value;
    }
}
