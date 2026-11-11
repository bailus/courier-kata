
❯ /opsx:explore In order to award those who send multiple parcels, special
discounts have been introduced.

Small parcel mania! Every 4th small parcel in an order is free!
Medium parcel mania! Every 3rd medium parcel in an order is free!
Mixed parcel mania! Every 5th parcel in an order is free!
Each parcel can only be used in a discount once
Within each discount, the cheapest parcel is the free one

requirements:
- The combination of discounts which saves the most money should be
selected every time
- Just like speedy shipping, discounts should be listed as a separate item in
the
output, with associated saving, e.g. “-$2”
- Discounts should not impact the price of individual parcels, i.e. their
individual
cost should remain the same as it was before
- Speedy shipping applies after discounts are taken into account

test:
6x medium parcels. 3 x $8 and 3 x $10. 1st discount should include all 3 $8
parcels and save $8. 2nd discount should include all 3 $10 parcels and save
$10.

create a new git branch for this

  Read 2 files, ran 9 shell commands (ctrl+o to expand)

● I created the branch multi-parcel-discounts from master. master was clean and
  has exactly the same files as the tip of heavy-parcel. I named the branch so
  the OpenSpec change can use the same name, as earlier branches did. I haven't
  written anything else.

  How the brief fits the current model

  Here is your example with Speedy shipping. The $10 Mediums weigh 4 kg, so a
  free one saves its $2 overweight charge as well:

    6 Medium: 3 x 3 kg ($8), 3 x 4 kg ($10), Speedy
    +-----------------------------------------+-------+
    | Medium  size rate  8 + 0   (x3)         |   $8  |  parcel lines unchanged
    | Medium  size rate  8 + 2   (x3)         |  $10  |
    +-----------------------------------------+-------+
    | Medium parcel mania  [the three $8s]    |  -$8  |  one line per discount,
    | Medium parcel mania  [the three $10s]   | -$10  |  negative Cost
    +-----------------------------------------+-------+
    | Speedy shipping  (54 - 18)              |  $36  |  applied after discounts
    +-----------------------------------------+-------+
    | Total                                   |  $72  |  still the sum of lines
    +-----------------------------------------+-------+

  The code change is contained. Cart.GetQuote (src/CourierKata/Cart.cs#L32-L43)
  already lists the parcels, then speedy shipping, then works out the total. The
  discount lines go between the parcels and speedy. Speedy's charge at
  Cart.cs:39 (src/CourierKata/Cart.cs#L39) becomes the parcel costs minus the
  savings, so with Speedy each discount saves twice its value. The assumptions
  log (docs/assumptions.md) left how speedy combines with discounts for this
  change to decide.

  The hard part: the best combination every time

                Small mania   Medium mania   Mixed mania
                4, 1 free     3, 1 free      5, 1 free
    Small           x                            x
    Medium                         x             x
    Large / XL                                   x

  Small and Medium parcels can go into their own discount or into Mixed. Neither
  simple order of applying discounts always finds the best saving:

    Mixed first?   your example [10 10 10 8 8 8]
                   Mixed {10,10,10,8,8} = -$8, one $8 left          -> $8
                   best: Medium {10,10,10} + Medium {8,8,8}         -> $18

    Size first?    Medium [12 12 8] + Large [15 15 15 15]
                   Medium {12,12,8} = -$8, 4 Larges can't make Mixed -> $8
                   best: Mixed {15,15,15,15,12}                      -> $12

  The $12 parcel is the 5 kg Medium from the existing spec.

  Finding the best answer is still cheap. Within one discount type, grouping
  parcels with their nearest neighbours by price never saves less. Your 6-Medium
  example is that rule. So only one group of each type needs to be open at a
  time, and one pass over the parcels sorted most expensive first finds the best
  combination:

    each parcel:  skip | pay into the open Small / Medium / Mixed group
                       | be the free (cheapest) parcel that closes it
    state = members waiting per type: 4 x 3 x 5 = 60 states, keep the best
  saving for each

  This is exact and runs in linear time after sorting, without trying every
  possible grouping. The details belong in design.md. The point here is that the
  requirement won't make quotes slow.

  What I'd assume unless you object

  - A discount is any group of the right size. "Every 4th" doesn't depend on the
    order parcels were added. Mixed means any 5 parcels, including 5 of the
    same size.
  - "Cheapest" and the saving use each parcel's charged cost, including
    overweight charges and the Heavy rate. Your $10 Medium shows this.
  - One DiscountLineItem per group. It holds the discount type, its parcels and
    a negative Cost. The library doesn't produce text, so "-$2" means Cost ==
    -2. Discount lines come after the parcels and before speedy. They're ordered
    by when each group's first parcel was added, which puts your $8 group first
    if those parcels were added first.
  - Equal savings need a fixed choice. Five $3 Small parcels save $3 either as
    Small mania or as Mixed. When savings are equal, I'd pick Small or Medium
    mania over Mixed.

  Two things to handle in the design:
  - LineItem has a closed set of types (LineItem.cs
    (src/CourierKata/LineItem.cs)). Adding a third type breaks callers that
    match every type, as speedy shipping did.
  - A record that holds an IReadOnlyList<Parcel> compares the list by reference.
    Unless the record compares the lists item by item, Assert.Equal on whole
    discount lines will fail.

  Decision needed

  The heavy-parcel change left this for you (see the Risks section of its
  design.md (openspec/changes/archive/2026-11-11-heavy-parcel/design.md)).
  Should a parcel charged at the Heavy rate count towards Small or Medium mania
  by its size, or only towards Mixed?

  - By its size (my recommendation). A 40 kg parcel in a 5x5x5 box is Small,
    which its line item already shows, and weight still never changes size. This
    never gives the customer a smaller saving. A Heavy-rate parcel costs at
    least $50, so it's rarely the free one; it mostly helps a group form. The
    main effect is a group of only Heavy-rate parcels: four 40 kg Small parcels
    save $50.
  - Mixed only. Heavy is treated as its own type, the way the original brief
    introduced it, and Small parcel mania stays about cheap small parcels. The
    same four parcels save nothing.

  Either way the search works the same; only which parcels can join which
  discount changes. Which do you want?

  Once that's decided, /opsx:propose can turn this into the
  multi-parcel-discounts change on this branch.

❯ by size
  /opsx:propose

● Skill(opsx:propose)
  ⎿  Successfully loaded skill · 1 tool allowed

  Ran 1 shell command (ctrl+o to expand)

● Bash(grep -v '^\s*#' openspec/config.yaml | grep -v '^\s*$'; echo "---";
      openspec new change "multi-parcel-discounts" && openspec status --change
      "multi-parcel-disco…)
  ⎿  schema: spec-driven
     ---
     - Creating change 'multi-parcel-discounts' with schema 'spec-driven'..
     … +109 lines (ctrl+o to expand)
  ⎿  Created openspec/changes/multi-parcel-discounts/.openspec.yaml (+2 -0)
      1 +schema: spec-driven                                             
      2 +created: 2026-11-11                                             
  ⎿  (files that changed while this command ran, by the command or by
     another process; a convenience view, not a review or audit of the
     command)

  Ran 1 shell command (ctrl+o to expand)

● Write(openspec/changes/multi-parcel-discounts/proposal.md)
  ⎿  Wrote 37 lines to openspec/changes/multi-parcel-discounts/proposal.md
       1 # Proposal
       2
       3 ## Why
       4
       5 To reward customers who send several parcels in one order, the c
         ourier is introducing three discounts that each make one parcel
         in a group free. A quote has to find the combination of discount
         s that saves the most, show each discount on its own line, and a
         pply speedy shipping to the discounted order.
       6
     … +31 lines (ctrl+o to expand)

  Ran 2 shell commands (ctrl+o to expand)

● Writing the parcel-discounts spec (new capability).

● Write(openspec/changes/multi-parcel-discounts/specs/parcel-discounts/spec.md)
  ⎿  Wrote 122 lines to
     openspec/changes/multi-parcel-discounts/specs/parcel-discounts/spec.md
       1 # Spec Delta
       2
       3 ## Purpose
       4
       5 Rewards customers who send several parcels in one order by makin
         g some of those parcels free. Covers which groups of parcels ear
         n a discount, which parcel in a group is free, choosing the comb
         ination of discounts that saves the most, and how discounts appe
         ar on the quote.
       6
     … +116 lines (ctrl+o to expand)

● Writing the parcel-pricing delta (modified capability). First pulling the two
  existing requirement blocks verbatim.

  Read 1 file (ctrl+o to expand)

● Write(openspec/changes/multi-parcel-discounts/specs/parcel-pricing/spec.md)
  ⎿  Wrote 65 lines to
     openspec/changes/multi-parcel-discounts/specs/parcel-pricing/spec.md
       1 # Spec Delta
       2
       3 ## MODIFIED Requirements
       4
       5 ### Requirement: Quote lists each parcel with its size and cost
       6 A quote SHALL contain one parcel line item per parcel in the car
         t, in the order the parcels were added. Each parcel line item MU
         ST show the parcel, its size, the rate it is charged at, its bas
         e cost, its overweight charge and its cost. Any discount line it
         ems MUST come after all the parcel line items. When speedy shipp
     … +60 lines (~4 KB) (ctrl+o to expand)

  Read 1 file, ran 2 shell commands (ctrl+o to expand)

● Write(openspec/changes/multi-parcel-discounts/design.md)
  ⎿  Wrote 131 lines to openspec/changes/multi-parcel-discounts/design.md
      1 # Design
      2
      3 ## Context
      4
      5 See proposal.md for the motivation and `specs/parcel-discounts/sp
        ec.md` for the rules. Today `Cart.GetQuote()` prices each parcel
        into a `ParcelLineItem` with the internal `ParcelPricing.Price`.
        When Speedy is selected, it appends a `SpeedyShippingLineItem` co
        sting the sum of the parcel costs, then totals the items. `LineIt
        em` is a closed hierarchy of two kinds, kept closed by an `intern
     … +127 lines (~12 KB) (ctrl+o to expand)

  Ran 2 shell commands (ctrl+o to expand)

● Write(openspec/changes/multi-parcel-discounts/tasks.md)
  ⎿  Wrote 77 lines to openspec/changes/multi-parcel-discounts/tasks.md
       1 # Tasks
       2
       3 ## 1. Discount line item (no behaviour change)
       4
       5 - [ ] 1.1 Write failing tests in `DiscountLineItemTests`:
       6   - `Cost` is minus `Saving` (a saving of $8 costs -$8)
       7   - two items with the same discount, equal parcels in the same
         order and the same saving are equal and have equal hash codes
       8   - items that differ in discount, parcels, parcel order or savi
         ng are not equal
     … +69 lines (ctrl+o to expand)

  Ran 1 shell command (ctrl+o to expand)

● Written for: you as reviewer and whoever runs /opsx:apply, in the same style
  as the earlier change artifacts.

  I've created the multi-parcel-discounts change in
  openspec/changes/multi-parcel-discounts/ on the multi-parcel-discounts branch.
  Heavy-rate parcels now join discounts by their size, as you chose. openspec
  validate --strict passes. Nothing is committed yet.

  Artifacts
  - proposal.md (openspec/changes/multi-parcel-discounts/proposal.md): why the
    change is needed, what changes, and the effect on the public API. The
    breaking change is that DiscountLineItem becomes a third kind of LineItem.
  - specs/parcel-discounts/spec.md
    (openspec/changes/multi-parcel-discounts/specs/parcel-discounts/spec.md): a
    new capability with 8 requirements and 21 scenarios. They cover which groups
    qualify, that the cheapest parcel is free, joining by size, that each
    parcel is used once, the best combination, how ties are settled, and how
    discount lines are listed and ordered. Your 6-Medium example is one of the
    scenarios.
  - specs/parcel-pricing/spec.md
    (openspec/changes/multi-parcel-discounts/specs/parcel-pricing/spec.md):
    changes to two existing requirements. Discount lines go between the parcel
    lines and speedy shipping, and speedy costs the parcel total minus the
    savings.
  - design.md (openspec/changes/multi-parcel-discounts/design.md): the shape of
    Discount and DiscountLineItem, and the search that finds the best
    combination exactly in a single pass over the sorted parcels, with why it
    can't miss a better one. It also shows why simple greedy orders fail and
    covers equality for the list of parcels.
  - tasks.md (openspec/changes/multi-parcel-discounts/tasks.md): 4 groups. The
    line item comes first, then choosing discounts test-first from your example,
    then speedy after discounts, then final checks. Each group ends in a commit
    that includes its assumption entries.

  Choices to check
  You only answered the Heavy-rate question, so these are my defaults:
  - Discounts are a new spec, parcel-discounts, rather than more requirements in
    parcel-pricing, which already has 13.
  - Equal savings go to the size discounts. If two combinations save the same,
    the one with fewer Mixed discounts wins, so five $3 Small parcels get Small
    parcel mania.
  - Each discount line shows its type, the Parcels in its group and a positive
    Saving. Its Cost is -Saving. Lines are ordered by each discount's first
    parcel in the cart.
  - Mixed parcel mania takes any 5 parcels, including 5 of the same size.
  - Two extra tests are planned. One compares the result for a few hundred
    random carts against a test that tries every possible grouping. The other
    quotes 1,000 Small parcels to show large carts stay fast.

  One example changed from our earlier discussion. The "size discounts first"
  counterexample now uses Medium parcels at $12, $8 and $8 instead of $12, $12
  and $8. With two $12 parcels, two different Mixed groups both save $12, and
  the spec couldn't say which one should be chosen.

  The proposal isn't committed yet. Earlier branches started with a "Propose …"
  commit, and I can make one here once you're happy with it.

  All artifacts needed for implementation are ready. When you've reviewed them,
  run /opsx:apply.

❯ commit, then apply

  Ran 1 shell command (ctrl+o to expand)

● Skill(opsx:apply)
  ⎿  Successfully loaded skill · 1 tool allowed

● Using change: multi-parcel-discounts (override with /opsx:apply <other>).

  Ran 1 shell command (ctrl+o to expand)

● Schema spec-driven, 0/12 tasks done. Starting task 1.1: discount line item
  tests.

● Write(tests/CourierKata.Tests/DiscountLineItemTests.cs)
  ⎿  Wrote 43 lines to tests/CourierKata.Tests/DiscountLineItemTests.cs
       1 namespace CourierKata.Tests;
       2
       3 public class DiscountLineItemTests
       4 {
       5     private static readonly Parcel Small = new(5m, 5m, 5m, 0.5m)
         ;
       6     private static readonly Parcel Medium = new(20m, 5m, 5m, 0.5
         m);
       7
       8     [Fact]
     … +35 lines (ctrl+o to expand)

  Ran 1 shell command (ctrl+o to expand)
● Write(src/CourierKata/Discount.cs)
  ⎿  Wrote 14 lines to src/CourierKata/Discount.cs
       1 namespace CourierKata;
       2
       3 /// <summary>A discount that makes the cheapest parcel in a grou
         p free.</summary>
       4 public enum Discount
       5 {
       6     /// <summary>A group of 4 Small parcels.</summary>
       7     SmallParcelMania,
       8
       9     /// <summary>A group of 3 Medium parcels.</summary>
     … +5 lines (ctrl+o to expand)

● Write(src/CourierKata/DiscountLineItem.cs)
  ⎿  Wrote 36 lines to src/CourierKata/DiscountLineItem.cs
       1 namespace CourierKata;
       2
       3 /// <summary>
       4 /// A discount applied to a group of parcels: which discount it
         is, the parcels in its group
       5 /// in the order they were added, and how much it saves in dolla
         rs.
       6 /// </summary>
       7 public sealed record DiscountLineItem(Discount Discount, IReadOn
         lyList<Parcel> Parcels, decimal Saving) : LineItem
     … +29 lines (ctrl+o to expand)

● Update(src/CourierKata/LineItem.cs)
  ⎿  Added 1 line, removed 1 line
      2  
      3  /// <summary>
      4  /// One line of a quote and its cost in dollars. The kinds of li
         ne item are fixed:
      5 -/// <see cref="ParcelLineItem"/> and <see cref="SpeedyShippingLi
        -neItem"/>.                                                      
      5 +/// <see cref="ParcelLineItem"/>, <see cref="DiscountLineItem"/>
        + and <see cref="SpeedyShippingLineItem"/>.                      
      6  /// </summary>
      7  public abstract record LineItem
      8  {

  Ran 1 shell command (ctrl+o to expand)

● Task 1.1 passes (76 tests, no warnings). Marking it and committing for 1.2.

● Bash(f=openspec/changes/multi-parcel-discounts/tasks.md; sed -i '5s/- \[ \]
      1.1/- [x] 1.1/' $f && sed -n 5p $f | cut -c1-30 && git add -A && git
      commit -q -m "Add di…)
  ⎿  - [x] 1.1 Write failing tests
     5142a0c Add discount line item
  ⎿  Updated openspec/changes/multi-parcel-discounts/tasks.md (+2 -2)
       2  
       3  ## 1. Discount line item (no behaviour change)
       4  
       5 -- [ ] 1.1 Write failing tests in `DiscountLineItemTests`:      
       5 +- [x] 1.1 Write failing tests in `DiscountLineItemTests`:      
       6    - `Cost` is minus `Saving` (a saving of $8 costs -$8)
       7    - two items with the same discount, equal parcels in the same
           order and the same saving are equal and have equal hash codes
       8    - items that differ in discount, parcels, parcel order or sav
          ing are not equal
       9  
      10    Add the public enum `Discount { SmallParcelMania, MediumParce
          lMania, MixedParcelMania }` and `sealed record DiscountLineItem
          (Discount Discount, IReadOnlyList<Parcel> Parcels, decimal Savi
          ng) : LineItem`, with XML doc comments, `Cost => -Saving`, `Sea
          lHierarchy()`, and `Equals`/`GetHashCode` that compare `Parcels
          ` item by item (see design.md). Update `LineItem`'s doc comment
           to list all three kinds. Verify that `docker compose run --rm 
          tests` passes with zero warnings.
      11 -- [ ] 1.2 Commit the enum, the record and their tests, and veri
         -fy the working tree is clean afterwards.                       
      11 +- [x] 1.2 Commit the enum, the record and their tests, and veri
         +fy the working tree is clean afterwards.                       
      12  
      13  ## 2. Choosing discounts (test first, starting with the brief's
           example)
      14  

● Starting task 2.1: the brief's example, test first.

● Write(tests/CourierKata.Tests/QuoteParcels.cs)
  ⎿  Wrote 52 lines to tests/CourierKata.Tests/QuoteParcels.cs
       1 namespace CourierKata.Tests;
       2
       3 /// <summary>Parcels at known costs, and helpers for quoting car
         ts of them.</summary>
       4 internal static class QuoteParcels
       5 {
       6     /// <summary>Small, $3.</summary>
       7     public static readonly Parcel Small = new(5m, 5m, 5m, 0.5m);
       8
       9     /// <summary>Small weighing 5 kg, $11.</summary>
     … +43 lines (ctrl+o to expand)

● Write(tests/CourierKata.Tests/DiscountCombinationTests.cs)
  ⎿  Wrote 23 lines to tests/CourierKata.Tests/DiscountCombinationTests.cs
       1 using static CourierKata.Tests.QuoteParcels;
       2
       3 namespace CourierKata.Tests;
       4
       5 public class DiscountCombinationTests
       6 {
       7     [Fact]
       8     public void Parcels_are_grouped_with_others_of_similar_cost(
         )
       9     {
     … +14 lines (ctrl+o to expand)

  Ran 1 shell command (ctrl+o to expand)

● Write(src/CourierKata/DiscountSelection.cs)
  ⎿  Wrote 152 lines to src/CourierKata/DiscountSelection.cs
       1 using System.Collections.Immutable;
       2
       3 namespace CourierKata;
       4
       5 /// <summary>Chooses the combination of multi-parcel discounts t
         hat saves the most.</summary>
       6 /// <remarks>
       7 /// <para>
       8 /// The parcels are walked once, most expensive first. Every gro
         up is built in that order, so the
     … +144 lines (ctrl+o to expand)

● Update(src/CourierKata/Cart.cs)
  ⎿  Added 5 lines, removed 1 line
      28          _parcels.Add(parcel);
      29      }
      30  
      31 -    /// <summary>Prices every parcel in the cart, followed by s
         -peedy shipping when it is selected.</summary>                  
      31 +    /// <summary>                                              
      32 +    /// Prices every parcel in the cart, followed by the discou
         +nts that save the most, then speedy                            
      33 +    /// shipping when it is selected.                          
      34 +    /// </summary>                                             
      35      public Quote GetQuote()
      36      {
      37          var parcelItems = _parcels.Select(ParcelPricing.Price).
          ToList();
      38          var items = new List<LineItem>(parcelItems);
      39 +        items.AddRange(DiscountSelection.Choose(parcelItems)); 
      40  
      41          if (Shipping == Shipping.Speedy)
      42          {

  Ran 1 shell command (ctrl+o to expand)

● Passes. Tidying the output-building step before moving on.

● Update(src/CourierKata/DiscountSelection.cs)
  ⎿  Added 1 line, removed 2 lines
      68          var best = paths.OfType<Path>().Aggregate((a, b) => IsB
          etter(b, a) ? b : a);
      69  
      70          return best.Closed
      71 -            .Select(group => group.Members.OrderBy(position => 
         -position).ToList())                                            
      72 -            .Zip(best.Closed, (members, group) => (members, gro
         -up))                                                           
      71 +            .Select(group => (group, members: group.Members.Ord
         +er().ToList()))                                                
      72              .OrderBy(entry => entry.members[0])
      73              .Select(entry => new DiscountLineItem(
      74                  entry.group.Discount,

● Task 2.2: discount rule tests.

● Write(tests/CourierKata.Tests/DiscountTests.cs)
  ⎿  Wrote 93 lines to tests/CourierKata.Tests/DiscountTests.cs
       1 using static CourierKata.Tests.QuoteParcels;
       2
       3 namespace CourierKata.Tests;
       4
       5 public class DiscountTests
       6 {
       7     [Fact]
       8     public void Four_small_parcels_get_small_parcel_mania()
       9     {
      10         var quote = QuoteFor(Times(4, Small));
     … +83 lines (ctrl+o to expand)

  Ran 1 shell command (ctrl+o to expand)

● Bash(f=openspec/changes/multi-parcel-discounts/tasks.md; sed -i -e '15s/^- \[
      \] 2\.1/- [x] 2.1/' -e '16s/^- \[ \] 2\.2/- [x] 2.2/' $f && sed -n
      '15p;16p' $f | cut -…)
  ⎿  - [x] 2.1 Wr
     - [x] 2.2 Ad
  ⎿  Updated openspec/changes/multi-parcel-discounts/tasks.md (+2 -2)
      12  
      13  ## 2. Choosing discounts (test first, starting with the brief's
           example)
      14  
      15 -- [ ] 2.1 Write a failing test in `DiscountCombinationTests` fo
         -r "Parcels are grouped with others of similar cost": three Medi
         -um parcels weighing 3 kg, then three weighing 4 kg, give the si
         -x unchanged parcel items, then a Medium parcel mania item of th
         -e three 3 kg parcels saving $8, then one of the three 4 kg parc
         -els saving $10, and a total of $36. Add the internal static `Di
         -scountSelection.Choose` as described in design.md: parcels sort
         -ed most expensive first, the 60-state walk, more saving and the
         -n fewer Mixed discounts winning, groups rebuilt with their parc
         -els in cart order in a read-only list, and items ordered by eac
         -h group's first parcel. Call it from `Cart.GetQuote()` so the d
         -iscount items come after the parcel items, leaving the speedy c
         -harge as it is for now. Verify with `docker compose run --rm te
         -sts`.                                                          
      16 -- [ ] 2.2 Add tests in `DiscountTests` for the scenarios under 
         -"Each discount needs a full group of parcels", "The cheapest pa
         -rcel in a group is free" and "A parcel joins discounts by its s
         -ize", implementing anything still missing:                     
      15 +- [x] 2.1 Write a failing test in `DiscountCombinationTests` fo
         +r "Parcels are grouped with others of similar cost": three Medi
         +um parcels weighing 3 kg, then three weighing 4 kg, give the si
         +x unchanged parcel items, then a Medium parcel mania item of th
         +e three 3 kg parcels saving $8, then one of the three 4 kg parc
         +els saving $10, and a total of $36. Add the internal static `Di
         +scountSelection.Choose` as described in design.md: parcels sort
         +ed most expensive first, the 60-state walk, more saving and the
         +n fewer Mixed discounts winning, groups rebuilt with their parc
         +els in cart order in a read-only list, and items ordered by eac
         +h group's first parcel. Call it from `Cart.GetQuote()` so the d
         +iscount items come after the parcel items, leaving the speedy c
         +harge as it is for now. Verify with `docker compose run --rm te
         +sts`.                                                          
      16 +- [x] 2.2 Add tests in `DiscountTests` for the scenarios under 
         +"Each discount needs a full group of parcels", "The cheapest pa
         +rcel in a group is free" and "A parcel joins discounts by its s
         +ize", implementing anything still missing:                     
      17    - four Small parcels at $3 give Small parcel mania of all fou
          r, saving $3, total $9
      18    - three Medium parcels weighing 3 kg give Medium parcel mania
           saving $8, total $16
      19    - five Large parcels give Mixed parcel mania saving $15, tota
          l $60

● Task 2.3: combination tests.

● Update(tests/CourierKata.Tests/DiscountCombinationTests.cs)
  ⎿  Added 68 lines
      20              quote.Items);
      21          Assert.Equal(36m, quote.Total);
      22      }
      23 +                                                               
      24 +    [Fact]                                                     
      25 +    public void Seven_small_parcels_get_exactly_one_discount() 
      26 +    {                                                          
      27 +        var quote = QuoteFor(Times(7, Small));                 
      28 +                                                               
      29 +        Assert.Equal([new DiscountLineItem(Discount.SmallParcel
         +Mania, Times(4, Small), 3m)], DiscountsIn(quote));             
      30 +        Assert.Equal(18m, quote.Total);                        
      31 +    }                                                          
      32 +                                                               
      33 +    [Fact]                                                     
      34 +    public void The_same_parcel_added_four_times_counts_four_ti
         +mes()                                                          
      35 +    {                                                          
      36 +        var parcel = new Parcel(5m, 5m, 5m, 0.5m);             
      37 +                                                               
      38 +        var quote = QuoteFor(parcel, parcel, parcel, parcel);  
      39 +                                                               
      40 +        Assert.Equal([new DiscountLineItem(Discount.SmallParcel
         +Mania, Times(4, parcel), 3m)], DiscountsIn(quote));            
      41 +    }                                                          
      42 +                                                               
      43 +    [Fact]                                                     
      44 +    public void Mixed_parcel_mania_is_chosen_when_it_saves_more
         +_than_a_size_discount()                                        
      45 +    {                                                          
      46 +        var quote = QuoteFor(Concat([Medium5Kg], Times(2, Mediu
         +m3Kg), Times(4, Large)));                                      
      47 +                                                               
      48 +        Assert.Equal(                                          
      49 +            [new DiscountLineItem(Discount.MixedParcelMania, Co
         +ncat([Medium5Kg], Times(4, Large)), 12m)],                     
      50 +            DiscountsIn(quote));                               
      51 +        Assert.Equal(76m, quote.Total);                        
      52 +    }                                                          
      53 +                                                               
      54 +    [Fact]                                                     
      55 +    public void Equal_savings_prefer_small_parcel_mania_to_mixe
         +d()                                                            
      56 +    {                                                          
      57 +        var quote = QuoteFor(Times(5, Small));                 
      58 +                                                               
      59 +        Assert.Equal([new DiscountLineItem(Discount.SmallParcel
         +Mania, Times(4, Small), 3m)], DiscountsIn(quote));             
      60 +        Assert.Equal(12m, quote.Total);                        
      61 +    }                                                          
      62 +                                                               
      63 +    [Fact]                                                     
      64 +    public void Equal_savings_prefer_two_size_discounts_to_one_
         +mixed_discount()                                               
      65 +    {                                                          
      66 +        var smalls = Concat(Times(3, Small5Kg), [Small]);      
      67 +        var mediums = Concat(Times(2, Medium5Kg), [Medium3Kg]);
      68 +                                                               
      69 +        var quote = QuoteFor(Concat(smalls, mediums));         
      70 +                                                               
      71 +        Assert.Equal(                                          
      72 +            [                                                  
      73 +                new DiscountLineItem(Discount.SmallParcelMania,
         + smalls, 3m),                                                  
      74 +                new DiscountLineItem(Discount.MediumParcelMania
         +, mediums, 8m),                                                
      75 +            ],                                                 
      76 +            DiscountsIn(quote));                               
      77 +        Assert.Equal(57m, quote.Total);                        
      78 +    }                                                          
      79 +                                                               
      80 +    [Fact]                                                     
      81 +    public void A_large_order_is_quoted_without_trying_every_gr
         +ouping()                                                       
      82 +    {                                                          
      83 +        var quote = QuoteFor(Times(1_000, Small));             
      84 +                                                               
      85 +        var discounts = DiscountsIn(quote);                    
      86 +                                                               
      87 +        Assert.Equal(250, discounts.Count);                    
      88 +        Assert.All(discounts, discount => Assert.Equal(Discount
         +.SmallParcelMania, discount.Discount));                        
      89 +        Assert.Equal(2_250m, quote.Total);                     
      90 +    }                                                          
      91  }

  Ran 1 shell command (ctrl+o to expand)

● Bash(f=openspec/changes/multi-parcel-discounts/tasks.md; sed -i '29s/^- \[ \]
      2\.3/- [x] 2.3/' $f && sed -n '29p' $f | cut -c1-12)
  ⎿  - [x] 2.3 Ad
  ⎿  Updated openspec/changes/multi-parcel-discounts/tasks.md (+1 -1)
      26    - carts of four Large parcels and of four XL parcels have no 
          discount item
      27  
      28    Verify with `docker compose run --rm tests`.
      29 -- [ ] 2.3 Add tests in `DiscountCombinationTests` for the scena
         -rios under "Each parcel is in at most one discount", "The combi
         -nation that saves the most is applied" and "Equal savings prefe
         -r size discounts", implementing anything still missing:        
      29 +- [x] 2.3 Add tests in `DiscountCombinationTests` for the scena
         +rios under "Each parcel is in at most one discount", "The combi
         +nation that saves the most is applied" and "Equal savings prefe
         +r size discounts", implementing anything still missing:        
      30    - seven Small parcels at $3 give exactly one discount, Small 
          parcel mania saving $3, total $18
      31    - the same Small parcel added four times gives Small parcel m
          ania of that parcel four times, saving $3
      32    - a Medium parcel weighing 5 kg, two weighing 3 kg and four L
          arge parcels give only Mixed parcel mania of the 5 kg Medium an
          d the four Large parcels, saving $12, total $76

● Write(tests/CourierKata.Tests/DiscountPropertyTests.cs)
  ⎿  Wrote 150 lines to tests/CourierKata.Tests/DiscountPropertyTests.cs
       1 using static CourierKata.Tests.QuoteParcels;
       2
       3 namespace CourierKata.Tests;
       4
       5 public class DiscountPropertyTests
       6 {
       7     /// <summary>Parcels with overweight charges, Heavy-rate cos
         ts and equal costs across sizes.</summary>
       8     private static readonly Parcel[] Palette =
       9     [
     … +141 lines (ctrl+o to expand)

  Ran 1 shell command (ctrl+o to expand)

● Passes. Sanity-checking that the property test can actually catch a wrong
  answer by temporarily breaking the tie rule:
  Ran 1 shell command (ctrl+o to expand)

● The property test catches the mutation; file restored. Marking 2.4 and moving
  to 2.5 (listing tests).

● Bash(f=openspec/changes/multi-parcel-discounts/tasks.md; sed -i '38s/^- \[ \]
      2\.4/- [x] 2.4/' $f && sed -n '38p' $f | cut -c1-12; grep -c "MixedCount <
      current" src…)
  ⎿  - [x] 2.4 Ad
     1
  ⎿  Updated openspec/changes/multi-parcel-discounts/tasks.md (+1 -1)
      35    - 1,000 Small parcels at $3 give 250 Small parcel mania disco
          unts and a total of $2,250
      36  
      37    Verify with `docker compose run --rm tests`.
      38 -- [ ] 2.4 Add a property test in `DiscountCombinationTests` for
         - "No other grouping saves more". Write a test-only exhaustive s
         -earch over every way of grouping a cart's parcels. Quote a few 
         -hundred carts of 0 to 10 parcels from a seeded `Random`. Draw t
         -he parcels from a fixed set that includes overweight parcels, H
         -eavy-rate parcels and equal costs across sizes, for example Sma
         -ll at $3, $5, $11, $15 and $50 (Heavy), Medium at $8, $10, $12 
         -and $50 (Heavy), Large at $15 and $17, and XL at $25. For each 
         -cart, check that every discount is valid: it has the right numb
         -er of parcels that can join it, its saving is its cheapest parc
         -el's cost, and no parcel is used more often than it was added. 
         -Also check that the total saving and the number of Mixed discou
         -nts match the exhaustive search. Verify with `docker compose ru
         -n --rm tests`.                                                 
      38 +- [x] 2.4 Add a property test in `DiscountCombinationTests` for
         + "No other grouping saves more". Write a test-only exhaustive s
         +earch over every way of grouping a cart's parcels. Quote a few 
         +hundred carts of 0 to 10 parcels from a seeded `Random`. Draw t
         +he parcels from a fixed set that includes overweight parcels, H
         +eavy-rate parcels and equal costs across sizes, for example Sma
         +ll at $3, $5, $11, $15 and $50 (Heavy), Medium at $8, $10, $12 
         +and $50 (Heavy), Large at $15 and $17, and XL at $25. For each 
         +cart, check that every discount is valid: it has the right numb
         +er of parcels that can join it, its saving is its cheapest parc
         +el's cost, and no parcel is used more often than it was added. 
         +Also check that the total saving and the number of Mixed discou
         +nts match the exhaustive search. Verify with `docker compose ru
         +n --rm tests`.                                                 
      39  - [ ] 2.5 Add tests in `DiscountTests` for the scenarios under 
          "Each discount is listed as its own line item", "Discount line 
          items are ordered by their first parcel" and "Discounts don't c
          hange parcel line items", implementing anything still missing:
      40    - three Medium parcels at $8 give three parcel items, then a 
          Medium parcel mania item of the three parcels with a saving of 
          $8 and a cost of -$8, total $16
      41    - two XL, a Medium and two Large parcels, added in that order
          , give a Mixed parcel mania item listing XL, XL, Medium, Large 
          and Large in that order

● Update(tests/CourierKata.Tests/DiscountTests.cs)
  ⎿  Added 64 lines
       90          Assert.Equal([new DiscountLineItem(Discount.SmallParce
           lMania, parcels, 3m)], DiscountsIn(quote));
       91          Assert.Equal(56m, quote.Total);
       92      }
       93 +                                                              
       94 +    [Fact]                                                    
       95 +    public void Discount_is_listed_after_the_parcels_with_its_
          +parcels_and_saving()                                          
       96 +    {                                                         
       97 +        var parcels = Times(3, Medium3Kg);                    
       98 +                                                              
       99 +        var quote = QuoteFor(parcels);                        
      100 +                                                              
      101 +        Assert.Equal(                                         
      102 +            [.. PricedOnTheirOwn(parcels), new DiscountLineIte
          +m(Discount.MediumParcelMania, parcels, 8m)],                  
      103 +            quote.Items);                                     
      104 +        Assert.Equal(-8m, quote.Items[^1].Cost);              
      105 +        Assert.Equal(16m, quote.Total);                       
      106 +    }                                                         
      107 +                                                              
      108 +    [Fact]                                                    
      109 +    public void Parcels_in_a_discount_keep_the_order_they_were
          +_added()                                                      
      110 +    {                                                         
      111 +        Parcel[] parcels = [ExtraLarge, ExtraLarge, Medium3Kg,
          + Large, Large];                                               
      112 +                                                              
      113 +        var discount = Assert.Single(DiscountsIn(QuoteFor(parc
          +els)));                                                       
      114 +                                                              
      115 +        Assert.Equal(parcels, discount.Parcels);              
      116 +    }                                                         
      117 +                                                              
      118 +    [Fact]                                                    
      119 +    public void Discount_whose_first_parcel_was_added_first_co
          +mes_first()                                                   
      120 +    {                                                         
      121 +        var quote = QuoteFor(Concat([Medium3Kg], Times(4, Smal
          +l), Times(2, Medium3Kg)));                                    
      122 +                                                              
      123 +        Assert.Equal(                                         
      124 +            [                                                 
      125 +                new DiscountLineItem(Discount.MediumParcelMani
          +a, Times(3, Medium3Kg), 8m),                                  
      126 +                new DiscountLineItem(Discount.SmallParcelMania
          +, Times(4, Small), 3m),                                       
      127 +            ],                                                
      128 +            DiscountsIn(quote));                              
      129 +        Assert.Equal(25m, quote.Total);                       
      130 +    }                                                         
      131 +                                                              
      132 +    [Fact]                                                    
      133 +    public void Discount_order_does_not_follow_the_size_of_the
          +_saving()                                                     
      134 +    {                                                         
      135 +        var quote = QuoteFor(Concat(Times(3, Medium4Kg), Times
          +(3, Medium3Kg)));                                             
      136 +                                                              
      137 +        Assert.Equal([10m, 8m], DiscountsIn(quote).Select(disc
          +ount => discount.Saving));                                    
      138 +    }                                                         
      139 +                                                              
      140 +    [Fact]                                                    
      141 +    public void Parcels_in_a_discount_keep_their_cost()       
      142 +    {                                                         
      143 +        var quote = QuoteFor(Times(3, Medium3Kg));            
      144 +                                                              
      145 +        Assert.Equal(                                         
      146 +            Enumerable.Repeat(new ParcelLineItem(Medium3Kg, Pa
          +rcelSize.Medium, ParcelRate.BySize, 8m, 0m), 3),              
      147 +            quote.Items.OfType<ParcelLineItem>());            
      148 +    }                                                         
      149 +                                                              
      150 +    [Fact]                                                    
      151 +    public void Discount_parcels_cannot_be_modified_through_th
          +e_returned_list()                                             
      152 +    {                                                         
      153 +        var discount = Assert.Single(DiscountsIn(QuoteFor(Time
          +s(4, Small))));                                               
      154 +                                                              
      155 +        Assert.False(discount.Parcels is ICollection<Parcel> {
          + IsReadOnly: false });                                        
      156 +    }                                                         
      157  }

  Ran 1 shell command (ctrl+o to expand)

● Task 2.6: assumptions, then commit.

● Bash(cat >> docs/assumptions.md <<'EOF'…)
  ⎿  Passed!  - Failed:     0, Passed:   103, Skipped:     0, Total:   103,
      Duration: 112 ms - CourierKata.Tests.dll (net10.0)
  ⎿  Updated docs/assumptions.md (+65 -0)
      273  - **Assumed:** Yes. The speedy charge is the sum of the parcel
            costs, whatever
      274    rate each is charged at.
      275  - **Why:** It keeps "speedy doubles the order cost" true, and 
           needs no change.
      276 +                                                              
      277 +## Multi-parcel discounts                                     
      278 +                                                              
      279 +### "Every Nth parcel" means any group of N                   
      280 +                                                              
      281 +- **Unclear:** "Every 4th small parcel in an order is free" co
          +uld mean the 4th,                                             
      282 +  8th, ... parcel added, while "the cheapest parcel is the fre
          +e one" says                                                   
      283 +  otherwise.                                                  
      284 +- **Assumed:** A discount is any group of 4 Small, 3 Medium or
          + 5 parcels, and                                               
      285 +  the cheapest in the group is free. The order parcels were ad
          +ded in doesn't                                                
      286 +  matter.                                                     
      287 +- **Why:** The brief's example groups the three $8 parcels and
          + the three $10                                                
      288 +  parcels together, which only works if groups can be any parc
          +els.                                                          
      289 +                                                              
      290 +### Mixed parcel mania takes any 5 parcels                    
      291 +                                                              
      292 +- **Unclear:** Whether a "mixed" group needs parcels of differ
          +ent sizes.                                                    
      293 +- **Assumed:** Any 5 parcels qualify, including 5 of the same 
          +size.                                                         
      294 +- **Why:** The brief says "every 5th parcel" with no size cond
          +ition.                                                        
      295 +                                                              
      296 +### A discount saves the free parcel's whole cost             
      297 +                                                              
      298 +- **Unclear:** Whether the saving is the size's price or the p
          +arcel's cost.                                                 
      299 +- **Assumed:** It's the free parcel's cost, including any over
          +weight charge,                                                
      300 +  at whichever rate it is charged.                            
      301 +- **Why:** The brief's example saves $10 on Medium parcels, wh
          +ich cost $8 plus                                              
      302 +  a $2 overweight charge.                                     
      303 +                                                              
      304 +### Heavy-rate parcels join discounts by their size           
      305 +                                                              
      306 +- **Unclear:** Whether a parcel charged at the Heavy rate coun
          +ts as Small or                                                
      307 +  Medium for Small and Medium parcel mania, or only for Mixed.
      308 +- **Assumed:** It joins by its size. A 40 kg parcel measuring 
          +5 x 5 x 5 cm is                                               
      309 +  Small, so four of them save $50.                            
      310 +- **Why:** The user chose this. Size stays decided by dimensio
          +ns alone, as its                                              
      311 +  line item shows, and it never saves the customer less.      
      312 +                                                              
     … 28 more lines (ctrl+o to expand)

  Ran 1 shell command (ctrl+o to expand)

● The line numbers I used for 2.5/2.6 were off (the guarded sed didn't change
  anything). Marking them correctly and amending:

● Bash(f=openspec/changes/multi-parcel-discounts/tasks.md; sed -i -e '39s/^- \[
      \] 2\.5/- [x] 2.5/' -e '48s/^- \[ \] 2\.6/- [x] 2.6/' $f && grep -nE '^-
      \[.\] [0-9]' $…)
  ⎿  5:- [x] 1.1 Writ
     11:- [x] 1.2 Com
     15:- [x] 2.1 Wri
     … +9 lines (ctrl+o to expand)
  ⎿  Updated openspec/changes/multi-parcel-discounts/tasks.md (+2 -2)
      36  
      37    Verify with `docker compose run --rm tests`.
      38  - [x] 2.4 Add a property test in `DiscountCombinationTests` for
           "No other grouping saves more". Write a test-only exhaustive s
          earch over every way of grouping a cart's parcels. Quote a few 
          hundred carts of 0 to 10 parcels from a seeded `Random`. Draw t
          he parcels from a fixed set that includes overweight parcels, H
          eavy-rate parcels and equal costs across sizes, for example Sma
          ll at $3, $5, $11, $15 and $50 (Heavy), Medium at $8, $10, $12 
          and $50 (Heavy), Large at $15 and $17, and XL at $25. For each 
          cart, check that every discount is valid: it has the right numb
          er of parcels that can join it, its saving is its cheapest parc
          el's cost, and no parcel is used more often than it was added. 
          Also check that the total saving and the number of Mixed discou
          nts match the exhaustive search. Verify with `docker compose ru
          n --rm tests`.
      39 -- [ ] 2.5 Add tests in `DiscountTests` for the scenarios under 
         -"Each discount is listed as its own line item", "Discount line 
         -items are ordered by their first parcel" and "Discounts don't c
         -hange parcel line items", implementing anything still missing: 
      39 +- [x] 2.5 Add tests in `DiscountTests` for the scenarios under 
         +"Each discount is listed as its own line item", "Discount line 
         +items are ordered by their first parcel" and "Discounts don't c
         +hange parcel line items", implementing anything still missing: 
      40    - three Medium parcels at $8 give three parcel items, then a 
          Medium parcel mania item of the three parcels with a saving of 
          $8 and a cost of -$8, total $16
      41    - two XL, a Medium and two Large parcels, added in that order
          , give a Mixed parcel mania item listing XL, XL, Medium, Large 
          and Large in that order
      42    - a Medium parcel at $8, four Small parcels, then two more Me
          dium parcels at $8 put the Medium parcel mania item before the 
          Small parcel mania item, total $25
     ...
      45    - a discount's `Parcels` can't be modified through the return
          ed list
      46  
      47    Verify with `docker compose run --rm tests`.
      48 -- [ ] 2.6 Add these entries to `docs/assumptions.md`:          
      48 +- [x] 2.6 Add these entries to `docs/assumptions.md`:          
      49    - "every Nth parcel" means any group of N, whatever order the
           parcels were added in
      50    - Mixed parcel mania takes any 5 parcels, including 5 of the 
          same size
      51    - a discount saves the free parcel's whole cost, including an
          y overweight charge, at whichever rate it's charged

● Task 3.1: speedy after discounts, test first.

  Read 1 file (ctrl+o to expand)

● Update(tests/CourierKata.Tests/SpeedyShippingTests.cs)
  ⎿  Added 14 lines
      67          Assert.Equal(0m, quote.Total);
      68      }
      69  
      70 +    [Fact]                                                     
      71 +    public void Speedy_shipping_applies_after_discounts()      
      72 +    {                                                          
      73 +        var medium3Kg = new Parcel(20m, 5m, 5m, 3m);           
      74 +        var medium4Kg = new Parcel(20m, 5m, 5m, 4m);           
      75 +                                                               
      76 +        var quote = CartWith(Shipping.Speedy, medium3Kg, medium
         +3Kg, medium3Kg, medium4Kg, medium4Kg, medium4Kg).GetQuote();   
      77 +                                                               
      78 +        Assert.Equal(54m, quote.Items.OfType<ParcelLineItem>().
         +Sum(item => item.Cost));                                       
      79 +        Assert.Equal([8m, 10m], quote.Items.OfType<DiscountLine
         +Item>().Select(item => item.Saving));                          
      80 +        Assert.Equal(new SpeedyShippingLineItem(36m), quote.Ite
         +ms[^1]);                                                       
      81 +        Assert.Equal(72m, quote.Total);                        
      82 +    }                                                          
      83 +                                                               
      84      private static Cart CartWith(Shipping shipping, params Parc
          el[] parcels)
      85      {
      86          var cart = new Cart { Shipping = shipping };

  Ran 1 shell command (ctrl+o to expand)

● Update(src/CourierKata/Cart.cs)
  ⎿  Added 2 lines, removed 1 line
      40  
      41          if (Shipping == Shipping.Speedy)
      42          {
      43 -            items.Add(new SpeedyShippingLineItem(parcelItems.Su
         -m(item => item.Cost)));                                        
      43 +            // Speedy applies after discounts: it doubles the p
         +arcel costs less the savings.                                  
      44 +            items.Add(new SpeedyShippingLineItem(items.Sum(item
         + => item.Cost)));                                              
      45          }
      46  
      47          return new Quote(items.AsReadOnly(), items.Sum(item => 
          item.Cost));

● Update(tests/CourierKata.Tests/SpeedyShippingTests.cs)
  ⎿  Added 32 lines
       81          Assert.Equal(72m, quote.Total);
       82      }
       83  
       84 +    [Fact]                                                    
       85 +    public void Discount_items_come_between_the_parcel_items_a
          +nd_speedy_shipping()                                          
       86 +    {                                                         
       87 +        var medium = new Parcel(20m, 5m, 5m, 3m);             
       88 +        var item = new ParcelLineItem(medium, ParcelSize.Mediu
          +m, ParcelRate.BySize, 8m, 0m);                                
       89 +                                                              
       90 +        var quote = CartWith(Shipping.Speedy, medium, medium, 
          +medium).GetQuote();                                           
       91 +                                                              
       92 +        Assert.Equal(                                         
       93 +            [                                                 
       94 +                item,                                         
       95 +                item,                                         
       96 +                item,                                         
       97 +                new DiscountLineItem(Discount.MediumParcelMani
          +a, [medium, medium, medium], 8m),                             
       98 +                new SpeedyShippingLineItem(16m),              
       99 +            ],                                                
      100 +            quote.Items);                                     
      101 +        Assert.Equal(32m, quote.Total);                       
      102 +    }                                                         
      103 +                                                              
      104 +    [Fact]                                                    
      105 +    public void Discounts_are_unaffected_by_speedy_shipping() 
      106 +    {                                                         
      107 +        var medium = new Parcel(20m, 5m, 5m, 3m);             
      108 +                                                              
      109 +        var standard = CartWith(Shipping.Standard, medium, med
          +ium, medium).GetQuote();                                      
      110 +        var speedy = CartWith(Shipping.Speedy, medium, medium,
          + medium).GetQuote();                                          
      111 +                                                              
      112 +        Assert.Single(standard.Items.OfType<DiscountLineItem>(
          +));                                                           
      113 +        Assert.Equal(standard.Items.OfType<DiscountLineItem>()
          +, speedy.Items.OfType<DiscountLineItem>());                   
      114 +    }                                                         
      115 +                                                              
      116      private static Cart CartWith(Shipping shipping, params Par
           cel[] parcels)
      117      {
      118          var cart = new Cart { Shipping = shipping };

  Ran 1 shell command (ctrl+o to expand)

● 3.1 and 3.2 pass with existing speedy tests unchanged. Task 3.3: rewriting the
  speedy assumption.

  Read 1 file (ctrl+o to expand)

● Update(docs/assumptions.md)
  ⎿  Added 9 lines, removed 7 lines
      109  
      110  ## Speedy shipping
      111  
      112 -### Speedy charge equals the parcel subtotal                  
      112 +### Speedy charge equals the discounted parcel subtotal       
      113  
      114  - **Unclear:** "Speedy shipping doubles the cost of the entire
            order" while
      115    also being "listed as a separate item" and not changing parc
           el prices.
      116  - **Assumed:** The speedy shipping line item costs the sum of 
           the parcel
      117 -  line-item costs. The total is still the sum of every line it
          -em, so it                                                     
      118 -  doubles.                                                    
      117 +  line-item costs less any discount savings, which is the sum 
          +of every line                                                 
      118 +  item before it. The total is still the sum of every line ite
          +m, so the                                                     
      119 +  discounted order doubles.                                   
      120  - **Why:** This is the only reading that doubles the total, ke
           eps parcel costs
      121    unchanged and shows speedy as its own item. It matches the b
           rief's example
      121 -  ($51 of parcels, $51 speedy, $102 total). Only parcel costs 
          -are doubled for                                               
      122 -  now; how speedy combines with later rules such as discounts 
          -is left to the                                                
      123 -  change that adds them.                                      
      122 +  ($51 of parcels, $51 speedy, $102 total). The discounts brie
          +f says "speedy                                                
      123 +  shipping applies after discounts are taken into account", so
          + savings come                                                 
      124 +  off before speedy doubles the order.                        
      125  
      126  ### Speedy item comes last
      127  
      128  - **Unclear:** Where the speedy item appears among the line it
           ems.
      128 -- **Assumed:** It comes after all the parcel line items, and a
          -ppears once.                                                  
      129 +- **Assumed:** It comes after all the parcel and discount line
          + items, and                                                   
      130 +  appears once.                                               
      131  - **Why:** Its cost depends on all the parcels, and the brief'
           s example lists
      132    it after them.
      133  

● Bash(docker compose run --rm tests 2>&1 | grep -E
      "error|warning|Passed!|Failed!" | head -3 &&
      f=openspec/changes/multi-parcel-discounts/tasks.md && sed -i -e '61s/^…)
  ⎿  Passed!  - Failed:     0, Passed:   106, Skipped:     0, Total:   106,
      Duration: 125 ms - CourierKata.Tests.dll (net10.0)
     61:- [x] 3.1 W
     … +3 lines (ctrl+o to expand)
  ⎿  Updated openspec/changes/multi-parcel-discounts/tasks.md (+3 -3)
      58  
      59  ## 3. Speedy shipping after discounts
      60  
      61 -- [ ] 3.1 Write a failing test in `SpeedyShippingTests` for "Sp
         -eedy shipping applies after discounts": three Medium parcels we
         -ighing 3 kg and three weighing 4 kg with Speedy give parcel ite
         -ms totalling $54, discounts saving $8 and $10, a speedy item of
         - $36 and a total of $72. Change `Cart.GetQuote()` so the speedy
         - item costs the sum of every item before it. Verify with `docke
         -r compose run --rm tests`.                                     
      62 -- [ ] 3.2 Add tests for these scenarios, implementing anything 
         -still missing:                                                 
      61 +- [x] 3.1 Write a failing test in `SpeedyShippingTests` for "Sp
         +eedy shipping applies after discounts": three Medium parcels we
         +ighing 3 kg and three weighing 4 kg with Speedy give parcel ite
         +ms totalling $54, discounts saving $8 and $10, a speedy item of
         + $36 and a total of $72. Change `Cart.GetQuote()` so the speedy
         + item costs the sum of every item before it. Verify with `docke
         +r compose run --rm tests`.                                     
      62 +- [x] 3.2 Add tests for these scenarios, implementing anything 
         +still missing:                                                 
      63    - "Discount items come between the parcel items and speedy sh
          ipping": three Medium parcels at $8 with Speedy give Medium $8,
           Medium $8, Medium $8, Medium parcel mania -$8, then speedy shi
          pping $16, in that order
      64    - "Discounts are unaffected by speedy shipping": three Medium
           parcels at $8 quoted with Standard and with Speedy have equal 
          discount items
      65  
      66    Check that the existing speedy shipping tests pass unchanged.
           Verify with `docker compose run --rm tests`.
      67 -- [ ] 3.3 Rewrite "Speedy charge equals the parcel subtotal" in
         - `docs/assumptions.md`: speedy now costs the parcel subtotal le
         -ss any discount savings, which is what "speedy shipping applies
         - after discounts" asks for. Verify that `docker compose run --r
         -m tests` passes, then commit the entry with the code and tests 
         -from 3.1–3.2, and verify the working tree is clean afterwards. 
      67 +- [x] 3.3 Rewrite "Speedy charge equals the parcel subtotal" in
         + `docs/assumptions.md`: speedy now costs the parcel subtotal le
         +ss any discount savings, which is what "speedy shipping applies
         + after discounts" asks for. Verify that `docker compose run --r
         +m tests` passes, then commit the entry with the code and tests 
         +from 3.1–3.2, and verify the working tree is clean afterwards. 
      68  
      69  ## 4. Final checks
      70  

● Task 4.1: checking the public surface and scenario coverage.

  Ran 1 shell command (ctrl+o to expand)

● Only Discount and DiscountLineItem are new public types, and every scenario in
  both spec deltas has a matching test. Marking 4.1 and committing.

● Bash(f=openspec/changes/multi-parcel-discounts/tasks.md; sed -i '71s/^- \[ \]
      4\.1/- [x] 4.1/' $f && git add $f && git commit -q -m "Mark
      multi-parcel-discounts fina…)
  ⎿  all_done {'total': 12, 'complete': 12, 'remaining': 0}
     79e2fda Mark multi-parcel-discounts final checks complete
     dc8e696 Apply speedy shipping after discounts
     … +3 lines (ctrl+o to expand)
  ⎿  Updated openspec/changes/multi-parcel-discounts/tasks.md (+1 -1)
      68  
      69  ## 4. Final checks
      70  
      71 -- [ ] 4.1 Check the public surface: it should be unchanged apar
         -t from the new `Discount` enum and `DiscountLineItem`. Check th
         -at every scenario in `specs/parcel-discounts/spec.md` and `spec
         -s/parcel-pricing/spec.md` has a matching test. Verify that `doc
         -ker compose run --rm tests` passes with zero warnings.         
      71 +- [x] 4.1 Check the public surface: it should be unchanged apar
         +t from the new `Discount` enum and `DiscountLineItem`. Check th
         +at every scenario in `specs/parcel-discounts/spec.md` and `spec
         +s/parcel-pricing/spec.md` has a matching test. Verify that `doc
         +ker compose run --rm tests` passes with zero warnings.         
      72  
      73  ## Workflow follow-up
      74  

● Implementation complete

  All 12 tasks in the multi-parcel-discounts change are done and committed on
  the multi-parcel-discounts branch. All 106 tests pass with no warnings (the
  project treats warnings as errors).

  Commits
  - f643b8e Propose multi-parcel discounts change
  - 5142a0c Add discount line item: the new Discount enum and DiscountLineItem.
    Two discount items are equal when their parcels match in order.
  - edf7c26 Apply the multi-parcel discounts that save the most: the new
    internal DiscountSelection, the call to it in Cart.GetQuote(), tests for
    every discount scenario in the spec, and seven new entries in the
    assumptions log.
  - dc8e696 Apply speedy shipping after discounts: speedy now costs the sum of
    every line item before it. The existing speedy tests pass unchanged, and the
    speedy entry in the assumptions log is rewritten.
  - 79e2fda Mark multi-parcel-discounts final checks complete

  Checks
  - Your example gives Medium parcel mania −$8 for the three 3 kg parcels, then
    −$10 for the three 4 kg parcels, and a total of $36. With Speedy, the speedy
    item is $36 and the total is $72.
  - One test quotes 300 random carts of up to 10 parcels and compares each
    result against a search that tries every possible grouping. Breaking the tie
    rule on purpose made it fail, along with three other tests, so it does
    catch wrong answers.
  - A cart of 1,000 Small parcels gets its 250 discounts quickly.
  - The only new public types are Discount and DiscountLineItem.

  Differences from the plan
  - The test that compares against every possible grouping is in its own
    DiscountPropertyTests class, not in DiscountCombinationTests as tasks.md
    said, so the brute-force search stays separate from the scenario tests.
  - I added a shared test helper, QuoteParcels, with parcels at known prices,
    which the discount tests use.

  Before archiving, you might want to run /opsx:verify. Then /opsx:archive
  copies both specs into openspec/specs/; after that, the purpose line in
  parcel-pricing still needs to mention discounts, as listed in tasks.md.

❯ verify, archive

● Skill(opsx:verify)
  ⎿  Successfully loaded skill · 1 tool allowed

● Using change: multi-parcel-discounts (override with /opsx:verify <other>).
  Ran 1 shell command (ctrl+o to expand)

● The verification passed with no critical issues or warnings. The only finding
  is that design.md places the all-groupings test in the wrong file, and I'll
  fix that next. · summary

● Update(openspec/changes/multi-parcel-discounts/design.md)
  ⎿  Added 1 line, removed 1 line
      107  - Each spec scenario gets a test, with expected discount items
            written out in full, for example `new DiscountLineItem(Discou
           nt.MediumParcelMania, [medium3Kg, medium3Kg, medium3Kg], 8m)`.
      108  - A property test checks "every time". It quotes a few hundred
            seeded random carts of up to 10 parcels, drawn from Small, Me
           dium, Large and XL parcels at various weights, including overw
           eight and Heavy-rate ones. For each cart, the discounts must b
           e valid: each group has the right number of parcels that can j
           oin it, its saving is its cheapest parcel's cost, and no parce
           l is used more often than it was added. Their total saving and
            number of Mixed discounts must also match a test-only search 
           over every grouping.
      109  - A cart of 1,000 Small parcels gets 250 Small parcel mania di
           scounts, which shows the search doesn't blow up.
      110 -- Test classes: `DiscountLineItemTests` for the record, `Disco
          -untTests` for the discount rules and how they're listed, `Disc
          -ountCombinationTests` for choosing the best combination, and n
          -ew cases in `SpeedyShippingTests`.                            
      110 +- Test classes: `DiscountLineItemTests` for the record, `Disco
          +untTests` for the discount rules and how they're listed, `Disc
          +ountCombinationTests` for choosing the best combination, `Disc
          +ountPropertyTests` for the check against every grouping, and n
          +ew cases in `SpeedyShippingTests`.                            
      111  
      112  ### Assumptions log
      113  New entries in `docs/assumptions.md`:

● Skill(opsx:archive)
  ⎿  Successfully loaded skill · 1 tool allowed

● Using change: multi-parcel-discounts (override with /opsx:archive <other>).

  Ran 1 shell command (ctrl+o to expand)

● All 4 artifacts and 12 tasks are complete. I'm syncing parcel-discounts (8 new
  requirements) and parcel-pricing (2 modified requirements), since earlier
  changes were synced and you asked to archive. · summary

● Bash(python3 -I - <<'EOF'
      import re, pathlib…)
  ⎿   openspec/changes/multi-parcel-discounts/design.md |  2 +-
      openspec/specs/parcel-pricing/spec.md             | 16 ++++++++++++++
     --
     … +6 lines (ctrl+o to expand)
  ⎿  Updated openspec/specs/parcel-pricing/spec.md (+14 -2)
      147  - **THEN** the quote contains two Small line items
      148  
      149  ### Requirement: Quote lists each parcel with its size and cos
           t
      150 -A quote SHALL contain one parcel line item per parcel in the c
          -art, in the order the parcels were added. Each parcel line ite
          -m MUST show the parcel, its size, the rate it is charged at, i
          -ts base cost, its overweight charge and its cost. When speedy 
          -shipping is selected, its line item MUST come after all the pa
          -rcel line items.                                              
      150 +A quote SHALL contain one parcel line item per parcel in the c
          +art, in the order the parcels were added. Each parcel line ite
          +m MUST show the parcel, its size, the rate it is charged at, i
          +ts base cost, its overweight charge and its cost. Any discount
          + line items MUST come after all the parcel line items. When sp
          +eedy shipping is selected, its line item MUST come after all t
          +he parcel and discount line items.                            
      151  
      152  #### Scenario: Line items follow insertion order
      153  - **WHEN** an XL, a Small and a Medium parcel are added in tha
           t order
     ...
      157  - **WHEN** a Medium and a Small parcel are added in that order
            with Speedy shipping
      158  - **THEN** the quote's line items are Medium $8, Small $3, spe
           edy shipping $11 in that order
      159  
      160 +#### Scenario: Discount items come between the parcel items an
          +d speedy shipping                                             
      161 +- **WHEN** three Medium parcels costing $8 each are added with
          + Speedy shipping                                              
      162 +- **THEN** the quote's line items are Medium $8, Medium $8, Me
          +dium $8, Medium parcel mania -$8, speedy shipping $16 in that 
          +order                                                         
      163 +                                                              
      164  #### Scenario: Overweight charge shown separately
      165  - **WHEN** a Small parcel weighing 1.2 kg is quoted
      166  - **THEN** its line item shows the size rate, a base cost of $
           3, an overweight charge of $2 and a cost of $5
     ...
      219  - **THEN** the quote's speedy shipping line item costs $28 and
            the total is $56
      220  
      221  ### Requirement: Speedy shipping doubles the order cost
      218 -When the shipping option is Speedy, the quote SHALL include ex
          -actly one speedy shipping line item that costs the sum of the 
          -parcel line-item costs, including any overweight charges. Each
          - parcel line item MUST cost the same as it would with Standard
          - shipping. When the shipping option is Standard, the quote MUS
          -T NOT include a speedy shipping line item.                    
      222 +When the shipping option is Speedy, the quote SHALL include ex
          +actly one speedy shipping line item that costs the sum of the 
          +parcel line-item costs, including any overweight charges, less
          + the savings of any discounts. Each parcel line item and each 
          +discount line item MUST be the same as it would be with Standa
          +rd shipping. When the shipping option is Standard, the quote M
          +UST NOT include a speedy shipping line item.                  
      223  
      224  #### Scenario: Speedy shipping on one parcel of each size
      225  - **WHEN** a Small, a Medium, a Large and an XL parcel are add
           ed, Speedy is selected, and a quote is requested
     ...
      233  - **WHEN** a parcel measuring 5 x 5 x 5 cm and weighing 40 kg 
           is added, Speedy is selected, and a quote is requested
      234  - **THEN** the parcel line item costs $50, the speedy shipping
            line item costs $50, and the total is $100
      235  
      236 +#### Scenario: Speedy shipping applies after discounts        
      237 +- **WHEN** three Medium parcels weighing 3 kg ($8 each) and th
          +ree Medium parcels weighing 4 kg ($10 each) are added, Speedy 
          +is selected, and a quote is requested                         
      238 +- **THEN** the parcel line items cost $54 in total, the discou
          +nts save $8 and $10, the speedy shipping line item costs $36, 
          +and the total is $72                                          
      239 +                                                              
      240  #### Scenario: Standard shipping has no speedy item
      241  - **WHEN** a Small parcel is added with Standard shipping and 
           a quote is requested
      242  - **THEN** the quote has only the parcel line item and its tot
           al is $3
     ...
      245  - **WHEN** the same parcels are quoted once with Standard and 
           once with Speedy shipping
      246  - **THEN** the parcel line items in both quotes are equal
      247  
      248 +#### Scenario: Discounts are unaffected by speedy shipping    
     … 6 more lines (ctrl+o to expand)
  ⎿  Created openspec/specs/parcel-discounts/spec.md (+122 -0)
        1 +# parcel-discounts Specification                              
        2 +                                                              
        3 +## Purpose                                                    
        4 +                                                              
        5 +Rewards customers who send several parcels in one order by mak
          +ing some of those parcels free. Covers which groups of parcels
          + earn a discount, which parcel in a group is free, choosing th
          +e combination of discounts that saves the most, and how discou
          +nts appear on the quote.                                      
        6 +                                                              
        7 +## Requirements                                               
        8 +                                                              
        9 +### Requirement: Each discount needs a full group of parcels  
       10 +Small parcel mania SHALL need a group of 4 Small parcels, Medi
          +um parcel mania a group of 3 Medium parcels, and Mixed parcel 
          +mania a group of 5 parcels of any sizes, including 5 of the sa
          +me size. A quote MUST NOT apply a discount to fewer parcels th
          +an that discount needs.                                       
       11 +                                                              
       12 +#### Scenario: Four Small parcels                             
       13 +- **WHEN** four Small parcels costing $3 each are quoted      
       14 +- **THEN** the quote has one Small parcel mania discount of th
          +e four parcels, saving $3, and the total is $9                
       15 +                                                              
       16 +#### Scenario: Three Medium parcels                           
       17 +- **WHEN** three Medium parcels weighing 3 kg, costing $8 each
          +, are quoted                                                  
       18 +- **THEN** the quote has one Medium parcel mania discount of t
          +he three parcels, saving $8, and the total is $16             
       19 +                                                              
       20 +#### Scenario: Five parcels of the same size                  
       21 +- **WHEN** five Large parcels costing $15 each are quoted     
       22 +- **THEN** the quote has one Mixed parcel mania discount of th
          +e five parcels, saving $15, and the total is $60              
       23 +                                                              
       24 +#### Scenario: Too few parcels for any discount               
       25 +- **WHEN** a cart of three Small parcels, a cart of two Medium
          + parcels and a cart with one parcel of each size are each quot
          +ed                                                            
       26 +- **THEN** none of the quotes has a discount line item        
       27 +                                                              
       28 +### Requirement: The cheapest parcel in a group is free       
       29 +Each discount SHALL make the cheapest parcel in its group free
          +, saving that parcel's whole cost, including any overweight ch
          +arge, at whichever rate it is charged. Which parcel is free MU
          +ST NOT depend on the order the parcels were added in.         
       30 +                                                              
       31 +#### Scenario: Cheapest parcel is free, not the last one added
       32 +- **WHEN** two XL parcels, a Medium parcel and two Large parce
          +ls, each within its weight limit, are added in that order     
       33 +- **THEN** the quote has one Mixed parcel mania discount of th
          +e five parcels, saving $8, and the total is $80               
       34 +                                                              
       35 +#### Scenario: Saving includes the overweight charge          
       36 +- **WHEN** three Medium parcels weighing 4 kg, costing $10 eac
          +h, are quoted                                                 
       37 +- **THEN** the quote has one Medium parcel mania discount savi
          +ng $10, and the total is $20                                  
       38 +                                                              
       39 +### Requirement: A parcel joins discounts by its size         
       40 +A parcel SHALL be able to join Small parcel mania only when it
          +s size is Small, Medium parcel mania only when its size is Med
          +ium, and Mixed parcel mania whatever its size. Size comes from
          + the parcel's dimensions alone, so a parcel charged at the Hea
          +vy rate joins discounts by its size.                          
     … 82 more lines (ctrl+o to expand)

  Ran 1 shell command (ctrl+o to expand)

● The sync checks out, with 9 requirements and strict validation passing. I'm
  updating the parcel-pricing purpose, then archiving and committing. · summary

● Bash(sed -i 's/producing a quote with per-parcel costs, any charge for the
      chosen shipping option, and a total\./producing a quote with per-parcel
      costs, any discoun…)
  ⎿  Prices a cart of parcels for delivery by classifying each parcel by it
     s size and producing a quote with per-parcel costs, any discounts, any
      charge for the chosen shipping option, and a total.
     … +6 lines (ctrl+o to expand)
  ⎿  Updated openspec/specs/parcel-pricing/spec.md (+1 -1)
      2  
      3  ## Purpose
      4  
      5 -Prices a cart of parcels for delivery by classifying each parcel
        - by its size and producing a quote with per-parcel costs, any ch
        -arge for the chosen shipping option, and a total.               
      5 +Prices a cart of parcels for delivery by classifying each parcel
        + by its size and producing a quote with per-parcel costs, any di
        +scounts, any charge for the chosen shipping option, and a total.
      6  
      7  ## Requirements
      8  
  ⎿  Created
     openspec/changes/archive/2026-11-11-multi-parcel-discounts/proposal.md
     (+37 -0)
       1 +# Proposal                                                     
       2 +                                                               
       3 +## Why                                                         
       4 +                                                               
       5 +To reward customers who send several parcels in one order, the 
         +courier is introducing three discounts that each make one parce
         +l in a group free. A quote has to find the combination of disco
         +unts that saves the most, show each discount on its own line, a
         +nd apply speedy shipping to the discounted order.              
       6 +                                                               
       7 +## What Changes                                                
       8 +                                                               
       9 +- Three discounts, each making the cheapest parcel in a group f
         +ree:                                                           
      10 +  - Small parcel mania: a group of 4 Small parcels.            
      11 +  - Medium parcel mania: a group of 3 Medium parcels.          
      12 +  - Mixed parcel mania: a group of any 5 parcels, whatever thei
         +r sizes.                                                       
      13 +- A discount saves the whole cost of its cheapest parcel, inclu
         +ding any overweight charge. "Every 4th parcel" means any group 
         +of 4, so the order parcels were added in doesn't matter.       
      14 +- A parcel joins discounts by its size, whatever rate it is cha
         +rged at. A 40 kg parcel measuring 5 x 5 x 5 cm is Small, so it 
         +can be part of Small parcel mania.                             
      15 +- Each parcel is in at most one discount. The quote applies the
         + combination of discounts that saves the most in total. When se
         +veral combinations save the same, it applies one with the fewes
         +t Mixed parcel mania discounts.                                
      16 +- Each discount applied is its own line item showing the discou
         +nt, the parcels in its group and its saving. Its cost is minus 
         +the saving (for example -$8), so the total is still the sum of 
         +the line items. Discount items come after the parcel items and 
         +before speedy shipping, ordered by when each discount's first p
         +arcel was added.                                               
      17 +- Parcel line items don't change when discounts apply.         
      18 +- Speedy shipping applies after discounts: the speedy item cost
         +s the parcel costs less the discount savings, so speedy still d
         +oubles the order.                                              
      19 +- **BREAKING**: `LineItem` gets a third kind, `DiscountLineItem
         +`. Callers that match on every kind of line item must handle it
         +.                                                              
      20 +- Record the new interpretations in `docs/assumptions.md`, and 
         +rewrite "Speedy charge equals the parcel subtotal", which left 
         +how speedy combines with discounts to this change.             
      21 +                                                               
      22 +Out of scope: other discounts, discount codes, and any change t
         +o how parcels are sized or priced.                             
      23 +                                                               
      24 +## Capabilities                                                
      25 +                                                               
      26 +### New Capabilities                                           
      27 +- `parcel-discounts`: discounts for sending several parcels in 
         +one order. Covers which groups of parcels qualify, which parcel
         + is free, choosing the combination that saves the most, and how
         + each discount appears on the quote.                           
      28 +                                                               
      29 +### Modified Capabilities                                      
      30 +- `parcel-pricing`: discount line items come between the parcel
         + items and speedy shipping, and the speedy charge becomes the p
         +arcel costs less any discount savings.                         
      31 +                                                               
      32 +## Impact                                                      
      33 +                                                               
      34 +- Public API: new enum `Discount { SmallParcelMania, MediumParc
         +elMania, MixedParcelMania }` and new `DiscountLineItem(Discount
         + Discount, IReadOnlyList<Parcel> Parcels, decimal Saving) : Lin
         +eItem`, whose `Cost` is `-Saving`. `Cart`, `Quote`, `ParcelLine
         +Item` and `SpeedyShippingLineItem` keep their shapes.          
      35 +- Internal: a new internal class chooses the discounts from the
         + priced parcel line items. `Cart.GetQuote()` adds the discount 
         +items and works out speedy from the discounted subtotal. `Parce
         +lPricing` doesn't change.                                      
      36 +- Tests: no existing test has enough parcels to earn a discount
         +, so none change. New tests cover each discount, choosing the b
         +est combination (including a check against an exhaustive search
         + on small random carts), the discount line item, and speedy aft
         +er discounts.                                                  
      37 +- Docs: new and rewritten entries in `docs/assumptions.md`.    
  ⎿  Created openspec/changes/archive/2026-11-11-multi-parcel-discounts/.op
     enspec.yaml (+2 -0)
      1 +schema: spec-driven                                             
      2 +created: 2026-11-11                                             
  ⎿  Created
     openspec/changes/archive/2026-11-11-multi-parcel-discounts/tasks.md
     (+77 -0)
       1 +# Tasks                                                        
       2 +                                                               
       3 +## 1. Discount line item (no behaviour change)                 
       4 +                                                               
       5 +- [x] 1.1 Write failing tests in `DiscountLineItemTests`:      
       6 +  - `Cost` is minus `Saving` (a saving of $8 costs -$8)        
       7 +  - two items with the same discount, equal parcels in the same
         + order and the same saving are equal and have equal hash codes 
       8 +  - items that differ in discount, parcels, parcel order or sav
         +ing are not equal                                              
       9 +                                                               
      10 +  Add the public enum `Discount { SmallParcelMania, MediumParce
         +lMania, MixedParcelMania }` and `sealed record DiscountLineItem
         +(Discount Discount, IReadOnlyList<Parcel> Parcels, decimal Savi
         +ng) : LineItem`, with XML doc comments, `Cost => -Saving`, `Sea
         +lHierarchy()`, and `Equals`/`GetHashCode` that compare `Parcels
         +` item by item (see design.md). Update `LineItem`'s doc comment
         + to list all three kinds. Verify that `docker compose run --rm 
         +tests` passes with zero warnings.                              
      11 +- [x] 1.2 Commit the enum, the record and their tests, and veri
         +fy the working tree is clean afterwards.                       
      12 +                                                               
      13 +## 2. Choosing discounts (test first, starting with the brief's
         + example)                                                      
      14 +                                                               
      15 +- [x] 2.1 Write a failing test in `DiscountCombinationTests` fo
         +r "Parcels are grouped with others of similar cost": three Medi
         +um parcels weighing 3 kg, then three weighing 4 kg, give the si
         +x unchanged parcel items, then a Medium parcel mania item of th
         +e three 3 kg parcels saving $8, then one of the three 4 kg parc
         +els saving $10, and a total of $36. Add the internal static `Di
         +scountSelection.Choose` as described in design.md: parcels sort
         +ed most expensive first, the 60-state walk, more saving and the
         +n fewer Mixed discounts winning, groups rebuilt with their parc
         +els in cart order in a read-only list, and items ordered by eac
         +h group's first parcel. Call it from `Cart.GetQuote()` so the d
         +iscount items come after the parcel items, leaving the speedy c
         +harge as it is for now. Verify with `docker compose run --rm te
         +sts`.                                                          
      16 +- [x] 2.2 Add tests in `DiscountTests` for the scenarios under 
         +"Each discount needs a full group of parcels", "The cheapest pa
         +rcel in a group is free" and "A parcel joins discounts by its s
         +ize", implementing anything still missing:                     
      17 +  - four Small parcels at $3 give Small parcel mania of all fou
         +r, saving $3, total $9                                         
      18 +  - three Medium parcels weighing 3 kg give Medium parcel mania
         + saving $8, total $16                                          
      19 +  - five Large parcels give Mixed parcel mania saving $15, tota
         +l $60                                                          
      20 +  - carts of three Small parcels, of two Medium parcels, and of
         + one parcel of each size have no discount item                 
      21 +  - two XL, a Medium and two Large parcels, added in that order
         +, give Mixed parcel mania of all five, saving $8, total $80    
      22 +  - three Medium parcels weighing 4 kg give Medium parcel mania
         + saving $10, total $20                                         
      23 +  - four 5 x 5 x 5 cm parcels weighing 40 kg are each Small at 
         +the Heavy rate, and give Small parcel mania saving $50, total $
         +150                                                            
      24 +  - three Small parcels at $3 and one 5 x 5 x 5 cm parcel weigh
         +ing 40 kg give Small parcel mania of all four, saving $3, total
         + $56                                                           
      25 +  - three Small parcels and a Medium parcel have no discount it
         +em                                                             
      26 +  - carts of four Large parcels and of four XL parcels have no 
         +discount item                                                  
      27 +                                                               
      28 +  Verify with `docker compose run --rm tests`.                 
      29 +- [x] 2.3 Add tests in `DiscountCombinationTests` for the scena
         +rios under "Each parcel is in at most one discount", "The combi
         +nation that saves the most is applied" and "Equal savings prefe
         +r size discounts", implementing anything still missing:        
      30 +  - seven Small parcels at $3 give exactly one discount, Small 
         +parcel mania saving $3, total $18                              
      31 +  - the same Small parcel added four times gives Small parcel m
         +ania of that parcel four times, saving $3                      
      32 +  - a Medium parcel weighing 5 kg, two weighing 3 kg and four L
         +arge parcels give only Mixed parcel mania of the 5 kg Medium an
         +d the four Large parcels, saving $12, total $76                
      33 +  - five Small parcels at $3 give Small parcel mania of four of
         + them, no Mixed discount, total $12                            
      34 +  - three Small parcels weighing 5 kg, a Small parcel at $3, tw
         +o Medium parcels weighing 5 kg and a Medium parcel weighing 3 k
         +g give Small parcel mania saving $3 and Medium parcel mania sav
         +ing $8, no Mixed discount, total $57                           
      35 +  - 1,000 Small parcels at $3 give 250 Small parcel mania disco
         +unts and a total of $2,250                                     
      36 +                                                               
      37 +  Verify with `docker compose run --rm tests`.                 
      38 +- [x] 2.4 Add a property test in `DiscountCombinationTests` for
         + "No other grouping saves more". Write a test-only exhaustive s
         +earch over every way of grouping a cart's parcels. Quote a few 
         +hundred carts of 0 to 10 parcels from a seeded `Random`. Draw t
         +he parcels from a fixed set that includes overweight parcels, H
         +eavy-rate parcels and equal costs across sizes, for example Sma
         +ll at $3, $5, $11, $15 and $50 (Heavy), Medium at $8, $10, $12 
         +and $50 (Heavy), Large at $15 and $17, and XL at $25. For each 
         +cart, check that every discount is valid: it has the right numb
         +er of parcels that can join it, its saving is its cheapest parc
         +el's cost, and no parcel is used more often than it was added. 
         +Also check that the total saving and the number of Mixed discou
         +nts match the exhaustive search. Verify with `docker compose ru
         +n --rm tests`.                                                 
      39 +- [x] 2.5 Add tests in `DiscountTests` for the scenarios under 
         +"Each discount is listed as its own line item", "Discount line 
         +items are ordered by their first parcel" and "Discounts don't c
         +hange parcel line items", implementing anything still missing: 
      40 +  - three Medium parcels at $8 give three parcel items, then a 
         +Medium parcel mania item of the three parcels with a saving of 
         +$8 and a cost of -$8, total $16                                
     … 37 more lines (ctrl+o to expand)
  ⎿  Created
     openspec/changes/archive/2026-11-11-multi-parcel-discounts/design.md
     (+131 -0)
        1 +# Design                                                      
        2 +                                                              
        3 +## Context                                                    
        4 +                                                              
        5 +See proposal.md for the motivation and `specs/parcel-discounts
          +/spec.md` for the rules. Today `Cart.GetQuote()` prices each p
          +arcel into a `ParcelLineItem` with the internal `ParcelPricing
          +.Price`. When Speedy is selected, it appends a `SpeedyShipping
          +LineItem` costing the sum of the parcel costs, then totals the
          + items. `LineItem` is a closed hierarchy of two kinds, kept cl
          +osed by an `internal abstract` member. A `ParcelLineItem` carr
          +ies both the parcel's dimensional `Size` and the `Rate` it is 
          +charged at, and the heavy-parcel change left it to this change
          + to decide which one discounts use. No existing test cart has 
          +enough parcels to earn a discount.                            
        6 +                                                              
        7 +## Goals / Non-Goals                                          
        8 +                                                              
        9 +**Goals:**                                                    
       10 +- The quote always applies a best combination of discounts, an
          +d finding it takes time that grows linearly with the number of
          + parcels, so large carts stay fast.                           
       11 +- Discount selection lives in one internal class that works on
          + the priced parcel line items, so it uses exactly the sizes an
          +d costs the quote shows.                                      
       12 +- The same cart always gets the same discounts.               
       13 +                                                              
       14 +**Non-Goals:**                                                
       15 +- A general promotions engine, a discount interface or configu
          +rable discounts. There are three fixed discounts.             
       16 +- Marking which parcel in a group is the free one. It's always
          + the cheapest, and its cost is the saving.                    
       17 +- Taking the shipping option into account when choosing discou
          +nts. Speedy doubles every amount, so it can't change which com
          +bination is best.                                             
       18 +                                                              
       19 +## Decisions                                                  
       20 +                                                              
       21 +### A third line-item kind for discounts                      
       22 +```                                                           
       23 +public enum Discount { SmallParcelMania, MediumParcelMania, Mi
          +xedParcelMania }                                              
       24 +                                                              
       25 +public sealed record DiscountLineItem(Discount Discount, IRead
          +OnlyList<Parcel> Parcels, decimal Saving) : LineItem          
       26 +{                                                             
       27 +    public override decimal Cost => -Saving;                  
       28 +}                                                             
       29 +```                                                           
       30 +- `Saving` is positive and `Cost` is its negative, so `Quote.T
          +otal` stays the sum of `Items`, and the brief's "-$8" is `Cost
          + == -8m`. `Cost` is worked out when it's read, like `ParcelLin
          +eItem.Cost`, so the two can't disagree.                       
       31 +- `Parcels` holds the parcels in the group, in cart order. The
          + brief describes a discount by the parcels it includes, and ea
          +ch parcel's cost is already on its own line item.             
       32 +- The enum is named `Discount`, the same as the property, as `
          +Cart.Shipping` does. Its values use the brief's names.        
       33 +- `LineItem`'s doc comment lists all three kinds. `DiscountLin
          +eItem` implements `SealHierarchy()`, so the set stays closed. 
       34 +- Alternatives considered:                                    
       35 +  - One combined discount line for the whole order. The brief'
          +s example lists two discounts separately.                     
       36 +  - Holding the group's `ParcelLineItem`s. They would show eac
          +h parcel's cost next to the saving, but they repeat the parcel
          + lines and make expected values in tests much longer.         
       37 +  - The parcels' positions in the cart. Unambiguous when a par
          +cel is added twice, but it's a loose primitive that callers ha
          +ve to index back into `Items` with.                           
       38 +  - A negative `Cost` alone. A named positive `Saving` reads l
          +ike the brief ("save $8").                                    
       39 +                                                              
       40 +### Discount line items compare their parcels by value        
     … 91 more lines (ctrl+o to expand)
  ⎿  … 8 more files changed

● The change passed verification and is archived to
  openspec/changes/archive/2026-11-11-multi-parcel-discounts/, with both specs
  synced and committed.

  Verify: no critical issues or warnings.
  - All 12 tasks are done.
  - All 11 requirements in the two specs (9 in parcel-discounts, 2 changed in
    parcel-pricing) are implemented.
  - Each of the 24 new or changed scenarios has a test.

  The one suggestion was that design.md named the wrong test class for the check
  against every possible grouping. I corrected it before archiving.

  Specs:
  - New parcel-discounts spec: its 9 requirements were added, with the purpose
    taken from the change.
  - parcel-pricing: the two requirements changed in place:
    - discount lines go between the parcel lines and speedy shipping;
    - speedy is charged after discounts.

    Their existing scenarios are unchanged. I also added discounts to its
    purpose line, as tasks.md listed.
  - Both specs pass openspec validate --strict.

  You asked to archive and earlier changes were all synced, so I synced without
  asking whether to skip it.

  The multi-parcel-discounts branch is 6 commits ahead of master, ending with
  5ceb663 Archive multi-parcel-discounts and sync specs. The working tree is
  clean, and there are no other changes in progress. It's ready to merge into
  master the same way as the earlier branches.

❯ squash and merge into master

  Ran 3 shell commands (ctrl+o to expand)

● The branch is squash-merged into master as a single commit, edca233 Add
  multi-parcel discounts, written in the same style as the earlier squash
  commits.

  - master's files now exactly match the tip of multi-parcel-discounts. The
    working tree is clean, and all 106 tests pass on master.
  - I've left the multi-parcel-discounts branch in place, as with the earlier
    feature branches.
  - Nothing was pushed, because the repo has no remote.