using static CourierKata.Tests.QuoteParcels;

namespace CourierKata.Tests;

public class DiscountTests
{
    [Fact]
    public void Four_small_parcels_get_small_parcel_mania()
    {
        var quote = QuoteFor(Times(4, Small));

        Assert.Equal([new DiscountLineItem(Discount.SmallParcelMania, Times(4, Small), 3m)], DiscountsIn(quote));
        Assert.Equal(9m, quote.Total);
    }

    [Fact]
    public void Three_medium_parcels_get_medium_parcel_mania()
    {
        var quote = QuoteFor(Times(3, Medium3Kg));

        Assert.Equal([new DiscountLineItem(Discount.MediumParcelMania, Times(3, Medium3Kg), 8m)], DiscountsIn(quote));
        Assert.Equal(16m, quote.Total);
    }

    [Fact]
    public void Five_parcels_of_the_same_size_get_mixed_parcel_mania()
    {
        var quote = QuoteFor(Times(5, Large));

        Assert.Equal([new DiscountLineItem(Discount.MixedParcelMania, Times(5, Large), 15m)], DiscountsIn(quote));
        Assert.Equal(60m, quote.Total);
    }

    public static TheoryData<Parcel[]> CartsWithoutADiscount => new()
    {
        Times(3, Small),
        Times(2, Medium3Kg),
        new[] { Small, Medium3Kg, Large, ExtraLarge },
        Concat(Times(3, Small), [Medium3Kg]),
        Times(4, Large),
        Times(4, ExtraLarge),
    };

    [Theory]
    [MemberData(nameof(CartsWithoutADiscount))]
    public void Carts_without_a_full_group_get_no_discount(Parcel[] parcels)
    {
        Assert.Empty(DiscountsIn(QuoteFor(parcels)));
    }

    [Fact]
    public void Cheapest_parcel_is_free_not_the_last_one_added()
    {
        Parcel[] parcels = [ExtraLarge, ExtraLarge, Medium3Kg, Large, Large];

        var quote = QuoteFor(parcels);

        Assert.Equal([new DiscountLineItem(Discount.MixedParcelMania, parcels, 8m)], DiscountsIn(quote));
        Assert.Equal(80m, quote.Total);
    }

    [Fact]
    public void Saving_includes_the_overweight_charge()
    {
        var quote = QuoteFor(Times(3, Medium4Kg));

        Assert.Equal([new DiscountLineItem(Discount.MediumParcelMania, Times(3, Medium4Kg), 10m)], DiscountsIn(quote));
        Assert.Equal(20m, quote.Total);
    }

    [Fact]
    public void Heavy_rate_parcels_join_by_their_size()
    {
        var quote = QuoteFor(Times(4, Small40Kg));

        Assert.All(
            quote.Items.OfType<ParcelLineItem>(),
            item => Assert.Equal(new ParcelLineItem(Small40Kg, ParcelSize.Small, ParcelRate.Heavy, 50m, 0m), item));
        Assert.Equal([new DiscountLineItem(Discount.SmallParcelMania, Times(4, Small40Kg), 50m)], DiscountsIn(quote));
        Assert.Equal(150m, quote.Total);
    }

    [Fact]
    public void A_heavy_rate_parcel_completes_a_group()
    {
        var parcels = Concat(Times(3, Small), [Small40Kg]);

        var quote = QuoteFor(parcels);

        Assert.Equal([new DiscountLineItem(Discount.SmallParcelMania, parcels, 3m)], DiscountsIn(quote));
        Assert.Equal(56m, quote.Total);
    }

    [Fact]
    public void Discount_is_listed_after_the_parcels_with_its_parcels_and_saving()
    {
        var parcels = Times(3, Medium3Kg);

        var quote = QuoteFor(parcels);

        Assert.Equal(
            [.. PricedOnTheirOwn(parcels), new DiscountLineItem(Discount.MediumParcelMania, parcels, 8m)],
            quote.Items);
        Assert.Equal(-8m, quote.Items[^1].Cost);
        Assert.Equal(16m, quote.Total);
    }

    [Fact]
    public void Parcels_in_a_discount_keep_the_order_they_were_added()
    {
        Parcel[] parcels = [ExtraLarge, ExtraLarge, Medium3Kg, Large, Large];

        var discount = Assert.Single(DiscountsIn(QuoteFor(parcels)));

        Assert.Equal(parcels, discount.Parcels);
    }

    [Fact]
    public void Discount_whose_first_parcel_was_added_first_comes_first()
    {
        var quote = QuoteFor(Concat([Medium3Kg], Times(4, Small), Times(2, Medium3Kg)));

        Assert.Equal(
            [
                new DiscountLineItem(Discount.MediumParcelMania, Times(3, Medium3Kg), 8m),
                new DiscountLineItem(Discount.SmallParcelMania, Times(4, Small), 3m),
            ],
            DiscountsIn(quote));
        Assert.Equal(25m, quote.Total);
    }

    [Fact]
    public void Discount_order_does_not_follow_the_size_of_the_saving()
    {
        var quote = QuoteFor(Concat(Times(3, Medium4Kg), Times(3, Medium3Kg)));

        Assert.Equal([10m, 8m], DiscountsIn(quote).Select(discount => discount.Saving));
    }

    [Fact]
    public void Parcels_in_a_discount_keep_their_cost()
    {
        var quote = QuoteFor(Times(3, Medium3Kg));

        Assert.Equal(
            Enumerable.Repeat(new ParcelLineItem(Medium3Kg, ParcelSize.Medium, ParcelRate.BySize, 8m, 0m), 3),
            quote.Items.OfType<ParcelLineItem>());
    }

    [Fact]
    public void Discount_parcels_cannot_be_modified_through_the_returned_list()
    {
        var discount = Assert.Single(DiscountsIn(QuoteFor(Times(4, Small))));

        Assert.False(discount.Parcels is ICollection<Parcel> { IsReadOnly: false });
    }
}
