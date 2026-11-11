namespace CourierKata;

/// <summary>A parcel to deliver, with its dimensions in centimetres and its weight in kilograms.</summary>
/// <exception cref="ArgumentOutOfRangeException">A dimension or the weight is zero or negative.</exception>
public sealed record Parcel(decimal Length, decimal Width, decimal Height, decimal Weight)
{
    /// <summary>Length in centimetres.</summary>
    public decimal Length { get; } = Positive(Length, nameof(Length));

    /// <summary>Width in centimetres.</summary>
    public decimal Width { get; } = Positive(Width, nameof(Width));

    /// <summary>Height in centimetres.</summary>
    public decimal Height { get; } = Positive(Height, nameof(Height));

    /// <summary>Weight in kilograms.</summary>
    public decimal Weight { get; } = Positive(Weight, nameof(Weight));

    private static decimal Positive(decimal value, string name)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value, name);
        return value;
    }
}
