# Tasks

## 1. Discount line item (no behaviour change)

- [x] 1.1 Write failing tests in `DiscountLineItemTests`:
  - `Cost` is minus `Saving` (a saving of $8 costs -$8)
  - two items with the same discount, equal parcels in the same order and the same saving are equal and have equal hash codes
  - items that differ in discount, parcels, parcel order or saving are not equal

  Add the public enum `Discount { SmallParcelMania, MediumParcelMania, MixedParcelMania }` and `sealed record DiscountLineItem(Discount Discount, IReadOnlyList<Parcel> Parcels, decimal Saving) : LineItem`, with XML doc comments, `Cost => -Saving`, `SealHierarchy()`, and `Equals`/`GetHashCode` that compare `Parcels` item by item (see design.md). Update `LineItem`'s doc comment to list all three kinds. Verify that `docker compose run --rm tests` passes with zero warnings.
- [x] 1.2 Commit the enum, the record and their tests, and verify the working tree is clean afterwards.

## 2. Choosing discounts (test first, starting with the brief's example)

- [x] 2.1 Write a failing test in `DiscountCombinationTests` for "Parcels are grouped with others of similar cost": three Medium parcels weighing 3 kg, then three weighing 4 kg, give the six unchanged parcel items, then a Medium parcel mania item of the three 3 kg parcels saving $8, then one of the three 4 kg parcels saving $10, and a total of $36. Add the internal static `DiscountSelection.Choose` as described in design.md: parcels sorted most expensive first, the 60-state walk, more saving and then fewer Mixed discounts winning, groups rebuilt with their parcels in cart order in a read-only list, and items ordered by each group's first parcel. Call it from `Cart.GetQuote()` so the discount items come after the parcel items, leaving the speedy charge as it is for now. Verify with `docker compose run --rm tests`.
- [x] 2.2 Add tests in `DiscountTests` for the scenarios under "Each discount needs a full group of parcels", "The cheapest parcel in a group is free" and "A parcel joins discounts by its size", implementing anything still missing:
  - four Small parcels at $3 give Small parcel mania of all four, saving $3, total $9
  - three Medium parcels weighing 3 kg give Medium parcel mania saving $8, total $16
  - five Large parcels give Mixed parcel mania saving $15, total $60
  - carts of three Small parcels, of two Medium parcels, and of one parcel of each size have no discount item
  - two XL, a Medium and two Large parcels, added in that order, give Mixed parcel mania of all five, saving $8, total $80
  - three Medium parcels weighing 4 kg give Medium parcel mania saving $10, total $20
  - four 5 x 5 x 5 cm parcels weighing 40 kg are each Small at the Heavy rate, and give Small parcel mania saving $50, total $150
  - three Small parcels at $3 and one 5 x 5 x 5 cm parcel weighing 40 kg give Small parcel mania of all four, saving $3, total $56
  - three Small parcels and a Medium parcel have no discount item
  - carts of four Large parcels and of four XL parcels have no discount item

  Verify with `docker compose run --rm tests`.
- [x] 2.3 Add tests in `DiscountCombinationTests` for the scenarios under "Each parcel is in at most one discount", "The combination that saves the most is applied" and "Equal savings prefer size discounts", implementing anything still missing:
  - seven Small parcels at $3 give exactly one discount, Small parcel mania saving $3, total $18
  - the same Small parcel added four times gives Small parcel mania of that parcel four times, saving $3
  - a Medium parcel weighing 5 kg, two weighing 3 kg and four Large parcels give only Mixed parcel mania of the 5 kg Medium and the four Large parcels, saving $12, total $76
  - five Small parcels at $3 give Small parcel mania of four of them, no Mixed discount, total $12
  - three Small parcels weighing 5 kg, a Small parcel at $3, two Medium parcels weighing 5 kg and a Medium parcel weighing 3 kg give Small parcel mania saving $3 and Medium parcel mania saving $8, no Mixed discount, total $57
  - 1,000 Small parcels at $3 give 250 Small parcel mania discounts and a total of $2,250

  Verify with `docker compose run --rm tests`.
- [x] 2.4 Add a property test in `DiscountCombinationTests` for "No other grouping saves more". Write a test-only exhaustive search over every way of grouping a cart's parcels. Quote a few hundred carts of 0 to 10 parcels from a seeded `Random`. Draw the parcels from a fixed set that includes overweight parcels, Heavy-rate parcels and equal costs across sizes, for example Small at $3, $5, $11, $15 and $50 (Heavy), Medium at $8, $10, $12 and $50 (Heavy), Large at $15 and $17, and XL at $25. For each cart, check that every discount is valid: it has the right number of parcels that can join it, its saving is its cheapest parcel's cost, and no parcel is used more often than it was added. Also check that the total saving and the number of Mixed discounts match the exhaustive search. Verify with `docker compose run --rm tests`.
- [x] 2.5 Add tests in `DiscountTests` for the scenarios under "Each discount is listed as its own line item", "Discount line items are ordered by their first parcel" and "Discounts don't change parcel line items", implementing anything still missing:
  - three Medium parcels at $8 give three parcel items, then a Medium parcel mania item of the three parcels with a saving of $8 and a cost of -$8, total $16
  - two XL, a Medium and two Large parcels, added in that order, give a Mixed parcel mania item listing XL, XL, Medium, Large and Large in that order
  - a Medium parcel at $8, four Small parcels, then two more Medium parcels at $8 put the Medium parcel mania item before the Small parcel mania item, total $25
  - three Medium parcels weighing 4 kg, then three weighing 3 kg, put the $10 discount before the $8 discount
  - three Medium parcels weighing 3 kg each have a parcel item showing Medium, the size rate, $8 + $0, the same as when quoted on its own
  - a discount's `Parcels` can't be modified through the returned list

  Verify with `docker compose run --rm tests`.
- [x] 2.6 Add these entries to `docs/assumptions.md`:
  - "every Nth parcel" means any group of N, whatever order the parcels were added in
  - Mixed parcel mania takes any 5 parcels, including 5 of the same size
  - a discount saves the free parcel's whole cost, including any overweight charge, at whichever rate it's charged
  - Heavy-rate parcels join discounts by their size
  - a parcel added more than once counts once for each time it was added
  - each discount is its own line item listing its parcels in cart order, with a positive saving and a negative cost. Discount items are ordered by their first parcel and come between the parcel items and speedy shipping
  - when savings are equal, the combination with fewer Mixed parcel mania discounts is chosen

  Verify that `docker compose run --rm tests` passes, then commit the entries with the code and tests from 2.1–2.5, and verify the working tree is clean afterwards.

## 3. Speedy shipping after discounts

- [x] 3.1 Write a failing test in `SpeedyShippingTests` for "Speedy shipping applies after discounts": three Medium parcels weighing 3 kg and three weighing 4 kg with Speedy give parcel items totalling $54, discounts saving $8 and $10, a speedy item of $36 and a total of $72. Change `Cart.GetQuote()` so the speedy item costs the sum of every item before it. Verify with `docker compose run --rm tests`.
- [x] 3.2 Add tests for these scenarios, implementing anything still missing:
  - "Discount items come between the parcel items and speedy shipping": three Medium parcels at $8 with Speedy give Medium $8, Medium $8, Medium $8, Medium parcel mania -$8, then speedy shipping $16, in that order
  - "Discounts are unaffected by speedy shipping": three Medium parcels at $8 quoted with Standard and with Speedy have equal discount items

  Check that the existing speedy shipping tests pass unchanged. Verify with `docker compose run --rm tests`.
- [x] 3.3 Rewrite "Speedy charge equals the parcel subtotal" in `docs/assumptions.md`: speedy now costs the parcel subtotal less any discount savings, which is what "speedy shipping applies after discounts" asks for. Verify that `docker compose run --rm tests` passes, then commit the entry with the code and tests from 3.1–3.2, and verify the working tree is clean afterwards.

## 4. Final checks

- [ ] 4.1 Check the public surface: it should be unchanged apart from the new `Discount` enum and `DiscountLineItem`. Check that every scenario in `specs/parcel-discounts/spec.md` and `specs/parcel-pricing/spec.md` has a matching test. Verify that `docker compose run --rm tests` passes with zero warnings.

## Workflow follow-up

- Archive the change once implementation is complete, keeping `openspec/specs/parcel-pricing/spec.md` and the new `openspec/specs/parcel-discounts/spec.md` in sync with what was built.
- After archiving, update the Purpose of `openspec/specs/parcel-pricing/spec.md` to mention discounts.
- Commit after archiving.
