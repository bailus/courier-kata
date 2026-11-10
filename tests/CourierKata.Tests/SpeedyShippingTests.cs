namespace CourierKata.Tests;

public class SpeedyShippingTests
{
    private static readonly Parcel Small = new(5m, 5m, 5m);
    private static readonly Parcel Medium = new(20m, 5m, 5m);
    private static readonly Parcel Large = new(70m, 5m, 5m);
    private static readonly Parcel ExtraLarge = new(150m, 5m, 5m);

    [Fact]
    public void Speedy_shipping_is_listed_separately_and_doubles_the_total()
    {
        var cart = CartWith(Shipping.Speedy, Small, Medium, Large, ExtraLarge);

        var quote = cart.GetQuote();

        Assert.Equal(
            [
                new ParcelLineItem(Small, ParcelSize.Small, 3m),
                new ParcelLineItem(Medium, ParcelSize.Medium, 8m),
                new ParcelLineItem(Large, ParcelSize.Large, 15m),
                new ParcelLineItem(ExtraLarge, ParcelSize.ExtraLarge, 25m),
                new SpeedyShippingLineItem(51m),
            ],
            quote.Items);
        Assert.Equal(102m, quote.Total);
    }

    [Fact]
    public void Standard_shipping_has_no_speedy_item()
    {
        var quote = CartWith(Shipping.Standard, Small).GetQuote();

        Assert.Equal([new ParcelLineItem(Small, ParcelSize.Small, 3m)], quote.Items);
        Assert.Equal(3m, quote.Total);
    }

    [Fact]
    public void Parcel_items_cost_the_same_with_speedy_shipping()
    {
        var standard = CartWith(Shipping.Standard, Small, Large, ExtraLarge).GetQuote();
        var speedy = CartWith(Shipping.Speedy, Small, Large, ExtraLarge).GetQuote();

        Assert.Equal(standard.Items, speedy.Items.OfType<ParcelLineItem>());
    }

    [Fact]
    public void Speedy_shipping_item_comes_after_the_parcel_items()
    {
        var quote = CartWith(Shipping.Speedy, Medium, Small).GetQuote();

        Assert.Equal(
            [
                new ParcelLineItem(Medium, ParcelSize.Medium, 8m),
                new ParcelLineItem(Small, ParcelSize.Small, 3m),
                new SpeedyShippingLineItem(11m),
            ],
            quote.Items);
    }

    [Fact]
    public void Speedy_shipping_on_an_empty_cart_costs_nothing()
    {
        var quote = CartWith(Shipping.Speedy).GetQuote();

        Assert.Equal([new SpeedyShippingLineItem(0m)], quote.Items);
        Assert.Equal(0m, quote.Total);
    }

    private static Cart CartWith(Shipping shipping, params Parcel[] parcels)
    {
        var cart = new Cart { Shipping = shipping };
        foreach (var parcel in parcels)
        {
            cart.AddParcel(parcel);
        }

        return cart;
    }
}
