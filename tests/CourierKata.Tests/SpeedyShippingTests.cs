namespace CourierKata.Tests;

public class SpeedyShippingTests
{
    private static readonly Parcel Small = new(5m, 5m, 5m, 0.5m);
    private static readonly Parcel Medium = new(20m, 5m, 5m, 0.5m);
    private static readonly Parcel Large = new(70m, 5m, 5m, 0.5m);
    private static readonly Parcel ExtraLarge = new(150m, 5m, 5m, 0.5m);

    [Fact]
    public void Speedy_shipping_is_listed_separately_and_doubles_the_total()
    {
        var cart = CartWith(Shipping.Speedy, Small, Medium, Large, ExtraLarge);

        var quote = cart.GetQuote();

        Assert.Equal(
            [
                new ParcelLineItem(Small, ParcelSize.Small, ParcelRate.BySize, 3m, 0m),
                new ParcelLineItem(Medium, ParcelSize.Medium, ParcelRate.BySize, 8m, 0m),
                new ParcelLineItem(Large, ParcelSize.Large, ParcelRate.BySize, 15m, 0m),
                new ParcelLineItem(ExtraLarge, ParcelSize.ExtraLarge, ParcelRate.BySize, 25m, 0m),
                new SpeedyShippingLineItem(51m),
            ],
            quote.Items);
        Assert.Equal(102m, quote.Total);
    }

    [Fact]
    public void Standard_shipping_has_no_speedy_item()
    {
        var quote = CartWith(Shipping.Standard, Small).GetQuote();

        Assert.Equal([new ParcelLineItem(Small, ParcelSize.Small, ParcelRate.BySize, 3m, 0m)], quote.Items);
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
                new ParcelLineItem(Medium, ParcelSize.Medium, ParcelRate.BySize, 8m, 0m),
                new ParcelLineItem(Small, ParcelSize.Small, ParcelRate.BySize, 3m, 0m),
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

    [Fact]
    public void Speedy_shipping_applies_after_discounts()
    {
        var medium3Kg = new Parcel(20m, 5m, 5m, 3m);
        var medium4Kg = new Parcel(20m, 5m, 5m, 4m);

        var quote = CartWith(Shipping.Speedy, medium3Kg, medium3Kg, medium3Kg, medium4Kg, medium4Kg, medium4Kg).GetQuote();

        Assert.Equal(54m, quote.Items.OfType<ParcelLineItem>().Sum(item => item.Cost));
        Assert.Equal([8m, 10m], quote.Items.OfType<DiscountLineItem>().Select(item => item.Saving));
        Assert.Equal(new SpeedyShippingLineItem(36m), quote.Items[^1]);
        Assert.Equal(72m, quote.Total);
    }

    [Fact]
    public void Discount_items_come_between_the_parcel_items_and_speedy_shipping()
    {
        var medium = new Parcel(20m, 5m, 5m, 3m);
        var item = new ParcelLineItem(medium, ParcelSize.Medium, ParcelRate.BySize, 8m, 0m);

        var quote = CartWith(Shipping.Speedy, medium, medium, medium).GetQuote();

        Assert.Equal(
            [
                item,
                item,
                item,
                new DiscountLineItem(Discount.MediumParcelMania, [medium, medium, medium], 8m),
                new SpeedyShippingLineItem(16m),
            ],
            quote.Items);
        Assert.Equal(32m, quote.Total);
    }

    [Fact]
    public void Discounts_are_unaffected_by_speedy_shipping()
    {
        var medium = new Parcel(20m, 5m, 5m, 3m);

        var standard = CartWith(Shipping.Standard, medium, medium, medium).GetQuote();
        var speedy = CartWith(Shipping.Speedy, medium, medium, medium).GetQuote();

        Assert.Single(standard.Items.OfType<DiscountLineItem>());
        Assert.Equal(standard.Items.OfType<DiscountLineItem>(), speedy.Items.OfType<DiscountLineItem>());
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
