namespace CourierKata.Tests;

public class DiscountLineItemTests
{
    private static readonly Parcel Small = new(5m, 5m, 5m, 0.5m);
    private static readonly Parcel Medium = new(20m, 5m, 5m, 0.5m);

    [Fact]
    public void Cost_is_minus_the_saving()
    {
        var item = new DiscountLineItem(Discount.MediumParcelMania, [Medium, Medium, Medium], 8m);

        Assert.Equal(-8m, item.Cost);
    }

    [Fact]
    public void Items_with_the_same_discount_parcels_and_saving_are_equal()
    {
        var first = new DiscountLineItem(Discount.SmallParcelMania, [Small, Small, Small, Medium], 3m);
        var second = new DiscountLineItem(Discount.SmallParcelMania, new List<Parcel> { Small, Small, Small, Medium }, 3m);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    public static TheoryData<DiscountLineItem> DifferentItems => new()
    {
        new DiscountLineItem(Discount.MixedParcelMania, [Small, Small, Small, Medium], 3m),
        new DiscountLineItem(Discount.SmallParcelMania, [Small, Small, Small, Small], 3m),
        new DiscountLineItem(Discount.SmallParcelMania, [Medium, Small, Small, Small], 3m),
        new DiscountLineItem(Discount.SmallParcelMania, [Small, Small, Small], 3m),
        new DiscountLineItem(Discount.SmallParcelMania, [Small, Small, Small, Medium], 5m),
    };

    [Theory]
    [MemberData(nameof(DifferentItems))]
    public void Items_that_differ_in_discount_parcels_parcel_order_or_saving_are_not_equal(DiscountLineItem other)
    {
        var item = new DiscountLineItem(Discount.SmallParcelMania, [Small, Small, Small, Medium], 3m);

        Assert.NotEqual(item, other);
    }
}
