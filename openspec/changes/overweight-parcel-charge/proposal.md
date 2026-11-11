# Proposal

## Why

Delivery drivers report that customers are taking advantage of pricing that only looks at dimensions, by sending small parcels that are very heavy. Each parcel size now has a weight limit, and parcels over it pay an extra charge.

## What Changes

- **BREAKING**: A parcel has a weight in kilograms as well as its three dimensions. The weight is required and must be greater than zero.
- Each size has a weight limit: Small 1 kg, Medium 3 kg, Large 6 kg, XL 10 kg.
- A parcel heavier than its size's limit pays $2 for each kilogram over the limit, and any part of a kilogram counts as a whole one. For example, 0.2 kg over costs $2. A parcel exactly at its limit pays nothing extra.
- **BREAKING**: A parcel line item shows its base cost (the size price) and its overweight charge separately. Its cost is the sum of the two. Parcels within their limit show an overweight charge of $0.
- Speedy shipping still costs the sum of the parcel costs, so it now doubles overweight charges too. For example, a 1.2 kg Small parcel with Speedy costs $5 + $5 = $10.
- Existing tests give their parcels weights within the limits, so their expected prices don't change.
- Record the new interpretations in `docs/assumptions.md`, and update the "cheapest option" entry.

Out of scope: choosing between pricing options, discounts, and any other change to how parcels are sized.

## Capabilities

### New Capabilities

None.

### Modified Capabilities
- `parcel-pricing`: parcels gain a validated weight. Each size gains a weight limit and an overweight charge. Parcel line items show the base cost and overweight charge. Speedy shipping gains a scenario that shows it doubles overweight charges.

## Impact

- Public API: `Parcel` gains a required `Weight` (`decimal`, kg). `ParcelLineItem` changes from `(Parcel, Size, Cost)` to `(Parcel, Size, BaseCost, OverweightCharge)`, and `Cost` is worked out from those two.
- Internal: `ParcelPricing` gains weight limits and the overweight calculation. `Cart` and `SpeedyShippingLineItem` don't change.
- Tests: every `new Parcel(...)` gains a weight within its size's limit. Every `new ParcelLineItem(...)` in expected values gains a `0m` overweight charge. New tests cover the overweight examples and weight validation.
- Docs: new and updated entries in `docs/assumptions.md`.
