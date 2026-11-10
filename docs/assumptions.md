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

### Cheapest option is the smallest size that fits

- **Unclear:** "In all circumstances the cheapest option for sending each parcel
  should be selected" implies a choice between options.
- **Assumed:** With size-only pricing, the only option is the smallest size the
  parcel fits, which is also the cheapest because prices rise with size. No
  general "compare every option" logic is built yet.
- **Why:** This change covers step 1 only. A selector between competing options
  belongs with the later rule (such as weight) that makes it necessary.

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
