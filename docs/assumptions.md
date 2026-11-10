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
