# Proposal

## Why

Logistics improvements let us deliver parcels faster, and customers will pay more for that. Customers need a way to choose faster delivery for an order, and the quote must show what that choice costs.

## What Changes

- Add a shipping choice to the cart: Standard (the default) or Speedy. The customer can change it at any time before requesting a quote, including switching back to Standard.
- Speedy shipping doubles the cost of the order. It appears in the quote as its own line item, after the parcel line items, costing the sum of the parcel costs. The quote total is still the sum of all line items, so it doubles.
- Parcel line items keep their normal cost whether or not Speedy is selected.
- With Standard shipping, the quote has no speedy line item. With Speedy and an empty cart, the quote has a $0 speedy line item.
- **BREAKING**: The quote's line items are no longer all parcel items. `LineItem` becomes a base type with a parcel item kind and a speedy shipping item kind. Callers that read a line item's parcel or size must first check that it is a parcel item.
- Record the new interpretations in `docs/assumptions.md`.

Out of scope: weight, discounts, other shipping speeds, and how speedy shipping combines with rules added in later steps.

## Capabilities

### New Capabilities

None.

### Modified Capabilities
- `parcel-pricing`: the quote can include a speedy shipping line item after the parcel items. A new requirement covers choosing the shipping option on the cart, and another covers how speedy shipping is priced. The snapshot requirement gains a scenario for changing the shipping option after a quote.

## Impact

- Public API: adds a `Shipping` enum and a `Cart.Shipping` property. `LineItem` becomes an abstract base type with `ParcelLineItem` and `SpeedyShippingLineItem` under it. `Quote`'s shape is unchanged.
- Internal: `ParcelPricing` returns parcel line items. `Cart.GetQuote` appends the speedy item when Speedy is selected.
- Tests: existing tests that read `Size` or `Parcel` from a line item change to use `ParcelLineItem`.
- Docs: new entries in `docs/assumptions.md`.
