# Design

## Context

See proposal.md for the motivation. Today `LineItem(Parcel, Size, Cost)` is a sealed record that only describes parcels, and `Quote(Items, Total)` totals the costs of its items. `ParcelPricing` (internal) prices one parcel, and `Cart.GetQuote()` maps parcels to line items. Speedy shipping is the first line item that isn't a parcel, so the line-item model has to change.

## Goals / Non-Goals

**Goals:**
- Speedy shipping appears as its own line item, and the total stays "the sum of the line items".
- Parcel pricing code does not know about speedy shipping.
- A closed set of line-item kinds that callers can match on exhaustively.

**Non-Goals:**
- A general list of surcharges or discounts, or a pricing pipeline. Later steps (weight, discounts) will add these when they need them.
- Setting the order in which order-level rules apply. Today speedy doubles the parcel subtotal and nothing else.

## Decisions

### Line items become a closed hierarchy
```
public abstract record LineItem(decimal Cost)            // private protected constructor
public sealed record ParcelLineItem(Parcel Parcel, ParcelSize Size, decimal Cost) : LineItem(Cost)
public sealed record SpeedyShippingLineItem(decimal Cost) : LineItem(Cost)
```
- Speedy is listed as a separate item in the same `Items` list, so `Quote` keeps its shape and `Total` stays the sum of `Items`.
- The base constructor is `private protected`, so only this assembly can add item kinds and callers can rely on there being exactly two.
- The name `LineItem` stays on the base so that code reading only `Cost` keeps compiling. The parcel-specific fields move to `ParcelLineItem`.
- Alternatives considered: a separate `Quote.SpeedyShipping` member (simpler for callers, but less literal to "listed as a separate item"), and a separate list of extras. The user chose the hierarchy.

### Shipping option lives on the cart
```
public enum Shipping { Standard, Speedy }
public sealed class Cart { public Shipping Shipping { get; set; } = Shipping.Standard; ... }
```
- Shipping is part of the order, alongside the parcels. One settable property handles selecting, deselecting and reading back the choice.
- The setter rejects undefined enum values (`Enum.IsDefined`) with `ArgumentOutOfRangeException` and leaves the current value as it was. `Parcel` rejects invalid dimensions in the same way.
- The enum is named `Shipping`, the same as the property. This is the usual C# "Color Color" pattern and resolves cleanly.
- Alternatives considered: a `GetQuote(Shipping)` parameter, select and deselect methods, and a `SelectShipping(Shipping)` method. The user chose the property.

### Speedy charge is calculated in the cart's quote assembly
`Cart.GetQuote()` does the following:
1. Prices the parcels into `ParcelLineItem`s using `ParcelPricing.Price`, which is unchanged apart from its return type.
2. If `Shipping == Speedy`, appends `new SpeedyShippingLineItem(sum of parcel costs)`.
3. Builds `Quote(items, items.Sum(Cost))`.

The speedy rule is one line: the charge equals the parcel subtotal. If more order-level rules arrive, it can move into its own internal class. Doing that now would be speculative.

### Test migration
Existing tests that read `.Size` or `.Parcel` change from `LineItem` to `ParcelLineItem`. They do this with `Assert.IsType<ParcelLineItem>` or by constructing `ParcelLineItem` in expected lists. Their assertions are otherwise unchanged. The new tests start with the user's example: one parcel of each size with Speedy gives parcel items of $3, $8, $15 and $25, a speedy item of $51, and a total of $102.

### Assumptions log
New entries in `docs/assumptions.md`:
- The speedy charge equals the parcel subtotal.
- The speedy item comes last.
- Standard shipping shows no speedy item.
- An empty cart with Speedy shows a $0 speedy item.
- Shipping is chosen on the cart and can be changed back.
- Undefined `Shipping` values are rejected.

## Risks / Trade-offs

- [Breaking change to `LineItem`: callers reading `Size` or `Parcel` must pattern-match] → It's an early-stage library and the only caller is the test project. The closed hierarchy keeps matching exhaustive.
- [A quote can show a "$0 speedy shipping" line on an empty cart] → Chosen so that the item appears exactly when Speedy is selected. Recorded as an assumption.
- [Later discounts or surcharges may need to apply before or after speedy] → Out of scope. The spec states that speedy doubles the parcel costs, so a later change can define the order explicitly.
- [Public records can be constructed with inconsistent values] → Accepted as before. They are output value types.
