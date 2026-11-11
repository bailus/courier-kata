# Tasks

## 1. Rate on parcel line items (no price change)

- [x] 1.1 Add the public enum `ParcelRate { BySize, Heavy }` with XML doc comments. Change `ParcelLineItem` to `(Parcel, Size, Rate, BaseCost, OverweightCharge)`, with `ParcelPricing` always setting `ParcelRate.BySize` for now. Update every expected `new ParcelLineItem(...)` in the tests to pass `ParcelRate.BySize`. Add an assertion that a 0.5 kg Small parcel's line item has `Rate == ParcelRate.BySize`. Verify that `docker compose run --rm tests` passes with zero warnings.
- [x] 1.2 Add to `docs/assumptions.md`, then commit with the code: the line item shows the rate separately from the size, and `Size` stays decided by dimensions alone.

## 2. Heavy rate and choosing the cheaper rate (test first, starting with the user's examples)

- [ ] 2.1 Write failing tests in `HeavyRateTests`: a 5 x 5 x 5 cm parcel weighing 40 kg and a 150 x 5 x 5 cm parcel weighing 40 kg each have a quote total of $50, keep their size (Small, XL), and are charged at `ParcelRate.Heavy` with a base cost of $50 and an overweight charge of $0. Make the overweight helper in `ParcelPricing` take the limit and per-kg rate, add the Heavy constants ($50, 50 kg, $1/kg), and pick Heavy only when it is strictly cheaper. Verify with `docker compose run --rm tests`.
- [ ] 2.2 Add tests for these spec scenarios, implementing anything still missing:
  - 60 kg costs $50 + $10
  - 50.2 kg costs $50 + $1
  - exactly 50 kg costs $50 + $0
  - 24 kg Small is charged at the size rate, $3 + $46 = $49
  - 24.1 kg Small is charged at the Heavy rate, $50
  - 24 kg Medium is a tie at $50 and is charged at the size rate, $8 + $42
  - 20 kg Small is still Small and charged at the size rate

  Verify with `docker compose run --rm tests`.
- [ ] 2.3 Add a test that a 40 kg 5 x 5 x 5 cm parcel with Speedy shipping gives a parcel item of $50, a speedy item of $50 and a total of $100. No code change should be needed. Verify with `docker compose run --rm tests`.
- [ ] 2.4 Update `docs/assumptions.md`, then commit with the code:
  - add: any parcel can use the Heavy rate, whatever its dimensions
  - add: the cheaper rate is chosen, and a tie goes to the size rate
  - add: exactly 50 kg pays nothing extra, and part-kilograms over 50 kg round up at $1 each
  - add: speedy shipping doubles Heavy-rate costs too
  - rewrite "Cheapest option is the smallest size that fits": it's still true among sizes, but the size rate is now compared with the Heavy rate

## 3. Final checks

- [ ] 3.1 Check the public surface: it should be unchanged apart from the new `ParcelRate` and the `Rate` property on `ParcelLineItem`. Check that every scenario in `specs/parcel-pricing/spec.md` has a matching test. Verify that `docker compose run --rm tests` passes with zero warnings.

## Workflow follow-up

- Archive the change once implementation is complete, keeping `openspec/specs/parcel-pricing/spec.md` in sync with what was built.
- Commit after archiving.
