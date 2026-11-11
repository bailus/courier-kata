using static CourierKata.Tests.QuoteParcels;

namespace CourierKata.Tests;

public class DiscountPropertyTests
{
    /// <summary>Parcels with overweight charges, Heavy-rate costs and equal costs across sizes.</summary>
    private static readonly Parcel[] Palette =
    [
        Small,                       // Small $3
        new(5m, 5m, 5m, 1.5m),       // Small $5
        Small5Kg,                    // Small $11
        new(5m, 5m, 5m, 7m),         // Small $15
        Small40Kg,                   // Small, Heavy rate $50
        Medium3Kg,                   // Medium $8
        Medium4Kg,                   // Medium $10
        Medium5Kg,                   // Medium $12
        new(20m, 5m, 5m, 40m),       // Medium, Heavy rate $50
        Large,                       // Large $15
        new(70m, 5m, 5m, 7m),        // Large $17
        ExtraLarge,                  // XL $25
    ];

    private static readonly Dictionary<Discount, int> GroupSizes = new()
    {
        [Discount.SmallParcelMania] = 4,
        [Discount.MediumParcelMania] = 3,
        [Discount.MixedParcelMania] = 5,
    };

    [Fact]
    public void No_other_grouping_saves_more()
    {
        var random = new Random(20261111);

        for (var cart = 0; cart < 300; cart++)
        {
            var parcels = Enumerable.Range(0, random.Next(0, 11))
                .Select(_ => Palette[random.Next(Palette.Length)])
                .ToArray();

            var quote = QuoteFor(parcels);
            var priced = quote.Items.OfType<ParcelLineItem>().ToList();
            var discounts = DiscountsIn(quote);

            AssertValid(priced, discounts);

            var (bestSaving, fewestMixed) = BestByTryingEveryGrouping(priced);
            Assert.Equal(bestSaving, discounts.Sum(discount => discount.Saving));
            Assert.Equal(fewestMixed, discounts.Count(discount => discount.Discount == Discount.MixedParcelMania));
        }
    }

    private static void AssertValid(IReadOnlyList<ParcelLineItem> priced, IReadOnlyList<DiscountLineItem> discounts)
    {
        var itemFor = priced.DistinctBy(item => item.Parcel).ToDictionary(item => item.Parcel);

        foreach (var discount in discounts)
        {
            Assert.Equal(GroupSizes[discount.Discount], discount.Parcels.Count);
            Assert.All(discount.Parcels, parcel => Assert.True(CanJoin(discount.Discount, itemFor[parcel].Size)));
            Assert.Equal(discount.Parcels.Min(parcel => itemFor[parcel].Cost), discount.Saving);
        }

        var used = discounts.SelectMany(discount => discount.Parcels).CountBy(parcel => parcel);
        var added = priced.CountBy(item => item.Parcel).ToDictionary();
        Assert.All(used, entry => Assert.True(entry.Value <= added[entry.Key]));
    }

    private static bool CanJoin(Discount discount, ParcelSize size) => discount switch
    {
        Discount.SmallParcelMania => size == ParcelSize.Small,
        Discount.MediumParcelMania => size == ParcelSize.Medium,
        _ => true,
    };

    /// <summary>The best (saving, fewest Mixed discounts) over every way of grouping the parcels.</summary>
    private static (decimal Saving, int MixedCount) BestByTryingEveryGrouping(IReadOnlyList<ParcelLineItem> priced)
    {
        var memo = new Dictionary<int, (decimal, int)>();
        return Best((1 << priced.Count) - 1);

        (decimal Saving, int MixedCount) Best(int remaining)
        {
            if (remaining == 0)
            {
                return (0m, 0);
            }

            if (memo.TryGetValue(remaining, out var known))
            {
                return known;
            }

            var first = System.Numerics.BitOperations.TrailingZeroCount(remaining);
            var rest = remaining & ~(1 << first);

            // The first remaining parcel is left out of every discount...
            var best = Best(rest);

            // ...or joins a group with other remaining parcels.
            foreach (var (discount, size) in GroupSizes)
            {
                if (!CanJoin(discount, priced[first].Size))
                {
                    continue;
                }

                var candidates = Enumerable.Range(0, priced.Count)
                    .Where(i => (rest & (1 << i)) != 0 && CanJoin(discount, priced[i].Size))
                    .ToArray();

                foreach (var others in Combinations(candidates, size - 1))
                {
                    var group = others.Append(first).ToArray();
                    var after = Best(rest & ~group.Aggregate(0, (mask, i) => mask | (1 << i)));
                    var candidate = (
                        Saving: after.Saving + group.Min(i => priced[i].Cost),
                        MixedCount: after.MixedCount + (discount == Discount.MixedParcelMania ? 1 : 0));

                    if (candidate.Saving > best.Saving
                        || (candidate.Saving == best.Saving && candidate.MixedCount < best.MixedCount))
                    {
                        best = candidate;
                    }
                }
            }

            memo[remaining] = best;
            return best;
        }
    }

    private static IEnumerable<int[]> Combinations(int[] items, int count, int start = 0)
    {
        if (count == 0)
        {
            yield return [];
            yield break;
        }

        for (var i = start; i <= items.Length - count; i++)
        {
            foreach (var tail in Combinations(items, count - 1, i + 1))
            {
                yield return [items[i], .. tail];
            }
        }
    }
}
