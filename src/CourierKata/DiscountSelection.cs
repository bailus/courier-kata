using System.Collections.Immutable;

namespace CourierKata;

/// <summary>Chooses the combination of multi-parcel discounts that saves the most.</summary>
/// <remarks>
/// <para>
/// The parcels are walked once, most expensive first. Every group is built in that order, so the
/// parcel that completes a group is its cheapest, which is the free one.
/// </para>
/// <para>
/// Only one group of each discount needs to be open at a time. Within one discount, regrouping the
/// same parcels into runs that are consecutive by cost never saves less: the j-th biggest saving
/// can't be more than the cost of the (j x group size)-th most expensive parcel, and consecutive
/// runs save exactly those amounts. Regrouping doesn't change how many groups there are, so it
/// keeps the preference for fewer Mixed discounts too. So the walk only has to track how many
/// parcels are in the open group of each discount: 4 x 3 x 5 = 60 states, each keeping the best
/// path that reaches it. That makes the search exact and linear in the number of parcels.
/// </para>
/// </remarks>
internal static class DiscountSelection
{
    private static readonly Discount[] Discounts =
        [Discount.SmallParcelMania, Discount.MediumParcelMania, Discount.MixedParcelMania];

    private static readonly int[] GroupSizes = [4, 3, 5];

    private static readonly int StateCount = GroupSizes.Aggregate(1, (count, size) => count * size);

    /// <summary>Returns one line item per discount applied, ordered by each discount's first parcel.</summary>
    /// <param name="parcels">The priced parcels, in the order they were added to the cart.</param>
    public static IReadOnlyList<DiscountLineItem> Choose(IReadOnlyList<ParcelLineItem> parcels)
    {
        var walkOrder = Enumerable.Range(0, parcels.Count)
            .OrderByDescending(position => parcels[position].Cost)
            .ThenBy(position => position);

        var paths = new Path?[StateCount];
        paths[0] = Path.Start;

        foreach (var position in walkOrder)
        {
            var parcel = parcels[position];
            var next = new Path?[StateCount];

            for (var state = 0; state < StateCount; state++)
            {
                if (paths[state] is not { } path)
                {
                    continue;
                }

                Keep(next, state, path);

                for (var d = 0; d < Discounts.Length; d++)
                {
                    if (CanJoin(Discounts[d], parcel.Size))
                    {
                        var (nextState, nextPath) = Join(state, path, d, position, parcel.Cost);
                        Keep(next, nextState, nextPath);
                    }
                }
            }

            paths = next;
        }

        var best = paths.OfType<Path>().Aggregate((a, b) => IsBetter(b, a) ? b : a);

        return best.Closed
            .Select(group => (group, members: group.Members.Order().ToList()))
            .OrderBy(entry => entry.members[0])
            .Select(entry => new DiscountLineItem(
                entry.group.Discount,
                entry.members.Select(position => parcels[position].Parcel).ToList().AsReadOnly(),
                entry.group.Saving))
            .ToList();
    }

    private static bool CanJoin(Discount discount, ParcelSize size) => discount switch
    {
        Discount.SmallParcelMania => size == ParcelSize.Small,
        Discount.MediumParcelMania => size == ParcelSize.Medium,
        Discount.MixedParcelMania => true,
        _ => throw new ArgumentOutOfRangeException(nameof(discount), discount, "Unknown discount."),
    };

    /// <summary>Adds the parcel to the open group of discount <paramref name="d"/>, closing it when it's full.</summary>
    private static (int State, Path Path) Join(int state, Path path, int d, int position, decimal cost)
    {
        var counts = Decode(state);
        var members = path.Open[d].Push(position);

        if (counts[d] < GroupSizes[d] - 1)
        {
            counts[d]++;
            return (Encode(counts), path with { Open = path.Open.SetItem(d, members) });
        }

        counts[d] = 0;
        return (Encode(counts), path with
        {
            Saving = path.Saving + cost,
            MixedCount = path.MixedCount + (Discounts[d] == Discount.MixedParcelMania ? 1 : 0),
            Open = path.Open.SetItem(d, []),
            Closed = path.Closed.Push(new Group(Discounts[d], members, cost)),
        });
    }

    /// <summary>Keeps the candidate unless the state already has a path at least as good.</summary>
    private static void Keep(Path?[] paths, int state, Path candidate)
    {
        if (paths[state] is not { } current || IsBetter(candidate, current))
        {
            paths[state] = candidate;
        }
    }

    /// <summary>More saving is better; on equal savings, fewer Mixed discounts is better.</summary>
    private static bool IsBetter(Path candidate, Path current) =>
        candidate.Saving > current.Saving
        || (candidate.Saving == current.Saving && candidate.MixedCount < current.MixedCount);

    private static int[] Decode(int state)
    {
        var counts = new int[GroupSizes.Length];
        for (var d = GroupSizes.Length - 1; d >= 0; d--)
        {
            counts[d] = state % GroupSizes[d];
            state /= GroupSizes[d];
        }

        return counts;
    }

    private static int Encode(int[] counts) =>
        counts.Select((count, d) => (count, d)).Aggregate(0, (state, entry) => (state * GroupSizes[entry.d]) + entry.count);

    /// <summary>A completed group: its discount, the cart positions of its parcels, and its saving.</summary>
    private sealed record Group(Discount Discount, ImmutableStack<int> Members, decimal Saving);

    /// <summary>The best way found so far to reach a state: its saving, its groups so far and its open groups.</summary>
    private sealed record Path(
        decimal Saving,
        int MixedCount,
        ImmutableArray<ImmutableStack<int>> Open,
        ImmutableStack<Group> Closed)
    {
        public static readonly Path Start = new(0m, 0, [.. Discounts.Select(_ => ImmutableStack<int>.Empty)], []);
    }
}
