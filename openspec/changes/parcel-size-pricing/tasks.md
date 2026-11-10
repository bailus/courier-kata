# Tasks

## 1. Scaffolding

- [x] 1.1 Create `CourierKata.sln`, `src/CourierKata` (net10.0 class library) and `tests/CourierKata.Tests` (xUnit, referencing the library), with a root `Directory.Build.props` enabling nullable, warnings as errors and implicit usings; verify `dotnet build` succeeds with zero warnings and `dotnet test` runs (no tests yet)
- [x] 1.2 Create `docs/assumptions.md` with a short header explaining its purpose; verify the file exists
- [x] 1.3 Commit the scaffold after `dotnet format` reports no changes

## 2. Size classification (tests first, one size at a time)

- [x] 2.1 Write a failing `[Theory]` for Small (e.g. 1x1x1, 9.99x5x5) that adds a `Parcel` to a `Cart` and asserts the quote's single line item has `ParcelSize.Small`; add the minimal `Parcel`, `ParcelSize`, `LineItem`, `Quote` and `Cart` to make it pass; verify with `dotnet test`
- [x] 2.2 Write a failing `[Theory]` for Medium (10x5x5 and 49.99x5x5) and extend classification to pass; verify with `dotnet test`
- [x] 2.3 Write a failing `[Theory]` for Large (50x5x5 and 99.99x5x5) and extend classification to pass; verify with `dotnet test`
- [x] 2.4 Write a failing `[Theory]` for XL (100x5x5 and 10,000x5x5) and extend classification to pass; verify with `dotnet test`
- [x] 2.5 Add a `[Theory]` that the largest dimension decides regardless of position (60x5x5, 5x60x5, 5x5x60 are all Large); verify it passes
- [x] 2.6 Refactor classification into an internal pricing class (design.md, Pricing internals) with no change to tests; verify `dotnet test` passes
- [x] 2.7 Add assumptions for the XL `>= 100` boundary, boundary values going up a bracket, three decimal cm dimensions, no upper size limit, and `AddParcel` naming to `docs/assumptions.md`; commit with the code

## 3. Cost per size

- [x] 3.1 Write a failing `[Theory]` asserting line-item costs Small $3, Medium $8, Large $15, XL $25, then implement the price lookup; verify with `dotnet test`
- [x] 3.2 Add assumptions for decimal USD with no currency type and "cheapest = smallest fitting size" for this step; commit with the code

## 4. Parcel validation

- [x] 4.1 Write failing tests that zero and negative values for each of length, width and height throw `ArgumentOutOfRangeException` whose `ParamName` names the dimension; implement validation in `Parcel`; verify with `dotnet test`
- [x] 4.2 Add the assumption that zero or negative dimensions are rejected; commit with the code

## 5. Cart and quote behaviour

- [ ] 5.1 Write failing tests for line items in insertion order (XL, Small, Medium) and for adding the same parcel twice; implement; verify with `dotnet test`
- [ ] 5.2 Write failing tests for the total (one of each size = $51) and the empty cart (no items, $0); implement; verify with `dotnet test`
- [ ] 5.3 Write failing tests that quoting twice gives equal results and that a quote taken before adding a parcel is unchanged afterwards; implement (copy items into a read-only list); verify with `dotnet test`
- [ ] 5.4 Add assumptions for empty cart = $0 and insertion order; commit with the code

## 6. Final checks

- [ ] 6.1 Confirm the public surface is only `Cart`, `Parcel`, `ParcelSize`, `LineItem` and `Quote` (everything else `internal`), and that every spec scenario in `specs/parcel-pricing/spec.md` has a matching test; verify `dotnet build` has zero warnings, `dotnet test` passes, `docker compose run --rm tests` passes, and `dotnet format --verify-no-changes` succeeds

## Workflow follow-up

- Archive the change once implementation is complete, keeping `openspec/specs/parcel-pricing/spec.md` in sync with what was built.
- Commit after archiving.
