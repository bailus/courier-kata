# Spec Delta

## MODIFIED Requirements

### Requirement: Quote lists each parcel with its size and cost
A quote SHALL contain one parcel line item per parcel in the cart, in the order the parcels were added. Each parcel line item MUST show the parcel, its size, the rate it is charged at, its base cost, its overweight charge and its cost. Any discount line items MUST come after all the parcel line items. When speedy shipping is selected, its line item MUST come after all the parcel and discount line items.

#### Scenario: Line items follow insertion order
- **WHEN** an XL, a Small and a Medium parcel are added in that order
- **THEN** the quote's line items are XL $25, Small $3, Medium $8 in that order

#### Scenario: Speedy shipping item comes last
- **WHEN** a Medium and a Small parcel are added in that order with Speedy shipping
- **THEN** the quote's line items are Medium $8, Small $3, speedy shipping $11 in that order

#### Scenario: Discount items come between the parcel items and speedy shipping
- **WHEN** three Medium parcels costing $8 each are added with Speedy shipping
- **THEN** the quote's line items are Medium $8, Medium $8, Medium $8, Medium parcel mania -$8, speedy shipping $16 in that order

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
When the shipping option is Speedy, the quote SHALL include exactly one speedy shipping line item that costs the sum of the parcel line-item costs, including any overweight charges, less the savings of any discounts. Each parcel line item and each discount line item MUST be the same as it would be with Standard shipping. When the shipping option is Standard, the quote MUST NOT include a speedy shipping line item.

#### Scenario: Speedy shipping on one parcel of each size
- **WHEN** a Small, a Medium, a Large and an XL parcel are added, Speedy is selected, and a quote is requested
- **THEN** the parcel line items cost $3, $8, $15 and $25, the speedy shipping line item costs $51, and the total is $102

#### Scenario: Speedy shipping on an overweight parcel
- **WHEN** a Small parcel weighing 1.2 kg is added, Speedy is selected, and a quote is requested
- **THEN** the parcel line item costs $5, the speedy shipping line item costs $5, and the total is $10

#### Scenario: Speedy shipping on a Heavy-rate parcel
- **WHEN** a parcel measuring 5 x 5 x 5 cm and weighing 40 kg is added, Speedy is selected, and a quote is requested
- **THEN** the parcel line item costs $50, the speedy shipping line item costs $50, and the total is $100

#### Scenario: Speedy shipping applies after discounts
- **WHEN** three Medium parcels weighing 3 kg ($8 each) and three Medium parcels weighing 4 kg ($10 each) are added, Speedy is selected, and a quote is requested
- **THEN** the parcel line items cost $54 in total, the discounts save $8 and $10, the speedy shipping line item costs $36, and the total is $72

#### Scenario: Standard shipping has no speedy item
- **WHEN** a Small parcel is added with Standard shipping and a quote is requested
- **THEN** the quote has only the parcel line item and its total is $3

#### Scenario: Parcel costs are unaffected by speedy shipping
- **WHEN** the same parcels are quoted once with Standard and once with Speedy shipping
- **THEN** the parcel line items in both quotes are equal

#### Scenario: Discounts are unaffected by speedy shipping
- **WHEN** three Medium parcels costing $8 each are quoted once with Standard and once with Speedy shipping
- **THEN** the discount line items in both quotes are equal

#### Scenario: Speedy shipping on an empty cart
- **WHEN** Speedy is selected on a cart with no parcels and a quote is requested
- **THEN** the quote has a single speedy shipping line item costing $0 and a total of $0
