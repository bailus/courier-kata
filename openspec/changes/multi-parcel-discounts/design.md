# Design

## Context

See proposal.md for the motivation and `specs/parcel-discounts/spec.md` for the rules. Today `Cart.GetQuote()` prices each parcel into a `ParcelLineItem` with the internal `ParcelPricing.Price`. When Speedy is selected, it appends a `SpeedyShippingLineItem` costing the sum of the parcel costs, then totals the items. `LineItem` is a closed hierarchy of two kinds, kept closed by an `internal abstract` member. A `ParcelLineItem` carries both the parcel's dimensional `Size` and the `Rate` it is charged at, and the heavy-parcel change left it to this change to decide which one discounts use. No existing test cart has enough parcels to earn a discount.

## Goals / Non-Goals

**Goals:**
- The quote always applies a best combination of discounts, and finding it takes time that grows linearly with the number of parcels, so large carts stay fast.
- Discount selection lives in one internal class that works on the priced parcel line items, so it uses exactly the sizes and costs the quote shows.
- The same cart always gets the same discounts.

**Non-Goals:**
- A general promotions engine, a discount interface or configurable discounts. There are three fixed discounts.
- Marking which parcel in a group is the free one. It's always the cheapest, and its cost is the saving.
- Taking the shipping option into account when choosing discounts. Speedy doubles every amount, so it can't change which combination is best.

## Decisions

### A third line-item kind for discounts
```
public enum Discount { SmallParcelMania, MediumParcelMania, MixedParcelMania }

public sealed record DiscountLineItem(Discount Discount, IReadOnlyList<Parcel> Parcels, decimal Saving) : LineItem
{
    public override decimal Cost => -Saving;
}
```
- `Saving` is positive and `Cost` is its negative, so `Quote.Total` stays the sum of `Items`, and the brief's "-$8" is `Cost == -8m`. `Cost` is worked out when it's read, like `ParcelLineItem.Cost`, so the two can't disagree.
- `Parcels` holds the parcels in the group, in cart order. The brief describes a discount by the parcels it includes, and each parcel's cost is already on its own line item.
- The enum is named `Discount`, the same as the property, as `Cart.Shipping` does. Its values use the brief's names.
- `LineItem`'s doc comment lists all three kinds. `DiscountLineItem` implements `SealHierarchy()`, so the set stays closed.
- Alternatives considered:
  - One combined discount line for the whole order. The brief's example lists two discounts separately.
  - Holding the group's `ParcelLineItem`s. They would show each parcel's cost next to the saving, but they repeat the parcel lines and make expected values in tests much longer.
  - The parcels' positions in the cart. Unambiguous when a parcel is added twice, but it's a loose primitive that callers have to index back into `Items` with.
  - A negative `Cost` alone. A named positive `Saving` reads like the brief ("save $8").

### Discount line items compare their parcels by value
- A record compares a list member by reference, so two discount items with the same parcels wouldn't be equal, and `Assert.Equal` on `quote.Items` would fail. `DiscountLineItem` declares `Equals(DiscountLineItem? other)`, comparing `Discount`, `Saving` and `Parcels` item by item, and overrides `GetHashCode` to match. Defining `Equals` without `GetHashCode` is a compiler warning, which is an error here.
- The `Parcels` list it's given is read-only, like `Quote.Items`, so a quote can't be changed through it.
- Alternative considered: a collection type with value equality. The BCL has none (`ImmutableArray<T>` also compares by reference), and a custom wrapper would add public surface for one property.

### Parcels join discounts by their size (the user chose this)
- Small parcel mania takes parcels whose `Size` is Small, Medium parcel mania those whose `Size` is Medium, and Mixed parcel mania any size. `Rate` plays no part.
- "Small" keeps the one meaning it has on the parcel line item, and weight still never changes size. It also never saves the customer less than limiting Heavy-rate parcels to Mixed would.
- In practice a Heavy-rate parcel mostly helps a group form: at $50 or more it's rarely the cheapest. A group of only Heavy-rate parcels frees one of them, so four 40 kg parcels measuring 5 x 5 x 5 cm save $50.
- Alternative considered: Heavy-rate parcels join only Mixed parcel mania, treating Heavy as its own parcel type as the original brief did.

### An exact search finds the best combination
Applying discounts in a fixed order doesn't always find the best saving:
```
Mixed first:  Medium [10 10 10 8 8 8]
              Mixed {10,10,10,8,8} saves $8, one $8 left over          -> $8
              best: Medium {10,10,10} + Medium {8,8,8}                 -> $18

Size first:   Medium [12 8 8] + Large [15 15 15 15]
              Medium {12,8,8} saves $8, four Larges can't make Mixed   -> $8
              best: Mixed {15,15,15,15,12}                             -> $12
```
Instead, the internal static class `DiscountSelection` takes the priced parcel line items in cart order and returns the discount line items. It walks the parcels once, most expensive first, keeping the best result for each possible state:
```
order  = parcels by cost, most expensive first; equal costs in cart order
state  = members so far in the open group of each discount:
         Small 0..3  x  Medium 0..2  x  Mixed 0..4      (60 states, a fixed-size array)
start  = every group empty, saving $0
for each parcel in order, from every reachable state:
    skip it                                 (it's in no discount)
    for each discount it can join:
        open group has fewer than size-1 members -> it joins and pays
        open group has size-1 members            -> it joins as the free parcel:
                                                    the group closes and the saving
                                                    grows by its cost
    each new state keeps its best value; on a tie it keeps the first one found
answer = the best final state; rebuild its closed groups from what the state remembers
```
- A value is better when it saves more in total, then when it has fewer Mixed parcel mania discounts (see "Equal savings prefer size discounts" in the spec).
- Why it's exact:
  - Within one discount, regrouping the same parcels into runs that are consecutive by cost never saves less. Sorted most expensive first, the j-th biggest saving can't be more than the cost of the (j x size)-th parcel, and consecutive runs save exactly those amounts. So some best combination never has more than one group of each discount open at a time during the walk, and that's all the state needs to track. The brief's 6-Medium example is this rule.
  - Regrouping within one discount doesn't change how many groups of each discount there are, so the fewer-Mixed preference survives it too.
  - The walk goes from most to least expensive, so the parcel that closes a group is its cheapest, which is the free one.
- Cost: one sort, then at most 60 states x 3 moves per parcel (skip, its size's discount, Mixed). A cart of 1,000 parcels is quick.
- Parcels left in a group that never closes are in no discount.
- Each state remembers how it was reached (for example the groups closed so far, in a list that's shared rather than copied), so the winner can be rebuilt without a second search.
- Output: one `DiscountLineItem` per closed group, with its parcels in cart order and a saving equal to its free parcel's cost. Items are ordered by each group's earliest cart position.
- Some ties remain after the fewer-Mixed rule, when parcels of equal cost could swap groups. These are settled by the fixed order of the walk, so the same cart always gets the same discounts. The spec doesn't say which of several equally priced parcels is used, and tests use identical parcels wherever it would show.
- Alternatives considered:
  - Applying discounts in a fixed order (size discounts first, or Mixed first). Shown to be wrong above.
  - Repeatedly taking the single group that saves most. No counterexample was found, but there's no proof either, and the requirement is "every time".
  - Trying every grouping. It's exact, but the number of groupings explodes with cart size, and carts have no size limit. It's used as the test oracle instead.

### The cart assembles the quote
```
parcelItems   = parcels.Select(ParcelPricing.Price)
discountItems = DiscountSelection.Choose(parcelItems)
items         = parcelItems, then discountItems
if Speedy:      items += SpeedyShippingLineItem(items.Sum(Cost))   // parcels less savings
return          Quote(items, items.Sum(Cost))
```
- Speedy now costs the sum of every item before it. That's the parcel costs less the savings, which is the brief's "speedy shipping applies after discounts", and the total is still double the discounted order.
- Selection doesn't need the shipping option, because Speedy can't change which combination is best.
- `ParcelPricing` doesn't change.

### Tests
- Test-first, starting with the brief's example: three 3 kg Medium parcels and three 4 kg Medium parcels give Medium parcel mania items saving $8 and then $10, and a total of $36.
- Each spec scenario gets a test, with expected discount items written out in full, for example `new DiscountLineItem(Discount.MediumParcelMania, [medium3Kg, medium3Kg, medium3Kg], 8m)`.
- A property test checks "every time". It quotes a few hundred seeded random carts of up to 10 parcels, drawn from Small, Medium, Large and XL parcels at various weights, including overweight and Heavy-rate ones. For each cart, the discounts must be valid: each group has the right number of parcels that can join it, its saving is its cheapest parcel's cost, and no parcel is used more often than it was added. Their total saving and number of Mixed discounts must also match a test-only search over every grouping.
- A cart of 1,000 Small parcels gets 250 Small parcel mania discounts, which shows the search doesn't blow up.
- Test classes: `DiscountLineItemTests` for the record, `DiscountTests` for the discount rules and how they're listed, `DiscountCombinationTests` for choosing the best combination, and new cases in `SpeedyShippingTests`.

### Assumptions log
New entries in `docs/assumptions.md`:
- "Every Nth parcel" means any group of N; the order the parcels were added in doesn't matter.
- Mixed parcel mania takes any 5 parcels, including 5 of the same size.
- A discount saves the free parcel's whole cost, including any overweight charge, at whichever rate it's charged.
- Heavy-rate parcels join discounts by their size.
- A parcel added more than once counts once for each time it was added.
- Each discount is its own line item, listing its parcels in cart order, with a positive saving and a negative cost. Discount items are ordered by their first parcel and come between the parcel items and speedy shipping.
- When savings are equal, the combination with fewer Mixed parcel mania discounts is chosen.

"Speedy charge equals the parcel subtotal" is rewritten: speedy now costs the parcel subtotal less any discount savings.

## Risks / Trade-offs

- [Breaking: callers that match every kind of `LineItem` need a `DiscountLineItem` case] → The only caller is the test project. No existing test earns a discount, so none change.
- [The search is harder to read than a greedy loop] → It's kept in one internal class with a comment on why it's exact, and it's checked against an exhaustive search on random carts.
- [Hand-written `Equals` and `GetHashCode` can fall out of step with the record's members] → Unit tests cover equality and hashing. Any member added later has to be added to both.
- [Which of several equally priced parcels is used follows the walk order, not a rule a customer would recognise] → The spec pins the saving and the discount kinds, and the choice is deterministic.
- [Mixed parcel mania for five parcels of one size may surprise] → The brief says "every 5th parcel" with no size condition. It's recorded as an assumption.
- [A discount line item doesn't say which parcel is free] → It's always the cheapest, and the saving is its cost. A property for it could be added later without changing selection.
