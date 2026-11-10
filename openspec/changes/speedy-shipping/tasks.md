# Tasks

## 1. Split line items into a closed hierarchy (refactor, no behaviour change)

- [ ] 1.1 Make `LineItem` an abstract record with `Cost` and a `private protected` constructor, add `sealed record ParcelLineItem(Parcel, ParcelSize, Cost) : LineItem`, and make `ParcelPricing.Price` return `ParcelLineItem`. Update the existing tests (`CartTests`, `ParcelCostTests`, `ParcelSizeTests`) to use `ParcelLineItem` where they read `Size` or `Parcel`. Verify that `docker compose run --rm tests` passes with no other test changes and zero warnings.
- [ ] 1.2 Commit the refactor.

## 2. Speedy shipping pricing (test first, starting with the brief's example)

- [ ] 2.1 Write a failing test: one parcel of each size with `cart.Shipping = Shipping.Speedy` gives parcel items of $3, $8, $15 and $25, then a `SpeedyShippingLineItem` of $51, and a total of $102. Add the `Shipping` enum, the `Cart.Shipping` property (default `Standard`), `SpeedyShippingLineItem`, and the speedy item in `Cart.GetQuote()` to make it pass. Verify with `docker compose run --rm tests`.
- [ ] 2.2 Add tests for these cases, implementing anything still missing:
  - standard shipping has no speedy item (Small only: total $3)
  - parcel items are equal under Standard and Speedy
  - the speedy item comes last (Medium, Small with Speedy: $8, $3, speedy $11)
  - an empty cart with Speedy has a single $0 speedy item and a $0 total

  Verify with `docker compose run --rm tests`.
- [ ] 2.3 Add these assumptions to `docs/assumptions.md`:
  - the speedy charge equals the parcel subtotal
  - the speedy item comes last
  - standard shipping shows no speedy item
  - an empty cart with Speedy shows a $0 speedy item

  Commit them with the code.

## 3. Shipping option on the cart

- [ ] 3.1 Add tests for these cases, implementing anything still missing:
  - a new cart defaults to `Standard`
  - switching from Speedy back to Standard removes the speedy item
  - selecting Speedy before adding parcels (Small and XL: speedy $28, total $56)
  - changing shipping after a quote leaves the earlier quote unchanged

  Verify with `docker compose run --rm tests`.
- [ ] 3.2 Write a failing test that setting `(Shipping)42` throws `ArgumentOutOfRangeException` and leaves `Shipping` unchanged. Implement the `Enum.IsDefined` guard in the setter. Verify with `docker compose run --rm tests`.
- [ ] 3.3 Add assumptions for choosing shipping on the cart (it can be changed back) and for rejecting undefined `Shipping` values. Commit them with the code.

## 4. Final checks

- [ ] 4.1 Check the public surface: it should be only `Cart`, `Parcel`, `ParcelSize`, `Shipping`, `LineItem`, `ParcelLineItem`, `SpeedyShippingLineItem` and `Quote`. Check that every scenario in `specs/parcel-pricing/spec.md` has a matching test. Verify that `docker compose run --rm tests` passes with zero warnings.

## Workflow follow-up

- Archive the change once implementation is complete, keeping `openspec/specs/parcel-pricing/spec.md` in sync with what was built.
- Commit after archiving.
