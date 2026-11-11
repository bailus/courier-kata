# Proposal

## Why

To reward customers who send several parcels in one order, the courier is introducing three discounts that each make one parcel in a group free. A quote has to find the combination of discounts that saves the most, show each discount on its own line, and apply speedy shipping to the discounted order.

## What Changes

- Three discounts, each making the cheapest parcel in a group free:
  - Small parcel mania: a group of 4 Small parcels.
  - Medium parcel mania: a group of 3 Medium parcels.
  - Mixed parcel mania: a group of any 5 parcels, whatever their sizes.
- A discount saves the whole cost of its cheapest parcel, including any overweight charge. "Every 4th parcel" means any group of 4, so the order parcels were added in doesn't matter.
- A parcel joins discounts by its size, whatever rate it is charged at. A 40 kg parcel measuring 5 x 5 x 5 cm is Small, so it can be part of Small parcel mania.
- Each parcel is in at most one discount. The quote applies the combination of discounts that saves the most in total. When several combinations save the same, it applies one with the fewest Mixed parcel mania discounts.
- Each discount applied is its own line item showing the discount, the parcels in its group and its saving. Its cost is minus the saving (for example -$8), so the total is still the sum of the line items. Discount items come after the parcel items and before speedy shipping, ordered by when each discount's first parcel was added.
- Parcel line items don't change when discounts apply.
- Speedy shipping applies after discounts: the speedy item costs the parcel costs less the discount savings, so speedy still doubles the order.
- **BREAKING**: `LineItem` gets a third kind, `DiscountLineItem`. Callers that match on every kind of line item must handle it.
- Record the new interpretations in `docs/assumptions.md`, and rewrite "Speedy charge equals the parcel subtotal", which left how speedy combines with discounts to this change.

Out of scope: other discounts, discount codes, and any change to how parcels are sized or priced.

## Capabilities

### New Capabilities
- `parcel-discounts`: discounts for sending several parcels in one order. Covers which groups of parcels qualify, which parcel is free, choosing the combination that saves the most, and how each discount appears on the quote.

### Modified Capabilities
- `parcel-pricing`: discount line items come between the parcel items and speedy shipping, and the speedy charge becomes the parcel costs less any discount savings.

## Impact

- Public API: new enum `Discount { SmallParcelMania, MediumParcelMania, MixedParcelMania }` and new `DiscountLineItem(Discount Discount, IReadOnlyList<Parcel> Parcels, decimal Saving) : LineItem`, whose `Cost` is `-Saving`. `Cart`, `Quote`, `ParcelLineItem` and `SpeedyShippingLineItem` keep their shapes.
- Internal: a new internal class chooses the discounts from the priced parcel line items. `Cart.GetQuote()` adds the discount items and works out speedy from the discounted subtotal. `ParcelPricing` doesn't change.
- Tests: no existing test has enough parcels to earn a discount, so none change. New tests cover each discount, choosing the best combination (including a check against an exhaustive search on small random carts), the discount line item, and speedy after discounts.
- Docs: new and rewritten entries in `docs/assumptions.md`.
