namespace CourierKata.Tests;

public class ParcelLineItemTests
{
    private static readonly Parcel Small = new(5m, 5m, 5m, 0.5m);

    [Fact]
    public void Cost_is_the_base_cost_plus_the_overweight_charge()
    {
        var item = new ParcelLineItem(Small, ParcelSize.Small, ParcelRate.BySize, 3m, 2m);

        Assert.Equal(5m, item.Cost);
    }

    [Fact]
    public void Cost_stays_consistent_when_a_copy_changes_the_overweight_charge()
    {
        var item = new ParcelLineItem(Small, ParcelSize.Small, ParcelRate.BySize, 3m, 0m);

        var heavier = item with { OverweightCharge = 4m };

        Assert.Equal(7m, heavier.Cost);
    }
}
