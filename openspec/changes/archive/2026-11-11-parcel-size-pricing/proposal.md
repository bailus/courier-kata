# Proposal

## Why

The courier kata needs its first behaviour: pricing a set of parcels by size. Nothing exists yet, so this change also establishes the library, its test project, and the public API shape that later steps (weight, speedy shipping, discounts) will build on.

## What Changes

- Create the solution, `src/CourierKata` class library and `tests/CourierKata.Tests` xUnit project (current LTS .NET, nullable enabled, warnings as errors).
- Add a `Cart` that accepts parcels through `AddParcel` and produces a quote.
- Classify each parcel by its largest dimension into Small (< 10 cm, $3), Medium (< 50 cm, $8), Large (< 100 cm, $15) or XL (>= 100 cm, $25).
- Produce a quote listing each parcel with its size and cost, in the order added, plus the total.
- Reject parcels with zero, negative or non-finite dimensions. Very large parcels are accepted and priced as XL.
- Create `docs/assumptions.md` recording the interpretations made where the brief is unclear.

Out of scope: weight, speedy shipping, discounts, multiple currencies, and any interface other than the C# API.

## Capabilities

### New Capabilities
- `parcel-pricing`: classifying parcels by size, pricing them, and producing a quote for a cart of parcels.

### Modified Capabilities

None.

## Impact

- New code only: `CourierKata.sln`, `src/CourierKata/`, `tests/CourierKata.Tests/`, `docs/assumptions.md`.
- New public API: `Cart`, `Parcel`, `ParcelSize`, `LineItem`, `Quote`.
- Dependencies: xUnit and the .NET test SDK in the test project only.
