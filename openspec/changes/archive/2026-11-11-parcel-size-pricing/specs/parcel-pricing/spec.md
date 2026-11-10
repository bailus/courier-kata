# Spec Delta

## Purpose

Prices a cart of parcels for delivery by classifying each parcel by its size and producing a quote with per-parcel costs and a total.

## ADDED Requirements

### Requirement: Parcel dimensions are validated
A parcel SHALL have three dimensions (length, width, height) in centimetres. Each dimension MUST be a positive, finite number. Creating a parcel with an invalid dimension MUST fail with an error that identifies the invalid dimension. There is no upper limit on a dimension.

#### Scenario: Zero dimension is rejected
- **WHEN** a parcel is created with a width of 0 cm
- **THEN** creation fails with an out-of-range error naming the width

#### Scenario: Negative dimension is rejected
- **WHEN** a parcel is created with a height of -5 cm
- **THEN** creation fails with an out-of-range error naming the height

#### Scenario: Very large parcel is accepted
- **WHEN** a parcel is created with a length of 10,000 cm
- **THEN** the parcel is created successfully

### Requirement: Parcels are classified by their largest dimension
Each parcel SHALL be classified into exactly one size by its largest dimension: Small when it is under 10 cm, Medium when under 50 cm, Large when under 100 cm, and XL when 100 cm or more. A dimension exactly on a boundary belongs to the larger size. The orientation of the dimensions MUST NOT affect the size.

#### Scenario: Small parcel
- **WHEN** a parcel measuring 9.99 x 5 x 5 cm is quoted
- **THEN** its size is Small

#### Scenario: Medium parcel at the lower boundary
- **WHEN** a parcel measuring 10 x 5 x 5 cm is quoted
- **THEN** its size is Medium

#### Scenario: Medium parcel below the upper boundary
- **WHEN** a parcel measuring 49.99 x 5 x 5 cm is quoted
- **THEN** its size is Medium

#### Scenario: Large parcel at the lower boundary
- **WHEN** a parcel measuring 50 x 5 x 5 cm is quoted
- **THEN** its size is Large

#### Scenario: Large parcel below the upper boundary
- **WHEN** a parcel measuring 99.99 x 5 x 5 cm is quoted
- **THEN** its size is Large

#### Scenario: XL parcel at the boundary
- **WHEN** a parcel measuring 100 x 5 x 5 cm is quoted
- **THEN** its size is XL

#### Scenario: Largest dimension decides regardless of position
- **WHEN** parcels measuring 60 x 5 x 5, 5 x 60 x 5 and 5 x 5 x 60 cm are quoted
- **THEN** each is classified as Large

### Requirement: Each size has a fixed cost
Each parcel SHALL cost the fixed price of its size: Small $3, Medium $8, Large $15, XL $25. Because prices rise with size, the smallest size a parcel fits is also its cheapest option.

#### Scenario: Cost per size
- **WHEN** a Small, a Medium, a Large and an XL parcel are each quoted on their own
- **THEN** their costs are $3, $8, $15 and $25 respectively

### Requirement: Cart accepts parcels
A cart SHALL accept parcels one at a time. A cart MAY hold any number of parcels, including the same parcel more than once.

#### Scenario: Adding the same parcel twice
- **WHEN** the same 5 x 5 x 5 cm parcel is added to a cart twice
- **THEN** the quote contains two Small line items

### Requirement: Quote lists each parcel with its size and cost
A quote SHALL contain one line item per parcel in the cart, in the order the parcels were added. Each line item MUST show the parcel, its size and its cost.

#### Scenario: Line items follow insertion order
- **WHEN** an XL, a Small and a Medium parcel are added in that order
- **THEN** the quote's line items are XL $25, Small $3, Medium $8 in that order

### Requirement: Quote includes the total cost
A quote SHALL include a total equal to the sum of its line-item costs.

#### Scenario: Total of mixed parcels
- **WHEN** one parcel of each size is added to a cart
- **THEN** the quote's total is $51

#### Scenario: Empty cart
- **WHEN** a quote is requested for a cart with no parcels
- **THEN** the quote has no line items and a total of $0

### Requirement: Requesting a quote does not change the cart
Requesting a quote SHALL NOT modify the cart. Later quotes MUST reflect any parcels added since.

#### Scenario: Quoting twice
- **WHEN** a quote is requested twice for the same cart with no changes in between
- **THEN** both quotes have the same line items and total

#### Scenario: Adding after a quote
- **WHEN** a quote is requested, then another parcel is added, then a second quote is requested
- **THEN** the second quote includes the new parcel and the first quote is unchanged
