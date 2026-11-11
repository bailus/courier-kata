# Design

## Context

See proposal.md for the motivation. Today the internal `ParcelPricing.Price(parcel)` classifies a parcel by its largest dimension. It then returns `ParcelLineItem(Parcel, Size, BaseCost, OverweightCharge)` using the size's price, the size's weight limit and $2 per part-kg over. It never compares options. `docs/assumptions.md` ("Cheapest option is the smallest size that fits") says a selector would come with a later rule that can make another option cheaper. The Heavy rate is that rule. `Cart.GetQuote()` and speedy shipping only read `Cost`, so the change stays inside `ParcelPricing` and `ParcelLineItem`, plus one new enum.

## Goals / Non-Goals

**Goals:**
- Each parcel line item says both what size the parcel is and which rate it was charged at.
- The choice between rates is made in one place, so a later rate can be added without changing `Cart`.

**Non-Goals:**
- A general list of "pricing options" or a strategy interface. There are two rates, and a pluggable mechanism would be speculative.
- Re-checking larger sizes as options. Charging at a larger size is never cheaper (see the spec), so the size rate always uses the size the dimensions fit.

## Decisions

### `Size` stays dimensional and a new `Rate` says how the parcel was charged (the user chose this)
```
public enum ParcelRate { BySize, Heavy }

public sealed record ParcelLineItem(
    Parcel Parcel, ParcelSize Size, ParcelRate Rate, decimal BaseCost, decimal OverweightCharge) : LineItem
```
- `Size` keeps one meaning: the size bracket from the dimensions. The spec rule "Weight MUST NOT affect the parcel's size" still holds.
- `Rate` comes straight after `Size`, so the line reads "Small, charged at the Heavy rate, $50 + $0".
- `BaseCost` and `OverweightCharge` are those of the chosen rate. At the Heavy rate they are $50 and $1 per part-kg over 50 kg. `Cost` is still worked out when read as `BaseCost + OverweightCharge`.
- The names: `ParcelRate` and `BySize` avoid clashing with `Shipping.Standard`. `BySize` reads naturally next to `Size`.
- Alternatives considered:
  - `ParcelSize.Heavy`. It's a smaller change, but it contradicts the size requirement and loses the dimensional size of heavy parcels.
  - `ParcelType { Small, Medium, Large, ExtraLarge, Heavy }`. Four of its five values would repeat `Size`.
  - `bool IsHeavy`. It's a loose primitive and can't grow.

### `ParcelPricing` prices both rates and keeps the cheaper one
```
size    = Classify(parcel)
bySize  = (BySize, PriceOf(size), Overweight(weight, LimitOf(size), $2))
heavy   = (Heavy,  $50,           Overweight(weight, 50 kg,         $1))
chosen  = heavy.Cost < bySize.Cost ? heavy : bySize
```
- The comparison is strict `<`, so a tie goes to the size rate. Heavy is only used when it actually saves money.
- The overweight helper takes the limit and the per-kg rate as parameters, so both rates use the same rounding with `Math.Ceiling`. That keeps "at the limit pays nothing" and "part-kg counts as whole" in one place.
- The Heavy figures ($50, 50 kg, $1/kg) are named constants next to the existing `$2/kg` constant.
- Alternative considered: a separate `HeavyPricing` class. It was rejected because the selection needs both rates, and splitting them would mean a third class just to compare them.

### Test migration
- Every expected `new ParcelLineItem(p, size, base, over)` in the tests (about 20) becomes `new ParcelLineItem(p, size, ParcelRate.BySize, base, over)`. None of the existing fixtures is heavy enough for the Heavy rate to be cheaper. The heaviest is 20 kg Small at $41.
- The Heavy rate and the rate choice get their own test class, `HeavyRateTests`. Its first tests are the user's two 40 kg examples.

### Assumptions log
New entries in `docs/assumptions.md`:
- any parcel can use the Heavy rate, whatever its dimensions
- the cheaper rate is chosen, and a tie goes to the size rate
- exactly 50 kg pays $50, and part-kilograms over 50 kg round up at $1 each
- weight still doesn't change size, and the line item shows the rate separately
- speedy shipping doubles Heavy-rate costs too

The "Cheapest option is the smallest size that fits" entry is rewritten. Among sizes, the smallest that fits is still the cheapest. But the size rate is now compared with the Heavy rate, which is cheaper from about 22–24 kg depending on size.

## Risks / Trade-offs

- [Breaking `ParcelLineItem` constructor] → The only caller is the test project, and this change updates every call.
- [A positional `ParcelRate` next to `ParcelSize` is easy to swap by mistake] → They are different enum types, so the compiler rejects a swap.
- [Later rules that group parcels by type, such as discounts, will have to decide whether a heavy Small parcel counts as Small or as Heavy] → Both facts are on the line item, so that change can choose. Nothing is decided here.
- [Ties exist only for Medium parcels over 23 kg up to 24 kg, both at $50] → The "Both rates cost the same" scenario covers it, so the tie rule is tested rather than left to chance.
