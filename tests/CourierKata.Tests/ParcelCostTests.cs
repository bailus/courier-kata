namespace CourierKata.Tests;

public class ParcelCostTests
{
    [Theory]
    [InlineData(5, ParcelSize.Small, 3)]
    [InlineData(20, ParcelSize.Medium, 8)]
    [InlineData(70, ParcelSize.Large, 15)]
    [InlineData(150, ParcelSize.ExtraLarge, 25)]
    public void Each_size_has_a_fixed_cost(int largestDimension, ParcelSize expectedSize, int expectedCost)
    {
        var cart = new Cart();
        cart.AddParcel(new Parcel(largestDimension, 1m, 1m));

        var item = Assert.IsType<ParcelLineItem>(Assert.Single(cart.GetQuote().Items));

        Assert.Equal(expectedSize, item.Size);
        Assert.Equal(expectedCost, item.Cost);
    }
}
