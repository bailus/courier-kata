namespace CourierKata.Tests;

public class CartTests
{
    private static readonly Parcel Small = new(5m, 5m, 5m);
    private static readonly Parcel Medium = new(20m, 5m, 5m);
    private static readonly Parcel Large = new(70m, 5m, 5m);
    private static readonly Parcel ExtraLarge = new(150m, 5m, 5m);

    [Fact]
    public void Line_items_are_in_the_order_parcels_were_added()
    {
        var cart = CartWith(ExtraLarge, Small, Medium);

        Assert.Equal(
            [
                new LineItem(ExtraLarge, ParcelSize.ExtraLarge, 25m),
                new LineItem(Small, ParcelSize.Small, 3m),
                new LineItem(Medium, ParcelSize.Medium, 8m),
            ],
            cart.GetQuote().Items);
    }

    [Fact]
    public void Same_parcel_added_twice_is_quoted_twice()
    {
        var cart = CartWith(Small, Small);

        Assert.Equal(
            [
                new LineItem(Small, ParcelSize.Small, 3m),
                new LineItem(Small, ParcelSize.Small, 3m),
            ],
            cart.GetQuote().Items);
    }

    [Fact]
    public void Total_is_the_sum_of_line_item_costs()
    {
        var cart = CartWith(Small, Medium, Large, ExtraLarge);

        Assert.Equal(51m, cart.GetQuote().Total);
    }

    [Fact]
    public void Empty_cart_has_no_line_items_and_costs_nothing()
    {
        var quote = new Cart().GetQuote();

        Assert.Empty(quote.Items);
        Assert.Equal(0m, quote.Total);
    }

    [Fact]
    public void Quoting_twice_gives_the_same_result()
    {
        var cart = CartWith(Small, Large);

        var first = cart.GetQuote();
        var second = cart.GetQuote();

        Assert.Equal(first.Items, second.Items);
        Assert.Equal(first.Total, second.Total);
    }

    [Fact]
    public void Adding_a_parcel_after_quoting_does_not_change_the_earlier_quote()
    {
        var cart = CartWith(Small);
        var before = cart.GetQuote();

        cart.AddParcel(ExtraLarge);
        var after = cart.GetQuote();

        Assert.Single(before.Items);
        Assert.Equal(3m, before.Total);
        Assert.Equal(2, after.Items.Count);
        Assert.Equal(28m, after.Total);
    }

    [Fact]
    public void Quote_items_cannot_be_modified_through_the_returned_list()
    {
        var items = CartWith(Small).GetQuote().Items;

        Assert.False(items is ICollection<LineItem> { IsReadOnly: false });
    }

    [Fact]
    public void Adding_a_null_parcel_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => new Cart().AddParcel(null!));
    }

    private static Cart CartWith(params Parcel[] parcels)
    {
        var cart = new Cart();
        foreach (var parcel in parcels)
        {
            cart.AddParcel(parcel);
        }

        return cart;
    }
}
