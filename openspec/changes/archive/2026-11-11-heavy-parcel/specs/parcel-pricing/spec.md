# Spec Delta

## ADDED Requirements

### Requirement: Heavy rate prices parcels by weight alone
Any parcel, whatever its dimensions, SHALL have a Heavy rate cost: a base cost of $50 plus $1 for each kilogram over 50 kg, rounding any part of a kilogram up to a whole kilogram. A parcel at or under 50 kg MUST have an overweight charge of $0 at the Heavy rate.

#### Scenario: Parcel well over 50 kg
- **WHEN** a parcel measuring 5 x 5 x 5 cm and weighing 60 kg is quoted
- **THEN** it is charged at the Heavy rate with a base cost of $50, an overweight charge of $10 and a cost of $60

#### Scenario: Parcel part of a kilogram over 50 kg
- **WHEN** a parcel measuring 5 x 5 x 5 cm and weighing 50.2 kg is quoted
- **THEN** it is charged at the Heavy rate with an overweight charge of $1 and a cost of $51

#### Scenario: Parcel exactly at 50 kg
- **WHEN** a parcel measuring 5 x 5 x 5 cm and weighing 50 kg is quoted
- **THEN** it is charged at the Heavy rate with an overweight charge of $0 and a cost of $50

### Requirement: Each parcel is charged at its cheaper rate
Each parcel SHALL be charged at whichever of its size rate and the Heavy rate costs less. Its size rate is its size's base cost plus that size's overweight charge. When both rates cost the same, the parcel MUST be charged at its size rate. The rate chosen MUST NOT change the parcel's size.

#### Scenario: Heavy Small parcel
- **WHEN** a parcel measuring 5 x 5 x 5 cm and weighing 40 kg is quoted
- **THEN** its size is Small, it is charged at the Heavy rate and the quote total is $50

#### Scenario: Heavy XL parcel
- **WHEN** a parcel measuring 150 x 5 x 5 cm and weighing 40 kg is quoted
- **THEN** its size is XL, it is charged at the Heavy rate and the quote total is $50

#### Scenario: Size rate is cheaper
- **WHEN** a parcel measuring 5 x 5 x 5 cm and weighing 24 kg is quoted
- **THEN** it is charged at its size rate with an overweight charge of $46 and a cost of $49

#### Scenario: Heavy rate becomes cheaper
- **WHEN** a parcel measuring 5 x 5 x 5 cm and weighing 24.1 kg is quoted
- **THEN** it is charged at the Heavy rate and its cost is $50

#### Scenario: Both rates cost the same
- **WHEN** a parcel measuring 20 x 5 x 5 cm and weighing 24 kg is quoted
- **THEN** its size is Medium, it is charged at its size rate with a base cost of $8 and an overweight charge of $42, and its cost is $50

## MODIFIED Requirements

### Requirement: Each size has a fixed cost
Each parcel SHALL have a size rate base cost equal to the fixed price of its size: Small $3, Medium $8, Large $15, XL $25. A parcel's cost MUST be its base cost plus its overweight charge. Because a larger size raises the base price by more than its higher weight limit can save, charging a parcel at a larger size is never cheaper than charging it at the size its dimensions fit.

#### Scenario: Cost per size
- **WHEN** a Small, a Medium, a Large and an XL parcel, each within its weight limit, are each quoted on their own
- **THEN** their costs are $3, $8, $15 and $25 respectively

### Requirement: Parcels over their size's weight limit pay an overweight charge
Each size SHALL have a weight limit: Small 1 kg, Medium 3 kg, Large 6 kg, XL 10 kg. At its size rate, a parcel heavier than its size's limit MUST pay $2 for each kilogram over the limit, rounding any part of a kilogram up to a whole kilogram. A parcel at or under its limit MUST pay an overweight charge of $0 at its size rate. Weight MUST NOT affect the parcel's size.

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
- **THEN** its size is Small and it is charged at its size rate

### Requirement: Quote lists each parcel with its size and cost
A quote SHALL contain one parcel line item per parcel in the cart, in the order the parcels were added. Each parcel line item MUST show the parcel, its size, the rate it is charged at, its base cost, its overweight charge and its cost. When speedy shipping is selected, its line item MUST come after all the parcel line items.

#### Scenario: Line items follow insertion order
- **WHEN** an XL, a Small and a Medium parcel are added in that order
- **THEN** the quote's line items are XL $25, Small $3, Medium $8 in that order

#### Scenario: Speedy shipping item comes last
- **WHEN** a Medium and a Small parcel are added in that order with Speedy shipping
- **THEN** the quote's line items are Medium $8, Small $3, speedy shipping $11 in that order

#### Scenario: Overweight charge shown separately
- **WHEN** a Small parcel weighing 1.2 kg is quoted
- **THEN** its line item shows the size rate, a base cost of $3, an overweight charge of $2 and a cost of $5

#### Scenario: Parcel within its limit shows no overweight charge
- **WHEN** a Small parcel weighing 0.5 kg is quoted
- **THEN** its line item shows the size rate, a base cost of $3, an overweight charge of $0 and a cost of $3

#### Scenario: Heavy rate shown on the line item
- **WHEN** a parcel measuring 5 x 5 x 5 cm and weighing 40 kg is quoted
- **THEN** its line item shows a size of Small, the Heavy rate, a base cost of $50, an overweight charge of $0 and a cost of $50

### Requirement: Speedy shipping doubles the order cost
When the shipping option is Speedy, the quote SHALL include exactly one speedy shipping line item that costs the sum of the parcel line-item costs, including any overweight charges. Each parcel line item MUST cost the same as it would with Standard shipping. When the shipping option is Standard, the quote MUST NOT include a speedy shipping line item.

#### Scenario: Speedy shipping on one parcel of each size
- **WHEN** a Small, a Medium, a Large and an XL parcel are added, Speedy is selected, and a quote is requested
- **THEN** the parcel line items cost $3, $8, $15 and $25, the speedy shipping line item costs $51, and the total is $102

#### Scenario: Speedy shipping on an overweight parcel
- **WHEN** a Small parcel weighing 1.2 kg is added, Speedy is selected, and a quote is requested
- **THEN** the parcel line item costs $5, the speedy shipping line item costs $5, and the total is $10

#### Scenario: Speedy shipping on a Heavy-rate parcel
- **WHEN** a parcel measuring 5 x 5 x 5 cm and weighing 40 kg is added, Speedy is selected, and a quote is requested
- **THEN** the parcel line item costs $50, the speedy shipping line item costs $50, and the total is $100

#### Scenario: Standard shipping has no speedy item
- **WHEN** a Small parcel is added with Standard shipping and a quote is requested
- **THEN** the quote has only the parcel line item and its total is $3

#### Scenario: Parcel costs are unaffected by speedy shipping
- **WHEN** the same parcels are quoted once with Standard and once with Speedy shipping
- **THEN** the parcel line items in both quotes are equal

#### Scenario: Speedy shipping on an empty cart
- **WHEN** Speedy is selected on a cart with no parcels and a quote is requested
- **THEN** the quote has a single speedy shipping line item costing $0 and a total of $0
