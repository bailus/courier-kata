# Tasks

## 1. Weight on parcels and split line-item costs (no price change)

- [x] 1.1 Change `ParcelLineItem` to `(Parcel, Size, BaseCost, OverweightCharge)`, with `Cost` worked out as `BaseCost + OverweightCharge` when read (see design.md, Risks). `ParcelPricing` sets `OverweightCharge = 0m` for now. Update every expected `new ParcelLineItem(...)` in the tests to pass `0m`. Add tests that `Cost` is the sum of the two parts, and that `item with { OverweightCharge = 4m }` gives the right `Cost`. Verify that `docker compose run --rm tests` passes with zero warnings.
- [x] 1.2 Write failing tests: a weight of 0 kg or -1 kg throws `ArgumentOutOfRangeException` naming `Weight`, and 0.5 kg is accepted. Add the required `Weight` parameter to `Parcel`, validated with `ThrowIfNegativeOrZero`. Give every existing `new Parcel(...)` in the tests a weight within its size's limit (for example `0.5m`), so no expected price changes. Verify with `docker compose run --rm tests`.
- [x] 1.3 Add these assumptions to `docs/assumptions.md`, then commit them with the code:
  - weight is required, positive and in `decimal` kilograms, with no upper limit
  - the overweight charge is shown separately, and is $0 within the limit

## 2. Overweight charge (test first, starting with the brief's examples)

- [x] 2.1 Write a failing test in `OverweightChargeTests`: a Small parcel weighing 1.2 kg has base $3, overweight $2, cost $5 and quote total $5. Add the weight limits (`LimitOf(size)`) and the $2-per-part-kg charge using `Math.Ceiling` in `ParcelPricing`. Verify with `docker compose run --rm tests`.
- [x] 2.2 Add tests for these cases, implementing anything still missing:
  - Medium 5 kg costs $12
  - Large 7 kg costs $17
  - XL 11 kg costs $27
  - each size exactly at its limit has an overweight charge of $0
  - a 5 x 5 x 5 cm parcel weighing 20 kg is still Small

  Verify with `docker compose run --rm tests`.
- [x] 2.3 Add a test that a Small 1.2 kg parcel with Speedy shipping gives a parcel item of $5, a speedy item of $5 and a total of $10. Verify with `docker compose run --rm tests`. No code change should be needed.
- [x] 2.4 Update `docs/assumptions.md`, then commit with the code:
  - add: at the limit pays nothing
  - add: part-kilograms round up
  - add: weight doesn't change size
  - add: Speedy doubles overweight charges too
  - update "Cheapest option is the smallest size that fits" with why weight doesn't need an option selector

## 3. Final checks

- [ ] 3.1 Check the public surface: it should be unchanged apart from `Parcel.Weight` and the `ParcelLineItem` shape. Check that every scenario in `specs/parcel-pricing/spec.md` has a matching test. Verify that `docker compose run --rm tests` passes with zero warnings.

## Workflow follow-up

- Archive the change once implementation is complete, keeping `openspec/specs/parcel-pricing/spec.md` in sync with what was built.
- Commit after archiving.
