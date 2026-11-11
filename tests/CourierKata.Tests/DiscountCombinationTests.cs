using static CourierKata.Tests.QuoteParcels;

namespace CourierKata.Tests;

public class DiscountCombinationTests
{
    [Fact]
    public void Parcels_are_grouped_with_others_of_similar_cost()
    {
        var parcels = Concat(Times(3, Medium3Kg), Times(3, Medium4Kg));

        var quote = QuoteFor(parcels);

        Assert.Equal(
            [
                .. PricedOnTheirOwn(parcels),
                new DiscountLineItem(Discount.MediumParcelMania, Times(3, Medium3Kg), 8m),
                new DiscountLineItem(Discount.MediumParcelMania, Times(3, Medium4Kg), 10m),
            ],
            quote.Items);
        Assert.Equal(36m, quote.Total);
    }

    [Fact]
    public void Seven_small_parcels_get_exactly_one_discount()
    {
        var quote = QuoteFor(Times(7, Small));

        Assert.Equal([new DiscountLineItem(Discount.SmallParcelMania, Times(4, Small), 3m)], DiscountsIn(quote));
        Assert.Equal(18m, quote.Total);
    }

    [Fact]
    public void The_same_parcel_added_four_times_counts_four_times()
    {
        var parcel = new Parcel(5m, 5m, 5m, 0.5m);

        var quote = QuoteFor(parcel, parcel, parcel, parcel);

        Assert.Equal([new DiscountLineItem(Discount.SmallParcelMania, Times(4, parcel), 3m)], DiscountsIn(quote));
    }

    [Fact]
    public void Mixed_parcel_mania_is_chosen_when_it_saves_more_than_a_size_discount()
    {
        var quote = QuoteFor(Concat([Medium5Kg], Times(2, Medium3Kg), Times(4, Large)));

        Assert.Equal(
            [new DiscountLineItem(Discount.MixedParcelMania, Concat([Medium5Kg], Times(4, Large)), 12m)],
            DiscountsIn(quote));
        Assert.Equal(76m, quote.Total);
    }

    [Fact]
    public void Equal_savings_prefer_small_parcel_mania_to_mixed()
    {
        var quote = QuoteFor(Times(5, Small));

        Assert.Equal([new DiscountLineItem(Discount.SmallParcelMania, Times(4, Small), 3m)], DiscountsIn(quote));
        Assert.Equal(12m, quote.Total);
    }

    [Fact]
    public void Equal_savings_prefer_two_size_discounts_to_one_mixed_discount()
    {
        var smalls = Concat(Times(3, Small5Kg), [Small]);
        var mediums = Concat(Times(2, Medium5Kg), [Medium3Kg]);

        var quote = QuoteFor(Concat(smalls, mediums));

        Assert.Equal(
            [
                new DiscountLineItem(Discount.SmallParcelMania, smalls, 3m),
                new DiscountLineItem(Discount.MediumParcelMania, mediums, 8m),
            ],
            DiscountsIn(quote));
        Assert.Equal(57m, quote.Total);
    }

    [Fact]
    public void A_large_order_is_quoted_without_trying_every_grouping()
    {
        var quote = QuoteFor(Times(1_000, Small));

        var discounts = DiscountsIn(quote);

        Assert.Equal(250, discounts.Count);
        Assert.All(discounts, discount => Assert.Equal(Discount.SmallParcelMania, discount.Discount));
        Assert.Equal(2_250m, quote.Total);
    }
}
