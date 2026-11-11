
❯ /opsx:explore There have been complaints from delivery drivers that people
are taking advantage of our dimension only shipping costs. A new weight limit
has been added for each parcel type, over which a charge per kg applies

$2/kg over weight limit for parcel size:
Small parcel: 1 kg
Medium parcel: 3 kg
Large parcel: 6 kg
XL parcel: 10 kg

assume $2 is charged for each part-kg

test the following:
small parcel 1.2kg total $5
small parcel 1.2kg plus speedy shipping total $10
medium parcel 5kg total $12
large parcel 7kg total $17
XL parcel 11kg total $27

modify the older tests so the parcels have weights below the over weight limits

  Ran 3 shell commands (ctrl+o to expand)

● I checked your five examples against the current code and spec, and all of
  them work out under the "charge for each part-kg" rule. Nothing is written
  yet.

  Your examples

   total = base price + $2 x ceil(weight - limit)   (only when weight > limit)

   size    base  limit  weight  over  part-kg rounded  charge  total
   ------  ----  -----  ------  ----  ---------------  ------  -----
   Small    $3    1kg    1.2    0.2         1            $2      $5   ok
   Medium   $8    3kg    5      2           2            $4     $12   ok
   Large   $15    6kg    7      1           1            $2     $17   ok
   XL      $25   10kg   11      1           1            $2     $27   ok

   Small 1.2kg + Speedy:  parcel $5, speedy $5, total $10              ok

  The Speedy example confirms one thing: the overweight charge is part of the
  parcel's cost, so Speedy doubles it too. That fits the current design, where
  the Speedy item costs the sum of the parcel costs. So if the charge goes into
  ParcelLineItem.Cost, nothing in Cart or the Speedy logic needs to change.

  Where it fits in the code

    Parcel(L, W, H)  --->  Parcel(L, W, H, Weight)      <- public API changes
          |
          v
    ParcelPricing.Price
      Classify(dims)       -> size          (unchanged)
      PriceOf(size)        -> base price    (unchanged)
      + overweight charge  -> new: limit per size, $2 per part-kg
          |
          v
    ParcelLineItem(Parcel, Size, Cost)      <- Cost = base + overweight
          |
          v
    Cart.GetQuote -> Speedy = sum of parcel costs   (unchanged)

  Updating the old tests should be mechanical. There are about 26 places that
  build parcels, and many of them share fixtures (Small, Medium, ExtraLarge).

  The "cheapest option" assumption still holds

  docs/assumptions.md says comparing pricing options was put off until "the
  later rule (such as weight) that makes it necessary." I checked whether weight
  makes that necessary now. It doesn't:

   heavy parcel, w kg, well over every limit:
     Small  3 + 2(w-1)  = 2w + 1
     Medium 8 + 2(w-3)  = 2w + 2
     Large 15 + 2(w-6)  = 2w + 3
     XL    25 + 2(w-10) = 2w + 5

  The smallest size is still cheapest at every weight, and rounding up part-kgs
  doesn't change that. A parcel also can't be charged at a smaller size than its
  dimensions allow. So no selector is needed yet, but the assumption entry
  should be updated to give this reason.

  Decisions to make

  These are my recommended defaults. Each would become an entry in
  docs/assumptions.md:

  ┌─────┬───────────────┬───────────────────────────────────────────────────┐
  │  #  │   Question    │                Recommended default                │
  ├─────┼───────────────┼───────────────────────────────────────────────────┤
  │     │ Exactly at    │                                                   │
  │ 1   │ the limit     │ No charge, because "over" means strictly greater  │
  │     │ (Small at 1.0 │                                                   │
  │     │  kg)?         │                                                   │
  ├─────┼───────────────┼───────────────────────────────────────────────────┤
  │ 2   │ Unit and type │ decimal kilograms, to match the dimensions        │
  │     │  of weight    │                                                   │
  ├─────┼───────────────┼───────────────────────────────────────────────────┤
  │     │ Zero or       │ Rejected with ArgumentOutOfRangeException, like   │
  │ 3   │ negative      │ dimensions                                        │
  │     │ weight?       │                                                   │
  ├─────┼───────────────┼───────────────────────────────────────────────────┤
  │     │               │ Rewrite "Each size has a fixed cost" to cover a   │
  │ 4   │ Spec change   │ base price, a weight limit and the overweight     │
  │     │               │ charge, rather than adding a separate requirement │
  │     │               │  next to it                                       │
  └─────┴───────────────┴───────────────────────────────────────────────────┘

  One question changes the public API, so I'd like your answer before anything
  else:

  Should a quote show the overweight charge separately? Right now
  ParcelLineItem(Parcel, Size, Cost) gives a single cost.

  - A: Fold it into Cost (my recommendation). This is the smallest API change.
    Cost stays the parcel's total and Speedy keeps working. The downside is that
    a customer can't see why a Small parcel costs $5.
  - B: Show it on the same line item, for example ParcelLineItem(Parcel, Size,
    BaseCost, OverweightCharge) with Cost calculated from the two. This is more
    open about the price. It changes a public record, and every new
    ParcelLineItem(...) in the tests needs editing.
  - C: Make it a separate OverweightLineItem, like the Speedy item. I'd avoid
    this: it pulls the charge away from its parcel and makes "Speedy = sum of
    parcel costs" less clear.

  Which do you want? If you have no strong preference, I'd go with A. Once
  that's settled, run /opsx:propose to turn this into a change. It will capture
  the decisions above, the five new test cases and the task to give the old
  tests weights under the limits.

❯ B

● Going with B: the quote will show the overweight charge as its own field on
  the parcel's line item. Nothing is written yet; this is still explore mode.

  What B looks like

    before:  ParcelLineItem(Parcel, Size, Cost)

    after:   ParcelLineItem(Parcel, Size, BaseCost, OverweightCharge)
                                             |            |
                                             +-----+------+
                                                   v
                                LineItem.Cost = BaseCost + OverweightCharge

  A C# record can work out its parent's value from its own fields, so this fits
  in one line:

  record ParcelLineItem(Parcel, Size, decimal BaseCost, decimal
  OverweightCharge)
      : LineItem(BaseCost + OverweightCharge)

  What this means:

  - Cost stays on every line item. Quote.Total and the Speedy calculation both
    add up Cost, so they don't change. The Small 1.2 kg + Speedy example still
    comes to $10.
  - Parcels under the limit show OverweightCharge = 0m rather than leaving it
    out. Every parcel line has the same shape, and $0 is the true answer. I'd
    record this in the assumptions.
  - Equality tests become more precise. An assertion like new
    ParcelLineItem(small1_2kg, Small, 3m, 2m) checks how the $5 is made up, not
    just the total.
  - About 10 existing new ParcelLineItem(...) assertions in CartTests,
    SpeedyShippingTests and ShippingOptionTests gain a 0m argument. That's on
    top of giving the old parcels a weight under their size's limit.

  Two smaller points

  - Name of the field. I'd use OverweightCharge. It says when the charge
    applies, and it reads well next to BaseCost. WeightCharge is shorter but
    suggests every parcel pays for weight.
  - Showing the overweight kg. You can work it out from Parcel.Weight and the
    size's limit. I'd leave it off unless you want quotes to show "2 kg over".

  The other defaults from before still apply unless you change them:
  - no charge at exactly the limit
  - weight is decimal kilograms
  - zero or negative weight is rejected
  - the "fixed cost" requirement in the spec gets rewritten
  - the "cheapest option" assumption gets updated

  If those are fine, run /opsx:propose to turn this into a change.

❯ /opsx:propose

● Bash(openspec context --json; openspec new change "overweight-parcel-charge"
      && openspec status --change "overweight-parcel-charge" --json)
  ⎿  {
       "root": {
         "path": "/home/sam/workspace/courier-kata",
     … +116 lines (ctrl+o to expand)
  ⎿  Created openspec/changes/overweight-parcel-charge/.openspec.yaml (+2
     -0)
      1 +schema: spec-driven                                             
      2 +created: 2026-11-11                                             
  ⎿  (files that changed while this command ran, by the command or by
     another process; a convenience view, not a review or audit of the
     command)

  Ran 2 shell commands (ctrl+o to expand)

● Write(openspec/changes/overweight-parcel-charge/proposal.md)
  ⎿  Wrote 33 lines to
     openspec/changes/overweight-parcel-charge/proposal.md
       1 # Proposal
       2
       3 ## Why
       4
       5 Delivery drivers report that customers are taking advantage of p
         ricing that only looks at dimensions, by sending small parcels t
         hat are very heavy. Each parcel size now has a weight limit, and
          parcels over it pay an extra charge.
       6
       7 ## What Changes
     … +26 lines (ctrl+o to expand)

  Ran 1 shell command (ctrl+o to expand)

● Writing the delta spec for parcel-pricing.

● Write(openspec/changes/overweight-parcel-charge/specs/parcel-pricing/spec.md)
  ⎿  Wrote 96 lines to
     openspec/changes/overweight-parcel-charge/specs/parcel-pricing/spec.md
       1 # Spec Delta
       2
       3 ## ADDED Requirements
       4
       5 ### Requirement: Parcel weight is validated
       6 A parcel SHALL have a weight in kilograms. The weight MUST be a
         positive number, and fractional weights are allowed. Creating a
         parcel with a zero or negative weight MUST fail with an out-of-r
         ange error that names the weight. There is no upper limit on wei
         ght.
     … +90 lines (ctrl+o to expand)

● Write(openspec/changes/overweight-parcel-charge/design.md)
  ⎿  Wrote 67 lines to openspec/changes/overweight-parcel-charge/design.md
       1 # Design
       2
       3 ## Context
       4
       5 See proposal.md for the motivation. Today `Parcel(Length, Width,
          Height)` validates its dimensions in its constructor. The inter
         nal `ParcelPricing.Price` classifies a parcel by its largest dim
         ension and returns `ParcelLineItem(Parcel, Size, Cost)`. `Cart.G
         etQuote()` prices each parcel and, with Speedy, adds a `SpeedySh
         ippingLineItem` costing the sum of the parcel `Cost`s. Weight af
     … +63 lines (~4 KB) (ctrl+o to expand)

● Update(openspec/changes/overweight-parcel-charge/design.md)
  ⎿  Added 1 line, removed 1 line
      64  
      65  - [Breaking `Parcel` and `ParcelLineItem` constructors] → The o
          nly caller is the test project. Updating every call is part of 
          this change.
      66  - [Later rules, such as a "heavy parcel" option, may make a dif
          ferent size cheaper] → That change will add the option selector
          . The assumption entry records why it isn't needed now.
      67 -- [`Cost` is now derived, so `with` expressions that change `Ba
         -seCost` recompute it, but nothing sets `Cost` directly] → This 
         -is intended. `Cost` is no longer a constructor parameter of `Pa
         -rcelLineItem`.                                                 
      67 +- [A `with` expression would leave `Cost` stale. Passing `BaseC
         +ost + OverweightCharge` to the base constructor stores `Cost` o
         +nce, and `item with { OverweightCharge = 4m }` uses the copy co
         +nstructor, which copies the old `Cost`] → Work out `Cost` when 
         +it's read instead of storing it. For example, make `LineItem.Co
         +st` abstract, have `ParcelLineItem` override it as `BaseCost + 
         +OverweightCharge`, and have `SpeedyShippingLineItem` keep its p
         +ositional `Cost`. Task 1.1 includes a test that `with` keeps `C
         +ost` consistent.                                               

● Update(openspec/changes/overweight-parcel-charge/design.md)
  ⎿  Added 1 line, removed 1 line
      29  public sealed record ParcelLineItem(Parcel Parcel, ParcelSize S
          ize, decimal BaseCost, decimal OverweightCharge)
      30      : LineItem(BaseCost + OverweightCharge)
      31  ```
      32 -- `Cost` is inherited from `LineItem` and always equals `BaseCo
         -st + OverweightCharge`. A caller can't build an inconsistent it
         -em.                                                            
      32 +- `Cost` always equals `BaseCost + OverweightCharge`, and a cal
         +ler can't build an inconsistent item. It's worked out when read
         +, not stored, so `with` expressions stay consistent (see Risks)
         +. The sketch above shows the shape, not the final mechanism.   
      33  - Parcels within their limit carry `OverweightCharge = 0m`, so 
          every parcel line has the same shape.
      34  - The name `OverweightCharge` says when the charge applies. `We
          ightCharge` would suggest every parcel pays for weight.
      35  - Alternatives considered: folding the charge into `Cost`, whic
          h is simpler but hides why a parcel costs more, and a separate 
          `OverweightLineItem`, which would separate the charge from its 
          parcel.

● Write(openspec/changes/overweight-parcel-charge/tasks.md)
  ⎿  Wrote 37 lines to openspec/changes/overweight-parcel-charge/tasks.md
      1 # Tasks
      2
      3 ## 1. Weight on parcels and split line-item costs (no price chang
        e)
      4
      5 - [ ] 1.1 Change `ParcelLineItem` to `(Parcel, Size, BaseCost, Ov
        erweightCharge)`, with `Cost` worked out as `BaseCost + Overweigh
        tCharge` when read (see design.md, Risks). `ParcelPricing` sets `
        OverweightCharge = 0m` for now. Update every expected `new Parcel
        LineItem(...)` in the tests to pass `0m`. Add tests that `Cost` i
     … +33 lines (~3 KB) (ctrl+o to expand)

  Ran 1 shell command (ctrl+o to expand)

● I've created the change overweight-parcel-charge in
  openspec/changes/overweight-parcel-charge/. It passes openspec validate
  --strict, and all four planning artifacts are complete. No code has changed
  yet.

  The artifacts:
  - proposal.md (openspec/changes/overweight-parcel-charge/proposal.md):
    explains the drivers' complaint and lists the two breaking changes. Parcel
    gets a required weight, and ParcelLineItem becomes (Parcel, Size, BaseCost,
    OverweightCharge).
  - specs/parcel-pricing/spec.md
    (openspec/changes/overweight-parcel-charge/specs/parcel-pricing/spec.md):
    the changes to the parcel-pricing spec.
    - New requirement: weight is validated. A weight of zero or less is
      rejected, and fractional weights are allowed.
    - New requirement: the overweight charge. It covers your five examples, plus
      each size exactly at its limit, plus a heavy parcel that stays Small.
    - Changed: base cost. Each size keeps its fixed price as the base cost. The
      spec now says why the smallest size is still the cheapest at any weight.
    - Changed: what a parcel line item shows. It lists the base cost and
      overweight charge separately.
    - Changed: Speedy shipping. It adds your 1.2 kg Speedy example, which comes
      to $10.
  - design.md (openspec/changes/overweight-parcel-charge/design.md): the
    technical decisions.
    - Weight is required rather than given a default, because a default would
      hide parcels nobody weighed.
    - Math.Ceiling on decimal gives the exact part-kg rounding.
    - All the old tests can use one weight, 0.5 kg, which is under every size's
      limit.
    - It lists the entries to add to docs/assumptions.md and the one to update.
  - tasks.md (openspec/changes/overweight-parcel-charge/tasks.md): three groups.
    a. Change the line item's shape and add weight to Parcel, with no change to
       any price.
    b. Add the overweight charge, test first, starting with your 1.2 kg Small
       example.
    c. Final checks.

    Each group commits its own tests and assumptions.

  One thing I added to design.md: with option B, a C# with expression on the
  line item could leave Cost out of date. If Cost is stored once when the item
  is built, then item with { OverweightCharge = 4m } copies the old Cost. The
  design works Cost out when it's read instead, and task 1.1 includes a test for
  this.

  The artifacts are ready for review. When you're ready, run /opsx:apply.

❯ /opsx:apply

● Using change: overweight-parcel-charge (override with /opsx:apply <other>).

  Ran 1 shell command (ctrl+o to expand)

● Progress 0/8 (spec-driven). Starting task 1.1: split ParcelLineItem costs.

● Bash(cd /home/sam/workspace/courier-kata
      cat > src/CourierKata/LineItem.cs <<'EOF'…)
  ⎿  tests/CourierKata.Tests/ParcelCostTests.cs:0
     tests/CourierKata.Tests/ParcelSizeTests.cs:0
     tests/CourierKata.Tests/ParcelTests.cs:0
     … +16 lines (ctrl+o to expand)
  ⎿  Updated src/CourierKata/LineItem.cs (+4 -2)
       6  /// </summary>
       7  public abstract record LineItem
       8  {
       9 -    private protected LineItem(decimal cost) => Cost = cost;   
       9 +    private protected LineItem()                               
      10 +    {                                                          
      11 +    }                                                          
      12  
      13      /// <summary>Cost in dollars.</summary>
      12 -    public decimal Cost { get; }                               
      14 +    public abstract decimal Cost { get; }                      
      15  
      16      // Records always have a protected copy constructor, so ano
          ther assembly
      17      // could still derive from LineItem through it. An internal
           abstract member
  ⎿  Updated src/CourierKata/SpeedyShippingLineItem.cs (+4 -1)
       1  namespace CourierKata;
       2  
       3  /// <summary>The extra charge for speedy shipping, in dollars.<
          /summary>
       4 -public sealed record SpeedyShippingLineItem(decimal Cost) : Lin
         -eItem(Cost)                                                    
       4 +public sealed record SpeedyShippingLineItem(decimal Cost) : Lin
         +eItem                                                          
       5  {
       6 +    /// <inheritdoc />                                         
       7 +    public override decimal Cost { get; } = Cost;              
       8 +                                                               
       9      internal override void SealHierarchy()
      10      {
      11      }
  ⎿  Updated src/CourierKata/ParcelLineItem.cs (+8 -2)
       1  namespace CourierKata;
       2  
       3 -/// <summary>One parcel in a quote, with the size it is charged
         - at and its cost in dollars.</summary>                         
       4 -public sealed record ParcelLineItem(Parcel Parcel, ParcelSize S
         -ize, decimal Cost) : LineItem(Cost)                            
       3 +/// <summary>                                                  
       4 +/// One parcel in a quote: the size it is charged at, the base 
         +cost of that size, and any                                     
       5 +/// charge for being over the size's weight limit, all in dolla
         +rs.                                                            
       6 +/// </summary>                                                 
       7 +public sealed record ParcelLineItem(Parcel Parcel, ParcelSize S
         +ize, decimal BaseCost, decimal OverweightCharge) : LineItem    
       8  {
       9 +    /// <summary>The base cost plus the overweight charge, in d
         +ollars.</summary>                                              
      10 +    public override decimal Cost => BaseCost + OverweightCharge
         +;                                                              
      11 +                                                               
      12      internal override void SealHierarchy()
      13      {
      14      }
  ⎿  Updated src/CourierKata/ParcelPricing.cs (+1 -1)
      28      public static ParcelLineItem Price(Parcel parcel)
      29      {
      30          var size = Classify(parcel);
      31 -        return new ParcelLineItem(parcel, size, PriceOf(size));
      31 +        return new ParcelLineItem(parcel, size, PriceOf(size), 
         +0m);                                                           
      32      }
      33  }
  ⎿  Updated tests/CourierKata.Tests/CartTests.cs (+5 -5)
      14  
      15          Assert.Equal(
      16              [
      17 -                new ParcelLineItem(ExtraLarge, ParcelSize.Extra
         -Large, 25m),                                                   
      18 -                new ParcelLineItem(Small, ParcelSize.Small, 3m)
         -,                                                              
      19 -                new ParcelLineItem(Medium, ParcelSize.Medium, 8
         -m),                                                            
      17 +                new ParcelLineItem(ExtraLarge, ParcelSize.Extra
         +Large, 25m, 0m),                                               
      18 +                new ParcelLineItem(Small, ParcelSize.Small, 3m,
         + 0m),                                                          
      19 +                new ParcelLineItem(Medium, ParcelSize.Medium, 8
         +m, 0m),                                                        
      20              ],
      21              cart.GetQuote().Items);
      22      }
     ...
      28  
      29          Assert.Equal(
      30              [
      31 -                new ParcelLineItem(Small, ParcelSize.Small, 3m)
         -,                                                              
      32 -                new ParcelLineItem(Small, ParcelSize.Small, 3m)
         -,                                                              
      31 +                new ParcelLineItem(Small, ParcelSize.Small, 3m,
         + 0m),                                                          
      32 +                new ParcelLineItem(Small, ParcelSize.Small, 3m,
         + 0m),                                                          
      33              ],
      34              cart.GetQuote().Items);
      35      }
  ⎿  … 3 more files changed

● Task 1.1 passes, with 46 tests green. Next is task 1.2: adding weight to
  Parcel, starting with failing tests.

● Bash(cd /home/sam/workspace/courier-kata
      sed -i 's/^- \[ \] 1.1 /- [x] 1.1 /'
      openspec/changes/overweight-parcel-charge/tasks.md…)
  ⎿  /src/tests/CourierKata.Tests/ParcelTests.cs(15,23): error CS1729: 'Par
     cel' does not contain a constructor that takes 4 arguments [/src/tests
     /CourierKata.Tests/CourierKata.Tests.csproj]
     … +13 lines (ctrl+o to expand)
  ⎿  Updated openspec/changes/overweight-parcel-charge/tasks.md (+1 -1)
      2  
      3  ## 1. Weight on parcels and split line-item costs (no price chan
         ge)
      4  
      5 -- [ ] 1.1 Change `ParcelLineItem` to `(Parcel, Size, BaseCost, O
        -verweightCharge)`, with `Cost` worked out as `BaseCost + Overwei
        -ghtCharge` when read (see design.md, Risks). `ParcelPricing` set
        -s `OverweightCharge = 0m` for now. Update every expected `new Pa
        -rcelLineItem(...)` in the tests to pass `0m`. Add tests that `Co
        -st` is the sum of the two parts, and that `item with { Overweigh
        -tCharge = 4m }` gives the right `Cost`. Verify that `docker comp
        -ose run --rm tests` passes with zero warnings.                  
      5 +- [x] 1.1 Change `ParcelLineItem` to `(Parcel, Size, BaseCost, O
        +verweightCharge)`, with `Cost` worked out as `BaseCost + Overwei
        +ghtCharge` when read (see design.md, Risks). `ParcelPricing` set
        +s `OverweightCharge = 0m` for now. Update every expected `new Pa
        +rcelLineItem(...)` in the tests to pass `0m`. Add tests that `Co
        +st` is the sum of the two parts, and that `item with { Overweigh
        +tCharge = 4m }` gives the right `Cost`. Verify that `docker comp
        +ose run --rm tests` passes with zero warnings.                  
      6  - [ ] 1.2 Write failing tests: a weight of 0 kg or -1 kg throws 
         `ArgumentOutOfRangeException` naming `Weight`, and 0.5 kg is acc
         epted. Add the required `Weight` parameter to `Parcel`, validate
         d with `ThrowIfNegativeOrZero`. Give every existing `new Parcel(
         ...)` in the tests a weight within its size's limit (for example
          `0.5m`), so no expected price changes. Verify with `docker comp
         ose run --rm tests`.
      7  - [ ] 1.3 Add these assumptions to `docs/assumptions.md`, then c
         ommit them with the code:
      8    - weight is required, positive and in `decimal` kilograms, wit
         h no upper limit
  ⎿  Updated tests/CourierKata.Tests/ParcelTests.cs (+21 -2)
      12      public void Dimension_of_zero_or_less_is_rejected(double le
          ngth, double width, double height, string invalidDimension)
      13      {
      14          var error = Assert.Throws<ArgumentOutOfRangeException>(
      15 -            () => new Parcel((decimal)length, (decimal)width, (
         -decimal)height));                                              
      15 +            () => new Parcel((decimal)length, (decimal)width, (
         +decimal)height, 0.5m));                                        
      16  
      17          Assert.Equal(invalidDimension, error.ParamName);
      18      }
     ...
      20      [Fact]
      21      public void Very_large_parcel_is_accepted()
      22      {
      23 -        var parcel = new Parcel(10_000m, 5m, 5m);              
      23 +        var parcel = new Parcel(10_000m, 5m, 5m, 0.5m);        
      24  
      25          Assert.Equal(10_000m, parcel.Length);
      26      }
      27 +                                                               
      28 +    [Theory]                                                   
      29 +    [InlineData(0)]                                            
      30 +    [InlineData(-1)]                                           
      31 +    public void Weight_of_zero_or_less_is_rejected(double weigh
         +t)                                                             
      32 +    {                                                          
      33 +        var error = Assert.Throws<ArgumentOutOfRangeException>(
      34 +            () => new Parcel(5m, 5m, 5m, (decimal)weight));    
      35 +                                                               
      36 +        Assert.Equal("Weight", error.ParamName);               
      37 +    }                                                          
      38 +                                                               
      39 +    [Fact]                                                     
      40 +    public void Fractional_weight_is_accepted()                
      41 +    {                                                          
      42 +        var parcel = new Parcel(5m, 5m, 5m, 0.5m);             
      43 +                                                               
      44 +        Assert.Equal(0.5m, parcel.Weight);                     
      45 +    }                                                          
      46  }

● Tests fail as expected. Now implementing Weight and migrating the fixtures.

● Bash(cd /home/sam/workspace/courier-kata
      cat > src/CourierKata/Parcel.cs <<'EOF'…)
  ⎿  CartTests.cs:5:    private static readonly Parcel Small = new(5m, 5m,
     5m, 0.5m);
     CartTests.cs:6:    private static readonly Parcel Medium = new(20m, 5m
     … +49 lines (ctrl+o to expand)
  ⎿  Updated src/CourierKata/Parcel.cs (+8 -5)
      1  namespace CourierKata;
      2  
      3 -/// <summary>A parcel to deliver, with its dimensions in centime
        -tres.</summary>                                                 
      4 -/// <exception cref="ArgumentOutOfRangeException">A dimension is
        - zero or negative.</exception>                                  
      5 -public sealed record Parcel(decimal Length, decimal Width, decim
        -al Height)                                                      
      3 +/// <summary>A parcel to deliver, with its dimensions in centime
        +tres and its weight in kilograms.</summary>                     
      4 +/// <exception cref="ArgumentOutOfRangeException">A dimension or
        + the weight is zero or negative.</exception>                    
      5 +public sealed record Parcel(decimal Length, decimal Width, decim
        +al Height, decimal Weight)                                      
      6  {
      7      /// <summary>Length in centimetres.</summary>
      8      public decimal Length { get; } = Positive(Length, nameof(Len
         gth));
     ...
      13      /// <summary>Height in centimetres.</summary>
      14      public decimal Height { get; } = Positive(Height, nameof(He
          ight));
      15  
      16 -    private static decimal Positive(decimal value, string dimen
         -sion)                                                          
      16 +    /// <summary>Weight in kilograms.</summary>                
      17 +    public decimal Weight { get; } = Positive(Weight, nameof(We
         +ight));                                                        
      18 +                                                               
      19 +    private static decimal Positive(decimal value, string name)
      20      {
      18 -        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value
         -, dimension);                                                  
      21 +        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value
         +, name);                                                       
      22          return value;
      23      }
      24  }
  ⎿  Updated tests/CourierKata.Tests/CartTests.cs (+4 -4)
       2  
       3  public class CartTests
       4  {
       5 -    private static readonly Parcel Small = new(5m, 5m, 5m);    
       6 -    private static readonly Parcel Medium = new(20m, 5m, 5m);  
       7 -    private static readonly Parcel Large = new(70m, 5m, 5m);   
       8 -    private static readonly Parcel ExtraLarge = new(150m, 5m, 5
         -m);                                                            
       5 +    private static readonly Parcel Small = new(5m, 5m, 5m, 0.5m
         +);                                                             
       6 +    private static readonly Parcel Medium = new(20m, 5m, 5m, 0.
         +5m);                                                           
       7 +    private static readonly Parcel Large = new(70m, 5m, 5m, 0.5
         +m);                                                            
       8 +    private static readonly Parcel ExtraLarge = new(150m, 5m, 5
         +m, 0.5m);                                                      
       9  
      10      [Fact]
      11      public void Line_items_are_in_the_order_parcels_were_added(
          )
  ⎿  Updated tests/CourierKata.Tests/ShippingOptionTests.cs (+2 -2)
      2  
      3  public class ShippingOptionTests
      4  {
      5 -    private static readonly Parcel Small = new(5m, 5m, 5m);     
      6 -    private static readonly Parcel ExtraLarge = new(150m, 5m, 5m
        -);                                                              
      5 +    private static readonly Parcel Small = new(5m, 5m, 5m, 0.5m)
        +;                                                               
      6 +    private static readonly Parcel ExtraLarge = new(150m, 5m, 5m
        +, 0.5m);                                                        
      7  
      8      [Fact]
      9      public void New_cart_uses_standard_shipping()
  ⎿  Updated tests/CourierKata.Tests/SpeedyShippingTests.cs (+4 -4)
       2  
       3  public class SpeedyShippingTests
       4  {
       5 -    private static readonly Parcel Small = new(5m, 5m, 5m);    
       6 -    private static readonly Parcel Medium = new(20m, 5m, 5m);  
       7 -    private static readonly Parcel Large = new(70m, 5m, 5m);   
       8 -    private static readonly Parcel ExtraLarge = new(150m, 5m, 5
         -m);                                                            
       5 +    private static readonly Parcel Small = new(5m, 5m, 5m, 0.5m
         +);                                                             
       6 +    private static readonly Parcel Medium = new(20m, 5m, 5m, 0.
         +5m);                                                           
       7 +    private static readonly Parcel Large = new(70m, 5m, 5m, 0.5
         +m);                                                            
       8 +    private static readonly Parcel ExtraLarge = new(150m, 5m, 5
         +m, 0.5m);                                                      
       9  
      10      [Fact]
      11      public void Speedy_shipping_is_listed_separately_and_double
          s_the_total()
  ⎿  Updated tests/CourierKata.Tests/ParcelLineItemTests.cs (+1 -1)
      2  
      3  public class ParcelLineItemTests
      4  {
      5 -    private static readonly Parcel Small = new(5m, 5m, 5m);     
      5 +    private static readonly Parcel Small = new(5m, 5m, 5m, 0.5m)
        +;                                                               
      6  
      7      [Fact]
      8      public void Cost_is_the_base_cost_plus_the_overweight_charge
         ()
  ⎿  … 2 more files changed

● Task 1.2 passes, with 49 tests green. Now task 1.3: record the assumptions and
  commit.
● Bash(cd /home/sam/workspace/courier-kata
      sed -i 's/^- \[ \] 1.2 /- [x] 1.2 /'
      openspec/changes/overweight-parcel-charge/tasks.md…)
  ⎿  8cdbb28 Add parcel weight and split line-item cost
  ⎿  Updated openspec/changes/overweight-parcel-charge/tasks.md (+2 -2)
       3  ## 1. Weight on parcels and split line-item costs (no price cha
          nge)
       4  
       5  - [x] 1.1 Change `ParcelLineItem` to `(Parcel, Size, BaseCost, 
          OverweightCharge)`, with `Cost` worked out as `BaseCost + Overw
          eightCharge` when read (see design.md, Risks). `ParcelPricing` 
          sets `OverweightCharge = 0m` for now. Update every expected `ne
          w ParcelLineItem(...)` in the tests to pass `0m`. Add tests tha
          t `Cost` is the sum of the two parts, and that `item with { Ove
          rweightCharge = 4m }` gives the right `Cost`. Verify that `dock
          er compose run --rm tests` passes with zero warnings.
       6 -- [ ] 1.2 Write failing tests: a weight of 0 kg or -1 kg throws
         - `ArgumentOutOfRangeException` naming `Weight`, and 0.5 kg is a
         -ccepted. Add the required `Weight` parameter to `Parcel`, valid
         -ated with `ThrowIfNegativeOrZero`. Give every existing `new Par
         -cel(...)` in the tests a weight within its size's limit (for ex
         -ample `0.5m`), so no expected price changes. Verify with `docke
         -r compose run --rm tests`.                                     
       7 -- [ ] 1.3 Add these assumptions to `docs/assumptions.md`, then 
         -commit them with the code:                                     
       6 +- [x] 1.2 Write failing tests: a weight of 0 kg or -1 kg throws
         + `ArgumentOutOfRangeException` naming `Weight`, and 0.5 kg is a
         +ccepted. Add the required `Weight` parameter to `Parcel`, valid
         +ated with `ThrowIfNegativeOrZero`. Give every existing `new Par
         +cel(...)` in the tests a weight within its size's limit (for ex
         +ample `0.5m`), so no expected price changes. Verify with `docke
         +r compose run --rm tests`.                                     
       7 +- [x] 1.3 Add these assumptions to `docs/assumptions.md`, then 
         +commit them with the code:                                     
       8    - weight is required, positive and in `decimal` kilograms, wi
          th no upper limit
       9    - the overweight charge is shown separately, and is $0 within
           the limit
      10  
  ⎿  Updated docs/assumptions.md (+26 -0)
      160  - **Why:** C# enums accept any underlying integer, so an unche
           cked value would
      161    silently be treated as Standard. Failing early matches how `
           Parcel` rejects
      162    invalid dimensions.
      163 +                                                              
      164 +## Weight                                                     
      165 +                                                              
      166 +### Weight is required, positive and in kilograms             
      167 +                                                              
      168 +- **Unclear:** The brief adds weight limits but doesn't say ho
          +w a parcel's                                                  
      169 +  weight is given, or what counts as a valid weight.          
      170 +- **Assumed:** `Parcel` takes a required `Weight` as `decimal`
          + kilograms.                                                   
      171 +  Fractional weights are allowed. Zero or negative weights thr
          +ow                                                            
      172 +  `ArgumentOutOfRangeException` naming `Weight`. There is no u
          +pper limit.                                                   
      173 +- **Why:** The limits are in kilograms, and the examples use f
          +ractions                                                      
      174 +  (1.2 kg). Making weight required rather than giving it a def
          +ault means a                                                  
      175 +  parcel can't skip the weight check, which is the loophole th
          +e change                                                      
      176 +  closes. Rejecting zero or less matches how dimensions are va
          +lidated.                                                      
      177 +                                                              
      178 +### Overweight charge is shown separately on the parcel's line
          + item                                                         
      179 +                                                              
      180 +- **Unclear:** Whether the quote should show the overweight ch
          +arge or fold it                                               
      181 +  into the parcel's cost.                                     
      182 +- **Assumed:** A parcel line item shows `BaseCost` (the size p
          +rice) and                                                     
      183 +  `OverweightCharge` separately. Its `Cost` is their sum. Parc
          +els within their                                              
      184 +  limit show an `OverweightCharge` of $0.                     
      185 +- **Why:** Customers can see why a parcel costs more than its 
          +size price.                                                   
      186 +  Keeping the charge on the parcel's own line ties it to that 
          +parcel, and `Cost`                                            
      187 +  still means what the line costs. A $0 charge gives every par
          +cel line the                                                  
      188 +  same shape.                                                 

● Group 1 is committed. Next is task 2.1: the first overweight test.

● Bash(cd /home/sam/workspace/courier-kata
      cat > tests/CourierKata.Tests/OverweightChargeTests.cs <<'EOF'…)
  ⎿  [xUnit.net 00:00:00.19]     CourierKata.Tests.OverweightChargeTests.Sm
     all_parcel_part_of_a_kilogram_over_its_limit_pays_for_a_whole_kilogram
      [FAIL]
     … +2 lines (ctrl+o to expand)
  ⎿  Created tests/CourierKata.Tests/OverweightChargeTests.cs (+23 -0)
       1 +namespace CourierKata.Tests;                                   
       2 +                                                               
       3 +public class OverweightChargeTests                             
       4 +{                                                              
       5 +    [Fact]                                                     
       6 +    public void Small_parcel_part_of_a_kilogram_over_its_limit_
         +pays_for_a_whole_kilogram()                                    
       7 +    {                                                          
       8 +        var parcel = new Parcel(5m, 5m, 5m, 1.2m);             
       9 +                                                               
      10 +        var quote = QuoteFor(parcel);                          
      11 +                                                               
      12 +        Assert.Equal([new ParcelLineItem(parcel, ParcelSize.Sma
         +ll, 3m, 2m)], quote.Items);                                    
      13 +        Assert.Equal(5m, quote.Items[0].Cost);                 
      14 +        Assert.Equal(5m, quote.Total);                         
      15 +    }                                                          
      16 +                                                               
      17 +    private static Quote QuoteFor(Parcel parcel, Shipping shipp
         +ing = Shipping.Standard)                                       
      18 +    {                                                          
      19 +        var cart = new Cart { Shipping = shipping };           
      20 +        cart.AddParcel(parcel);                                
      21 +        return cart.GetQuote();                                
      22 +    }                                                          
      23 +}                                                              

● Bash(cd /home/sam/workspace/courier-kata
      cat > src/CourierKata/ParcelPricing.cs <<'EOF'…)
  ⎿  Passed!  - Failed:     0, Passed:    50, Skipped:     0, Total:    50,
      Duration: 49 ms - CourierKata.Tests.dll (net10.0)
  ⎿  Updated src/CourierKata/ParcelPricing.cs (+22 -3)
       3  /// <summary>The rules that decide what size a parcel is charge
          d at and what it costs.</summary>
       4  internal static class ParcelPricing
       5  {
       6 +    private const decimal OverweightChargePerKilogram = 2m;    
       7 +                                                               
       8 +    public static ParcelLineItem Price(Parcel parcel)          
       9 +    {                                                          
      10 +        var size = Classify(parcel);                           
      11 +        return new ParcelLineItem(parcel, size, PriceOf(size), 
         +OverweightCharge(parcel.Weight, size));                        
      12 +    }                                                          
      13 +                                                               
      14      private static ParcelSize Classify(Parcel parcel)
      15      {
      16          var largest = Math.Max(parcel.Length, Math.Max(parcel.W
          idth, parcel.Height));
     ...
      33          _ => throw new ArgumentOutOfRangeException(nameof(size)
          , size, "Unknown parcel size."),
      34      };
      35  
      28 -    public static ParcelLineItem Price(Parcel parcel)          
      36 +    /// <summary>The weight in kilograms a parcel of this size 
         +can be before it pays an overweight charge.</summary>          
      37 +    private static decimal WeightLimitOf(ParcelSize size) => si
         +ze switch                                                      
      38      {
      30 -        var size = Classify(parcel);                           
      31 -        return new ParcelLineItem(parcel, size, PriceOf(size), 
         -0m);                                                           
      39 +        ParcelSize.Small => 1m,                                
      40 +        ParcelSize.Medium => 3m,                               
      41 +        ParcelSize.Large => 6m,                                
      42 +        ParcelSize.ExtraLarge => 10m,                          
      43 +        _ => throw new ArgumentOutOfRangeException(nameof(size)
         +, size, "Unknown parcel size."),                               
      44 +    };                                                         
      45 +                                                               
      46 +    /// <summary>Charges for each kilogram over the limit, coun
         +ting any part of a kilogram as a whole one.</summary>          
      47 +    private static decimal OverweightCharge(decimal weight, Par
         +celSize size)                                                  
      48 +    {                                                          
      49 +        var over = weight - WeightLimitOf(size);               
      50 +        return over > 0m ? OverweightChargePerKilogram * Math.C
         +eiling(over) : 0m;                                             
      51      }
      52  }

● Task 2.1 passes. Now tasks 2.2 and 2.3: the remaining examples, the at-limit
  cases, the size check and the Speedy example.

● Bash(cd /home/sam/workspace/courier-kata
      sed -i 's/^- \[ \] 2.1 /- [x] 2.1 /'
      openspec/changes/overweight-parcel-charge/tasks.md…)
  ⎿  Passed!  - Failed:     0, Passed:    60, Skipped:     0, Total:    60,
      Duration: 50 ms - CourierKata.Tests.dll (net10.0)
  ⎿  Updated openspec/changes/overweight-parcel-charge/tasks.md (+1 -1)
      10  
      11  ## 2. Overweight charge (test first, starting with the brief's 
          examples)
      12  
      13 -- [ ] 2.1 Write a failing test in `OverweightChargeTests`: a Sm
         -all parcel weighing 1.2 kg has base $3, overweight $2, cost $5 
         -and quote total $5. Add the weight limits (`LimitOf(size)`) and
         - the $2-per-part-kg charge using `Math.Ceiling` in `ParcelPrici
         -ng`. Verify with `docker compose run --rm tests`.              
      13 +- [x] 2.1 Write a failing test in `OverweightChargeTests`: a Sm
         +all parcel weighing 1.2 kg has base $3, overweight $2, cost $5 
         +and quote total $5. Add the weight limits (`LimitOf(size)`) and
         + the $2-per-part-kg charge using `Math.Ceiling` in `ParcelPrici
         +ng`. Verify with `docker compose run --rm tests`.              
      14  - [ ] 2.2 Add tests for these cases, implementing anything stil
          l missing:
      15    - Medium 5 kg costs $12
      16    - Large 7 kg costs $17
  ⎿  Updated tests/CourierKata.Tests/OverweightChargeTests.cs (+64 -0)
      14          Assert.Equal(5m, quote.Total);
      15      }
      16  
      17 +    [Theory]                                                   
      18 +    [InlineData(20, 5, ParcelSize.Medium, 8, 4)]               
      19 +    [InlineData(70, 7, ParcelSize.Large, 15, 2)]               
      20 +    [InlineData(150, 11, ParcelSize.ExtraLarge, 25, 2)]        
      21 +    public void Parcel_over_its_limit_pays_2_dollars_per_kilogr
         +am_over(                                                       
      22 +        int largestDimension, int weight, ParcelSize expectedSi
         +ze, int expectedBaseCost, int expectedOverweightCharge)        
      23 +    {                                                          
      24 +        var parcel = new Parcel(largestDimension, 5m, 5m, weigh
         +t);                                                            
      25 +                                                               
      26 +        var quote = QuoteFor(parcel);                          
      27 +                                                               
      28 +        Assert.Equal(                                          
      29 +            [new ParcelLineItem(parcel, expectedSize, expectedB
         +aseCost, expectedOverweightCharge)],                           
      30 +            quote.Items);                                      
      31 +        Assert.Equal(expectedBaseCost + expectedOverweightCharg
         +e, quote.Total);                                               
      32 +    }                                                          
      33 +                                                               
      34 +    [Theory]                                                   
      35 +    [InlineData(5, 1, ParcelSize.Small)]                       
      36 +    [InlineData(20, 3, ParcelSize.Medium)]                     
      37 +    [InlineData(70, 6, ParcelSize.Large)]                      
      38 +    [InlineData(150, 10, ParcelSize.ExtraLarge)]               
      39 +    public void Parcel_exactly_at_its_limit_pays_no_overweight_
         +charge(int largestDimension, int weight, ParcelSize expectedSiz
         +e)                                                             
      40 +    {                                                          
      41 +        var item = Assert.IsType<ParcelLineItem>(              
      42 +            Assert.Single(QuoteFor(new Parcel(largestDimension,
         + 5m, 5m, weight)).Items));                                     
      43 +                                                               
      44 +        Assert.Equal(expectedSize, item.Size);                 
      45 +        Assert.Equal(0m, item.OverweightCharge);               
      46 +    }                                                          
      47 +                                                               
      48 +    [Fact]                                                     
      49 +    public void Parcel_within_its_limit_shows_a_zero_overweight
         +_charge()                                                      
      50 +    {                                                          
      51 +        var parcel = new Parcel(5m, 5m, 5m, 0.5m);             
      52 +                                                               
      53 +        Assert.Equal([new ParcelLineItem(parcel, ParcelSize.Sma
         +ll, 3m, 0m)], QuoteFor(parcel).Items);                         
     … 30 more lines (ctrl+o to expand)

● Tasks 2.2 and 2.3 pass, with 60 tests green. Now task 2.4: the assumptions.

● Bash(cd /home/sam/workspace/courier-kata
      sed -i -E 's/^- \[ \] 2\.(2|3) /- [x] 2.\1 /'
      openspec/changes/overweight-parcel-charge/tasks.md…)
  ⎿  Passed!  - Failed:     0, Passed:    60, Skipped:     0, Total:    60,
      Duration: 49 ms - CourierKata.Tests.dll (net10.0)
     aaf2f19 Charge $2 per part-kg over each size's weight limit
  ⎿  Updated openspec/changes/overweight-parcel-charge/tasks.md (+3 -3)
      11  ## 2. Overweight charge (test first, starting with the brief's 
          examples)
      12  
      13  - [x] 2.1 Write a failing test in `OverweightChargeTests`: a Sm
          all parcel weighing 1.2 kg has base $3, overweight $2, cost $5 
          and quote total $5. Add the weight limits (`LimitOf(size)`) and
           the $2-per-part-kg charge using `Math.Ceiling` in `ParcelPrici
          ng`. Verify with `docker compose run --rm tests`.
      14 -- [ ] 2.2 Add tests for these cases, implementing anything stil
         -l missing:                                                     
      14 +- [x] 2.2 Add tests for these cases, implementing anything stil
         +l missing:                                                     
      15    - Medium 5 kg costs $12
      16    - Large 7 kg costs $17
      17    - XL 11 kg costs $27
     ...
      19    - a 5 x 5 x 5 cm parcel weighing 20 kg is still Small
      20  
      21    Verify with `docker compose run --rm tests`.
      22 -- [ ] 2.3 Add a test that a Small 1.2 kg parcel with Speedy shi
         -pping gives a parcel item of $5, a speedy item of $5 and a tota
         -l of $10. Verify with `docker compose run --rm tests`. No code 
         -change should be needed.                                       
      23 -- [ ] 2.4 Update `docs/assumptions.md`, then commit with the co
         -de:                                                            
      22 +- [x] 2.3 Add a test that a Small 1.2 kg parcel with Speedy shi
         +pping gives a parcel item of $5, a speedy item of $5 and a tota
         +l of $10. Verify with `docker compose run --rm tests`. No code 
         +change should be needed.                                       
      23 +- [x] 2.4 Update `docs/assumptions.md`, then commit with the co
         +de:                                                            
      24    - add: at the limit pays nothing
      25    - add: part-kilograms round up
      26    - add: weight doesn't change size
  ⎿  Updated docs/assumptions.md (+43 -5)
      61  
      62  - **Unclear:** "In all circumstances the cheapest option for se
          nding each parcel
      63    should be selected" implies a choice between options.
      64 -- **Assumed:** With size-only pricing, the only option is the s
         -mallest size the                                               
      65 -  parcel fits, which is also the cheapest because prices rise w
         -ith size. No                                                   
      66 -  general "compare every option" logic is built yet.           
      67 -- **Why:** This change covers step 1 only. A selector between c
         -ompeting options                                               
      68 -  belongs with the later rule (such as weight) that makes it ne
         -cessary.                                                       
      64 +- **Assumed:** The only option is the smallest size the parcel'
         +s dimensions                                                   
      65 +  fit. It is also the cheapest, so no general "compare every op
         +tion" logic is                                                 
      66 +  built.                                                       
      67 +- **Why:** Prices rise with size. Weight limits also rise with 
         +size, but each                                                 
      68 +  step up adds more base price than its higher limit can save i
         +n overweight                                                   
      69 +  charges: Small to Medium adds $5 but saves at most $4 (2 kg),
         + Medium to Large                                               
      70 +  adds $7 but saves at most $6, and Large to XL adds $10 but sa
         +ves at most $8.                                                
      71 +  So the smallest size is cheapest at every weight. A selector 
         +belongs with a                                                 
      72 +  later rule that can make a larger option cheaper.            
      73  
      74  ## Validation
      75  
     ...
      190    Keeping the charge on the parcel's own line ties it to that 
           parcel, and `Cost`
      191    still means what the line costs. A $0 charge gives every par
           cel line the
      192    same shape.
      193 +                                                              
      194 +### A parcel at its weight limit pays nothing extra           
      195 +                                                              
      196 +- **Unclear:** Whether a parcel weighing exactly its size's li
          +mit (such as a                                                
      197 +  Small parcel at 1 kg) is over the limit.                    
      198 +- **Assumed:** Only weight strictly above the limit is charged
          +.                                                             
      199 +- **Why:** The brief charges "over weight limit", and a parcel
          + at the limit                                                 
      200 +  isn't over it.                                              
      201 +                                                              
      202 +### Each part of a kilogram over the limit is charged as a who
          +le kilogram                                                   
      203 +                                                              
      204 +- **Unclear:** How weight that is part of a kilogram over the 
          +limit is charged.                                             
      205 +- **Assumed:** The weight over the limit is rounded up to the 
          +next whole                                                    
      206 +  kilogram, at $2 each. 1.2 kg on a Small parcel is 0.2 kg ove
          +r, so it pays $2.                                             
      207 +- **Why:** The brief says to charge $2 for each part-kg, and i
          +ts example                                                    
      208 +  (Small 1.2 kg costs $5) needs this rounding.                
      209 +                                                              
     … 17 more lines (ctrl+o to expand)

● Group 2 is committed. Now task 3.1: final checks.

  Ran 1 shell command (ctrl+o to expand)

● Bash(cd /home/sam/workspace/courier-kata
      sed -i 's/^- \[ \] 3.1 /- [x] 3.1 /'
      openspec/changes/overweight-parcel-charge/tasks.md…)
  ⎿  all_done {'total': 8, 'complete': 8, 'remaining': 0}
     6e1645a Mark overweight-parcel-charge final checks complete
     aaf2f19 Charge $2 per part-kg over each size's weight limit
     8cdbb28 Add parcel weight and split line-item cost
  ⎿  Updated openspec/changes/overweight-parcel-charge/tasks.md (+1 -1)
      29  
      30  ## 3. Final checks
      31  
      32 -- [ ] 3.1 Check the public surface: it should be unchanged apar
         -t from `Parcel.Weight` and the `ParcelLineItem` shape. Check th
         -at every scenario in `specs/parcel-pricing/spec.md` has a match
         -ing test. Verify that `docker compose run --rm tests` passes wi
         -th zero warnings.                                              
      32 +- [x] 3.1 Check the public surface: it should be unchanged apar
         +t from `Parcel.Weight` and the `ParcelLineItem` shape. Check th
         +at every scenario in `specs/parcel-pricing/spec.md` has a match
         +ing test. Verify that `docker compose run --rm tests` passes wi
         +th zero warnings.                                              
      33  
      34  ## Workflow follow-up
      35  

● The overweight charge is built and all 8 tasks in overweight-parcel-charge are
  done. All 60 tests pass with no warnings. All five of your examples are
  covered by tests and give the totals you specified: $5, $10, $12, $17 and $27.

  What changed:
  - Parcel now takes a required fourth argument, Weight, in kilograms. A weight
    of zero or less is rejected with an error naming Weight.
  - ParcelLineItem is now (Parcel, Size, BaseCost, OverweightCharge), and its
    Cost is the sum of the two. Cost is calculated each time it's read, so
    copying a line item with with can't leave it out of date. There's a test for
    this.
  - Pricing in ParcelPricing.cs (src/CourierKata/ParcelPricing.cs) charges $2
    for each kilogram over the size's limit, rounding part-kilograms up. A
    parcel exactly at its limit pays nothing extra, and weight never changes a
    parcel's size. Speedy shipping needed no change: it already charges the sum
    of the parcel costs, so it doubles the overweight charge too.
  - Old tests all use parcels weighing 0.5 kg, which is under every size's
    limit, so their expected prices didn't change. Their expected line items now
    include a 0m overweight charge.
  - New tests are in OverweightChargeTests.cs
    (tests/CourierKata.Tests/OverweightChargeTests.cs) (your five examples, each
    size exactly at its limit, a heavy parcel staying Small) and
    ParcelLineItemTests.cs (tests/CourierKata.Tests/ParcelLineItemTests.cs).
    ParcelTests.cs (tests/CourierKata.Tests/ParcelTests.cs) gained the weight
    checks.
  - docs/assumptions.md has six new entries. I also rewrote the "cheapest
    option" entry to explain why weight still doesn't need a way to choose
    between sizes: each step up in size costs more than its higher weight limit
    can save.

  There are three commits: 8cdbb28 adds weight to parcels and splits the line
  item's cost, aaf2f19 adds the overweight charge, and 6e1645a ticks off the
  final check in tasks.md.

  The main spec in openspec/specs/parcel-pricing/spec.md doesn't include this
  change yet; archiving the change will add it. Run /opsx:archive when you're
  ready, and I'll commit afterwards, as CLAUDE.md asks.

❯ /opsx:archive

● Using change: overweight-parcel-charge (override with /opsx:archive <other>).

  Ran 1 shell command (ctrl+o to expand)

● User answered Claude's questions:
  ⎿  · All 4 artifacts and 8/8 tasks are done. The parcel-pricing delta isn't in
     the main spec yet: it adds 2 requirements (parcel weight validation,
     overweight charge) and modifies 3 (fixed cost, quote line items, speedy
     shipping). Sync it into openspec/specs/parcel-pricing/spec.md before
     archiving? → Sync now (Recommended)

● Bash(cd /home/sam/workspace/courier-kata
      openspec instructions specs --change overweight-parcel-charge --json |
      python3 -c 'import json,sys; d=json.load(sys.stdin);…)
  ⎿  rules: None
      openspec/specs/parcel-pricing/spec.md | 64 ++++++++++++++++++++++++++
     ++++++---
     … +16 lines (ctrl+o to expand)
  ⎿  Updated openspec/specs/parcel-pricing/spec.md (+59 -5)
      2  
      3  ## Purpose
      4  
      5 -Prices a cart of parcels for delivery by classifying each parcel
        - by its size and producing a quote with per-parcel costs, any ch
        -arge for the chosen shipping option, and a total.               
      5 +Prices a cart of parcels for delivery by classifying each parcel
        + by its size, charging for weight over its size's limit, and pro
        +ducing a quote with per-parcel costs, any charge for the chosen 
        +shipping option, and a total.                                   
      6  
      7  ## Requirements
      8  
     ...
      21  - **WHEN** a parcel is created with a length of 10,000 cm
      22  - **THEN** the parcel is created successfully
      23  
      24 +### Requirement: Parcel weight is validated                    
      25 +A parcel SHALL have a weight in kilograms. The weight MUST be a
         + positive number, and fractional weights are allowed. Creating 
         +a parcel with a zero or negative weight MUST fail with an out-o
         +f-range error that names the weight. There is no upper limit on
         + weight.                                                       
      26 +                                                               
      27 +#### Scenario: Zero weight is rejected                         
      28 +- **WHEN** a parcel is created with a weight of 0 kg           
      29 +- **THEN** creation fails with an out-of-range error naming the
         + weight                                                        
      30 +                                                               
      31 +#### Scenario: Negative weight is rejected                     
      32 +- **WHEN** a parcel is created with a weight of -1 kg          
      33 +- **THEN** creation fails with an out-of-range error naming the
         + weight                                                        
      34 +                                                               
      35 +#### Scenario: Fractional weight is accepted                   
      36 +- **WHEN** a parcel is created with a weight of 0.5 kg         
      37 +- **THEN** the parcel is created successfully                  
      38 +                                                               
      39  ### Requirement: Parcels are classified by their largest dimens
          ion
      40  Each parcel SHALL be classified into exactly one size by its la
          rgest dimension: Small when it is under 10 cm, Medium when unde
          r 50 cm, Large when under 100 cm, and XL when 100 cm or more. A
           dimension exactly on a boundary belongs to the larger size. Th
          e orientation of the dimensions MUST NOT affect the size.
      41  
     ...
       68  - **THEN** each is classified as Large
       69  
       70  ### Requirement: Each size has a fixed cost
       56 -Each parcel SHALL cost the fixed price of its size: Small $3, 
          -Medium $8, Large $15, XL $25. Because prices rise with size, t
          -he smallest size a parcel fits is also its cheapest option.   
       71 +Each parcel SHALL have a base cost equal to the fixed price of
          + its size: Small $3, Medium $8, Large $15, XL $25. A parcel's 
          +cost MUST be its base cost plus its overweight charge. Because
          + a larger size raises the base price by more than its higher w
          +eight limit can save, the smallest size a parcel fits is also 
          +its cheapest option at any weight.                            
       72  
       73  #### Scenario: Cost per size
       59 -- **WHEN** a Small, a Medium, a Large and an XL parcel are eac
          -h quoted on their own                                         
       74 +- **WHEN** a Small, a Medium, a Large and an XL parcel, each w
          +ithin its weight limit, are each quoted on their own          
       75  - **THEN** their costs are $3, $8, $15 and $25 respectively
       76  
     … 69 more lines (ctrl+o to expand)

● The sync rules say an existing main spec's Purpose must stay as it is, and I'd
  edited it. I'm reverting that line.

● Bash(cd /home/sam/workspace/courier-kata
      sed -i "s/by its size, charging for weight over its size's limit, and
      producing a quote/by its size and producing a quote/"…)
  ⎿  diff --git a/openspec/specs/parcel-pricing/spec.md b/openspec/specs/pa
     rcel-pricing/spec.md
     index e4a70c3..496b38e 100644
     … +34 lines (ctrl+o to expand)
  ⎿  Updated openspec/specs/parcel-pricing/spec.md (+1 -1)
      2  
      3  ## Purpose
      4  
      5 -Prices a cart of parcels for delivery by classifying each parcel
        - by its size, charging for weight over its size's limit, and pro
        -ducing a quote with per-parcel costs, any charge for the chosen 
        -shipping option, and a total.                                   
      5 +Prices a cart of parcels for delivery by classifying each parcel
        + by its size and producing a quote with per-parcel costs, any ch
        +arge for the chosen shipping option, and a total.               
      6  
      7  ## Requirements
      8  

● The sync checks out, so I'm archiving and committing.

● Bash(cd /home/sam/workspace/courier-kata
      T=openspec/changes/archive/2026-11-11-overweight-parcel-charge…)
  ⎿  .
     ..
     design.md
     … +8 lines (ctrl+o to expand)
  ⎿  Created openspec/changes/archive/2026-11-11-overweight-parcel-charge/p
     roposal.md (+33 -0)
       1 +# Proposal                                                     
       2 +                                                               
       3 +## Why                                                         
       4 +                                                               
       5 +Delivery drivers report that customers are taking advantage of 
         +pricing that only looks at dimensions, by sending small parcels
         + that are very heavy. Each parcel size now has a weight limit, 
         +and parcels over it pay an extra charge.                       
       6 +                                                               
       7 +## What Changes                                                
       8 +                                                               
       9 +- **BREAKING**: A parcel has a weight in kilograms as well as i
         +ts three dimensions. The weight is required and must be greater
         + than zero.                                                    
      10 +- Each size has a weight limit: Small 1 kg, Medium 3 kg, Large 
         +6 kg, XL 10 kg.                                                
      11 +- A parcel heavier than its size's limit pays $2 for each kilog
         +ram over the limit, and any part of a kilogram counts as a whol
         +e one. For example, 0.2 kg over costs $2. A parcel exactly at i
         +ts limit pays nothing extra.                                   
      12 +- **BREAKING**: A parcel line item shows its base cost (the siz
         +e price) and its overweight charge separately. Its cost is the 
         +sum of the two. Parcels within their limit show an overweight c
         +harge of $0.                                                   
      13 +- Speedy shipping still costs the sum of the parcel costs, so i
         +t now doubles overweight charges too. For example, a 1.2 kg Sma
         +ll parcel with Speedy costs $5 + $5 = $10.                     
      14 +- Existing tests give their parcels weights within the limits, 
         +so their expected prices don't change.                         
      15 +- Record the new interpretations in `docs/assumptions.md`, and 
         +update the "cheapest option" entry.                            
      16 +                                                               
      17 +Out of scope: choosing between pricing options, discounts, and 
         +any other change to how parcels are sized.                     
      18 +                                                               
      19 +## Capabilities                                                
      20 +                                                               
      21 +### New Capabilities                                           
      22 +                                                               
      23 +None.                                                          
      24 +                                                               
      25 +### Modified Capabilities                                      
      26 +- `parcel-pricing`: parcels gain a validated weight. Each size 
         +gains a weight limit and an overweight charge. Parcel line item
         +s show the base cost and overweight charge. Speedy shipping gai
         +ns a scenario that shows it doubles overweight charges.        
      27 +                                                               
      28 +## Impact                                                      
      29 +                                                               
      30 +- Public API: `Parcel` gains a required `Weight` (`decimal`, kg
         +). `ParcelLineItem` changes from `(Parcel, Size, Cost)` to `(Pa
         +rcel, Size, BaseCost, OverweightCharge)`, and `Cost` is worked 
         +out from those two.                                            
      31 +- Internal: `ParcelPricing` gains weight limits and the overwei
         +ght calculation. `Cart` and `SpeedyShippingLineItem` don't chan
         +ge.                                                            
      32 +- Tests: every `new Parcel(...)` gains a weight within its size
         +'s limit. Every `new ParcelLineItem(...)` in expected values ga
         +ins a `0m` overweight charge. New tests cover the overweight ex
         +amples and weight validation.                                  
      33 +- Docs: new and updated entries in `docs/assumptions.md`.      
  ⎿  Created openspec/changes/archive/2026-11-11-overweight-parcel-charge/.
     openspec.yaml (+2 -0)
      1 +schema: spec-driven                                             
      2 +created: 2026-11-11                                             
  ⎿  Created
     openspec/changes/archive/2026-11-11-overweight-parcel-charge/tasks.md
     (+37 -0)
       1 +# Tasks                                                        
       2 +                                                               
       3 +## 1. Weight on parcels and split line-item costs (no price cha
         +nge)                                                           
       4 +                                                               
       5 +- [x] 1.1 Change `ParcelLineItem` to `(Parcel, Size, BaseCost, 
         +OverweightCharge)`, with `Cost` worked out as `BaseCost + Overw
         +eightCharge` when read (see design.md, Risks). `ParcelPricing` 
         +sets `OverweightCharge = 0m` for now. Update every expected `ne
         +w ParcelLineItem(...)` in the tests to pass `0m`. Add tests tha
         +t `Cost` is the sum of the two parts, and that `item with { Ove
         +rweightCharge = 4m }` gives the right `Cost`. Verify that `dock
         +er compose run --rm tests` passes with zero warnings.          
       6 +- [x] 1.2 Write failing tests: a weight of 0 kg or -1 kg throws
         + `ArgumentOutOfRangeException` naming `Weight`, and 0.5 kg is a
         +ccepted. Add the required `Weight` parameter to `Parcel`, valid
         +ated with `ThrowIfNegativeOrZero`. Give every existing `new Par
         +cel(...)` in the tests a weight within its size's limit (for ex
         +ample `0.5m`), so no expected price changes. Verify with `docke
         +r compose run --rm tests`.                                     
       7 +- [x] 1.3 Add these assumptions to `docs/assumptions.md`, then 
         +commit them with the code:                                     
       8 +  - weight is required, positive and in `decimal` kilograms, wi
         +th no upper limit                                              
       9 +  - the overweight charge is shown separately, and is $0 within
         + the limit                                                     
      10 +                                                               
      11 +## 2. Overweight charge (test first, starting with the brief's 
         +examples)                                                      
      12 +                                                               
      13 +- [x] 2.1 Write a failing test in `OverweightChargeTests`: a Sm
         +all parcel weighing 1.2 kg has base $3, overweight $2, cost $5 
         +and quote total $5. Add the weight limits (`LimitOf(size)`) and
         + the $2-per-part-kg charge using `Math.Ceiling` in `ParcelPrici
         +ng`. Verify with `docker compose run --rm tests`.              
      14 +- [x] 2.2 Add tests for these cases, implementing anything stil
         +l missing:                                                     
      15 +  - Medium 5 kg costs $12                                      
      16 +  - Large 7 kg costs $17                                       
      17 +  - XL 11 kg costs $27                                         
      18 +  - each size exactly at its limit has an overweight charge of 
         +$0                                                             
      19 +  - a 5 x 5 x 5 cm parcel weighing 20 kg is still Small        
      20 +                                                               
      21 +  Verify with `docker compose run --rm tests`.                 
      22 +- [x] 2.3 Add a test that a Small 1.2 kg parcel with Speedy shi
         +pping gives a parcel item of $5, a speedy item of $5 and a tota
         +l of $10. Verify with `docker compose run --rm tests`. No code 
         +change should be needed.                                       
      23 +- [x] 2.4 Update `docs/assumptions.md`, then commit with the co
         +de:                                                            
      24 +  - add: at the limit pays nothing                             
      25 +  - add: part-kilograms round up                               
      26 +  - add: weight doesn't change size                            
      27 +  - add: Speedy doubles overweight charges too                 
      28 +  - update "Cheapest option is the smallest size that fits" wit
         +h why weight doesn't need an option selector                   
      29 +                                                               
      30 +## 3. Final checks                                             
      31 +                                                               
      32 +- [x] 3.1 Check the public surface: it should be unchanged apar
         +t from `Parcel.Weight` and the `ParcelLineItem` shape. Check th
         +at every scenario in `specs/parcel-pricing/spec.md` has a match
         +ing test. Verify that `docker compose run --rm tests` passes wi
         +th zero warnings.                                              
      33 +                                                               
      34 +## Workflow follow-up                                          
      35 +                                                               
      36 +- Archive the change once implementation is complete, keeping `
         +openspec/specs/parcel-pricing/spec.md` in sync with what was bu
         +ilt.                                                           
      37 +- Commit after archiving.                                      
  ⎿  Created
     openspec/changes/archive/2026-11-11-overweight-parcel-charge/design.md
     (+67 -0)
       1 +# Design                                                       
       2 +                                                               
       3 +## Context                                                     
       4 +                                                               
       5 +See proposal.md for the motivation. Today `Parcel(Length, Width
         +, Height)` validates its dimensions in its constructor. The int
         +ernal `ParcelPricing.Price` classifies a parcel by its largest 
         +dimension and returns `ParcelLineItem(Parcel, Size, Cost)`. `Ca
         +rt.GetQuote()` prices each parcel and, with Speedy, adds a `Spe
         +edyShippingLineItem` costing the sum of the parcel `Cost`s. Wei
         +ght affects only the parcel's own price, so the change stays wi
         +thin `Parcel`, `ParcelPricing` and `ParcelLineItem`.           
       6 +                                                               
       7 +## Goals / Non-Goals                                           
       8 +                                                               
       9 +**Goals:**                                                     
      10 +- Quotes show the overweight charge on the parcel's own line it
         +em, separate from the size price.                              
      11 +- `Cost` keeps meaning "what this line item costs", so `Quote.T
         +otal` and Speedy don't change.                                 
      12 +                                                               
      13 +**Non-Goals:**                                                 
      14 +- A general selector that compares pricing options. Weight does
         +n't need one, because the smallest size is still cheapest (see 
         +the spec).                                                     
      15 +- A `Weight` or `Money` value type. Weight is plain `decimal` k
         +ilograms, just as dimensions are plain `decimal` centimetres.  
      16 +                                                               
      17 +## Decisions                                                   
      18 +                                                               
      19 +### Weight is a required fourth `Parcel` parameter             
      20 +```                                                            
      21 +public sealed record Parcel(decimal Length, decimal Width, deci
         +mal Height, decimal Weight)                                    
      22 +```                                                            
      23 +- It's required, not optional with a default. A default (say 0 
         +kg or 1 kg) would hide parcels that callers forgot to weigh, wh
         +ich is the loophole this change closes.                        
      24 +- It's validated in the constructor with `ThrowIfNegativeOrZero
         +`, as the dimensions are. The parameter name in the error is `W
         +eight`.                                                        
      25 +- Alternative considered: a separate `Weight` property set afte
         +r construction. It was rejected because a parcel could then exi
         +st without a weight.                                           
      26 +                                                               
      27 +### Line item shows base cost and overweight charge (the user c
         +hose this)                                                     
      28 +```                                                            
      29 +public sealed record ParcelLineItem(Parcel Parcel, ParcelSize S
         +ize, decimal BaseCost, decimal OverweightCharge)               
      30 +    : LineItem(BaseCost + OverweightCharge)                    
      31 +```                                                            
      32 +- `Cost` always equals `BaseCost + OverweightCharge`, and a cal
         +ler can't build an inconsistent item. It's worked out when read
         +, not stored, so `with` expressions stay consistent (see Risks)
         +. The sketch above shows the shape, not the final mechanism.   
      33 +- Parcels within their limit carry `OverweightCharge = 0m`, so 
         +every parcel line has the same shape.                          
      34 +- The name `OverweightCharge` says when the charge applies. `We
         +ightCharge` would suggest every parcel pays for weight.        
      35 +- Alternatives considered: folding the charge into `Cost`, whic
         +h is simpler but hides why a parcel costs more, and a separate 
         +`OverweightLineItem`, which would separate the charge from its 
         +parcel.                                                        
      36 +                                                               
      37 +### Overweight calculation lives in `ParcelPricing`            
      38 +```                                                            
      39 +over   = parcel.Weight - LimitOf(size)                         
      40 +charge = over > 0 ? 2m * Math.Ceiling(over) : 0m               
     … 27 more lines (ctrl+o to expand)
  ⎿  Created openspec/changes/archive/2026-11-11-overweight-parcel-charge/s
     pecs/parcel-pricing/spec.md (+96 -0)
       1 +# Spec Delta                                                   
       2 +                                                               
       3 +## ADDED Requirements                                          
       4 +                                                               
       5 +### Requirement: Parcel weight is validated                    
       6 +A parcel SHALL have a weight in kilograms. The weight MUST be a
         + positive number, and fractional weights are allowed. Creating 
         +a parcel with a zero or negative weight MUST fail with an out-o
         +f-range error that names the weight. There is no upper limit on
         + weight.                                                       
       7 +                                                               
       8 +#### Scenario: Zero weight is rejected                         
       9 +- **WHEN** a parcel is created with a weight of 0 kg           
      10 +- **THEN** creation fails with an out-of-range error naming the
         + weight                                                        
      11 +                                                               
      12 +#### Scenario: Negative weight is rejected                     
      13 +- **WHEN** a parcel is created with a weight of -1 kg          
      14 +- **THEN** creation fails with an out-of-range error naming the
         + weight                                                        
      15 +                                                               
      16 +#### Scenario: Fractional weight is accepted                   
      17 +- **WHEN** a parcel is created with a weight of 0.5 kg         
      18 +- **THEN** the parcel is created successfully                  
      19 +                                                               
      20 +### Requirement: Parcels over their size's weight limit pay an 
         +overweight charge                                              
      21 +Each size SHALL have a weight limit: Small 1 kg, Medium 3 kg, L
         +arge 6 kg, XL 10 kg. A parcel heavier than its size's limit MUS
         +T pay $2 for each kilogram over the limit, rounding any part of
         + a kilogram up to a whole kilogram. A parcel at or under its li
         +mit MUST pay an overweight charge of $0. Weight MUST NOT affect
         + the parcel's size.                                            
      22 +                                                               
      23 +#### Scenario: Small parcel part of a kilogram over            
      24 +- **WHEN** a Small parcel weighing 1.2 kg is quoted            
      25 +- **THEN** its overweight charge is $2, its cost is $5 and the 
         +quote total is $5                                              
      26 +                                                               
      27 +#### Scenario: Medium parcel whole kilograms over              
      28 +- **WHEN** a Medium parcel weighing 5 kg is quoted             
      29 +- **THEN** its overweight charge is $4, its cost is $12 and the
         + quote total is $12                                            
      30 +                                                               
      31 +#### Scenario: Large parcel over                               
      32 +- **WHEN** a Large parcel weighing 7 kg is quoted              
      33 +- **THEN** its overweight charge is $2, its cost is $17 and the
         + quote total is $17                                            
      34 +                                                               
      35 +#### Scenario: XL parcel over                                  
      36 +- **WHEN** an XL parcel weighing 11 kg is quoted               
      37 +- **THEN** its overweight charge is $2, its cost is $27 and the
         + quote total is $27                                            
      38 +                                                               
      39 +#### Scenario: Parcel exactly at its weight limit              
      40 +- **WHEN** a Small parcel weighing 1 kg, a Medium weighing 3 kg
         +, a Large weighing 6 kg and an XL weighing 10 kg are each quote
         +d                                                              
     … 56 more lines (ctrl+o to expand)
  ⎿  … 5 more files changed

● The overweight-parcel-charge change (spec-driven schema) is archived to
  openspec/changes/archive/2026-11-11-overweight-parcel-charge/, and its spec
  changes are merged into the main spec. All 4 artifacts and all 8 tasks were
  complete, so there are no warnings.

  Spec sync: openspec/specs/parcel-pricing/spec.md now has the two new
  requirements: parcel weight validation, and the overweight charge. They sit
  next to the dimension and size-cost requirements they relate to, rather than
  at the end. Three existing requirements were replaced with their updated
  versions: fixed cost, quote line items, and Speedy shipping. All five match
  the change's spec exactly, and the main spec passes openspec validate
  --strict.

  During the merge I briefly added a mention of weight to the spec's Purpose
  paragraph. The sync rules say an existing Purpose must stay as written, so I
  reverted it before committing. It still doesn't mention weight; if you want it
  to, that's a separate edit.

  Commit: 30cbf19 Archive overweight-parcel-charge and sync parcel-pricing spec.
  The working tree is clean.

❯ make a new branch for these changes, move master back to 9cb886b
  then squash and merge the new branch into master

  Ran 1 shell command (ctrl+o to expand)

● The overweight-charge work is now a single commit on master, and the file
  contents are identical to before.

  - New branch overweight-parcel-charge points at 30cbf19 and keeps the four
    original commits.
  - master was moved back to 9cb886b (Add speedy shipping). Nothing was lost,
    because the branch already held those commits.
  - The squash merge is commit 3dc32f3, "Add overweight charge per parcel size",
    on top of 9cb886b. Its message explains why the charge exists, the weight
    limits and $2/kg rule, the Parcel and line-item changes, and the spec sync.

  A mistake on my part: CLAUDE.md says commit messages mustn't have a
  "Co-Authored-By: Claude" line or mention Claude, but my four earlier commits
  included one. The squash commit on master doesn't. The old commits still have
  it on the overweight-parcel-charge branch, which could leak the line if that
  branch is ever pushed. I can delete the branch if you don't need it.

  The working tree is clean, and nothing has been pushed.