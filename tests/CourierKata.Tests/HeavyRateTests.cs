namespace CourierKata.Tests;

public class HeavyRateTests
{
    [Theory]
    [InlineData(5, ParcelSize.Small)]
    [InlineData(150, ParcelSize.ExtraLarge)]
    public void Forty_kilogram_parcel_costs_50_dollars_at_the_heavy_rate_whatever_its_size(
        int largestDimension, ParcelSize expectedSize)
    {
        var parcel = new Parcel(largestDimension, 5m, 5m, 40m);

        var quote = QuoteFor(parcel);

        Assert.Equal([new ParcelLineItem(parcel, expectedSize, ParcelRate.Heavy, 50m, 0m)], quote.Items);
        Assert.Equal(50m, quote.Total);
    }

    [Theory]
    [InlineData("60", 10)]
    [InlineData("50.2", 1)]
    [InlineData("50", 0)]
    public void Heavy_rate_charges_1_dollar_per_part_kilogram_over_50_kilograms(string weight, int expectedOverweightCharge)
    {
        var parcel = new Parcel(5m, 5m, 5m, decimal.Parse(weight, System.Globalization.CultureInfo.InvariantCulture));

        var quote = QuoteFor(parcel);

        Assert.Equal(
            [new ParcelLineItem(parcel, ParcelSize.Small, ParcelRate.Heavy, 50m, expectedOverweightCharge)],
            quote.Items);
        Assert.Equal(50m + expectedOverweightCharge, quote.Total);
    }

    [Fact]
    public void Size_rate_is_kept_while_it_is_cheaper()
    {
        var parcel = new Parcel(5m, 5m, 5m, 24m);

        Assert.Equal(
            [new ParcelLineItem(parcel, ParcelSize.Small, ParcelRate.BySize, 3m, 46m)],
            QuoteFor(parcel).Items);
    }

    [Fact]
    public void Heavy_rate_is_used_once_it_is_cheaper()
    {
        var parcel = new Parcel(5m, 5m, 5m, 24.1m);

        Assert.Equal(
            [new ParcelLineItem(parcel, ParcelSize.Small, ParcelRate.Heavy, 50m, 0m)],
            QuoteFor(parcel).Items);
    }

    [Fact]
    public void Tie_between_the_rates_keeps_the_size_rate()
    {
        var parcel = new Parcel(20m, 5m, 5m, 24m);

        var quote = QuoteFor(parcel);

        Assert.Equal([new ParcelLineItem(parcel, ParcelSize.Medium, ParcelRate.BySize, 8m, 42m)], quote.Items);
        Assert.Equal(50m, quote.Total);
    }

    private static Quote QuoteFor(Parcel parcel)
    {
        var cart = new Cart();
        cart.AddParcel(parcel);
        return cart.GetQuote();
    }
}
