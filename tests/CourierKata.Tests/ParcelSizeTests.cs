namespace CourierKata.Tests;

public class ParcelSizeTests
{
    [Theory]
    [MemberData(nameof(SmallParcels))]
    public void Parcel_with_all_dimensions_under_10cm_is_small(decimal length, decimal width, decimal height)
    {
        Assert.Equal(ParcelSize.Small, SizeOf(length, width, height));
    }

    public static TheoryData<decimal, decimal, decimal> SmallParcels => new()
    {
        { 1m, 1m, 1m },
        { 9.99m, 5m, 5m },
        { 9.99m, 9.99m, 9.99m },
    };

    [Theory]
    [MemberData(nameof(MediumParcels))]
    public void Parcel_with_largest_dimension_from_10cm_to_under_50cm_is_medium(decimal length, decimal width, decimal height)
    {
        Assert.Equal(ParcelSize.Medium, SizeOf(length, width, height));
    }

    public static TheoryData<decimal, decimal, decimal> MediumParcels => new()
    {
        { 10m, 5m, 5m },
        { 49.99m, 5m, 5m },
        { 49.99m, 49.99m, 49.99m },
    };

    [Theory]
    [MemberData(nameof(LargeParcels))]
    public void Parcel_with_largest_dimension_from_50cm_to_under_100cm_is_large(decimal length, decimal width, decimal height)
    {
        Assert.Equal(ParcelSize.Large, SizeOf(length, width, height));
    }

    public static TheoryData<decimal, decimal, decimal> LargeParcels => new()
    {
        { 50m, 5m, 5m },
        { 99.99m, 5m, 5m },
        { 99.99m, 99.99m, 99.99m },
    };

    [Theory]
    [MemberData(nameof(ExtraLargeParcels))]
    public void Parcel_with_any_dimension_of_100cm_or_more_is_extra_large(decimal length, decimal width, decimal height)
    {
        Assert.Equal(ParcelSize.ExtraLarge, SizeOf(length, width, height));
    }

    public static TheoryData<decimal, decimal, decimal> ExtraLargeParcels => new()
    {
        { 100m, 5m, 5m },
        { 100m, 100m, 100m },
        { 10_000m, 5m, 5m },
    };

    [Theory]
    [InlineData(60, 5, 5)]
    [InlineData(5, 60, 5)]
    [InlineData(5, 5, 60)]
    public void Largest_dimension_decides_the_size_whichever_position_it_is_in(int length, int width, int height)
    {
        Assert.Equal(ParcelSize.Large, SizeOf(length, width, height));
    }

    private static ParcelSize SizeOf(decimal length, decimal width, decimal height)
    {
        var cart = new Cart();
        cart.AddParcel(new Parcel(length, width, height));

        return Assert.Single(cart.GetQuote().Items).Size;
    }
}
