# Spec Delta

## ADDED Requirements

### Requirement: Shipping option is chosen on the cart
A cart SHALL have a shipping option of either Standard or Speedy, which is Standard by default. The shipping option MAY be changed at any time, in either direction, and the cart MUST report its current option. Setting an option that is neither Standard nor Speedy MUST fail with an out-of-range error. The shipping option and the parcels can be set in any order without changing the quote.

#### Scenario: Default is Standard
- **WHEN** a new cart is created
- **THEN** its shipping option is Standard

#### Scenario: Switching back to Standard removes speedy shipping
- **WHEN** a cart's shipping option is set to Speedy and then back to Standard, and a quote is requested
- **THEN** the quote has no speedy shipping line item and its total is the sum of the parcel costs

#### Scenario: Undefined shipping option is rejected
- **WHEN** a cart's shipping option is set to a value that is neither Standard nor Speedy
- **THEN** the change fails with an out-of-range error and the shipping option is unchanged

#### Scenario: Selecting Speedy before adding parcels
- **WHEN** Speedy is selected on an empty cart and then a Small and an XL parcel are added
- **THEN** the quote's speedy shipping line item costs $28 and the total is $56

### Requirement: Speedy shipping doubles the order cost
When the shipping option is Speedy, the quote SHALL include exactly one speedy shipping line item that costs the sum of the parcel line-item costs. Each parcel line item MUST cost the same as it would with Standard shipping. When the shipping option is Standard, the quote MUST NOT include a speedy shipping line item.

#### Scenario: Speedy shipping on one parcel of each size
- **WHEN** a Small, a Medium, a Large and an XL parcel are added, Speedy is selected, and a quote is requested
- **THEN** the parcel line items cost $3, $8, $15 and $25, the speedy shipping line item costs $51, and the total is $102

#### Scenario: Standard shipping has no speedy item
- **WHEN** a Small parcel is added with Standard shipping and a quote is requested
- **THEN** the quote has only the parcel line item and its total is $3

#### Scenario: Parcel costs are unaffected by speedy shipping
- **WHEN** the same parcels are quoted once with Standard and once with Speedy shipping
- **THEN** the parcel line items in both quotes are equal

#### Scenario: Speedy shipping on an empty cart
- **WHEN** Speedy is selected on a cart with no parcels and a quote is requested
- **THEN** the quote has a single speedy shipping line item costing $0 and a total of $0

## MODIFIED Requirements

### Requirement: Quote lists each parcel with its size and cost
A quote SHALL contain one parcel line item per parcel in the cart, in the order the parcels were added. Each parcel line item MUST show the parcel, its size and its cost. When speedy shipping is selected, its line item MUST come after all the parcel line items.

#### Scenario: Line items follow insertion order
- **WHEN** an XL, a Small and a Medium parcel are added in that order
- **THEN** the quote's line items are XL $25, Small $3, Medium $8 in that order

#### Scenario: Speedy shipping item comes last
- **WHEN** a Medium and a Small parcel are added in that order with Speedy shipping
- **THEN** the quote's line items are Medium $8, Small $3, speedy shipping $11 in that order

### Requirement: Requesting a quote does not change the cart
Requesting a quote SHALL NOT modify the cart. Later quotes MUST reflect any parcels added and any shipping option changed since.

#### Scenario: Quoting twice
- **WHEN** a quote is requested twice for the same cart with no changes in between
- **THEN** both quotes have the same line items and total

#### Scenario: Adding after a quote
- **WHEN** a quote is requested, then another parcel is added, then a second quote is requested
- **THEN** the second quote includes the new parcel and the first quote is unchanged

#### Scenario: Changing shipping after a quote
- **WHEN** a quote is requested with Standard shipping, then Speedy is selected, then a second quote is requested
- **THEN** the second quote includes the speedy shipping line item and the first quote is unchanged
