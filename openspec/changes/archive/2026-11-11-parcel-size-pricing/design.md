# Design

## Context

The repository has no code yet; this change creates the solution and sets the public API. See proposal.md for scope. The project conventions require a C# class library with no other interface, small public surface, `internal` implementation details, nullable reference types and warnings as errors. Tests run locally with `dotnet test` or in Docker via `docker compose run --rm tests` (.NET 10 SDK image).

## Goals / Non-Goals

**Goals:**
- A minimal public API that later kata steps can extend without breaking callers.
- Pricing rules kept in one internal place so they can change independently of `Cart`.

**Non-Goals:**
- Any "evaluate every option, pick the cheapest" machinery. With fixed prices rising by size, the smallest fitting size is the cheapest. A general selector waits until a later step (e.g. weight) makes it necessary.
- A `Money` type or currency handling.

## Decisions

### Project layout and tooling
`CourierKata.sln`, `src/CourierKata` (net10.0 class library) and `tests/CourierKata.Tests` (xUnit). A `Directory.Build.props` at the root sets `Nullable`, `TreatWarningsAsErrors`, `ImplicitUsings` and `LangVersion` once for both projects. The test project uses `InternalsVisibleTo` only if internal tests prove necessary; tests should go through the public API by default.

### Public types
```
public sealed record Parcel(decimal Length, decimal Width, decimal Height)  // validates in constructor
public enum ParcelSize { Small, Medium, Large, ExtraLarge }
public sealed record LineItem(Parcel Parcel, ParcelSize Size, decimal Cost)
public sealed record Quote(IReadOnlyList<LineItem> Items, decimal Total)
public sealed class Cart { void AddParcel(Parcel parcel); Quote GetQuote(); }
```
- `Parcel` as a validating record (value object): an invalid parcel cannot exist, so `Cart` never re-checks. Alternative: `AddParcel(l, w, h)` with primitives. Rejected because it spreads validation and loses the domain type in `LineItem`.
- `decimal` for both centimetres and dollars: exact boundary comparisons (9.99 vs 10) and exact money. `double` was rejected because of rounding at boundaries. Since `decimal` has no NaN or infinity, "non-finite" can't happen, so validation only needs `> 0`.
- `ExtraLarge` rather than `XL` as the enum name, for C# naming. The spec's "XL" maps to it.
- `AddParcel` in PascalCase, following C# convention rather than the brief's `addParcel`.
- `GetQuote()` is a pure read that returns a new immutable `Quote` (items copied into a read-only list). It returns a new value rather than exposing internal state, so earlier quotes stay unchanged when the cart changes.
- `Quote.Total` is computed by the `Quote` creator (the internal pricing code), not supplied by callers. Because `Quote` is a public record, a caller could build one with an inconsistent total. That's acceptable for a value-carrying output type; if needed later, a private constructor plus factory can lock it down.

### Pricing internals
An `internal static class PriceList` (or similar) holds:
- `ParcelSize Classify(Parcel)`: uses `Math.Max` of the three dimensions against ordered thresholds 10 / 50 / 100.
- `decimal PriceOf(ParcelSize)`: a switch expression.

`Cart.GetQuote()` maps each parcel to a `LineItem` using these and sums the costs. Keeping classification and price separate lets weight-based rules be added later without touching `Cart`.

### Assumptions log
`docs/assumptions.md` is created with entries for: the XL boundary (`>= 100`), boundary values going up a bracket, three decimal dimensions in cm, rejecting zero/negative dimensions, no upper size limit (very large parcels are XL, not rejected), `AddParcel` naming, decimal USD with no currency type, empty cart gives $0, insertion order, and "cheapest = smallest fitting size" for this step. Each entry goes in the same commit as the code that depends on it.

### Test approach
Test-first, one behaviour per test, starting with per-size classification using `[Theory]` rows at each boundary (just below and exactly on 10, 50 and 100), then costs, then cart and quote behaviour. Tests exercise only the public API.

## Risks / Trade-offs

- [Brief's `addParcel` naming differs from `AddParcel`] → recorded in assumptions; trivial to alias if a reviewer insists.
- [No upper size limit may not match real couriers] → recorded as an assumption; a later step can add a limit as a spec change.
- [Public `Quote` record can be constructed with an inconsistent total] → accepted for now; documented above.
