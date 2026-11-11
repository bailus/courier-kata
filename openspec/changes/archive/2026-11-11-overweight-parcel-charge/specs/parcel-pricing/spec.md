# Spec Delta

## ADDED Requirements

### Requirement: Parcel weight is validated
A parcel SHALL have a weight in kilograms. The weight MUST be a positive number, and fractional weights are allowed. Creating a parcel with a zero or negative weight MUST fail with an out-of-range error that names the weight. There is no upper limit on weight.

#### Scenario: Zero weight is rejected
- **WHEN** a parcel is created with a weight of 0 kg
- **THEN** creation fails with an out-of-range error naming the weight

#### Scenario: Negative weight is rejected
- **WHEN** a parcel is created with a weight of -1 kg
- **THEN** creation fails with an out-of-range error naming the weight

#### Scenario: Fractional weight is accepted
- **WHEN** a parcel is created with a weight of 0.5 kg
- **THEN** the parcel is created successfully

### Requirement: Parcels over their size's weight limit pay an overweight charge
Each size SHALL have a weight limit: Small 1 kg, Medium 3 kg, Large 6 kg, XL 10 kg. A parcel heavier than its size's limit MUST pay $2 for each kilogram over the limit, rounding any part of a kilogram up to a whole kilogram. A parcel at or under its limit MUST pay an overweight charge of $0. Weight MUST NOT affect the parcel's size.

#### Scenario: Small parcel part of a kilogram over
- **WHEN** a Small parcel weighing 1.2 kg is quoted
- **THEN** its overweight charge is $2, its cost is $5 and the quote total is $5

#### Scenario: Medium parcel whole kilograms over
- **WHEN** a Medium parcel weighing 5 kg is quoted
- **THEN** its overweight charge is $4, its cost is $12 and the quote total is $12

#### Scenario: Large parcel over
- **WHEN** a Large parcel weighing 7 kg is quoted
- **THEN** its overweight charge is $2, its cost is $17 and the quote total is $17

#### Scenario: XL parcel over
- **WHEN** an XL parcel weighing 11 kg is quoted
- **THEN** its overweight charge is $2, its cost is $27 and the quote total is $27

#### Scenario: Parcel exactly at its weight limit
- **WHEN** a Small parcel weighing 1 kg, a Medium weighing 3 kg, a Large weighing 6 kg and an XL weighing 10 kg are each quoted
- **THEN** each has an overweight charge of $0

#### Scenario: Heavy parcel keeps its size
- **WHEN** a parcel measuring 5 x 5 x 5 cm and weighing 20 kg is quoted
- **THEN** its size is Small

## MODIFIED Requirements

### Requirement: Each size has a fixed cost
Each parcel SHALL have a base cost equal to the fixed price of its size: Small $3, Medium $8, Large $15, XL $25. A parcel's cost MUST be its base cost plus its overweight charge. Because a larger size raises the base price by more than its higher weight limit can save, the smallest size a parcel fits is also its cheapest option at any weight.

#### Scenario: Cost per size
- **WHEN** a Small, a Medium, a Large and an XL parcel, each within its weight limit, are each quoted on their own
- **THEN** their costs are $3, $8, $15 and $25 respectively

### Requirement: Quote lists each parcel with its size and cost
A quote SHALL contain one parcel line item per parcel in the cart, in the order the parcels were added. Each parcel line item MUST show the parcel, its size, its base cost, its overweight charge and its cost. When speedy shipping is selected, its line item MUST come after all the parcel line items.

#### Scenario: Line items follow insertion order
- **WHEN** an XL, a Small and a Medium parcel are added in that order
- **THEN** the quote's line items are XL $25, Small $3, Medium $8 in that order

#### Scenario: Speedy shipping item comes last
- **WHEN** a Medium and a Small parcel are added in that order with Speedy shipping
- **THEN** the quote's line items are Medium $8, Small $3, speedy shipping $11 in that order

#### Scenario: Overweight charge shown separately
- **WHEN** a Small parcel weighing 1.2 kg is quoted
- **THEN** its line item shows a base cost of $3, an overweight charge of $2 and a cost of $5

#### Scenario: Parcel within its limit shows no overweight charge
- **WHEN** a Small parcel weighing 0.5 kg is quoted
- **THEN** its line item shows a base cost of $3, an overweight charge of $0 and a cost of $3

### Requirement: Speedy shipping doubles the order cost
When the shipping option is Speedy, the quote SHALL include exactly one speedy shipping line item that costs the sum of the parcel line-item costs, including any overweight charges. Each parcel line item MUST cost the same as it would with Standard shipping. When the shipping option is Standard, the quote MUST NOT include a speedy shipping line item.

#### Scenario: Speedy shipping on one parcel of each size
- **WHEN** a Small, a Medium, a Large and an XL parcel are added, Speedy is selected, and a quote is requested
- **THEN** the parcel line items cost $3, $8, $15 and $25, the speedy shipping line item costs $51, and the total is $102

#### Scenario: Speedy shipping on an overweight parcel
- **WHEN** a Small parcel weighing 1.2 kg is added, Speedy is selected, and a quote is requested
- **THEN** the parcel line item costs $5, the speedy shipping line item costs $5, and the total is $10

#### Scenario: Standard shipping has no speedy item
- **WHEN** a Small parcel is added with Standard shipping and a quote is requested
- **THEN** the quote has only the parcel line item and its total is $3

#### Scenario: Parcel costs are unaffected by speedy shipping
- **WHEN** the same parcels are quoted once with Standard and once with Speedy shipping
- **THEN** the parcel line items in both quotes are equal

#### Scenario: Speedy shipping on an empty cart
- **WHEN** Speedy is selected on a cart with no parcels and a quote is requested
- **THEN** the quote has a single speedy shipping line item costing $0 and a total of $0
