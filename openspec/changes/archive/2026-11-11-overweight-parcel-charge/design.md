# Design

## Context

See proposal.md for the motivation. Today `Parcel(Length, Width, Height)` validates its dimensions in its constructor. The internal `ParcelPricing.Price` classifies a parcel by its largest dimension and returns `ParcelLineItem(Parcel, Size, Cost)`. `Cart.GetQuote()` prices each parcel and, with Speedy, adds a `SpeedyShippingLineItem` costing the sum of the parcel `Cost`s. Weight affects only the parcel's own price, so the change stays within `Parcel`, `ParcelPricing` and `ParcelLineItem`.

## Goals / Non-Goals

**Goals:**
- Quotes show the overweight charge on the parcel's own line item, separate from the size price.
- `Cost` keeps meaning "what this line item costs", so `Quote.Total` and Speedy don't change.

**Non-Goals:**
- A general selector that compares pricing options. Weight doesn't need one, because the smallest size is still cheapest (see the spec).
- A `Weight` or `Money` value type. Weight is plain `decimal` kilograms, just as dimensions are plain `decimal` centimetres.

## Decisions

### Weight is a required fourth `Parcel` parameter
```
public sealed record Parcel(decimal Length, decimal Width, decimal Height, decimal Weight)
```
- It's required, not optional with a default. A default (say 0 kg or 1 kg) would hide parcels that callers forgot to weigh, which is the loophole this change closes.
- It's validated in the constructor with `ThrowIfNegativeOrZero`, as the dimensions are. The parameter name in the error is `Weight`.
- Alternative considered: a separate `Weight` property set after construction. It was rejected because a parcel could then exist without a weight.

### Line item shows base cost and overweight charge (the user chose this)
```
public sealed record ParcelLineItem(Parcel Parcel, ParcelSize Size, decimal BaseCost, decimal OverweightCharge)
    : LineItem(BaseCost + OverweightCharge)
```
- `Cost` always equals `BaseCost + OverweightCharge`, and a caller can't build an inconsistent item. It's worked out when read, not stored, so `with` expressions stay consistent (see Risks). The sketch above shows the shape, not the final mechanism.
- Parcels within their limit carry `OverweightCharge = 0m`, so every parcel line has the same shape.
- The name `OverweightCharge` says when the charge applies. `WeightCharge` would suggest every parcel pays for weight.
- Alternatives considered: folding the charge into `Cost`, which is simpler but hides why a parcel costs more, and a separate `OverweightLineItem`, which would separate the charge from its parcel.

### Overweight calculation lives in `ParcelPricing`
```
over   = parcel.Weight - LimitOf(size)
charge = over > 0 ? 2m * Math.Ceiling(over) : 0m
```
- `LimitOf(size)` is a switch next to `PriceOf(size)`, so all per-size figures are in one place. `$2/kg` is a named constant.
- `Math.Ceiling` on `decimal` is exact, so 1.2 kg gives `Ceiling(0.2) = 1` and 1.0 kg gives no charge, with no floating-point edge cases.
- Size is still decided by dimensions alone. Weight only adds a charge.

### Test migration
- Shared fixtures (`Small`, `Medium`, `Large`, `ExtraLarge` in `CartTests`, `SpeedyShippingTests` and `ShippingOptionTests`) get a weight within the Small limit, such as `0.5m`. That's under every size's limit, so one value works everywhere and existing expected costs don't change.
- Data-driven tests (`ParcelCostTests`, `ParcelSizeTests`, `ParcelTests`) pass a fixed in-limit weight in their `new Parcel(...)` calls rather than gaining a new data column.
- Expected `new ParcelLineItem(p, size, cost)` values become `new ParcelLineItem(p, size, cost, 0m)`.
- Overweight behaviour gets its own test class, `OverweightChargeTests`. Its first test is the brief's 1.2 kg Small example.

### Assumptions log
New entries in `docs/assumptions.md`:
- weight is required, positive and in `decimal` kilograms
- exactly at the limit pays nothing
- part-kilograms round up, per kilogram over
- weight doesn't change size
- the overweight charge is shown separately, $0 when within the limit
- Speedy doubles overweight charges too

The existing "Cheapest option is the smallest size that fits" entry is updated: weight was expected to need an option selector, but it doesn't, because each step up in size adds more base price than its higher limit saves ($5 vs $4, $7 vs $6, $10 vs $8).

## Risks / Trade-offs

- [Breaking `Parcel` and `ParcelLineItem` constructors] → The only caller is the test project. Updating every call is part of this change.
- [Later rules, such as a "heavy parcel" option, may make a different size cheaper] → That change will add the option selector. The assumption entry records why it isn't needed now.
- [A `with` expression would leave `Cost` stale. Passing `BaseCost + OverweightCharge` to the base constructor stores `Cost` once, and `item with { OverweightCharge = 4m }` uses the copy constructor, which copies the old `Cost`] → Work out `Cost` when it's read instead of storing it. For example, make `LineItem.Cost` abstract, have `ParcelLineItem` override it as `BaseCost + OverweightCharge`, and have `SpeedyShippingLineItem` keep its positional `Cost`. Task 1.1 includes a test that `with` keeps `Cost` consistent.
