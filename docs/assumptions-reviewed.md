# Assumptions

Decisions made where the requirements were unclear.

## Parcel size classification

### Three decimal dimensions in centimetres
A parcel has length, width and height as `decimal` centimetres.

### No upper size limit
There is no maximum size. Any parcel 100 cm or more is priced as XL.

## Pricing

### Costs are plain decimal dollars
Costs are `decimal` amounts in dollars, with no currency type.

## Validation

### Zero or negative dimensions are rejected
Creating a `Parcel` with any dimension that is zero or negative
throws `ArgumentOutOfRangeException` naming that dimension.

## Cart and quote

### Line items keep insertion order
The brief asks for "a collection of items" without specifying an order.
The implemented quote lists one line item per parcel in the order the parcels
were added.

### Empty cart costs nothing
An empty cart has a total of $0; it is not an error.

### Getting a quote does not change the cart
The brief describes calculating a cost, not completing a purchase.
`GetQuote()` is a read. It can be called repeatedly, and each
quote is an independent snapshot that later `AddParcel` calls don't change.

## Speedy shipping

### Empty cart with speedy shipping shows a $0 speedy item
It has a single speedy line item costing $0 and a total of $0.

## Weight

### Weight is required, positive and in kilograms
`Parcel` takes a required `Weight` as `decimal` kilograms.
  Fractional weights are allowed. Zero or negative weights throw
  `ArgumentOutOfRangeException` naming `Weight`. There is no upper limit.

### Overweight charge is shown separately on the parcel's line item
Assumed:** A parcel line item shows `BaseCost` (the size price) and
  `OverweightCharge` separately. Its `Cost` is their sum. Parcels within their
  limit show an `OverweightCharge` of $0.

### Each part of a kilogram over the limit is charged as a whole kilogram
The weight over the limit is rounded up to the next whole
  kilogram, at $2 each. 1.2 kg on a Small parcel is 0.2 kg over, so it pays $2.

## Heavy parcels

### Heavy is a rate, not a size
The brief calls Heavy "a new parcel type", but the other types
  are sizes decided by dimensions, and a heavy parcel still has dimensions.

`Size` stays decided by dimensions alone. A parcel line item
  also shows the `ParcelRate` it was charged at: `BySize` or `Heavy`. A 40 kg
  parcel measuring 5 x 5 x 5 cm is Small, charged at the Heavy rate.

### A tie goes to the size rate
This only happens for Medium parcels over 23 kg up to 24 kg, which cost $50 either way.
The parcel is charged at its size rate.

## Multi-parcel discounts

### Equal savings prefer fewer Mixed discounts
Which combination is used when several save the same, such as
  five $3 Small parcels as Small parcel mania or as Mixed parcel mania.
The one with the fewest Mixed parcel mania discounts. Any tie
  left after that is settled by a fixed search order, so the same cart always
  gets the same discounts.
