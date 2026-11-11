namespace CourierKata.Tests;

public class OverweightChargeTests
{
    [Fact]
    public void Small_parcel_part_of_a_kilogram_over_its_limit_pays_for_a_whole_kilogram()
    {
        var parcel = new Parcel(5m, 5m, 5m, 1.2m);

        var quote = QuoteFor(parcel);

        Assert.Equal([new ParcelLineItem(parcel, ParcelSize.Small, ParcelRate.BySize, 3m, 2m)], quote.Items);
        Assert.Equal(5m, quote.Items[0].Cost);
        Assert.Equal(5m, quote.Total);
    }

    [Theory]
    [InlineData(20, 5, ParcelSize.Medium, 8, 4)]
    [InlineData(70, 7, ParcelSize.Large, 15, 2)]
    [InlineData(150, 11, ParcelSize.ExtraLarge, 25, 2)]
    public void Parcel_over_its_limit_pays_2_dollars_per_kilogram_over(
        int largestDimension, int weight, ParcelSize expectedSize, int expectedBaseCost, int expectedOverweightCharge)
    {
        var parcel = new Parcel(largestDimension, 5m, 5m, weight);

        var quote = QuoteFor(parcel);

        Assert.Equal(
            [new ParcelLineItem(parcel, expectedSize, ParcelRate.BySize, expectedBaseCost, expectedOverweightCharge)],
            quote.Items);
        Assert.Equal(expectedBaseCost + expectedOverweightCharge, quote.Total);
    }

    [Theory]
    [InlineData(5, 1, ParcelSize.Small)]
    [InlineData(20, 3, ParcelSize.Medium)]
    [InlineData(70, 6, ParcelSize.Large)]
    [InlineData(150, 10, ParcelSize.ExtraLarge)]
    public void Parcel_exactly_at_its_limit_pays_no_overweight_charge(int largestDimension, int weight, ParcelSize expectedSize)
    {
        var item = Assert.IsType<ParcelLineItem>(
            Assert.Single(QuoteFor(new Parcel(largestDimension, 5m, 5m, weight)).Items));

        Assert.Equal(expectedSize, item.Size);
        Assert.Equal(0m, item.OverweightCharge);
    }

    [Fact]
    public void Parcel_within_its_limit_shows_a_zero_overweight_charge()
    {
        var parcel = new Parcel(5m, 5m, 5m, 0.5m);

        var item = Assert.IsType<ParcelLineItem>(Assert.Single(QuoteFor(parcel).Items));

        Assert.Equal(new ParcelLineItem(parcel, ParcelSize.Small, ParcelRate.BySize, 3m, 0m), item);
        Assert.Equal(ParcelRate.BySize, item.Rate);
    }

    [Fact]
    public void Heavy_parcel_keeps_the_size_of_its_dimensions()
    {
        var item = Assert.IsType<ParcelLineItem>(
            Assert.Single(QuoteFor(new Parcel(5m, 5m, 5m, 20m)).Items));

        Assert.Equal(ParcelSize.Small, item.Size);
    }

    [Fact]
    public void Speedy_shipping_doubles_the_overweight_charge_too()
    {
        var parcel = new Parcel(5m, 5m, 5m, 1.2m);

        var quote = QuoteFor(parcel, Shipping.Speedy);

        Assert.Equal(
            [
                new ParcelLineItem(parcel, ParcelSize.Small, ParcelRate.BySize, 3m, 2m),
                new SpeedyShippingLineItem(5m),
            ],
            quote.Items);
        Assert.Equal(10m, quote.Total);
    }

    private static Quote QuoteFor(Parcel parcel, Shipping shipping = Shipping.Standard)
    {
        var cart = new Cart { Shipping = shipping };
        cart.AddParcel(parcel);
        return cart.GetQuote();
    }
}
