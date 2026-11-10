namespace CourierKata.Tests;

public class ParcelTests
{
    [Theory]
    [InlineData(0, 5, 5, "Length")]
    [InlineData(-1, 5, 5, "Length")]
    [InlineData(5, 0, 5, "Width")]
    [InlineData(5, -5, 5, "Width")]
    [InlineData(5, 5, 0, "Height")]
    [InlineData(5, 5, -0.01, "Height")]
    public void Dimension_of_zero_or_less_is_rejected(double length, double width, double height, string invalidDimension)
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Parcel((decimal)length, (decimal)width, (decimal)height));

        Assert.Equal(invalidDimension, error.ParamName);
    }

    [Fact]
    public void Very_large_parcel_is_accepted()
    {
        var parcel = new Parcel(10_000m, 5m, 5m);

        Assert.Equal(10_000m, parcel.Length);
    }
}
