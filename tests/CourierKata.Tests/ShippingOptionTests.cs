namespace CourierKata.Tests;

public class ShippingOptionTests
{
    private static readonly Parcel Small = new(5m, 5m, 5m);
    private static readonly Parcel ExtraLarge = new(150m, 5m, 5m);

    [Fact]
    public void New_cart_uses_standard_shipping()
    {
        Assert.Equal(Shipping.Standard, new Cart().Shipping);
    }

    [Fact]
    public void Switching_back_to_standard_removes_speedy_shipping()
    {
        var cart = new Cart();
        cart.AddParcel(Small);
        cart.Shipping = Shipping.Speedy;

        cart.Shipping = Shipping.Standard;
        var quote = cart.GetQuote();

        Assert.Equal(Shipping.Standard, cart.Shipping);
        Assert.Equal([new ParcelLineItem(Small, ParcelSize.Small, 3m)], quote.Items);
        Assert.Equal(3m, quote.Total);
    }

    [Fact]
    public void Speedy_can_be_selected_before_adding_parcels()
    {
        var cart = new Cart { Shipping = Shipping.Speedy };
        cart.AddParcel(Small);
        cart.AddParcel(ExtraLarge);

        var quote = cart.GetQuote();

        Assert.Equal(new SpeedyShippingLineItem(28m), quote.Items[^1]);
        Assert.Equal(56m, quote.Total);
    }

    [Fact]
    public void Changing_shipping_after_quoting_does_not_change_the_earlier_quote()
    {
        var cart = new Cart();
        cart.AddParcel(Small);
        var before = cart.GetQuote();

        cart.Shipping = Shipping.Speedy;
        var after = cart.GetQuote();

        Assert.Equal([new ParcelLineItem(Small, ParcelSize.Small, 3m)], before.Items);
        Assert.Equal(3m, before.Total);
        Assert.Equal(new SpeedyShippingLineItem(3m), after.Items[^1]);
        Assert.Equal(6m, after.Total);
    }

    [Fact]
    public void Undefined_shipping_option_is_rejected()
    {
        var cart = new Cart { Shipping = Shipping.Speedy };

        var error = Assert.Throws<ArgumentOutOfRangeException>(() => cart.Shipping = (Shipping)42);

        Assert.Equal("value", error.ParamName);
        Assert.Equal(Shipping.Speedy, cart.Shipping);
    }
}
