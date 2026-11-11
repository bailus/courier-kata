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
            () => new Parcel((decimal)length, (decimal)width, (decimal)height, 0.5m));

        Assert.Equal(invalidDimension, error.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Weight_of_zero_or_less_is_rejected(double weight)
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Parcel(5m, 5m, 5m, (decimal)weight));

        Assert.Equal("Weight", error.ParamName);
    }
}
