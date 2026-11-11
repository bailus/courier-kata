
❯ /opsx:explore Some of the extra weight charges for certain goods were
excessive. A new parcel type has been added to try and address overweight
parcels
Heavy parcel, $50 up to over 50 kg, extra $1 for each kg over 50

assume $1 is charged for each part-kg

tests:
40kg parcel with small dimensions total $50
40kg parcel with XL dimensions total $50

make a new git branch for this

  Ran 4 shell commands (ctrl+o to expand)

● I've created and switched to the heavy-parcel branch, starting from master at
  3dc32f3. It follows the pattern of naming each branch after its change.
  Nothing else is written yet.

  What's there today

  ParcelPricing.cs (src/CourierKata/ParcelPricing.cs) only ever prices one
  option: the smallest size the parcel's dimensions fit, plus $2 for each kg (or
  part-kg) over that size's limit. docs/assumptions.md says why there is only
  one option: "A selector belongs with a later rule that can make a larger
  option cheaper." Heavy is that rule. Picking the cheapest option is now real
  logic, and that assumption needs rewriting.

              Parcel
                |
       +--------+---------+
       v                  v
    size option        heavy option
    (by dimensions)    (any dimensions)
    base 3/8/15/25     base $50
    +$2/kg over        +$1/kg over 50 kg
    1/3/6/10 kg        (part-kg rounded up)
       |                  |
       +------> min <-----+
                 |
           ParcelLineItem

  Both of your test cases come out at $50:

  ┌──────────────┬─────────────────┬──────────────┬───────────┐
  │    Parcel    │   Size option   │ Heavy option │  Chosen   │
  ├──────────────┼─────────────────┼──────────────┼───────────┤
  │ 40 kg, Small │ 3 + 2x39 = $81  │ $50          │ Heavy $50 │
  ├──────────────┼─────────────────┼──────────────┼───────────┤
  │ 40 kg, XL    │ 25 + 2x30 = $85 │ $50          │ Heavy $50 │
  └──────────────┴─────────────────┴──────────────┴───────────┘

  Heavy takes over well below 50 kg

  ┌────────┬──────────────────────┬─────────────────────┬──────────────────┐
  │  Size  │   Size option is     │         Tie         │ Heavy is cheaper │
  │        │    cheaper up to     │                     │       above      │
  ├────────┼──────────────────────┼─────────────────────┼──────────────────┤
  │ Small  │ 24 kg ($49)          │ never (cost is      │ 24 kg            │
  │        │                      │ always odd)         │                  │
  ├────────┼──────────────────────┼─────────────────────┼──────────────────┤
  │ Medium │ 23 kg ($48)          │ 23–24 kg, both $50  │ 24 kg            │
  ├────────┼──────────────────────┼─────────────────────┼──────────────────┤
  │ Large  │ 23 kg ($49)          │ never               │ 23 kg            │
  ├────────┼──────────────────────┼─────────────────────┼──────────────────┤
  │ XL     │ 22 kg ($49)          │ never               │ 22 kg            │
  └────────┴──────────────────────┴─────────────────────┴──────────────────┘

  So Heavy is an option at every weight, and the cheapest one wins. That fits
  your 40 kg tests and the brief's "address overweight parcels".

  Open decisions

  1. Is Heavy a size? (this decides the others)

  A) ParcelSize.Heavy                  B) Size stays dimensional,
                                          line item adds what it's charged as
  ParcelLineItem                       ParcelLineItem
    Size            = Heavy              Size            = Small
    BaseCost        = 50                 ChargedAs/Type  = Heavy
    OverweightCharge= 0                  BaseCost        = 50
                                         OverweightCharge= 0

  - A is the smallest change. The enum's doc comment already says "the size
    category a parcel is charged at". But it breaks the spec rule "Weight MUST
    NOT affect the parcel's size", which would have to become "weight decides
    whether a parcel is charged as Heavy". Heavy also sits awkwardly next to
    dimensional sizes.
  - B keeps Size accurate. A 40 kg parcel that is 5 cm on every side is still
    Small, and the line item also says it's charged at the Heavy rate. It adds
    one public concept, but no information is lost. That matters if a later rule
    ever groups parcels by size or by type, since we'd otherwise have to decide
    which "small" counts.

  I lean towards B. It keeps two separate facts apart and doesn't contradict an
  existing requirement. A is fine if you'd rather keep the API as small as
  possible. Which do you want?

  2. Proposed defaults for the assumptions log (say if any are wrong):
  - Medium 23–24 kg ties at $50: use the size option. Heavy should only apply
    when it actually saves money.
  - Exactly 50 kg: no extra charge. This matches the existing rule "a parcel at
    its limit pays nothing extra". I'm reading "$50 up to over 50 kg" as "$50 up
    to 50 kg".
  - Part-kg: $1 for each part-kg over 50 kg, as you said. It reuses the existing
    round-up logic at a different rate.
  - Speedy shipping: still doubles every parcel cost, Heavy included. This needs
    no change.
  - Dimensions: they don't limit Heavy, so any size can be charged as Heavy.
    Both of your tests need this.

  3. Spec changes this would cause in parcel-pricing:
  - New requirement: Heavy pricing, and choosing the cheapest option.
  - "Each size has a fixed cost" currently claims the smallest size is the
    cheapest option at any weight. That's still true among the sizes, but it's
    no longer the whole story.
  - "Heavy parcel keeps its size" (5x5x5 cm, 20 kg is Small) still passes ($41 <
    $50) under either option.

  Once you've answered question 1, running /opsx:propose heavy-parcel will turn
  this into a change with a proposal, specs, design and tasks.

❯ B

● B it is. Size stays based on dimensions only, and the line item also records
  which rate it was charged at. The existing spec rule "Weight MUST NOT affect
  the parcel's size" stays true as written.

  What to call the new thing

  ParcelLineItem(Parcel, Size, Rate, BaseCost, OverweightCharge)
                                ^^^^
                      which price list was used

  Option: ParcelRate { BySize, Heavy }
  40 kg Small parcel: Size=Small, Rate=Heavy
  0.5 kg Small parcel: Size=Small, Rate=BySize
  Notes: Says only what's new. An enum leaves room for other rates later.
  ────────────────────────────────────────
  Option: ParcelType { Small, Medium, Large, ExtraLarge, Heavy }
  40 kg Small parcel: Size=Small, Type=Heavy
  0.5 kg Small parcel: Size=Small, Type=Small
  Notes: Matches the brief's wording ("parcel type"), but four of its five
  values
    repeat Size.
  ────────────────────────────────────────
  Option: bool IsHeavy
  40 kg Small parcel: IsHeavy=true
  0.5 kg Small parcel: IsHeavy=false
  Notes: Smallest change, but it's a loose primitive, which CLAUDE.md asks us to

    avoid, and it can't grow.

  I recommend ParcelRate { BySize, Heavy } on a Rate property. It doesn't repeat
  Size, and it reads naturally: "Small, charged at the Heavy rate". I'm
  avoiding the name Standard because Shipping.Standard already uses it.

  Do you want ParcelRate / Rate, or one of the other two?

  Still open from last time

  You haven't confirmed these defaults yet, so I'm not treating them as agreed:

  - Tie (only Medium at 23–24 kg, both $50): charge the size rate.
  - Exactly 50 kg: no extra charge.
  - Part-kg over 50: $1 each, rounded up.
  - Speedy shipping: doubles Heavy costs as well.
  - Dimensions: they don't limit Heavy.

  Once you've settled the name and confirmed or changed these defaults,
  /opsx:propose heavy-parcel will write the change on this branch.

❯ those defaults are ok
  /opsx:propose

● Skill(opsx:propose)
  ⎿  Successfully loaded skill · 1 tool allowed

● Bash(openspec context --json >/dev/null && echo ok; openspec new change
      heavy-parcel && openspec status --change heavy-parcel --json; openspec
      instructions proposal…)
  ⎿  ok
     - Creating change 'heavy-parcel' with schema 'spec-driven'...
     Created change 'heavy-parcel' at openspec/changes/heavy-parcel/
     … +172 lines (ctrl+o to expand)
  ⎿  Created openspec/changes/heavy-parcel/.openspec.yaml (+2 -0)
      1 +schema: spec-driven                                             
      2 +created: 2026-11-11                                             

  Ran 1 shell command (ctrl+o to expand)

● Write(openspec/changes/heavy-parcel/proposal.md)
  ⎿  Wrote 33 lines to openspec/changes/heavy-parcel/proposal.md
       1 # Proposal
       2
       3 ## Why
       4
       5 Overweight charges for some goods were excessive. A very heavy p
         arcel pays $2 for every kilogram over its size's small weight li
         mit, so a 40 kg parcel costs $81–$85 whatever its dimensions. A
         new Heavy parcel rate caps that: a flat $50 for up to 50 kg, the
         n $1 per kilogram over.
       6
     … +27 lines (ctrl+o to expand)

  Ran 1 shell command (ctrl+o to expand)

● Writing the parcel-pricing delta spec.

● Write(openspec/changes/heavy-parcel/specs/parcel-pricing/spec.md)
  ⎿  Wrote 127 lines to
     openspec/changes/heavy-parcel/specs/parcel-pricing/spec.md
       1 # Spec Delta
       2
       3 ## ADDED Requirements
       4
       5 ### Requirement: Heavy rate prices parcels by weight alone
       6 Any parcel, whatever its dimensions, SHALL have a Heavy rate cos
         t: a base cost of $50 plus $1 for each kilogram over 50 kg, roun
         ding any part of a kilogram up to a whole kilogram. A parcel at
         or under 50 kg MUST have an overweight charge of $0 at the Heavy
          rate.
     … +121 lines (ctrl+o to expand)

  Ran 1 shell command (ctrl+o to expand)

● Write(openspec/changes/heavy-parcel/design.md)
  ⎿  Wrote 66 lines to openspec/changes/heavy-parcel/design.md
       1 # Design
       2
       3 ## Context
       4
       5 See proposal.md for the motivation. Today the internal `ParcelPr
         icing.Price(parcel)` classifies a parcel by its largest dimensio
         n. It then returns `ParcelLineItem(Parcel, Size, BaseCost, Overw
         eightCharge)` using the size's price, the size's weight limit an
         d $2 per part-kg over. It never compares options. `docs/assumpti
         ons.md` ("Cheapest option is the smallest size that fits") says
     … +62 lines (~5 KB) (ctrl+o to expand)

  Ran 1 shell command (ctrl+o to expand)

● Write(openspec/changes/heavy-parcel/tasks.md)
  ⎿  Wrote 36 lines to openspec/changes/heavy-parcel/tasks.md
      1 # Tasks
      2
      3 ## 1. Rate on parcel line items (no price change)
      4
      5 - [ ] 1.1 Add the public enum `ParcelRate { BySize, Heavy }` with
         XML doc comments. Change `ParcelLineItem` to `(Parcel, Size, Rat
        e, BaseCost, OverweightCharge)`, with `ParcelPricing` always sett
        ing `ParcelRate.BySize` for now. Update every expected `new Parce
        lLineItem(...)` in the tests to pass `ParcelRate.BySize`. Add an
        assertion that a 0.5 kg Small parcel's line item has `Rate == Par
     … +32 lines (~3 KB) (ctrl+o to expand)

  Ran 1 shell command (ctrl+o to expand)

● Update(openspec/changes/heavy-parcel/specs/parcel-pricing/spec.md)
  ⎿  Added 1 line, removed 1 line
      72  - **WHEN** a Small parcel weighing 1 kg, a Medium weighing 3 kg
          , a Large weighing 6 kg and an XL weighing 10 kg are each quote
          d
      73  - **THEN** each has an overweight charge of $0
      74  
      75 -#### Scenario: Weight does not change size                     
      75 +#### Scenario: Heavy parcel keeps its size                     
      76  - **WHEN** a parcel measuring 5 x 5 x 5 cm and weighing 20 kg i
          s quoted
      77  - **THEN** its size is Small and it is charged at its size rate
      78  

  Ran 2 shell commands (ctrl+o to expand)

● I've created the heavy-parcel change on the heavy-parcel branch and committed
  it as 7c44ac1 Propose heavy parcel rate change. It passes openspec validate
  --strict. No code has been written yet.

  One choice I made for you: you picked option B but didn't pick a name, so I
  went with the one I recommended. That's a new enum ParcelRate { BySize, Heavy
  }, and ParcelLineItem becomes (Parcel, Size, Rate, BaseCost,
  OverweightCharge). It's easy to rename before you start building it if you'd
  prefer something else.

  What's in the change (in openspec/changes/heavy-parcel/):
  - proposal.md (openspec/changes/heavy-parcel/proposal.md): why the change is
    needed, the Heavy rate, choosing the cheaper rate, and the breaking change
    to ParcelLineItem.
  - specs/parcel-pricing/spec.md
    (openspec/changes/heavy-parcel/specs/parcel-pricing/spec.md): the spec
    changes.
    - Two new requirements: the Heavy rate, and charging each parcel at its
      cheaper rate. Your two 40 kg tests are scenarios here, along with 60 kg,
      50.2 kg, exactly 50 kg, where the cheaper rate switches over (Small at 24
      kg vs 24.1 kg), and the Medium 24 kg tie.
    - Four updated requirements: the line item shows its rate, and speedy
      shipping gets a 40 kg scenario ($50 + $50 = $100).
    - The old claim that the smallest size is always cheapest now says only that
      a larger size is never cheaper.
  - design.md (openspec/changes/heavy-parcel/design.md): ParcelPricing prices
    both rates and uses Heavy only when it's strictly cheaper, so a tie goes to
    the size rate. Both rates share the overweight round-up logic, and the
    alternatives we rejected are noted.
  - tasks.md (openspec/changes/heavy-parcel/tasks.md): three groups.
    a. Add Rate with no price change, updating about 20 expected line items in
       the tests to BySize.
    b. Add the Heavy rate test-first, starting with your 40 kg examples, along
       with the entries for docs/assumptions.md.
    c. Final checks.

  One wording quirk: the existing scenario "Heavy parcel keeps its size" (20 kg
  Small) had to keep its name, because validation won't let an updated
  requirement drop or rename one of its scenarios. It now also says that parcel
  is charged at its size rate. Under the new rate the name reads a bit oddly,
  and it would need a separate rename if you want to change it.

  When you're ready, run /opsx:apply to build it.

❯ /opsx:apply

  Ran 1 shell command (ctrl+o to expand)

● Task 1.1: add the rate and migrate the tests.

● Bash(cd /home/sam/workspace/courier-kata
      cat > src/CourierKata/ParcelRate.cs <<'EOF'…)
  ⎿  tests/CourierKata.Tests/OverweightChargeTests.cs:12:        Assert.Equ
     al([new ParcelLineItem(parcel, ParcelSize.Small, ParcelRate.BySize, 3m
     , 2m)], quote.Items);
     … +41 lines (ctrl+o to expand)
  ⎿  Created src/CourierKata/ParcelRate.cs (+11 -0)
       1 +namespace CourierKata;                                         
       2 +                                                               
       3 +/// <summary>Which price list a parcel is charged at.</summary>
       4 +public enum ParcelRate                                         
       5 +{                                                              
       6 +    /// <summary>The price of the parcel's size, plus $2 per ki
         +logram over that size's weight limit.</summary>                
       7 +    BySize,                                                    
       8 +                                                               
       9 +    /// <summary>A flat $50 for any dimensions, plus $1 per kil
         +ogram over 50 kg.</summary>                                    
      10 +    Heavy,                                                     
      11 +}                                                              
  ⎿  Updated src/CourierKata/ParcelLineItem.cs (+3 -3)
       1  namespace CourierKata;
       2  
       3  /// <summary>
       4 -/// One parcel in a quote: the size it is charged at, the base 
         -cost of that size, and any                                     
       5 -/// charge for being over the size's weight limit, all in dolla
         -rs.                                                            
       4 +/// One parcel in a quote: its size (from its dimensions), the 
         +rate it is charged at, and that                                
       5 +/// rate's base cost and charge for being over the rate's weigh
         +t limit, both in dollars.                                      
       6  /// </summary>
       7 -public sealed record ParcelLineItem(Parcel Parcel, ParcelSize S
         -ize, decimal BaseCost, decimal OverweightCharge) : LineItem    
       7 +public sealed record ParcelLineItem(Parcel Parcel, ParcelSize S
         +ize, ParcelRate Rate, decimal BaseCost, decimal OverweightCharg
         +e) : LineItem                                                  
       8  {
       9      /// <summary>The base cost plus the overweight charge, in d
          ollars.</summary>
      10      public override decimal Cost => BaseCost + OverweightCharge
          ;
  ⎿  Updated src/CourierKata/ParcelPricing.cs (+1 -1)
       8      public static ParcelLineItem Price(Parcel parcel)
       9      {
      10          var size = Classify(parcel);
      11 -        return new ParcelLineItem(parcel, size, PriceOf(size), 
         -OverweightCharge(parcel.Weight, size));                        
      11 +        return new ParcelLineItem(parcel, size, ParcelRate.BySi
         +ze, PriceOf(size), OverweightCharge(parcel.Weight, size));     
      12      }
      13  
      14      private static ParcelSize Classify(Parcel parcel)
  ⎿  Updated tests/CourierKata.Tests/OverweightChargeTests.cs (+4 -4)
       9  
      10          var quote = QuoteFor(parcel);
      11  
      12 -        Assert.Equal([new ParcelLineItem(parcel, ParcelSize.Sma
         -ll, 3m, 2m)], quote.Items);                                    
      12 +        Assert.Equal([new ParcelLineItem(parcel, ParcelSize.Sma
         +ll, ParcelRate.BySize, 3m, 2m)], quote.Items);                 
      13          Assert.Equal(5m, quote.Items[0].Cost);
      14          Assert.Equal(5m, quote.Total);
      15      }
     ...
      26          var quote = QuoteFor(parcel);
      27  
      28          Assert.Equal(
      29 -            [new ParcelLineItem(parcel, expectedSize, expectedB
         -aseCost, expectedOverweightCharge)],                           
      29 +            [new ParcelLineItem(parcel, expectedSize, ParcelRat
         +e.BySize, expectedBaseCost, expectedOverweightCharge)],        
      30              quote.Items);
      31          Assert.Equal(expectedBaseCost + expectedOverweightCharg
          e, quote.Total);
      32      }
     ...
      50      {
      51          var parcel = new Parcel(5m, 5m, 5m, 0.5m);
      52  
      53 -        Assert.Equal([new ParcelLineItem(parcel, ParcelSize.Sma
         -ll, 3m, 0m)], QuoteFor(parcel).Items);                         
      53 +        Assert.Equal([new ParcelLineItem(parcel, ParcelSize.Sma
         +ll, ParcelRate.BySize, 3m, 0m)], QuoteFor(parcel).Items);      
      54      }
      55  
      56      [Fact]
     ...
      71  
      72          Assert.Equal(
      73              [
      74 -                new ParcelLineItem(parcel, ParcelSize.Small, 3m
         -, 2m),                                                         
      74 +                new ParcelLineItem(parcel, ParcelSize.Small, Pa
         +rcelRate.BySize, 3m, 2m),                                      
      75                  new SpeedyShippingLineItem(5m),
      76              ],
      77              quote.Items);
  ⎿  Updated tests/CourierKata.Tests/ParcelLineItemTests.cs (+2 -2)
       7      [Fact]
       8      public void Cost_is_the_base_cost_plus_the_overweight_charg
          e()
       9      {
      10 -        var item = new ParcelLineItem(Small, ParcelSize.Small, 
         -3m, 2m);                                                       
      10 +        var item = new ParcelLineItem(Small, ParcelSize.Small, 
         +ParcelRate.BySize, 3m, 2m);                                    
      11  
      12          Assert.Equal(5m, item.Cost);
      13      }
     ...
      15      [Fact]
      16      public void Cost_stays_consistent_when_a_copy_changes_the_o
          verweight_charge()
      17      {
      18 -        var item = new ParcelLineItem(Small, ParcelSize.Small, 
         -3m, 0m);                                                       
      18 +        var item = new ParcelLineItem(Small, ParcelSize.Small, 
         +ParcelRate.BySize, 3m, 0m);                                    
      19  
      20          var heavier = item with { OverweightCharge = 4m };
      21  
  ⎿  … 3 more files changed

● Add the rate assertion to the within-limit test, then run tests.

● Bash(cd /home/sam/workspace/courier-kata
      python3 - <<'EOF'…)
  ⎿    Determining projects to restore...
       All projects are up-to-date for restore.
       CourierKata -> /artifacts/bin/CourierKata/debug/CourierKata.dll
     … +8 lines (ctrl+o to expand)
  ⎿  Updated tests/CourierKata.Tests/OverweightChargeTests.cs (+4 -1)
      50      {
      51          var parcel = new Parcel(5m, 5m, 5m, 0.5m);
      52  
      53 -        Assert.Equal([new ParcelLineItem(parcel, ParcelSize.Sma
         -ll, ParcelRate.BySize, 3m, 0m)], QuoteFor(parcel).Items);      
      53 +        var item = Assert.IsType<ParcelLineItem>(Assert.Single(
         +QuoteFor(parcel).Items));                                      
      54 +                                                               
      55 +        Assert.Equal(new ParcelLineItem(parcel, ParcelSize.Smal
         +l, ParcelRate.BySize, 3m, 0m), item);                          
      56 +        Assert.Equal(ParcelRate.BySize, item.Rate);            
      57      }
      58  
      59      [Fact]

● Task 1.2: assumptions, then commit group 1.

● Bash(cd /home/sam/workspace/courier-kata
      cat >> docs/assumptions.md <<'EOF'…)  ⎿  36d611e Show the rate each parcel is charged at
  ⎿  Updated docs/assumptions.md (+13 -0)
      224    their overweight charges.
      225  - **Why:** The brief's example (Small 1.2 kg with Speedy costs
            $10) needs it,
      226    and it keeps "speedy doubles the order cost" true.
      227 +                                                              
      228 +## Heavy parcels                                              
      229 +                                                              
      230 +### Heavy is a rate, not a size                               
      231 +                                                              
      232 +- **Unclear:** The brief calls Heavy "a new parcel type", but 
          +the other types                                               
      233 +  are sizes decided by dimensions, and a heavy parcel still ha
          +s dimensions.                                                 
      234 +- **Assumed:** `Size` stays decided by dimensions alone. A par
          +cel line item                                                 
      235 +  also shows the `ParcelRate` it was charged at: `BySize` or `
          +Heavy`. A 40 kg                                               
      236 +  parcel measuring 5 x 5 x 5 cm is Small, charged at the Heavy
          + rate.                                                        
      237 +- **Why:** Keeping both facts means neither is lost. A later r
          +ule that groups                                               
      238 +  parcels by size or by type can choose which one it needs, an
          +d the rule that                                               
      239 +  weight doesn't change size still holds.                     
  ⎿  Updated openspec/changes/heavy-parcel/tasks.md (+2 -2)
      2  
      3  ## 1. Rate on parcel line items (no price change)
      4  
      5 -- [ ] 1.1 Add the public enum `ParcelRate { BySize, Heavy }` wit
        -h XML doc comments. Change `ParcelLineItem` to `(Parcel, Size, R
        -ate, BaseCost, OverweightCharge)`, with `ParcelPricing` always s
        -etting `ParcelRate.BySize` for now. Update every expected `new P
        -arcelLineItem(...)` in the tests to pass `ParcelRate.BySize`. Ad
        -d an assertion that a 0.5 kg Small parcel's line item has `Rate 
        -== ParcelRate.BySize`. Verify that `docker compose run --rm test
        -s` passes with zero warnings.                                   
      6 -- [ ] 1.2 Add to `docs/assumptions.md`, then commit with the cod
        -e: the line item shows the rate separately from the size, and `S
        -ize` stays decided by dimensions alone.                         
      5 +- [x] 1.1 Add the public enum `ParcelRate { BySize, Heavy }` wit
        +h XML doc comments. Change `ParcelLineItem` to `(Parcel, Size, R
        +ate, BaseCost, OverweightCharge)`, with `ParcelPricing` always s
        +etting `ParcelRate.BySize` for now. Update every expected `new P
        +arcelLineItem(...)` in the tests to pass `ParcelRate.BySize`. Ad
        +d an assertion that a 0.5 kg Small parcel's line item has `Rate 
        +== ParcelRate.BySize`. Verify that `docker compose run --rm test
        +s` passes with zero warnings.                                   
      6 +- [x] 1.2 Add to `docs/assumptions.md`, then commit with the cod
        +e: the line item shows the rate separately from the size, and `S
        +ize` stays decided by dimensions alone.                         
      7  
      8  ## 2. Heavy rate and choosing the cheaper rate (test first, star
         ting with the user's examples)
      9  

● Task 2.1: write the failing tests first.

● Bash(cd /home/sam/workspace/courier-kata
      cat > tests/CourierKata.Tests/HeavyRateTests.cs <<'EOF'…)
  ⎿    Failed CourierKata.Tests.HeavyRateTests.Forty_kilogram_parcel_costs_
     50_dollars_at_the_heavy_rate_whatever_its_size(largestDimension: 150,
     expectedSize: ExtraLarge) [14 ms]
     … +5 lines (ctrl+o to expand)
  ⎿  Created tests/CourierKata.Tests/HeavyRateTests.cs (+25 -0)
       1 +namespace CourierKata.Tests;                                   
       2 +                                                               
       3 +public class HeavyRateTests                                    
       4 +{                                                              
       5 +    [Theory]                                                   
       6 +    [InlineData(5, ParcelSize.Small)]                          
       7 +    [InlineData(150, ParcelSize.ExtraLarge)]                   
       8 +    public void Forty_kilogram_parcel_costs_50_dollars_at_the_h
         +eavy_rate_whatever_its_size(                                   
       9 +        int largestDimension, ParcelSize expectedSize)         
      10 +    {                                                          
      11 +        var parcel = new Parcel(largestDimension, 5m, 5m, 40m);
      12 +                                                               
      13 +        var quote = QuoteFor(parcel);                          
      14 +                                                               
      15 +        Assert.Equal([new ParcelLineItem(parcel, expectedSize, 
         +ParcelRate.Heavy, 50m, 0m)], quote.Items);                     
      16 +        Assert.Equal(50m, quote.Total);                        
      17 +    }                                                          
      18 +                                                               
      19 +    private static Quote QuoteFor(Parcel parcel, Shipping shipp
         +ing = Shipping.Standard)                                       
      20 +    {                                                          
      21 +        var cart = new Cart { Shipping = shipping };           
      22 +        cart.AddParcel(parcel);                                
      23 +        return cart.GetQuote();                                
      24 +    }                                                          
      25 +}                                                              

● Failing as expected. Now the implementation.

● Bash(cd /home/sam/workspace/courier-kata
      python3 - <<'EOF'…)
  ⎿          return heavy.Cost < bySize.Cost ? heavy : bySize;
         }
     … +40 lines (ctrl+o to expand)
  ⎿  Updated src/CourierKata/ParcelPricing.cs (+25 -5)
       1  namespace CourierKata;
       2  
       3 -/// <summary>The rules that decide what size a parcel is charge
         -d at and what it costs.</summary>                              
       3 +/// <summary>The rules that decide what size a parcel is, which
         + rate it is charged at, and what it costs.</summary>           
       4  internal static class ParcelPricing
       5  {
       6      private const decimal OverweightChargePerKilogram = 2m;
       7  
       8 +    private const decimal HeavyBaseCost = 50m;                 
       9 +    private const decimal HeavyWeightLimit = 50m;              
      10 +    private const decimal HeavyOverweightChargePerKilogram = 1m
         +;                                                              
      11 +                                                               
      12 +    /// <summary>Prices the parcel at its size rate and at the 
         +Heavy rate, and keeps the cheaper. A tie keeps the size rate.</
         +summary>                                                       
      13      public static ParcelLineItem Price(Parcel parcel)
      14      {
      15          var size = Classify(parcel);
      11 -        return new ParcelLineItem(parcel, size, ParcelRate.BySi
         -ze, PriceOf(size), OverweightCharge(parcel.Weight, size));     
      16 +                                                               
      17 +        var bySize = new ParcelLineItem(                       
      18 +            parcel,                                            
      19 +            size,                                              
      20 +            ParcelRate.BySize,                                 
      21 +            PriceOf(size),                                     
      22 +            OverweightCharge(parcel.Weight, WeightLimitOf(size)
         +, OverweightChargePerKilogram));                               
      23 +                                                               
      24 +        var heavy = new ParcelLineItem(                        
      25 +            parcel,                                            
      26 +            size,                                              
      27 +            ParcelRate.Heavy,                                  
      28 +            HeavyBaseCost,                                     
      29 +            OverweightCharge(parcel.Weight, HeavyWeightLimit, H
         +eavyOverweightChargePerKilogram));                             
      30 +                                                               
      31 +        return heavy.Cost < bySize.Cost ? heavy : bySize;      
      32      }
      33  
      34      private static ParcelSize Classify(Parcel parcel)
     ...
      64      };
      65  
      66      /// <summary>Charges for each kilogram over the limit, coun
          ting any part of a kilogram as a whole one.</summary>
      47 -    private static decimal OverweightCharge(decimal weight, Par
         -celSize size)                                                  
     … 8 more lines (ctrl+o to expand)

● Tasks 2.2 and 2.3: the remaining scenarios.

● Bash(cd /home/sam/workspace/courier-kata
      python3 - <<'EOF'…)
  ⎿  Passed!  - Failed:     0, Passed:    70, Skipped:     0, Total:    70,
      Duration: 61 ms - CourierKata.Tests.dll (net10.0)
  ⎿  Updated tests/CourierKata.Tests/HeavyRateTests.cs (+73 -0)
      16          Assert.Equal(50m, quote.Total);
      17      }
      18  
      19 +    [Theory]                                                   
      20 +    [InlineData("60", 10)]                                     
      21 +    [InlineData("50.2", 1)]                                    
      22 +    [InlineData("50", 0)]                                      
      23 +    public void Heavy_rate_charges_1_dollar_per_part_kilogram_o
         +ver_50_kilograms(string weight, int expectedOverweightCharge)  
      24 +    {                                                          
      25 +        var parcel = new Parcel(5m, 5m, 5m, decimal.Parse(weigh
         +t, System.Globalization.CultureInfo.InvariantCulture));        
      26 +                                                               
      27 +        var quote = QuoteFor(parcel);                          
      28 +                                                               
      29 +        Assert.Equal(                                          
      30 +            [new ParcelLineItem(parcel, ParcelSize.Small, Parce
         +lRate.Heavy, 50m, expectedOverweightCharge)],                  
      31 +            quote.Items);                                      
      32 +        Assert.Equal(50m + expectedOverweightCharge, quote.Tota
         +l);                                                            
      33 +    }                                                          
      34 +                                                               
      35 +    [Fact]                                                     
      36 +    public void Size_rate_is_kept_while_it_is_cheaper()        
      37 +    {                                                          
      38 +        var parcel = new Parcel(5m, 5m, 5m, 24m);              
      39 +                                                               
      40 +        Assert.Equal(                                          
      41 +            [new ParcelLineItem(parcel, ParcelSize.Small, Parce
         +lRate.BySize, 3m, 46m)],                                       
      42 +            QuoteFor(parcel).Items);                           
      43 +    }                                                          
      44 +                                                               
      45 +    [Fact]                                                     
      46 +    public void Heavy_rate_is_used_once_it_is_cheaper()        
      47 +    {                                                          
      48 +        var parcel = new Parcel(5m, 5m, 5m, 24.1m);            
      49 +                                                               
      50 +        Assert.Equal(                                          
      51 +            [new ParcelLineItem(parcel, ParcelSize.Small, Parce
         +lRate.Heavy, 50m, 0m)],                                        
      52 +            QuoteFor(parcel).Items);                           
      53 +    }                                                          
      54 +                                                               
      55 +    [Fact]                                                     
     … 39 more lines (ctrl+o to expand)

● The existing 20 kg test in OverweightChargeTests covers the same scenario, so
  I'll extend it instead of duplicating.

● Bash(cd /home/sam/workspace/courier-kata
      python3 - <<'EOF'…)
  ⎿  Passed!  - Failed:     0, Passed:    69, Skipped:     0, Total:    69,
      Duration: 53 ms - CourierKata.Tests.dll (net10.0)
  ⎿  Updated tests/CourierKata.Tests/HeavyRateTests.cs (+0 -10)
      64      }
      65  
      66      [Fact]
      67 -    public void Heavy_parcel_keeps_its_size_and_the_size_rate_w
         -hen_cheaper()                                                  
      68 -    {                                                          
      69 -        var item = Assert.IsType<ParcelLineItem>(              
      70 -            Assert.Single(QuoteFor(new Parcel(5m, 5m, 5m, 20m))
         -.Items));                                                      
      71 -                                                               
      72 -        Assert.Equal(ParcelSize.Small, item.Size);             
      73 -        Assert.Equal(ParcelRate.BySize, item.Rate);            
      74 -    }                                                          
      75 -                                                               
      76 -    [Fact]                                                     
      67      public void Speedy_shipping_doubles_heavy_rate_costs_too()
      68      {
      69          var parcel = new Parcel(5m, 5m, 5m, 40m);
  ⎿  Updated tests/CourierKata.Tests/OverweightChargeTests.cs (+1 -0)
      63              Assert.Single(QuoteFor(new Parcel(5m, 5m, 5m, 20m))
          .Items));
      64  
      65          Assert.Equal(ParcelSize.Small, item.Size);
      66 +        Assert.Equal(ParcelRate.BySize, item.Rate);            
      67      }
      68  
      69      [Fact]

● Task 2.4: assumptions, then commit.

● Bash(cd /home/sam/workspace/courier-kata
      python3 - <<'EOF'…)
  ⎿  Passed!  - Failed:     0, Passed:    69, Skipped:     0, Total:    69,
      Duration: 53 ms - CourierKata.Tests.dll (net10.0)
     225065c Charge heavy parcels at a flat $50 rate when cheaper
  ⎿  Updated docs/assumptions.md (+44 -8)
      57  - **Why:** The brief only uses `$` and has no multi-currency re
          quirement, so a
      58    `Money` type would add nothing yet. `decimal` keeps amounts e
          xact.
      59  
      60 -### Cheapest option is the smallest size that fits             
      60 +### Cheapest option is the size rate or the Heavy rate, whichev
         +er is less                                                     
      61  
      62  - **Unclear:** "In all circumstances the cheapest option for se
          nding each parcel
      63    should be selected" implies a choice between options.
      64 -- **Assumed:** The only option is the smallest size the parcel'
         -s dimensions                                                   
      65 -  fit. It is also the cheapest, so no general "compare every op
         -tion" logic is                                                 
      66 -  built.                                                       
      67 -- **Why:** Prices rise with size. Weight limits also rise with 
         -size, but each                                                 
      68 -  step up adds more base price than its higher limit can save i
         -n overweight                                                   
      64 +- **Assumed:** Each parcel is priced at its size rate (the smal
         +lest size its                                                  
      65 +  dimensions fit, plus that size's overweight charge) and at th
         +e Heavy rate,                                                  
      66 +  and is charged at the cheaper. Larger sizes are never compare
         +d.                                                             
      67 +- **Why:** Among sizes, the smallest that fits is always cheape
         +st. Each step up                                               
      68 +  adds more base price than its higher weight limit can save in
         + overweight                                                    
      69    charges: Small to Medium adds $5 but saves at most $4 (2 kg),
           Medium to Large
      70    adds $7 but saves at most $6, and Large to XL adds $10 but sa
          ves at most $8.
      71 -  So the smallest size is cheapest at every weight. A selector 
         -belongs with a                                                 
      72 -  later rule that can make a larger option cheaper.            
      71 +  The Heavy rate is different: its flat $50 becomes cheaper tha
         +n the size rate                                                
      72 +  from just over 22 kg (XL) to 24 kg (Small and Medium).       
      73  
      74  ## Validation
      75  
     ...
      237  - **Why:** Keeping both facts means neither is lost. A later r
           ule that groups
      238    parcels by size or by type can choose which one it needs, an
           d the rule that
      239    weight doesn't change size still holds.
      240 +                                                              
      241 +### Any parcel can use the Heavy rate                         
      242 +                                                              
      243 +- **Unclear:** Whether the Heavy rate is limited to certain di
          +mensions or                                                   
      244 +  weights.                                                    
      245 +- **Assumed:** Every parcel is priced at the Heavy rate as wel
          +l as its size                                                 
      246 +  rate, whatever its dimensions or weight. It's only used when
          + cheaper.                                                     
      247 +- **Why:** The examples charge $50 for 40 kg parcels with both
          + Small and XL                                                 
      248 +  dimensions, and 40 kg is under Heavy's 50 kg limit, so Heavy
          + can't need a                                                 
      249 +  minimum weight or a size.                                   
     … 26 more lines (ctrl+o to expand)
  ⎿  Updated openspec/changes/heavy-parcel/tasks.md (+4 -4)
       7  
       8  ## 2. Heavy rate and choosing the cheaper rate (test first, sta
          rting with the user's examples)
       9  
      10 -- [ ] 2.1 Write failing tests in `HeavyRateTests`: a 5 x 5 x 5 
         -cm parcel weighing 40 kg and a 150 x 5 x 5 cm parcel weighing 4
         -0 kg each have a quote total of $50, keep their size (Small, XL
         -), and are charged at `ParcelRate.Heavy` with a base cost of $5
         -0 and an overweight charge of $0. Make the overweight helper in
         - `ParcelPricing` take the limit and per-kg rate, add the Heavy 
         -constants ($50, 50 kg, $1/kg), and pick Heavy only when it is s
         -trictly cheaper. Verify with `docker compose run --rm tests`.  
      11 -- [ ] 2.2 Add tests for these spec scenarios, implementing anyt
         -hing still missing:                                            
      10 +- [x] 2.1 Write failing tests in `HeavyRateTests`: a 5 x 5 x 5 
         +cm parcel weighing 40 kg and a 150 x 5 x 5 cm parcel weighing 4
         +0 kg each have a quote total of $50, keep their size (Small, XL
         +), and are charged at `ParcelRate.Heavy` with a base cost of $5
         +0 and an overweight charge of $0. Make the overweight helper in
         + `ParcelPricing` take the limit and per-kg rate, add the Heavy 
         +constants ($50, 50 kg, $1/kg), and pick Heavy only when it is s
         +trictly cheaper. Verify with `docker compose run --rm tests`.  
      11 +- [x] 2.2 Add tests for these spec scenarios, implementing anyt
         +hing still missing:                                            
      12    - 60 kg costs $50 + $10
      13    - 50.2 kg costs $50 + $1
      14    - exactly 50 kg costs $50 + $0
     ...
      18    - 20 kg Small is still Small and charged at the size rate
      19  
      20    Verify with `docker compose run --rm tests`.
      21 -- [ ] 2.3 Add a test that a 40 kg 5 x 5 x 5 cm parcel with Spee
         -dy shipping gives a parcel item of $50, a speedy item of $50 an
         -d a total of $100. No code change should be needed. Verify with
         - `docker compose run --rm tests`.                              
      22 -- [ ] 2.4 Update `docs/assumptions.md`, then commit with the co
         -de:                                                            
      21 +- [x] 2.3 Add a test that a 40 kg 5 x 5 x 5 cm parcel with Spee
         +dy shipping gives a parcel item of $50, a speedy item of $50 an
         +d a total of $100. No code change should be needed. Verify with
         + `docker compose run --rm tests`.                              
      22 +- [x] 2.4 Update `docs/assumptions.md`, then commit with the co
         +de:                                                            
      23    - add: any parcel can use the Heavy rate, whatever its dimens
          ions
      24    - add: the cheaper rate is chosen, and a tie goes to the size
           rate
      25    - add: exactly 50 kg pays nothing extra, and part-kilograms o
          ver 50 kg round up at $1 each

● Task 3.1: final checks.

  Ran 1 shell command (ctrl+o to expand)

● Bash(cd /home/sam/workspace/courier-kata
      sed -i 's/- \[ \] 3\.1 /- [x] 3.1 /'
      openspec/changes/heavy-parcel/tasks.md…)
  ⎿  all_done {'total': 7, 'complete': 7, 'remaining': 0}
     5cc08df Mark heavy-parcel final checks complete
     225065c Charge heavy parcels at a flat $50 rate when cheaper
     … +2 lines (ctrl+o to expand)
  ⎿  Updated openspec/changes/heavy-parcel/tasks.md (+1 -1)
      28  
      29  ## 3. Final checks
      30  
      31 -- [ ] 3.1 Check the public surface: it should be unchanged apar
         -t from the new `ParcelRate` and the `Rate` property on `ParcelL
         -ineItem`. Check that every scenario in `specs/parcel-pricing/sp
         -ec.md` has a matching test. Verify that `docker compose run --r
         -m tests` passes with zero warnings.                            
      31 +- [x] 3.1 Check the public surface: it should be unchanged apar
         +t from the new `ParcelRate` and the `Rate` property on `ParcelL
         +ineItem`. Check that every scenario in `specs/parcel-pricing/sp
         +ec.md` has a matching test. Verify that `docker compose run --r
         +m tests` passes with zero warnings.                            
      32  
      33  ## Workflow follow-up
      34  

● The Heavy rate is built. All 7 tasks in the heavy-parcel change are done, and
  all 69 tests pass with no warnings. Both of your test cases come out at $50: a
  40 kg parcel with Small dimensions, and a 40 kg parcel with XL dimensions.

  How it works now
  - Each parcel is priced at its size rate and at the Heavy rate, and is charged
    at whichever is cheaper. If both cost the same, it's charged at the size
    rate.
  - The Heavy rate is $50 up to 50 kg, then $1 for each kilogram or
    part-kilogram over 50 kg.
  - Size is still based only on dimensions, so a 40 kg parcel measuring 5 x 5 x
    5 cm is still Small.
  - Parcel line items now show the rate they were charged at, using a new enum,
    ParcelRate { BySize, Heavy }. This changes the ParcelLineItem constructor,
    so the existing expected values in the tests now pass ParcelRate.BySize.

  Changes
  - ParcelRate.cs (src/CourierKata/ParcelRate.cs) is the new enum.
  - ParcelLineItem.cs (src/CourierKata/ParcelLineItem.cs) gains the Rate
    property.
  - ParcelPricing.cs (src/CourierKata/ParcelPricing.cs) prices both rates and
    picks the cheaper. Both rates use the same overweight helper, which now
    takes the weight limit and the charge per kilogram.
  - HeavyRateTests.cs (tests/CourierKata.Tests/HeavyRateTests.cs) covers:
    - your two 40 kg examples
    - 60 kg, 50.2 kg and exactly 50 kg
    - Small at 24 kg ($49 at the size rate) vs 24.1 kg ($50 at the Heavy rate)
    - the Medium 24 kg tie at $50
    - speedy shipping on a 40 kg parcel ($50 + $50 = $100)
  - The existing test that a 20 kg Small parcel keeps its size now also checks
    it's charged at the size rate.
  - docs/assumptions.md:
    - The "cheapest option" entry now compares the size rate with the Heavy
      rate.
    - There's a new "Heavy parcels" section with five entries, including the tie
      rule and that any parcel can use the Heavy rate.

  Commits on heavy-parcel, each with its tests passing:
  - 36d611e Show the rate each parcel is charged at
  - 225065c Charge heavy parcels at a flat $50 rate when cheaper
  - 5cc08df Mark heavy-parcel final checks complete

  /opsx:archive will copy the spec changes into
  openspec/specs/parcel-pricing/spec.md and commit them.

❯ /opsx:archive

  Ran 1 shell command (ctrl+o to expand)

● Using change: heavy-parcel. All 4 artifacts and 7/7 tasks are complete. The
  parcel-pricing delta isn't synced yet; it adds 2 requirements and modifies 4.

● User answered Claude's questions:
  ⎿  · The heavy-parcel delta adds 2 requirements (Heavy rate, cheaper rate
     chosen) and modifies 4 in openspec/specs/parcel-pricing/spec.md. Sync
     before archiving? → Sync now (Recommended)

● Bash(cd /home/sam/workspace/courier-kata; openspec instructions specs --change
      heavy-parcel --json | python3 -c "import
      json,sys;print(json.load(sys.stdin).get('rule…)
  ⎿  None
     Specification 'parcel-pricing' is valid
      openspec/specs/parcel-pricing/spec.md | 58 ++++++++++++++++++++++++++
     … +5 lines (ctrl+o to expand)
  ⎿  Updated openspec/specs/parcel-pricing/spec.md (+52 -6)
      68  - **THEN** each is classified as Large
      69  
      70  ### Requirement: Each size has a fixed cost
      71 -Each parcel SHALL have a base cost equal to the fixed price of 
         -its size: Small $3, Medium $8, Large $15, XL $25. A parcel's co
         -st MUST be its base cost plus its overweight charge. Because a 
         -larger size raises the base price by more than its higher weigh
         -t limit can save, the smallest size a parcel fits is also its c
         -heapest option at any weight.                                  
      71 +Each parcel SHALL have a size rate base cost equal to the fixed
         + price of its size: Small $3, Medium $8, Large $15, XL $25. A p
         +arcel's cost MUST be its base cost plus its overweight charge. 
         +Because a larger size raises the base price by more than its hi
         +gher weight limit can save, charging a parcel at a larger size 
         +is never cheaper than charging it at the size its dimensions fi
         +t.                                                             
      72  
      73  #### Scenario: Cost per size
      74  - **WHEN** a Small, a Medium, a Large and an XL parcel, each wi
          thin its weight limit, are each quoted on their own
      75  - **THEN** their costs are $3, $8, $15 and $25 respectively
      76  
      77  ### Requirement: Parcels over their size's weight limit pay an 
          overweight charge
      78 -Each size SHALL have a weight limit: Small 1 kg, Medium 3 kg, L
         -arge 6 kg, XL 10 kg. A parcel heavier than its size's limit MUS
         -T pay $2 for each kilogram over the limit, rounding any part of
         - a kilogram up to a whole kilogram. A parcel at or under its li
         -mit MUST pay an overweight charge of $0. Weight MUST NOT affect
         - the parcel's size.                                            
      78 +Each size SHALL have a weight limit: Small 1 kg, Medium 3 kg, L
         +arge 6 kg, XL 10 kg. At its size rate, a parcel heavier than it
         +s size's limit MUST pay $2 for each kilogram over the limit, ro
         +unding any part of a kilogram up to a whole kilogram. A parcel 
         +at or under its limit MUST pay an overweight charge of $0 at it
         +s size rate. Weight MUST NOT affect the parcel's size.         
      79  
      80  #### Scenario: Small parcel part of a kilogram over
      81  - **WHEN** a Small parcel weighing 1.2 kg is quoted
     ...
       99  
      100  #### Scenario: Heavy parcel keeps its size
      101  - **WHEN** a parcel measuring 5 x 5 x 5 cm and weighing 20 kg 
           is quoted
      102 -- **THEN** its size is Small                                  
      102 +- **THEN** its size is Small and it is charged at its size rat
          +e                                                             
      103  
      104 +### Requirement: Heavy rate prices parcels by weight alone    
      105 +Any parcel, whatever its dimensions, SHALL have a Heavy rate c
          +ost: a base cost of $50 plus $1 for each kilogram over 50 kg, 
          +rounding any part of a kilogram up to a whole kilogram. A parc
          +el at or under 50 kg MUST have an overweight charge of $0 at t
          +he Heavy rate.                                                
      106 +                                                              
      107 +#### Scenario: Parcel well over 50 kg                         
      108 +- **WHEN** a parcel measuring 5 x 5 x 5 cm and weighing 60 kg 
          +is quoted                                                     
      109 +- **THEN** it is charged at the Heavy rate with a base cost of
          + $50, an overweight charge of $10 and a cost of $60           
      110 +                                                              
      111 +#### Scenario: Parcel part of a kilogram over 50 kg           
      112 +- **WHEN** a parcel measuring 5 x 5 x 5 cm and weighing 50.2 k
          +g is quoted                                                   
      113 +- **THEN** it is charged at the Heavy rate with an overweight 
          +charge of $1 and a cost of $51                                
      114 +                                                              
      115 +#### Scenario: Parcel exactly at 50 kg                        
      116 +- **WHEN** a parcel measuring 5 x 5 x 5 cm and weighing 50 kg 
          +is quoted                                                     
      117 +- **THEN** it is charged at the Heavy rate with an overweight 
          +charge of $0 and a cost of $50                                
      118 +                                                              
      119 +### Requirement: Each parcel is charged at its cheaper rate   
      120 +Each parcel SHALL be charged at whichever of its size rate and
          + the Heavy rate costs less. Its size rate is its size's base c
          +ost plus that size's overweight charge. When both rates cost t
          +he same, the parcel MUST be charged at its size rate. The rate
          + chosen MUST NOT change the parcel's size.                    
      121 +                                                              
     … 59 more lines (ctrl+o to expand)

● Bash(cd /home/sam/workspace/courier-kata; for s in "Heavy rate prices parcels
      by weight alone" "Each parcel is charged at its cheaper rate" "Heavy rate
      shown on the…)
  ⎿  1
     1
     1
     … +6 lines (ctrl+o to expand)
  ⎿  Created openspec/changes/archive/2026-11-11-heavy-parcel/proposal.md
     (+33 -0)
       1 +# Proposal                                                     
       2 +                                                               
       3 +## Why                                                         
       4 +                                                               
       5 +Overweight charges for some goods were excessive. A very heavy 
         +parcel pays $2 for every kilogram over its size's small weight 
         +limit, so a 40 kg parcel costs $81–$85 whatever its dimensions.
         + A new Heavy parcel rate caps that: a flat $50 for up to 50 kg,
         + then $1 per kilogram over.                                    
       6 +                                                               
       7 +## What Changes                                                
       8 +                                                               
       9 +- New Heavy rate: base cost $50, weight limit 50 kg, and $1 for
         + each kilogram over 50 kg, with any part of a kilogram counted 
         +as a whole one. A parcel at exactly 50 kg pays $50.            
      10 +- Any parcel can use the Heavy rate, whatever its dimensions.  
      11 +- Each parcel is charged at whichever rate is cheaper: its size
         + rate (the size price plus $2 per kg over the size's limit) or 
         +the Heavy rate. When both cost the same, the size rate is used.
         + For example, a 40 kg parcel costs $50 whether its dimensions a
         +re Small or XL.                                                
      12 +- A parcel's size is still decided only by its dimensions. A 40
         + kg parcel measuring 5 x 5 x 5 cm is Small, and it's charged at
         + the Heavy rate.                                               
      13 +- **BREAKING**: A parcel line item shows which rate it was char
         +ged at (`BySize` or `Heavy`), next to its size, base cost and o
         +verweight charge.                                              
      14 +- Speedy shipping still costs the sum of the parcel costs, so i
         +t doubles Heavy-rate costs too.                                
      15 +- Record the new interpretations in `docs/assumptions.md`, and 
         +replace the "cheapest option is the smallest size that fits" en
         +try, which said a selector would come with a rule like this one
         +.                                                              
      16 +                                                               
      17 +Out of scope: discounts, and any change to how parcels are size
         +d.                                                             
      18 +                                                               
      19 +## Capabilities                                                
      20 +                                                               
      21 +### New Capabilities                                           
      22 +                                                               
      23 +None.                                                          
      24 +                                                               
      25 +### Modified Capabilities                                      
      26 +- `parcel-pricing`: adds the Heavy rate and choosing the cheape
         +r rate for each parcel. Parcel line items show their rate. Upda
         +tes the claim that the smallest size is always the cheapest opt
         +ion, and the scenario showing that weight doesn't change size. 
      27 +                                                               
      28 +## Impact                                                      
      29 +                                                               
      30 +- Public API: new enum `ParcelRate { BySize, Heavy }`. `ParcelL
         +ineItem` changes from `(Parcel, Size, BaseCost, OverweightCharg
         +e)` to `(Parcel, Size, Rate, BaseCost, OverweightCharge)`.     
      31 +- Internal: `ParcelPricing` works out both rates and picks the 
         +cheaper one. `Cart`, `Quote` and `SpeedyShippingLineItem` don't
         + change.                                                       
      32 +- Tests: about 20 expected `new ParcelLineItem(...)` values gai
         +n `ParcelRate.BySize`. New tests cover the Heavy rate, choosing
         + between the rates, and the tie case.                          
      33 +- Docs: new and updated entries in `docs/assumptions.md`.      
  ⎿  Created
     openspec/changes/archive/2026-11-11-heavy-parcel/.openspec.yaml (+2
     -0)
      1 +schema: spec-driven                                             
      2 +created: 2026-11-11                                             
  ⎿  Created openspec/changes/archive/2026-11-11-heavy-parcel/tasks.md (+36
     -0)
       1 +# Tasks                                                        
       2 +                                                               
       3 +## 1. Rate on parcel line items (no price change)              
       4 +                                                               
       5 +- [x] 1.1 Add the public enum `ParcelRate { BySize, Heavy }` wi
         +th XML doc comments. Change `ParcelLineItem` to `(Parcel, Size,
         + Rate, BaseCost, OverweightCharge)`, with `ParcelPricing` alway
         +s setting `ParcelRate.BySize` for now. Update every expected `n
         +ew ParcelLineItem(...)` in the tests to pass `ParcelRate.BySize
         +`. Add an assertion that a 0.5 kg Small parcel's line item has 
         +`Rate == ParcelRate.BySize`. Verify that `docker compose run --
         +rm tests` passes with zero warnings.                           
       6 +- [x] 1.2 Add to `docs/assumptions.md`, then commit with the co
         +de: the line item shows the rate separately from the size, and 
         +`Size` stays decided by dimensions alone.                      
       7 +                                                               
       8 +## 2. Heavy rate and choosing the cheaper rate (test first, sta
         +rting with the user's examples)                                
       9 +                                                               
      10 +- [x] 2.1 Write failing tests in `HeavyRateTests`: a 5 x 5 x 5 
         +cm parcel weighing 40 kg and a 150 x 5 x 5 cm parcel weighing 4
         +0 kg each have a quote total of $50, keep their size (Small, XL
         +), and are charged at `ParcelRate.Heavy` with a base cost of $5
         +0 and an overweight charge of $0. Make the overweight helper in
         + `ParcelPricing` take the limit and per-kg rate, add the Heavy 
         +constants ($50, 50 kg, $1/kg), and pick Heavy only when it is s
         +trictly cheaper. Verify with `docker compose run --rm tests`.  
      11 +- [x] 2.2 Add tests for these spec scenarios, implementing anyt
         +hing still missing:                                            
      12 +  - 60 kg costs $50 + $10                                      
      13 +  - 50.2 kg costs $50 + $1                                     
      14 +  - exactly 50 kg costs $50 + $0                               
      15 +  - 24 kg Small is charged at the size rate, $3 + $46 = $49    
      16 +  - 24.1 kg Small is charged at the Heavy rate, $50            
      17 +  - 24 kg Medium is a tie at $50 and is charged at the size rat
         +e, $8 + $42                                                    
      18 +  - 20 kg Small is still Small and charged at the size rate    
      19 +                                                               
      20 +  Verify with `docker compose run --rm tests`.                 
      21 +- [x] 2.3 Add a test that a 40 kg 5 x 5 x 5 cm parcel with Spee
         +dy shipping gives a parcel item of $50, a speedy item of $50 an
         +d a total of $100. No code change should be needed. Verify with
         + `docker compose run --rm tests`.                              
      22 +- [x] 2.4 Update `docs/assumptions.md`, then commit with the co
         +de:                                                            
      23 +  - add: any parcel can use the Heavy rate, whatever its dimens
         +ions                                                           
      24 +  - add: the cheaper rate is chosen, and a tie goes to the size
         + rate                                                          
      25 +  - add: exactly 50 kg pays nothing extra, and part-kilograms o
         +ver 50 kg round up at $1 each                                  
      26 +  - add: speedy shipping doubles Heavy-rate costs too          
      27 +  - rewrite "Cheapest option is the smallest size that fits": i
         +t's still true among sizes, but the size rate is now compared w
         +ith the Heavy rate                                             
      28 +                                                               
      29 +## 3. Final checks                                             
      30 +                                                               
      31 +- [x] 3.1 Check the public surface: it should be unchanged apar
         +t from the new `ParcelRate` and the `Rate` property on `ParcelL
         +ineItem`. Check that every scenario in `specs/parcel-pricing/sp
         +ec.md` has a matching test. Verify that `docker compose run --r
         +m tests` passes with zero warnings.                            
      32 +                                                               
      33 +## Workflow follow-up                                          
      34 +                                                               
      35 +- Archive the change once implementation is complete, keeping `
         +openspec/specs/parcel-pricing/spec.md` in sync with what was bu
         +ilt.                                                           
      36 +- Commit after archiving.                                      
  ⎿  Created openspec/changes/archive/2026-11-11-heavy-parcel/design.md
     (+66 -0)
       1 +# Design                                                       
       2 +                                                               
       3 +## Context                                                     
       4 +                                                               
       5 +See proposal.md for the motivation. Today the internal `ParcelP
         +ricing.Price(parcel)` classifies a parcel by its largest dimens
         +ion. It then returns `ParcelLineItem(Parcel, Size, BaseCost, Ov
         +erweightCharge)` using the size's price, the size's weight limi
         +t and $2 per part-kg over. It never compares options. `docs/ass
         +umptions.md` ("Cheapest option is the smallest size that fits")
         + says a selector would come with a later rule that can make ano
         +ther option cheaper. The Heavy rate is that rule. `Cart.GetQuot
         +e()` and speedy shipping only read `Cost`, so the change stays 
         +inside `ParcelPricing` and `ParcelLineItem`, plus one new enum.
       6 +                                                               
       7 +## Goals / Non-Goals                                           
       8 +                                                               
       9 +**Goals:**                                                     
      10 +- Each parcel line item says both what size the parcel is and w
         +hich rate it was charged at.                                   
      11 +- The choice between rates is made in one place, so a later rat
         +e can be added without changing `Cart`.                        
      12 +                                                               
      13 +**Non-Goals:**                                                 
      14 +- A general list of "pricing options" or a strategy interface. 
         +There are two rates, and a pluggable mechanism would be specula
         +tive.                                                          
      15 +- Re-checking larger sizes as options. Charging at a larger siz
         +e is never cheaper (see the spec), so the size rate always uses
         + the size the dimensions fit.                                  
      16 +                                                               
      17 +## Decisions                                                   
      18 +                                                               
      19 +### `Size` stays dimensional and a new `Rate` says how the parc
         +el was charged (the user chose this)                           
      20 +```                                                            
      21 +public enum ParcelRate { BySize, Heavy }                       
      22 +                                                               
      23 +public sealed record ParcelLineItem(                           
      24 +    Parcel Parcel, ParcelSize Size, ParcelRate Rate, decimal Ba
         +seCost, decimal OverweightCharge) : LineItem                   
      25 +```                                                            
      26 +- `Size` keeps one meaning: the size bracket from the dimension
         +s. The spec rule "Weight MUST NOT affect the parcel's size" sti
         +ll holds.                                                      
      27 +- `Rate` comes straight after `Size`, so the line reads "Small,
         + charged at the Heavy rate, $50 + $0".                         
      28 +- `BaseCost` and `OverweightCharge` are those of the chosen rat
         +e. At the Heavy rate they are $50 and $1 per part-kg over 50 kg
         +. `Cost` is still worked out when read as `BaseCost + Overweigh
         +tCharge`.                                                      
      29 +- The names: `ParcelRate` and `BySize` avoid clashing with `Shi
         +pping.Standard`. `BySize` reads naturally next to `Size`.      
      30 +- Alternatives considered:                                     
      31 +  - `ParcelSize.Heavy`. It's a smaller change, but it contradic
         +ts the size requirement and loses the dimensional size of heavy
         + parcels.                                                      
      32 +  - `ParcelType { Small, Medium, Large, ExtraLarge, Heavy }`. F
         +our of its five values would repeat `Size`.                    
      33 +  - `bool IsHeavy`. It's a loose primitive and can't grow.     
      34 +                                                               
      35 +### `ParcelPricing` prices both rates and keeps the cheaper one
      36 +```                                                            
      37 +size    = Classify(parcel)                                     
      38 +bySize  = (BySize, PriceOf(size), Overweight(weight, LimitOf(si
         +ze), $2))                                                      
      39 +heavy   = (Heavy,  $50,           Overweight(weight, 50 kg,    
         +     $1))                                                      
      40 +chosen  = heavy.Cost < bySize.Cost ? heavy : bySize            
     … 26 more lines (ctrl+o to expand)
  ⎿  Created openspec/changes/archive/2026-11-11-heavy-parcel/specs/parcel-
     pricing/spec.md (+127 -0)
        1 +# Spec Delta                                                  
        2 +                                                              
        3 +## ADDED Requirements                                         
        4 +                                                              
        5 +### Requirement: Heavy rate prices parcels by weight alone    
        6 +Any parcel, whatever its dimensions, SHALL have a Heavy rate c
          +ost: a base cost of $50 plus $1 for each kilogram over 50 kg, 
          +rounding any part of a kilogram up to a whole kilogram. A parc
          +el at or under 50 kg MUST have an overweight charge of $0 at t
          +he Heavy rate.                                                
        7 +                                                              
        8 +#### Scenario: Parcel well over 50 kg                         
        9 +- **WHEN** a parcel measuring 5 x 5 x 5 cm and weighing 60 kg 
          +is quoted                                                     
       10 +- **THEN** it is charged at the Heavy rate with a base cost of
          + $50, an overweight charge of $10 and a cost of $60           
       11 +                                                              
       12 +#### Scenario: Parcel part of a kilogram over 50 kg           
       13 +- **WHEN** a parcel measuring 5 x 5 x 5 cm and weighing 50.2 k
          +g is quoted                                                   
       14 +- **THEN** it is charged at the Heavy rate with an overweight 
          +charge of $1 and a cost of $51                                
       15 +                                                              
       16 +#### Scenario: Parcel exactly at 50 kg                        
       17 +- **WHEN** a parcel measuring 5 x 5 x 5 cm and weighing 50 kg 
          +is quoted                                                     
       18 +- **THEN** it is charged at the Heavy rate with an overweight 
          +charge of $0 and a cost of $50                                
       19 +                                                              
       20 +### Requirement: Each parcel is charged at its cheaper rate   
       21 +Each parcel SHALL be charged at whichever of its size rate and
          + the Heavy rate costs less. Its size rate is its size's base c
          +ost plus that size's overweight charge. When both rates cost t
          +he same, the parcel MUST be charged at its size rate. The rate
          + chosen MUST NOT change the parcel's size.                    
       22 +                                                              
       23 +#### Scenario: Heavy Small parcel                             
       24 +- **WHEN** a parcel measuring 5 x 5 x 5 cm and weighing 40 kg 
          +is quoted                                                     
       25 +- **THEN** its size is Small, it is charged at the Heavy rate 
          +and the quote total is $50                                    
       26 +                                                              
       27 +#### Scenario: Heavy XL parcel                                
       28 +- **WHEN** a parcel measuring 150 x 5 x 5 cm and weighing 40 k
          +g is quoted                                                   
       29 +- **THEN** its size is XL, it is charged at the Heavy rate and
          + the quote total is $50                                       
       30 +                                                              
       31 +#### Scenario: Size rate is cheaper                           
       32 +- **WHEN** a parcel measuring 5 x 5 x 5 cm and weighing 24 kg 
          +is quoted                                                     
       33 +- **THEN** it is charged at its size rate with an overweight c
          +harge of $46 and a cost of $49                                
       34 +                                                              
       35 +#### Scenario: Heavy rate becomes cheaper                     
       36 +- **WHEN** a parcel measuring 5 x 5 x 5 cm and weighing 24.1 k
          +g is quoted                                                   
       37 +- **THEN** it is charged at the Heavy rate and its cost is $50
       38 +                                                              
       39 +#### Scenario: Both rates cost the same                       
       40 +- **WHEN** a parcel measuring 20 x 5 x 5 cm and weighing 24 kg
          + is quoted                                                    
     … 87 more lines (ctrl+o to expand)
  ⎿  … 5 more files changed

● I've archived the heavy-parcel change and merged its spec changes into the
  main spec. It's committed on the heavy-parcel branch as 9aeded2 Archive
  heavy-parcel and sync parcel-pricing spec.

  - Spec: openspec/specs/parcel-pricing/spec.md now has 13 requirements instead
    of 11.
    - The two new ones, for the Heavy rate and charging each parcel at its
      cheaper rate, sit right after the size overweight requirement.
    - The four updated requirements replace their old versions.
    - The spec passes openspec validate --strict.
  - Archive: the change is now in
    openspec/changes/archive/2026-11-11-heavy-parcel/.
  - Tests: all 69 still pass.
  - Completion: all 4 planning documents and all 7 tasks were complete, so there
    are no warnings.

  The branch hasn't been merged into master or pushed. Earlier features were
  squashed onto master as single commits; tell me if you want this one done the
  same way.

❯ squash and merge into master

  Ran 2 shell commands (ctrl+o to expand)

● I've squashed the heavy-parcel branch into a single commit on master: 16c16fd
  Add heavy parcel rate. All 69 tests pass on master. The commit message is
  written in the same style as the earlier feature commits.

  The heavy-parcel branch is still there with its individual commits, as the
  earlier feature branches are. Nothing has been pushed.