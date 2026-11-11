# Assumptions

A running log of decisions made where the requirements were unclear. Each entry
records what was unclear, what was assumed, and why. Entries are added in the
same commit as the code that depends on them.

## Parcel size classification

### XL threshold is 100 cm or more

- **Unclear:** The brief says "XL parcel: any dimension 100cm" with no comparison.
- **Assumed:** A parcel is XL when its largest dimension is `>= 100` cm.
- **Why:** The other brackets use a strict `<`, so `>= 100` is the only reading
  that leaves no gaps between brackets.

### Boundary values belong to the larger size

- **Unclear:** Which size a parcel takes when a dimension is exactly 10 or 50 cm.
- **Assumed:** It goes up a bracket: 10 cm is Medium, 50 cm is Large.
- **Why:** This follows directly from the strict `<` in "all dimensions < 10 cm".

### Size is decided by the largest dimension

- **Unclear:** How "all dimensions" and "any dimension" combine across three
  measurements.
- **Assumed:** Only the largest of the three dimensions matters; which position
  it is in (length, width or height) does not.
- **Why:** "All dimensions < N" is equivalent to "largest dimension < N".

### Three decimal dimensions in centimetres

- **Unclear:** How many dimensions a parcel has, and whether they can be
  fractional.
- **Assumed:** A parcel has length, width and height as `decimal` centimetres.
- **Why:** Real parcels have three dimensions and fractional sizes; `decimal`
  makes boundary comparisons such as 9.99 vs 10 exact.

### No upper size limit

- **Unclear:** Whether very large parcels should be rejected.
- **Assumed:** There is no maximum. Any parcel 100 cm or more is priced as XL.
- **Why:** The brief gives no limit, and XL is open-ended ("any dimension 100cm").

### `AddParcel` naming

- **Unclear:** The brief names the method `addParcel`.
- **Assumed:** The method is `Cart.AddParcel`.
- **Why:** PascalCase is the C# convention for public methods; the brief's
  camelCase reads as language-neutral.

## Pricing

### Costs are plain decimal dollars

- **Unclear:** What currency costs are in and how money should be represented.
- **Assumed:** Costs are `decimal` amounts in US dollars, with no currency type.
- **Why:** The brief only uses `$` and has no multi-currency requirement, so a
  `Money` type would add nothing yet. `decimal` keeps amounts exact.

### Cheapest option is the size rate or the Heavy rate, whichever is less

- **Unclear:** "In all circumstances the cheapest option for sending each parcel
  should be selected" implies a choice between options.
- **Assumed:** Each parcel is priced at its size rate (the smallest size its
  dimensions fit, plus that size's overweight charge) and at the Heavy rate,
  and is charged at the cheaper. Larger sizes are never compared.
- **Why:** Among sizes, the smallest that fits is always cheapest. Each step up
  adds more base price than its higher weight limit can save in overweight
  charges: Small to Medium adds $5 but saves at most $4 (2 kg), Medium to Large
  adds $7 but saves at most $6, and Large to XL adds $10 but saves at most $8.
  The Heavy rate is different: its flat $50 becomes cheaper than the size rate
  from just over 22 kg (XL) to 24 kg (Small and Medium).

## Validation

### Zero or negative dimensions are rejected

- **Unclear:** The brief does not say what happens with a dimension of zero or
  less.
- **Assumed:** Creating a `Parcel` with any dimension that is zero or negative
  throws `ArgumentOutOfRangeException` naming that dimension.
- **Why:** A physical parcel can't measure 0 cm or less, and failing early is
  better than quoting $3 for nonsense input. Validating in the `Parcel`
  constructor means an invalid parcel can never reach a `Cart`. `decimal` has no
  NaN or infinity, so no other checks are needed.

## Cart and quote

### Line items keep insertion order

- **Unclear:** The brief asks for "a collection of items" without an order.
- **Assumed:** A quote lists one line item per parcel in the order the parcels
  were added, including repeats of the same parcel.
- **Why:** It's predictable for callers and easy to match items back to input.

### Empty cart costs nothing

- **Unclear:** What a quote for an empty cart should be.
- **Assumed:** It has no line items and a total of $0; it is not an error.
- **Why:** Zero is the natural total of nothing, and an empty cart is a valid
  state before parcels are added.

### Getting a quote does not change the cart

- **Unclear:** Whether producing the output "checks out" the cart.
- **Assumed:** `GetQuote()` is a read. It can be called repeatedly, and each
  quote is an independent snapshot that later `AddParcel` calls don't change.
- **Why:** The brief describes calculating a cost, not completing a purchase.

## Speedy shipping

### Speedy charge equals the parcel subtotal

- **Unclear:** "Speedy shipping doubles the cost of the entire order" while
  also being "listed as a separate item" and not changing parcel prices.
- **Assumed:** The speedy shipping line item costs the sum of the parcel
  line-item costs. The total is still the sum of every line item, so it
  doubles.
- **Why:** This is the only reading that doubles the total, keeps parcel costs
  unchanged and shows speedy as its own item. It matches the brief's example
  ($51 of parcels, $51 speedy, $102 total). Only parcel costs are doubled for
  now; how speedy combines with later rules such as discounts is left to the
  change that adds them.

### Speedy item comes last

- **Unclear:** Where the speedy item appears among the line items.
- **Assumed:** It comes after all the parcel line items, and appears once.
- **Why:** Its cost depends on all the parcels, and the brief's example lists
  it after them.

### Standard shipping shows no speedy item

- **Unclear:** Whether a quote without speedy shipping lists it at $0.
- **Assumed:** With standard shipping there is no speedy line item.
- **Why:** The item represents a choice the customer made; a $0 line for an
  option they didn't choose adds noise.

### Empty cart with speedy shipping shows a $0 speedy item

- **Unclear:** What a quote looks like when speedy is selected but the cart is
  empty.
- **Assumed:** It has a single speedy line item costing $0 and a total of $0.
- **Why:** The rule stays simple: the speedy item appears exactly when speedy
  is selected, regardless of the cart's contents.

### Shipping is chosen on the cart and can be changed back

- **Unclear:** How the user selects speedy shipping, and whether they can
  undo it.
- **Assumed:** `Cart.Shipping` is a settable property, `Standard` by default.
  It can be changed in either direction at any time, before or after parcels
  are added; quotes already taken don't change.
- **Why:** Shipping speed is part of the order, like the parcels. A single
  property covers selecting, deselecting and reading back the choice, and an
  enum leaves room for other speeds.

### Undefined shipping values are rejected

- **Unclear:** What happens if a value outside the `Shipping` enum (such as
  `(Shipping)42`) is set.
- **Assumed:** The setter throws `ArgumentOutOfRangeException` and the current
  shipping option is kept.
- **Why:** C# enums accept any underlying integer, so an unchecked value would
  silently be treated as Standard. Failing early matches how `Parcel` rejects
  invalid dimensions.

## Weight

### Weight is required, positive and in kilograms

- **Unclear:** The brief adds weight limits but doesn't say how a parcel's
  weight is given, or what counts as a valid weight.
- **Assumed:** `Parcel` takes a required `Weight` as `decimal` kilograms.
  Fractional weights are allowed. Zero or negative weights throw
  `ArgumentOutOfRangeException` naming `Weight`. There is no upper limit.
- **Why:** The limits are in kilograms, and the examples use fractions
  (1.2 kg). Making weight required rather than giving it a default means a
  parcel can't skip the weight check, which is the loophole the change
  closes. Rejecting zero or less matches how dimensions are validated.

### Overweight charge is shown separately on the parcel's line item

- **Unclear:** Whether the quote should show the overweight charge or fold it
  into the parcel's cost.
- **Assumed:** A parcel line item shows `BaseCost` (the size price) and
  `OverweightCharge` separately. Its `Cost` is their sum. Parcels within their
  limit show an `OverweightCharge` of $0.
- **Why:** Customers can see why a parcel costs more than its size price.
  Keeping the charge on the parcel's own line ties it to that parcel, and `Cost`
  still means what the line costs. A $0 charge gives every parcel line the
  same shape.

### A parcel at its weight limit pays nothing extra

- **Unclear:** Whether a parcel weighing exactly its size's limit (such as a
  Small parcel at 1 kg) is over the limit.
- **Assumed:** Only weight strictly above the limit is charged.
- **Why:** The brief charges "over weight limit", and a parcel at the limit
  isn't over it.

### Each part of a kilogram over the limit is charged as a whole kilogram

- **Unclear:** How weight that is part of a kilogram over the limit is charged.
- **Assumed:** The weight over the limit is rounded up to the next whole
  kilogram, at $2 each. 1.2 kg on a Small parcel is 0.2 kg over, so it pays $2.
- **Why:** The brief says to charge $2 for each part-kg, and its example
  (Small 1.2 kg costs $5) needs this rounding.

### Weight doesn't change a parcel's size

- **Unclear:** Whether a parcel over its weight limit moves up a size.
- **Assumed:** Size is decided by dimensions alone. Weight only adds an
  overweight charge on top of the size price.
- **Why:** The brief's examples price heavy parcels at their size's price plus
  the charge (Medium 5 kg costs $8 + $4). Moving up a size is never cheaper
  anyway (see "Cheapest option is the smallest size that fits").

### Speedy shipping doubles overweight charges too

- **Unclear:** Whether the speedy charge covers the overweight charge or only
  the size price.
- **Assumed:** The speedy charge is the sum of the parcel costs, including
  their overweight charges.
- **Why:** The brief's example (Small 1.2 kg with Speedy costs $10) needs it,
  and it keeps "speedy doubles the order cost" true.

## Heavy parcels

### Heavy is a rate, not a size

- **Unclear:** The brief calls Heavy "a new parcel type", but the other types
  are sizes decided by dimensions, and a heavy parcel still has dimensions.
- **Assumed:** `Size` stays decided by dimensions alone. A parcel line item
  also shows the `ParcelRate` it was charged at: `BySize` or `Heavy`. A 40 kg
  parcel measuring 5 x 5 x 5 cm is Small, charged at the Heavy rate.
- **Why:** Keeping both facts means neither is lost. A later rule that groups
  parcels by size or by type can choose which one it needs, and the rule that
  weight doesn't change size still holds.

### Any parcel can use the Heavy rate

- **Unclear:** Whether the Heavy rate is limited to certain dimensions or
  weights.
- **Assumed:** Every parcel is priced at the Heavy rate as well as its size
  rate, whatever its dimensions or weight. It's only used when cheaper.
- **Why:** The examples charge $50 for 40 kg parcels with both Small and XL
  dimensions, and 40 kg is under Heavy's 50 kg limit, so Heavy can't need a
  minimum weight or a size.

### A tie goes to the size rate

- **Unclear:** Which rate is used when both cost the same. This only happens
  for Medium parcels over 23 kg up to 24 kg, which cost $50 either way.
- **Assumed:** The parcel is charged at its size rate.
- **Why:** The price is the same, and Heavy exists to cut excessive overweight
  charges, so it's only used when it actually saves money.

### Heavy overweight charge matches the size rate's rules

- **Unclear:** The brief says "$50 up to over 50 kg", and doesn't say how part
  of a kilogram over 50 kg is charged.
- **Assumed:** A parcel at exactly 50 kg pays $50 with no extra charge. Weight
  over 50 kg is rounded up to the next whole kilogram, at $1 each, so 50.2 kg
  pays $51.
- **Why:** The user asked for $1 per part-kg. Treating the limit itself as
  "not over" matches how the size limits work.

### Speedy shipping doubles Heavy-rate costs too

- **Unclear:** Whether the speedy charge covers parcels charged at the Heavy
  rate.
- **Assumed:** Yes. The speedy charge is the sum of the parcel costs, whatever
  rate each is charged at.
- **Why:** It keeps "speedy doubles the order cost" true, and needs no change.
