# Proposal

## Why

Overweight charges for some goods were excessive. A very heavy parcel pays $2 for every kilogram over its size's small weight limit, so a 40 kg parcel costs $81–$85 whatever its dimensions. A new Heavy parcel rate caps that: a flat $50 for up to 50 kg, then $1 per kilogram over.

## What Changes

- New Heavy rate: base cost $50, weight limit 50 kg, and $1 for each kilogram over 50 kg, with any part of a kilogram counted as a whole one. A parcel at exactly 50 kg pays $50.
- Any parcel can use the Heavy rate, whatever its dimensions.
- Each parcel is charged at whichever rate is cheaper: its size rate (the size price plus $2 per kg over the size's limit) or the Heavy rate. When both cost the same, the size rate is used. For example, a 40 kg parcel costs $50 whether its dimensions are Small or XL.
- A parcel's size is still decided only by its dimensions. A 40 kg parcel measuring 5 x 5 x 5 cm is Small, and it's charged at the Heavy rate.
- **BREAKING**: A parcel line item shows which rate it was charged at (`BySize` or `Heavy`), next to its size, base cost and overweight charge.
- Speedy shipping still costs the sum of the parcel costs, so it doubles Heavy-rate costs too.
- Record the new interpretations in `docs/assumptions.md`, and replace the "cheapest option is the smallest size that fits" entry, which said a selector would come with a rule like this one.

Out of scope: discounts, and any change to how parcels are sized.

## Capabilities

### New Capabilities

None.

### Modified Capabilities
- `parcel-pricing`: adds the Heavy rate and choosing the cheaper rate for each parcel. Parcel line items show their rate. Updates the claim that the smallest size is always the cheapest option, and the scenario showing that weight doesn't change size.

## Impact

- Public API: new enum `ParcelRate { BySize, Heavy }`. `ParcelLineItem` changes from `(Parcel, Size, BaseCost, OverweightCharge)` to `(Parcel, Size, Rate, BaseCost, OverweightCharge)`.
- Internal: `ParcelPricing` works out both rates and picks the cheaper one. `Cart`, `Quote` and `SpeedyShippingLineItem` don't change.
- Tests: about 20 expected `new ParcelLineItem(...)` values gain `ParcelRate.BySize`. New tests cover the Heavy rate, choosing between the rates, and the tie case.
- Docs: new and updated entries in `docs/assumptions.md`.
