# Spec Delta

## Purpose

Rewards customers who send several parcels in one order by making some of those parcels free. Covers which groups of parcels earn a discount, which parcel in a group is free, choosing the combination of discounts that saves the most, and how discounts appear on the quote.

## ADDED Requirements

### Requirement: Each discount needs a full group of parcels
Small parcel mania SHALL need a group of 4 Small parcels, Medium parcel mania a group of 3 Medium parcels, and Mixed parcel mania a group of 5 parcels of any sizes, including 5 of the same size. A quote MUST NOT apply a discount to fewer parcels than that discount needs.

#### Scenario: Four Small parcels
- **WHEN** four Small parcels costing $3 each are quoted
- **THEN** the quote has one Small parcel mania discount of the four parcels, saving $3, and the total is $9

#### Scenario: Three Medium parcels
- **WHEN** three Medium parcels weighing 3 kg, costing $8 each, are quoted
- **THEN** the quote has one Medium parcel mania discount of the three parcels, saving $8, and the total is $16

#### Scenario: Five parcels of the same size
- **WHEN** five Large parcels costing $15 each are quoted
- **THEN** the quote has one Mixed parcel mania discount of the five parcels, saving $15, and the total is $60

#### Scenario: Too few parcels for any discount
- **WHEN** a cart of three Small parcels, a cart of two Medium parcels and a cart with one parcel of each size are each quoted
- **THEN** none of the quotes has a discount line item

### Requirement: The cheapest parcel in a group is free
Each discount SHALL make the cheapest parcel in its group free, saving that parcel's whole cost, including any overweight charge, at whichever rate it is charged. Which parcel is free MUST NOT depend on the order the parcels were added in.

#### Scenario: Cheapest parcel is free, not the last one added
- **WHEN** two XL parcels, a Medium parcel and two Large parcels, each within its weight limit, are added in that order
- **THEN** the quote has one Mixed parcel mania discount of the five parcels, saving $8, and the total is $80

#### Scenario: Saving includes the overweight charge
- **WHEN** three Medium parcels weighing 4 kg, costing $10 each, are quoted
- **THEN** the quote has one Medium parcel mania discount saving $10, and the total is $20

### Requirement: A parcel joins discounts by its size
A parcel SHALL be able to join Small parcel mania only when its size is Small, Medium parcel mania only when its size is Medium, and Mixed parcel mania whatever its size. Size comes from the parcel's dimensions alone, so a parcel charged at the Heavy rate joins discounts by its size.

#### Scenario: Heavy-rate parcels join by their size
- **WHEN** four parcels measuring 5 x 5 x 5 cm and weighing 40 kg are quoted
- **THEN** each is Small and charged at the Heavy rate for $50, the quote has one Small parcel mania discount saving $50, and the total is $150

#### Scenario: A Heavy-rate parcel completes a group
- **WHEN** three Small parcels costing $3 each and a parcel measuring 5 x 5 x 5 cm and weighing 40 kg are quoted
- **THEN** the quote has one Small parcel mania discount of the four parcels, saving $3, and the total is $56

#### Scenario: A Medium parcel can't complete Small parcel mania
- **WHEN** three Small parcels costing $3 each and a Medium parcel costing $8 are quoted
- **THEN** the quote has no discount line item

#### Scenario: Large and XL parcels only join Mixed parcel mania
- **WHEN** a cart of four Large parcels and a cart of four XL parcels are each quoted
- **THEN** neither quote has a discount line item

### Requirement: Each parcel is in at most one discount
Each parcel SHALL be in at most one discount. A parcel added to the cart more than once MUST count once for each time it was added.

#### Scenario: Seven Small parcels
- **WHEN** seven Small parcels costing $3 each are quoted
- **THEN** the quote has exactly one discount, a Small parcel mania discount of four of the parcels saving $3, and the total is $18

#### Scenario: The same parcel added four times
- **WHEN** the same Small parcel is added to a cart four times
- **THEN** the quote has one Small parcel mania discount of that parcel four times, saving $3

### Requirement: The combination that saves the most is applied
A quote SHALL apply the combination of discounts that saves the most in total, out of every way of grouping the cart's parcels into discounts.

#### Scenario: Parcels are grouped with others of similar cost
- **WHEN** three Medium parcels weighing 3 kg ($8 each) and then three Medium parcels weighing 4 kg ($10 each) are added
- **THEN** the quote has a Medium parcel mania discount of the three 3 kg parcels saving $8, then a Medium parcel mania discount of the three 4 kg parcels saving $10, and the total is $36

#### Scenario: Mixed parcel mania saves more than a size discount
- **WHEN** a Medium parcel weighing 5 kg ($12), two Medium parcels weighing 3 kg ($8 each) and four Large parcels ($15 each) are quoted
- **THEN** the quote's only discount is a Mixed parcel mania discount of the 5 kg Medium parcel and the four Large parcels, saving $12, and the total is $76

#### Scenario: No other grouping saves more
- **WHEN** any cart is quoted
- **THEN** no other way of grouping its parcels into discounts saves more in total

### Requirement: Equal savings prefer size discounts
When more than one combination of discounts saves the most, a quote SHALL apply one with the fewest Mixed parcel mania discounts.

#### Scenario: Small parcel mania rather than Mixed parcel mania
- **WHEN** five Small parcels costing $3 each are quoted
- **THEN** the quote has one Small parcel mania discount of four of the parcels saving $3, no Mixed parcel mania discount, and a total of $12

#### Scenario: Two size discounts rather than one Mixed discount
- **WHEN** three Small parcels weighing 5 kg ($11 each), a Small parcel costing $3, two Medium parcels weighing 5 kg ($12 each) and a Medium parcel weighing 3 kg ($8) are quoted
- **THEN** the quote has a Small parcel mania discount saving $3 and a Medium parcel mania discount saving $8, rather than a Mixed parcel mania discount saving $11, and the total is $57

### Requirement: Each discount is listed as its own line item
A quote SHALL include one discount line item for each discount it applies. Each discount line item MUST show which discount it is, the parcels in its group in the order they were added, and its saving. Its cost MUST be minus its saving, so the quote's total is still the sum of its line items.

#### Scenario: Discount line item
- **WHEN** three Medium parcels costing $8 each are quoted
- **THEN** after the three parcel line items comes a Medium parcel mania line item showing the three parcels, a saving of $8 and a cost of -$8, and the total is $16

#### Scenario: Parcels in a discount keep the order they were added
- **WHEN** two XL parcels, a Medium parcel and two Large parcels are added in that order
- **THEN** the Mixed parcel mania line item shows the XL, XL, Medium, Large and Large parcels in that order

### Requirement: Discount line items are ordered by their first parcel
Discount line items SHALL be ordered by when the first parcel in each discount was added to the cart.

#### Scenario: Discount whose first parcel was added first comes first
- **WHEN** a Medium parcel costing $8, four Small parcels costing $3 each and two more Medium parcels costing $8 each are added in that order
- **THEN** the Medium parcel mania line item comes before the Small parcel mania line item, and the total is $25

#### Scenario: Order doesn't follow the size of the saving
- **WHEN** three Medium parcels weighing 4 kg ($10 each) and then three Medium parcels weighing 3 kg ($8 each) are added
- **THEN** the discount saving $10 comes before the discount saving $8

### Requirement: Discounts don't change parcel line items
Applying discounts SHALL NOT change any parcel line item. Each parcel line item MUST show the same size, rate, base cost, overweight charge and cost as it would with no discount.

#### Scenario: Parcels in a discount keep their cost
- **WHEN** three Medium parcels weighing 3 kg are quoted
- **THEN** each parcel line item shows a size of Medium, the size rate, a base cost of $8, an overweight charge of $0 and a cost of $8
