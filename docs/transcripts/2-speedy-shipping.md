
❯ /opsx:explore Thanks to logistics improvements we can deliver parcels faster.
This means we can charge more money. Speedy shipping can be selected by the
user to take advantage of our improvements.
Speedy shipping doubles the cost of the entire order.
Speedy shipping should be listed as a separate item in the output, with its
associated cost.
Speedy shipping should not impact the price of individual parcels, i.e. their
individual cost should remain the same as it was before.

start with a test:
Small parcel: all dimensions < 10 cm, $3
Medium parcel: all dimensions < 50 cm, $8
Large parcel: all dimensions < 100 cm, $15
XL parcel: any dimension 100cm, $25
Speedy shipping selected: $51
Total: $102

  Ran 2 shell commands (ctrl+o to expand)

● I've read the current spec, code and assumptions log. Here's where this change
  would plug in.

  What exists now

  Cart --AddParcel--> [Parcel, Parcel, ...]
    |
    +-- GetQuote() --> Quote( Items: [LineItem(Parcel, Size, Cost), ...],
                              Total: sum(Items.Cost) )

  LineItem is about a single parcel: it has a Parcel and a Size. Speedy shipping
  isn't a parcel, so the change can't just add another LineItem. That's the
  main design question.

  Your example as a scenario

    Small   $3  |
    Medium  $8  |  parcel line items, prices unchanged
    Large  $15  |
    XL     $25  |
    -----------
    Speedy $51  <-- equals the parcel subtotal
    -----------
    Total $102  <-- subtotal x 2

  So "doubles the cost of the order" works out as: a speedy charge equal to the
  parcel subtotal, and a total of twice that. The parcel items stay as they are,
  which meets the "individual cost unchanged" rule.

  The decision that blocks the rest: how speedy shipping appears in the Quote

   A) Dedicated member               B) Polymorphic items             C) Parcels
  + extras list
   ----------------------           ---------------------
  ------------------------
   Quote(                           Quote(                           Quote(
     Items,         // parcels        Items: [ParcelItem...,           Items,
       // parcels
     SpeedyShipping?, // $51 or null          SpeedyItem],              Extras:
  [Charge("Speedy", 51)],
     Total)                           Total)                           Total)

  ┌──────────────────────┬──────────────┬──────────────────┬───────────────┐
  │                      │ A: dedicated │ B: polymorphic   │  C: extras    │
  │                      │    member    │      items       │     list      │
  ├──────────────────────┼──────────────┼──────────────────┼───────────────┤
  │ Breaks LineItem      │ no           │ yes (becomes a   │ no            │
  │                      │              │ base type)       │               │
  ├──────────────────────┼──────────────┼──────────────────┼───────────────┤
  │ Caller effort        │ read one     │ must type-switch │ loop over a   │
  │                      │ property     │                  │ generic list  │
  ├──────────────────────┼──────────────┼──────────────────┼───────────────┤
  │ Literal "separate    │ close        │ exact            │ close         │
  │ item in the output"  │              │                  │               │
  ├──────────────────────┼──────────────┼──────────────────┼───────────────┤
  │ Future surcharges or │ add another  │ add another      │ add another   │
  │  discounts           │ member       │ subtype          │ entry         │
  └──────────────────────┴──────────────┴──────────────────┴───────────────┘

  I recommend A. It's the smallest change to the public API, it's strongly
  typed, and parcel line items stay exactly as they are. If a later step adds
  more order-level items (discounts, say), C is an easy refactor at that point.
  Building that general list now would be speculative.

  Next questions, once A/B/C is settled

  - How it's selected. I lean towards a parameter:
    cart.GetQuote(Shipping.Speedy), with Standard as the default. GetQuote is
    already a side-effect-free read, so a caller can compare both quotes from
    the same cart. The alternative is order state on the cart, like
    cart.SelectSpeedyShipping(). That's closer to "the user selects it", but it
    adds mutable state and raises the question of how to turn it off.
  - When it's not selected. Is the speedy item missing entirely, or shown as $0?
    I'd say missing.
  - Empty cart with speedy selected. Does it show a speedy item of $0, or none?
    I'd show $0 for consistency, and log that in docs/assumptions.md.
  - Future ordering (just flagging). Later steps may add discounts or weight
    surcharges. "The entire order" will then need a defined order of operations:
    does speedy double the total before or after a discount? That's not in
    scope yet, but the spec should say "doubles the sum of the parcel costs" so
    it's clear what it doubles today.

  Which output shape do you want: A, B or C? It sets the public API and the
  wording of the spec scenarios. After that, the selection mechanism is the next
  thing to decide.

❯ B

● B it is. Here's what that means in practice.

  Shape under B

              LineItem (abstract)
              + Cost : decimal
                    ^
          +---------+------------+
          |                      |
    ParcelLineItem         SpeedyShippingLineItem
    + Parcel               (just Cost)
    + Size

  Quote.Items = [ Parcel S $3, Parcel M $8, Parcel L $15, Parcel XL $25, Speedy
  $51 ]
  Quote.Total = $102

  Things B decides for us

  - The total rule stays the same. "Total = sum of line-item costs" (51 + 51
    = 102) still holds, so that requirement's wording doesn't change. It doubles
    because the speedy item is in the list. That's a nice property of B.
  - The speedy item goes last. Its cost depends on all the parcel items, and
    putting it last matches your example. It appears at most once.
  - The current LineItem gets split. The existing record becomes the abstract
    base, and the parcel fields move to ParcelLineItem. Existing tests that read
    item.Size or item.Parcel need a cast or pattern match, and
    ParcelPricing.Price returns ParcelLineItem. This change is internal to the
    repo, but it is a breaking change to the public API, which is fine at this
    stage.
  - The hierarchy should be closed. The base gets a private protected
    constructor so callers can't invent new item kinds. Callers can then match
    on a known set of types, and the API stays deliberate.
  - The spec has two requirements to change. "Quote lists each parcel..."
    becomes "one parcel item per parcel in insertion order, followed by a speedy
    item when selected." A new "Speedy shipping" requirement covers your $51 /
    $102 scenario, plus the unselected and empty-cart cases.

  Next decision: how the user selects speedy shipping

   1) Quote-time parameter                  2) State on the cart
   -----------------------                  --------------------
   cart.GetQuote(Shipping.Speedy)           cart.SelectSpeedyShipping();
   cart.GetQuote()  // Standard default     cart.GetQuote()

  ┌───────────────────────┬─────────────────┬──────────────────────────────┐
  │                       │  1: parameter   │        2: cart state         │
  ├───────────────────────┼─────────────────┼──────────────────────────────┤
  │ Matches "selected by  │ reasonably      │ closely (it's part of the    │
  │ the user"             │                 │ order)                       │
  ├───────────────────────┼─────────────────┼──────────────────────────────┤
  │ Cart stays            │ yes             │ no, it gains a flag          │
  │ parcels-only          │                 │                              │
  ├───────────────────────┼─────────────────┼──────────────────────────────┤
  │ Turning it off        │ pass Standard   │ needs a deselect method or a │
  │                       │                 │  settable property           │
  ├───────────────────────┼─────────────────┼──────────────────────────────┤
  │ Comparing both prices │ two calls on    │ toggle, quote, toggle back   │
  │                       │ one cart        │                              │
  ├───────────────────────┼─────────────────┼──────────────────────────────┤
  │ Fits "GetQuote is a   │ yes             │ yes                          │
  │ pure read"            │                 │                              │
  └───────────────────────┴─────────────────┴──────────────────────────────┘

  I recommend option 1 with a Shipping enum (Standard, Speedy) and Standard as
  the default. It keeps the existing GetQuote() calls compiling, avoids mutable
  flags, and an enum leaves room if "overnight" or something similar ever comes
  along. Option 2 makes sense if you see speedy as part of the order, the way
  the parcels are.

  Which do you want: 1 (parameter) or 2 (cart state)?

❯ 2

● Option 2 it is: speedy shipping is part of the order and lives on the cart.

  What this means

  Cart
   +-- parcels  : [Parcel, ...]      <-- AddParcel
   +-- shipping : Standard | Speedy  <-- new order-level choice
   |
   +-- GetQuote() --> Quote(Items: [ParcelLineItem..., SpeedyShippingLineItem?],
  Total)

  - Quotes stay snapshots. The existing "getting a quote doesn't change the
    cart" rule carries over: a quote taken before selecting speedy keeps no
    speedy item afterwards. I'd add a scenario for that.
  - It needs a way back. Once shipping is cart state, the user has to be able to
    switch back to standard. That's the main thing that changes the API.
  - Selection is order-independent. Choosing speedy before or after adding
    parcels gives the same quote, because the charge is worked out when the
    quote is made.

  Next decision: what the cart API looks like

   a) Settable enum property            b) Pair of methods                 c)
  One method taking an enum
   -------------------------            ------------------
  ----------------------------
   cart.Shipping = Shipping.Speedy;     cart.SelectSpeedyShipping();
  cart.SelectShipping(Shipping.Speedy);
   cart.Shipping = Shipping.Standard;   cart.SelectStandardShipping();
  cart.SelectShipping(Shipping.Standard);
   var s = cart.Shipping;               (no read-back unless added)        (no
  read-back unless added)

  ┌────────────────┬───────────────────┬───────────────┬───────────────────┐
  │                │    a: property    │  b: methods   │ c: method + enum  │
  ├────────────────┼───────────────────┼───────────────┼───────────────────┤
  │ Reads like the │ good              │ best ("select │ good              │
  │  domain        │                   │  speedy")     │                   │
  ├────────────────┼───────────────────┼───────────────┼───────────────────┤
  │ Turning it off │ assign Standard   │ second method │ pass Standard     │
  ├────────────────┼───────────────────┼───────────────┼───────────────────┤
  │ Caller can     │                   │ needs an      │ needs an extra    │
  │ read the       │ yes               │ extra getter  │ getter            │
  │ current choice │                   │               │                   │
  ├────────────────┼───────────────────┼───────────────┼───────────────────┤
  │ Extends to a   │ add an enum value │ add a method  │ add an enum value │
  │ third option   │                   │               │                   │
  ├────────────────┼───────────────────┼───────────────┼───────────────────┤
  │                │ an undefined enum │               │ an undefined enum │
  │ Invalid input  │  cast must be     │ impossible    │  cast must be     │
  │                │ rejected          │               │ rejected          │
  └────────────────┴───────────────────┴───────────────┴───────────────────┘

  I recommend (a), a Shipping property with Standard as the default. Selecting,
  deselecting and reading back are all one member, and the setter rejects
  undefined values like (Shipping)42 with ArgumentOutOfRangeException, the same
  as how Parcel rejects bad dimensions. That rejection would go in
  docs/assumptions.md.

  Once this is settled, these are the defaults I'd propose. None of them is
  confirmed yet:

  - With standard shipping, the quote has no speedy item. It doesn't show a $0
    speedy line.
  - With speedy selected and an empty cart, the quote shows a $0 speedy item and
    a $0 total.
  - Speedy doubles "the sum of the parcel costs", worded that way so later steps
    like discounts can define their own order of operations.

  Which cart API do you want: a, b or c?

❯ a

● That settles the API. Here's where things stand.

  Decided

  ┌─────┬────────────────────────────────────────────────────────────────────┐
  │  #  │                              Decision                              │
  ├─────┼────────────────────────────────────────────────────────────────────┤
  │     │ Speedy shipping appears as its own item in the quote's item list.  │
  │ 1   │ The current LineItem becomes a closed abstract base with           │
  │     │ ParcelLineItem(Parcel, Size, Cost) and                             │
  │     │ SpeedyShippingLineItem(Cost) under it.                             │
  ├─────┼────────────────────────────────────────────────────────────────────┤
  │ 2   │ Speedy shipping is stored on the cart, not passed into GetQuote(). │
  ├─────┼────────────────────────────────────────────────────────────────────┤
  │     │ It's set through a property, Cart.Shipping, which takes a Shipping │
  │ 3   │  enum (Standard, Speedy) and defaults to Standard. Setting an      │
  │     │ undefined value throws ArgumentOutOfRangeException.                │
  └─────┴────────────────────────────────────────────────────────────────────┘

  Follows from those

  - When speedy is selected, its item comes last, after all the parcel items,
    and appears once.
  - The speedy cost equals the sum of the parcel costs. The total is still the
    sum of all items, so it comes out at twice the parcel subtotal: $51 + $51 =
    $102.
  - Each parcel item keeps its normal price, whether or not speedy is selected.
  - A quote is a snapshot. Changing Shipping after getting a quote leaves that
    quote as it was.
  - Setting Shipping before or after adding parcels gives the same result.

  var cart = new Cart();
  cart.AddParcel(small); cart.AddParcel(medium); cart.AddParcel(large);
  cart.AddParcel(xl);
  cart.Shipping = Shipping.Speedy;

  cart.GetQuote()
    Items: Parcel Small $3 | Parcel Medium $8 | Parcel Large $15 | Parcel XL $25
  | Speedy $51
    Total: $102

  Still to decide: an empty cart with speedy selected

   (i) Show a $0 speedy item            (ii) Leave the speedy item out
   Items: [Speedy $0]                   Items: []
   Total: $0                            Total: $0

  I recommend (i). "Speedy is selected, so a speedy item appears" is one simple
  rule with no special case, and the output reflects what the user chose. Option
  (ii) avoids showing a meaningless $0 line, but it means the item's presence
  depends on the cart contents as well as the selection.

  The other proposed default is that standard shipping shows no speedy item at
  all. I think that one is clear-cut, so I'll treat it as settled unless you
  object.

  For an empty cart with speedy selected, do you want (i) or (ii)? Once that's
  answered, the change is fully specified, and /opsx:propose can turn it into a
  change with a proposal, a spec delta, a design and tasks. Your $51 / $102
  example would be the first test task.

❯ i

● Option (i): selecting speedy always shows a speedy item, even on an empty
  cart, where it's $0.

  That was the last open question, so the change is fully specified.

  Summary

  ┌──────────────┬──────────────────────────────────────────────────────────┐
  │     Area     │                         Decision                         │
  ├──────────────┼──────────────────────────────────────────────────────────┤
  │              │ LineItem becomes a closed abstract base (private         │
  │ Output shape │ protected constructor) with ParcelLineItem(Parcel, Size, │
  │              │  Cost) and SpeedyShippingLineItem(Cost)                  │
  ├──────────────┼──────────────────────────────────────────────────────────┤
  │ Selection    │ Cart.Shipping property, Shipping enum { Standard, Speedy │
  │              │  }, default Standard                                     │
  ├──────────────┼──────────────────────────────────────────────────────────┤
  │ Validation   │ Setting an undefined Shipping value throws               │
  │              │ ArgumentOutOfRangeException                              │
  ├──────────────┼──────────────────────────────────────────────────────────┤
  │ Speedy cost  │ Sum of the parcel costs; parcel item costs don't change  │
  ├──────────────┼──────────────────────────────────────────────────────────┤
  │ Total        │ Still the sum of all items, so doubled when speedy is    │
  │              │ selected                                                 │
  ├──────────────┼──────────────────────────────────────────────────────────┤
  │ Item order   │ Parcel items in the order added, then the speedy item    │
  │              │ last, once                                               │
  ├──────────────┼──────────────────────────────────────────────────────────┤
  │ Standard     │ No speedy item                                           │
  │ shipping     │                                                          │
  ├──────────────┼──────────────────────────────────────────────────────────┤
  │ Empty cart + │ One speedy item at $0, total $0                          │
  │  speedy      │                                                          │
  ├──────────────┼──────────────────────────────────────────────────────────┤
  │              │ Changing Shipping after getting a quote leaves that      │
  │ Snapshots    │ quote as it was; setting it before or after adding       │
  │              │ parcels gives the same result                            │
  └──────────────┴──────────────────────────────────────────────────────────┘

  Spec changes this needs (parcel-pricing)

  - Modified: "Quote lists each parcel with its size and cost". Parcel items
    come in the order added, followed by a speedy item when speedy is selected.
  - Added: "Speedy shipping doubles the order cost", with these scenarios:
    - your $51 / $102 example
    - standard shipping has no speedy item
    - parcel item costs are the same with and without speedy
    - empty cart: a $0 speedy item and a $0 total
  - Added: "Shipping option is chosen on the cart", with these scenarios:
    - the default is Standard
    - switching back to Standard removes the speedy item
    - an undefined value is rejected
  - Modified: "Requesting a quote does not change the cart". Adds a scenario:
    changing shipping after getting a quote leaves that quote unchanged.

  New docs/assumptions.md entries: the speedy cost is based on the parcel
  subtotal, an empty cart with speedy shows a $0 speedy item, undefined enum
  values are rejected, and the speedy item comes last.

  Next step: run /opsx:propose to turn this into a change with a proposal, a
  spec delta, a design and tasks. The first task will be a failing test for your
  $51 / $102 example. Explore mode doesn't write code, so implementation
  happens after that, through /opsx:apply.

❯ /opsx:propose

● Bash(cd /home/sam/workspace/courier-kata && openspec context --json &&
      openspec new change "speedy-shipping" && openspec status --change
      speedy-shipping --json)
  ⎿  {
       "root": {
         "path": "/home/sam/workspace/courier-kata",
     … +116 lines (ctrl+o to expand)
  ⎿  Created openspec/changes/speedy-shipping/.openspec.yaml (+2 -0)
      1 +schema: spec-driven                                             
      2 +created: 2026-11-11                                             
  ⎿  (files that changed while this command ran, by the command or by
     another process; a convenience view, not a review or audit of the
     command)

  Read 1 file, ran 2 shell commands (ctrl+o to expand)

● Now writing the proposal.

● Write(openspec/changes/speedy-shipping/proposal.md)
  ⎿  Wrote 32 lines to openspec/changes/speedy-shipping/proposal.md
       1 # Proposal
       2
       3 ## Why
       4
       5 Logistics improvements let us deliver parcels faster, and custom
         ers will pay more for that. Customers need a way to choose faste
         r delivery for an order, and the quote must show what that choic
         e costs.
       6
       7 ## What Changes
     … +25 lines (ctrl+o to expand)

  Ran 1 shell command (ctrl+o to expand)

● Writing the parcel-pricing spec delta.

● Write(openspec/changes/speedy-shipping/specs/parcel-pricing/spec.md)
  ⎿  Wrote 69 lines to
     openspec/changes/speedy-shipping/specs/parcel-pricing/spec.md
       1 # Spec Delta
       2
       3 ## ADDED Requirements
       4
       5 ### Requirement: Shipping option is chosen on the cart
       6 A cart SHALL have a shipping option of either Standard or Speedy
         , which is Standard by default. The shipping option MAY be chang
         ed at any time, in either direction, and the cart MUST report it
         s current option. Setting an option that is neither Standard nor
          Speedy MUST fail with an out-of-range error. The shipping optio
     … +64 lines (~4 KB) (ctrl+o to expand)

● Write(openspec/changes/speedy-shipping/design.md)
  ⎿  Wrote 66 lines to openspec/changes/speedy-shipping/design.md
       1 # Design
       2
       3 ## Context
       4
       5 See proposal.md for the motivation. Today `LineItem(Parcel, Size
         , Cost)` is a sealed record that only describes parcels, and `Qu
         ote(Items, Total)` totals the costs of its items. `ParcelPricing
         ` (internal) prices one parcel, and `Cart.GetQuote()` maps parce
         ls to line items. Speedy shipping is the first line item that is
         n't a parcel, so the line-item model has to change.
     … +61 lines (ctrl+o to expand)

● Write(openspec/changes/speedy-shipping/tasks.md)
  ⎿  Wrote 45 lines to openspec/changes/speedy-shipping/tasks.md
       1 # Tasks
       2
       3 ## 1. Split line items into a closed hierarchy (refactor, no beh
         aviour change)
       4
       5 - [ ] 1.1 Make `LineItem` an abstract record with `Cost` and a `
         private protected` constructor, add `sealed record ParcelLineIte
         m(Parcel, ParcelSize, Cost) : LineItem`, and make `ParcelPricing
         .Price` return `ParcelLineItem`. Update the existing tests (`Car
         tTests`, `ParcelCostTests`, `ParcelSizeTests`) to use `ParcelLin
     … +41 lines (~3 KB) (ctrl+o to expand)

  Ran 1 shell command (ctrl+o to expand)

● I've created the speedy-shipping change in openspec/changes/speedy-shipping/.
  It has all four planning artifacts and passes openspec validate --strict. No
  code has been changed.

  What's in it

  - proposal.md (openspec/changes/speedy-shipping/proposal.md): why the change
    is needed and what it changes. The change to LineItem is marked as breaking
    for the public API, and weight, discounts and other shipping speeds are out
    of scope.
  - specs/parcel-pricing/spec.md
    (openspec/changes/speedy-shipping/specs/parcel-pricing/spec.md): changes to
    the existing parcel-pricing spec.
    - New: "Shipping option is chosen on the cart", covering the Standard
      default, switching back to Standard, rejecting an undefined value, and
      selecting Speedy before adding parcels.
    - New: "Speedy shipping doubles the order cost", with your $51 / $102
      example plus the standard-shipping, unchanged-parcel-cost and
      $0-empty-cart cases.
    - Modified: "Quote lists each parcel...", so the speedy item comes after the
      parcel items.
    - Modified: "Requesting a quote does not change the cart", with a new
      scenario for changing shipping after a quote.
  - design.md (openspec/changes/speedy-shipping/design.md): how it fits
    together.
    - LineItem becomes an abstract record with a private protected constructor,
      so only ParcelLineItem and SpeedyShippingLineItem can exist.
    - A Shipping enum and a Cart.Shipping property; the property rejects
      undefined values with an Enum.IsDefined check.
    - Cart.GetQuote() adds the speedy charge, equal to the parcel subtotal, so
      the parcel pricing code doesn't change.
  - tasks.md (openspec/changes/speedy-shipping/tasks.md): four groups.
    a. A refactor with no behaviour change: split LineItem into the new types,
       update the three existing test files, and commit.
    b. Speedy pricing, test first, starting with your $51 / $102 example.
    c. The shipping option on the cart, including validation.
    d. Final checks.

    The new docs/assumptions.md entries are committed in groups 2 and 3,
    alongside the code that depends on them.

  All artifacts needed for implementation are ready for review. When you're
  happy with them, run /opsx:apply.

❯ /opsx:apply

● Using change: speedy-shipping (override with /opsx:apply <other>).

  Ran 1 shell command (ctrl+o to expand)

● Starting task 1.1: splitting LineItem into the new types.

  Ran 1 shell command (ctrl+o to expand)

● Write(src/CourierKata/LineItem.cs)
  ⎿  Added 11 lines, removed 2 lines
       1  namespace CourierKata;
       2  
       3 -/// <summary>One parcel in a quote, with the size it is charged
         - at and its cost in dollars.</summary>                         
       4 -public sealed record LineItem(Parcel Parcel, ParcelSize Size, d
         -ecimal Cost);                                                  
       3 +/// <summary>                                                  
       4 +/// One line of a quote and its cost in dollars. The kinds of l
         +ine item are fixed:                                            
       5 +/// <see cref="ParcelLineItem"/> and <see cref="SpeedyShippingL
         +ineItem"/>.                                                    
       6 +/// </summary>                                                 
       7 +public abstract record LineItem                                
       8 +{                                                              
       9 +    private protected LineItem(decimal cost) => Cost = cost;   
      10 +                                                               
      11 +    /// <summary>Cost in dollars.</summary>                    
      12 +    public decimal Cost { get; }                               
      13 +}                                                              

● Write(src/CourierKata/ParcelLineItem.cs)
  ⎿  Wrote 4 lines to src/CourierKata/ParcelLineItem.cs
      1 namespace CourierKata;
      2
      3 /// <summary>One parcel in a quote, with the size it is charged a
        t and its cost in dollars.</summary>
      4 public sealed record ParcelLineItem(Parcel Parcel, ParcelSize Siz
        e, decimal Cost) : LineItem(Cost);

● Bash(sed -i 's/public static LineItem Price/public static ParcelLineItem
      Price/; s/return new LineItem(/return new ParcelLineItem(/'
      src/CourierKata/ParcelPricing.cs…)  ⎿   src/CourierKata/LineItem.cs                | 13 +++++++++++--
      src/CourierKata/ParcelPricing.cs           |  4 ++--
      tests/CourierKata.Tests/CartTests.cs       | 10 +++++-----
     … +15 lines (ctrl+o to expand)
  ⎿  Updated src/CourierKata/ParcelPricing.cs (+2 -2)
      25          _ => throw new ArgumentOutOfRangeException(nameof(size)
          , size, "Unknown parcel size."),
      26      };
      27  
      28 -    public static LineItem Price(Parcel parcel)                
      28 +    public static ParcelLineItem Price(Parcel parcel)          
      29      {
      30          var size = Classify(parcel);
      31 -        return new LineItem(parcel, size, PriceOf(size));      
      31 +        return new ParcelLineItem(parcel, size, PriceOf(size));
      32      }
      33  }
  ⎿  Updated tests/CourierKata.Tests/CartTests.cs (+5 -5)
      14  
      15          Assert.Equal(
      16              [
      17 -                new LineItem(ExtraLarge, ParcelSize.ExtraLarge,
         - 25m),                                                         
      18 -                new LineItem(Small, ParcelSize.Small, 3m),     
      19 -                new LineItem(Medium, ParcelSize.Medium, 8m),   
      17 +                new ParcelLineItem(ExtraLarge, ParcelSize.Extra
         +Large, 25m),                                                   
      18 +                new ParcelLineItem(Small, ParcelSize.Small, 3m)
         +,                                                              
      19 +                new ParcelLineItem(Medium, ParcelSize.Medium, 8
         +m),                                                            
      20              ],
      21              cart.GetQuote().Items);
      22      }
     ...
      28  
      29          Assert.Equal(
      30              [
      31 -                new LineItem(Small, ParcelSize.Small, 3m),     
      32 -                new LineItem(Small, ParcelSize.Small, 3m),     
      31 +                new ParcelLineItem(Small, ParcelSize.Small, 3m)
         +,                                                              
      32 +                new ParcelLineItem(Small, ParcelSize.Small, 3m)
         +,                                                              
      33              ],
      34              cart.GetQuote().Items);
      35      }
  ⎿  Updated tests/CourierKata.Tests/ParcelCostTests.cs (+1 -1)
      12          var cart = new Cart();
      13          cart.AddParcel(new Parcel(largestDimension, 1m, 1m));
      14  
      15 -        var item = Assert.Single(cart.GetQuote().Items);       
      15 +        var item = Assert.IsType<ParcelLineItem>(Assert.Single(
         +cart.GetQuote().Items));                                       
      16  
      17          Assert.Equal(expectedSize, item.Size);
      18          Assert.Equal(expectedCost, item.Cost);
  ⎿  Updated tests/CourierKata.Tests/ParcelSizeTests.cs (+1 -1)
      72          var cart = new Cart();
      73          cart.AddParcel(new Parcel(length, width, height));
      74  
      75 -        return Assert.Single(cart.GetQuote().Items).Size;      
      75 +        return Assert.IsType<ParcelLineItem>(Assert.Single(cart
         +.GetQuote().Items)).Size;                                      
      76      }
      77  }

● Bash(git add openspec/changes/speedy-shipping && git commit -q -m "Propose
      speedy shipping change" && sed -i 's/^- \[ \] 1\.1 /- [x] 1.1 /; s/^- \[
      \] 1\.2 /- [x] 1.…)
  ⎿  31a7b54 Split LineItem into a closed line-item hierarchy
     4351976 Propose speedy shipping change
     48f692b Add parcel pricing by size
  ⎿  Updated openspec/changes/speedy-shipping/tasks.md (+2 -2)
      2  
      3  ## 1. Split line items into a closed hierarchy (refactor, no beh
         aviour change)
      4  
      5 -- [ ] 1.1 Make `LineItem` an abstract record with `Cost` and a `
        -private protected` constructor, add `sealed record ParcelLineIte
        -m(Parcel, ParcelSize, Cost) : LineItem`, and make `ParcelPricing
        -.Price` return `ParcelLineItem`. Update the existing tests (`Car
        -tTests`, `ParcelCostTests`, `ParcelSizeTests`) to use `ParcelLin
        -eItem` where they read `Size` or `Parcel`. Verify that `docker c
        -ompose run --rm tests` passes with no other test changes and zer
        -o warnings.                                                     
      6 -- [ ] 1.2 Commit the refactor.                                  
      5 +- [x] 1.1 Make `LineItem` an abstract record with `Cost` and a `
        +private protected` constructor, add `sealed record ParcelLineIte
        +m(Parcel, ParcelSize, Cost) : LineItem`, and make `ParcelPricing
        +.Price` return `ParcelLineItem`. Update the existing tests (`Car
        +tTests`, `ParcelCostTests`, `ParcelSizeTests`) to use `ParcelLin
        +eItem` where they read `Size` or `Parcel`. Verify that `docker c
        +ompose run --rm tests` passes with no other test changes and zer
        +o warnings.                                                     
      6 +- [x] 1.2 Commit the refactor.                                  
      7  
      8  ## 2. Speedy shipping pricing (test first, starting with the bri
         ef's example)
      9  

● Task 2.1: writing the failing test for the brief's example.

● Write(tests/CourierKata.Tests/SpeedyShippingTests.cs)
  ⎿  Wrote 39 lines to tests/CourierKata.Tests/SpeedyShippingTests.cs
       1 namespace CourierKata.Tests;
       2
       3 public class SpeedyShippingTests
       4 {
       5     private static readonly Parcel Small = new(5m, 5m, 5m);
       6     private static readonly Parcel Medium = new(20m, 5m, 5m);
       7     private static readonly Parcel Large = new(70m, 5m, 5m);
       8     private static readonly Parcel ExtraLarge = new(150m, 5m, 5m
         );
       9
     … +30 lines (ctrl+o to expand)

  Ran 1 shell command (ctrl+o to expand)

● It fails as expected because the types don't exist yet. Now implementing them.

● Write(src/CourierKata/Shipping.cs)
  ⎿  Wrote 11 lines to src/CourierKata/Shipping.cs
       1 namespace CourierKata;
       2
       3 /// <summary>How fast an order is delivered.</summary>
       4 public enum Shipping
       5 {
       6     /// <summary>Normal delivery, at no extra cost.</summary>
       7     Standard,
       8
       9     /// <summary>Faster delivery, which doubles the cost of the
         order.</summary>
     … +2 lines (ctrl+o to expand)

● Write(src/CourierKata/SpeedyShippingLineItem.cs)
  ⎿  Wrote 4 lines to src/CourierKata/SpeedyShippingLineItem.cs
      1 namespace CourierKata;
      2
      3 /// <summary>The extra charge for speedy shipping, in dollars.</s
        ummary>
      4 public sealed record SpeedyShippingLineItem(decimal Cost) : LineI
        tem(Cost);

● Write(src/CourierKata/Cart.cs)
  ⎿  Added 14 lines, removed 4 lines
       1  namespace CourierKata;
       2  
       3 -/// <summary>Collects parcels to be sent and prices them.</summ
         -ary>                                                           
       3 +/// <summary>Collects parcels to be sent, with how fast to ship
         + them, and prices them.</summary>                              
       4  public sealed class Cart
       5  {
       6      private readonly List<Parcel> _parcels = [];
       7  
       8 +    /// <summary>How fast the order is shipped. Defaults to <se
         +e cref="Shipping.Standard"/>.</summary>                        
       9 +    public Shipping Shipping { get; set; } = Shipping.Standard;
      10 +                                                               
      11      /// <summary>Adds a parcel to the cart.</summary>
      12      public void AddParcel(Parcel parcel)
      13      {
     ...
      15          _parcels.Add(parcel);
      16      }
      17  
      15 -    /// <summary>Prices every parcel in the cart.</summary>    
      18 +    /// <summary>Prices every parcel in the cart, followed by s
         +peedy shipping when it is selected.</summary>                  
      19      public Quote GetQuote()
      20      {
      18 -        var items = _parcels.Select(ParcelPricing.Price).ToList
         -().AsReadOnly();                                               
      19 -        return new Quote(items, items.Sum(item => item.Cost)); 
      21 +        var parcelItems = _parcels.Select(ParcelPricing.Price).
         +ToList();                                                      
      22 +        var items = new List<LineItem>(parcelItems);           
      23 +                                                               
      24 +        if (Shipping == Shipping.Speedy)                       
      25 +        {                                                      
      26 +            items.Add(new SpeedyShippingLineItem(parcelItems.Su
         +m(item => item.Cost)));                                        
      27 +        }                                                      
      28 +                                                               
      29 +        return new Quote(items.AsReadOnly(), items.Sum(item => 
         +item.Cost));                                                   
      30      }
      31  }

  Ran 1 shell command (ctrl+o to expand)

● Task 2.1 passes. Moving on to the task 2.2 tests.

● Update(tests/CourierKata.Tests/SpeedyShippingTests.cs)
  ⎿  Added 41 lines
      26          Assert.Equal(102m, quote.Total);
      27      }
      28  
      29 +    [Fact]                                                     
      30 +    public void Standard_shipping_has_no_speedy_item()         
      31 +    {                                                          
      32 +        var quote = CartWith(Shipping.Standard, Small).GetQuote
         +();                                                            
      33 +                                                               
      34 +        Assert.Equal([new ParcelLineItem(Small, ParcelSize.Smal
         +l, 3m)], quote.Items);                                         
      35 +        Assert.Equal(3m, quote.Total);                         
      36 +    }                                                          
      37 +                                                               
      38 +    [Fact]                                                     
      39 +    public void Parcel_items_cost_the_same_with_speedy_shipping
         +()                                                             
      40 +    {                                                          
      41 +        var standard = CartWith(Shipping.Standard, Small, Large
         +, ExtraLarge).GetQuote();                                      
      42 +        var speedy = CartWith(Shipping.Speedy, Small, Large, Ex
         +traLarge).GetQuote();                                          
      43 +                                                               
      44 +        Assert.Equal(standard.Items, speedy.Items.OfType<Parcel
         +LineItem>());                                                  
      45 +    }                                                          
      46 +                                                               
      47 +    [Fact]                                                     
      48 +    public void Speedy_shipping_item_comes_after_the_parcel_ite
         +ms()                                                           
      49 +    {                                                          
      50 +        var quote = CartWith(Shipping.Speedy, Medium, Small).Ge
         +tQuote();                                                      
      51 +                                                               
      52 +        Assert.Equal(                                          
      53 +            [                                                  
      54 +                new ParcelLineItem(Medium, ParcelSize.Medium, 8
         +m),                                                            
      55 +                new ParcelLineItem(Small, ParcelSize.Small, 3m)
         +,                                                              
      56 +                new SpeedyShippingLineItem(11m),               
      57 +            ],                                                 
      58 +            quote.Items);                                      
      59 +    }                                                          
      60 +                                                               
      61 +    [Fact]                                                     
      62 +    public void Speedy_shipping_on_an_empty_cart_costs_nothing(
         +)                                                              
      63 +    {                                                          
      64 +        var quote = CartWith(Shipping.Speedy).GetQuote();      
      65 +                                                               
      66 +        Assert.Equal([new SpeedyShippingLineItem(0m)], quote.It
         +ems);                                                          
      67 +        Assert.Equal(0m, quote.Total);                         
      68 +    }                                                          
      69 +                                                               
      70      private static Cart CartWith(Shipping shipping, params Parc
          el[] parcels)
      71      {
      72          var cart = new Cart { Shipping = shipping };

● Bash(sed -i 's/^- \[ \] 2\.1 /- [x] 2.1 /'
      openspec/changes/speedy-shipping/tasks.md; docker compose run --rm tests
      2>&1 | grep -E "error|Passed!|Failed!" | head -5)
  ⎿  Passed!  - Failed:     0, Passed:    39, Skipped:     0, Total:    39,
      Duration: 42 ms - CourierKata.Tests.dll (net10.0)
  ⎿  Updated openspec/changes/speedy-shipping/tasks.md (+1 -1)
       7  
       8  ## 2. Speedy shipping pricing (test first, starting with the br
          ief's example)
       9  
      10 -- [ ] 2.1 Write a failing test: one parcel of each size with `c
         -art.Shipping = Shipping.Speedy` gives parcel items of $3, $8, $
         -15 and $25, then a `SpeedyShippingLineItem` of $51, and a total
         - of $102. Add the `Shipping` enum, the `Cart.Shipping` property
         - (default `Standard`), `SpeedyShippingLineItem`, and the speedy
         - item in `Cart.GetQuote()` to make it pass. Verify with `docker
         - compose run --rm tests`.                                      
      10 +- [x] 2.1 Write a failing test: one parcel of each size with `c
         +art.Shipping = Shipping.Speedy` gives parcel items of $3, $8, $
         +15 and $25, then a `SpeedyShippingLineItem` of $51, and a total
         + of $102. Add the `Shipping` enum, the `Cart.Shipping` property
         + (default `Standard`), `SpeedyShippingLineItem`, and the speedy
         + item in `Cart.GetQuote()` to make it pass. Verify with `docker
         + compose run --rm tests`.                                      
      11  - [ ] 2.2 Add tests for these cases, implementing anything stil
          l missing:
      12    - standard shipping has no speedy item (Small only: total $3)
      13    - parcel items are equal under Standard and Speedy

● These pass straight away because the 2.1 code already covers them. Task 2.3:
  adding the assumptions.

● Bash(cat >> docs/assumptions.md <<'EOF'…)
  ⎿  26bdd9e Add speedy shipping as a separate quote line item
  ⎿  Updated docs/assumptions.md (+37 -0)
      102  - **Assumed:** `GetQuote()` is a read. It can be called repeat
           edly, and each
      103    quote is an independent snapshot that later `AddParcel` call
           s don't change.
      104  - **Why:** The brief describes calculating a cost, not complet
           ing a purchase.
      105 +                                                              
      106 +## Speedy shipping                                            
      107 +                                                              
      108 +### Speedy charge equals the parcel subtotal                  
      109 +                                                              
      110 +- **Unclear:** "Speedy shipping doubles the cost of the entire
          + order" while                                                 
      111 +  also being "listed as a separate item" and not changing parc
          +el prices.                                                    
      112 +- **Assumed:** The speedy shipping line item costs the sum of 
          +the parcel                                                    
      113 +  line-item costs. The total is still the sum of every line it
          +em, so it                                                     
      114 +  doubles.                                                    
      115 +- **Why:** This is the only reading that doubles the total, ke
          +eps parcel costs                                              
      116 +  unchanged and shows speedy as its own item. It matches the b
          +rief's example                                                
      117 +  ($51 of parcels, $51 speedy, $102 total). Only parcel costs 
          +are doubled for                                               
      118 +  now; how speedy combines with later rules such as discounts 
          +is left to the                                                
      119 +  change that adds them.                                      
      120 +                                                              
      121 +### Speedy item comes last                                    
      122 +                                                              
      123 +- **Unclear:** Where the speedy item appears among the line it
          +ems.                                                          
      124 +- **Assumed:** It comes after all the parcel line items, and a
          +ppears once.                                                  
      125 +- **Why:** Its cost depends on all the parcels, and the brief'
          +s example lists                                               
      126 +  it after them.                                              
      127 +                                                              
      128 +### Standard shipping shows no speedy item                    
      129 +                                                              
      130 +- **Unclear:** Whether a quote without speedy shipping lists i
          +t at $0.                                                      
      131 +- **Assumed:** With standard shipping there is no speedy line 
          +item.                                                         
      132 +- **Why:** The item represents a choice the customer made; a $
          +0 line for an                                                 
      133 +  option they didn't choose adds noise.                       
      134 +                                                              
      135 +### Empty cart with speedy shipping shows a $0 speedy item    
      136 +                                                              
      137 +- **Unclear:** What a quote looks like when speedy is selected
          + but the cart is                                              
      138 +  empty.                                                      
      139 +- **Assumed:** It has a single speedy line item costing $0 and
          + a total of $0.                                               
      140 +- **Why:** The rule stays simple: the speedy item appears exac
          +tly when speedy                                               
      141 +  is selected, regardless of the cart's contents.             
  ⎿  Updated openspec/changes/speedy-shipping/tasks.md (+2 -2)
       8  ## 2. Speedy shipping pricing (test first, starting with the br
          ief's example)
       9  
      10  - [x] 2.1 Write a failing test: one parcel of each size with `c
          art.Shipping = Shipping.Speedy` gives parcel items of $3, $8, $
          15 and $25, then a `SpeedyShippingLineItem` of $51, and a total
           of $102. Add the `Shipping` enum, the `Cart.Shipping` property
           (default `Standard`), `SpeedyShippingLineItem`, and the speedy
           item in `Cart.GetQuote()` to make it pass. Verify with `docker
           compose run --rm tests`.
      11 -- [ ] 2.2 Add tests for these cases, implementing anything stil
         -l missing:                                                     
      11 +- [x] 2.2 Add tests for these cases, implementing anything stil
         +l missing:                                                     
      12    - standard shipping has no speedy item (Small only: total $3)
      13    - parcel items are equal under Standard and Speedy
      14    - the speedy item comes last (Medium, Small with Speedy: $8, 
          $3, speedy $11)
      15    - an empty cart with Speedy has a single $0 speedy item and a
           $0 total
      16  
      17    Verify with `docker compose run --rm tests`.
      18 -- [ ] 2.3 Add these assumptions to `docs/assumptions.md`:      
      18 +- [x] 2.3 Add these assumptions to `docs/assumptions.md`:      
      19    - the speedy charge equals the parcel subtotal
      20    - the speedy item comes last
      21    - standard shipping shows no speedy item

● Task 3.1: tests for the shipping option on the cart.

● Write(tests/CourierKata.Tests/ShippingOptionTests.cs)
  ⎿  Wrote 68 lines to tests/CourierKata.Tests/ShippingOptionTests.cs
       1 namespace CourierKata.Tests;
       2
       3 public class ShippingOptionTests
       4 {
       5     private static readonly Parcel Small = new(5m, 5m, 5m);
       6     private static readonly Parcel ExtraLarge = new(150m, 5m, 5m
         );
       7
       8     [Fact]
       9     public void New_cart_uses_standard_shipping()
     … +59 lines (ctrl+o to expand)

  Ran 1 shell command (ctrl+o to expand)

● The four 3.1 tests pass. The 3.2 rejection test fails as intended, so now
  adding the guard.

● Update(src/CourierKata/Cart.cs)
  ⎿  Added 14 lines, removed 1 line
       6      private readonly List<Parcel> _parcels = [];
       7  
       8      /// <summary>How fast the order is shipped. Defaults to <se
          e cref="Shipping.Standard"/>.</summary>
       9 -    public Shipping Shipping { get; set; } = Shipping.Standard;
       9 +    /// <exception cref="ArgumentOutOfRangeException">The value
         + is not a defined <see cref="CourierKata.Shipping"/>.</exceptio
         +n>                                                             
      10 +    public Shipping Shipping                                   
      11 +    {                                                          
      12 +        get;                                                   
      13 +        set                                                    
      14 +        {                                                      
      15 +            if (!Enum.IsDefined(value))                        
      16 +            {                                                  
      17 +                throw new ArgumentOutOfRangeException(nameof(va
         +lue), value, "Unknown shipping option.");                      
      18 +            }                                                  
      19  
      20 +            field = value;                                     
      21 +        }                                                      
      22 +    } = Shipping.Standard;                                     
      23 +                                                               
      24      /// <summary>Adds a parcel to the cart.</summary>
      25      public void AddParcel(Parcel parcel)
      26      {

● Bash(sed -i 's/^- \[ \] 3\.1 /- [x] 3.1 /'
      openspec/changes/speedy-shipping/tasks.md; docker compose run --rm tests
      2>&1 | grep -E "error|warn|Passed!|Failed!|\[FAIL…)
  ⎿  Passed!  - Failed:     0, Passed:    44, Skipped:     0, Total:    44,
      Duration: 47 ms - CourierKata.Tests.dll (net10.0)
  ⎿  Updated openspec/changes/speedy-shipping/tasks.md (+1 -1)
      25  
      26  ## 3. Shipping option on the cart
      27  
      28 -- [ ] 3.1 Add tests for these cases, implementing anything stil
         -l missing:                                                     
      28 +- [x] 3.1 Add tests for these cases, implementing anything stil
         +l missing:                                                     
      29    - a new cart defaults to `Standard`
      30    - switching from Speedy back to Standard removes the speedy i
          tem
      31    - selecting Speedy before adding parcels (Small and XL: speed
          y $28, total $56)

● Task 3.3: adding the assumptions and committing.

● Bash(cat >> docs/assumptions.md <<'EOF'…)
  ⎿  30ea2b7 Validate the cart's shipping option
  ⎿  Updated docs/assumptions.md (+21 -0)
      139  - **Assumed:** It has a single speedy line item costing $0 and
            a total of $0.
      140  - **Why:** The rule stays simple: the speedy item appears exac
           tly when speedy
      141    is selected, regardless of the cart's contents.
      142 +                                                              
      143 +### Shipping is chosen on the cart and can be changed back    
      144 +                                                              
      145 +- **Unclear:** How the user selects speedy shipping, and wheth
          +er they can                                                   
      146 +  undo it.                                                    
      147 +- **Assumed:** `Cart.Shipping` is a settable property, `Standa
          +rd` by default.                                               
      148 +  It can be changed in either direction at any time, before or
          + after parcels                                                
      149 +  are added; quotes already taken don't change.               
      150 +- **Why:** Shipping speed is part of the order, like the parce
          +ls. A single                                                  
      151 +  property covers selecting, deselecting and reading back the 
          +choice, and an                                                
      152 +  enum leaves room for other speeds.                          
      153 +                                                              
      154 +### Undefined shipping values are rejected                    
      155 +                                                              
      156 +- **Unclear:** What happens if a value outside the `Shipping` 
          +enum (such as                                                 
      157 +  `(Shipping)42`) is set.                                     
      158 +- **Assumed:** The setter throws `ArgumentOutOfRangeException`
          + and the current                                              
      159 +  shipping option is kept.                                    
      160 +- **Why:** C# enums accept any underlying integer, so an unche
          +cked value would                                              
      161 +  silently be treated as Standard. Failing early matches how `
          +Parcel` rejects                                               
      162 +  invalid dimensions.                                         
  ⎿  Updated openspec/changes/speedy-shipping/tasks.md (+2 -2)
      32    - changing shipping after a quote leaves the earlier quote un
          changed
      33  
      34    Verify with `docker compose run --rm tests`.
      35 -- [ ] 3.2 Write a failing test that setting `(Shipping)42` thro
         -ws `ArgumentOutOfRangeException` and leaves `Shipping` unchange
         -d. Implement the `Enum.IsDefined` guard in the setter. Verify w
         -ith `docker compose run --rm tests`.                           
      36 -- [ ] 3.3 Add assumptions for choosing shipping on the cart (it
         - can be changed back) and for rejecting undefined `Shipping` va
         -lues. Commit them with the code.                               
      35 +- [x] 3.2 Write a failing test that setting `(Shipping)42` thro
         +ws `ArgumentOutOfRangeException` and leaves `Shipping` unchange
         +d. Implement the `Enum.IsDefined` guard in the setter. Verify w
         +ith `docker compose run --rm tests`.                           
      36 +- [x] 3.3 Add assumptions for choosing shipping on the cart (it
         + can be changed back) and for rejecting undefined `Shipping` va
         +lues. Commit them with the code.                               
      37  
      38  ## 4. Final checks
      39  

● Task 4.1: final checks. I'm checking the public surface, and also whether the
  line-item hierarchy really is closed to code outside the library.

  Ran 1 shell command (ctrl+o to expand)

● A plain private protected doesn't seal the hierarchy, since records generate a
  protected copy constructor an outside assembly can derive through, and the
  probe built. I'm switching to an internal abstract member that outside types
  can't implement. · summary

● Write(src/CourierKata/LineItem.cs)
  ⎿  Added 6 lines
      10  
      11      /// <summary>Cost in dollars.</summary>
      12      public decimal Cost { get; }
      13 +                                                               
      14 +    // Records always have a protected copy constructor, so ano
         +ther assembly                                                  
      15 +    // could still derive from LineItem through it. An internal
         + abstract member                                               
      16 +    // can only be implemented inside this assembly, which keep
         +s the set of                                                   
      17 +    // line-item kinds closed.                                 
      18 +    internal abstract void SealHierarchy();                    
      19  }
● Write(src/CourierKata/ParcelLineItem.cs)
  ⎿  Added 6 lines, removed 1 line
      1  namespace CourierKata;
      2  
      3  /// <summary>One parcel in a quote, with the size it is charged 
         at and its cost in dollars.</summary>
      4 -public sealed record ParcelLineItem(Parcel Parcel, ParcelSize Si
        -ze, decimal Cost) : LineItem(Cost);                             
      4 +public sealed record ParcelLineItem(Parcel Parcel, ParcelSize Si
        +ze, decimal Cost) : LineItem(Cost)                              
      5 +{                                                               
      6 +    internal override void SealHierarchy()                      
      7 +    {                                                           
      8 +    }                                                           
      9 +}                                                               

● Write(src/CourierKata/SpeedyShippingLineItem.cs)
  ⎿  Added 6 lines, removed 1 line
      1  namespace CourierKata;
      2  
      3  /// <summary>The extra charge for speedy shipping, in dollars.</
         summary>
      4 -public sealed record SpeedyShippingLineItem(decimal Cost) : Line
        -Item(Cost);                                                     
      4 +public sealed record SpeedyShippingLineItem(decimal Cost) : Line
        +Item(Cost)                                                      
      5 +{                                                               
      6 +    internal override void SealHierarchy()                      
      7 +    {                                                           
      8 +    }                                                           
      9 +}                                                               

  Ran 1 shell command (ctrl+o to expand)

● The probe now fails with CS0534 and all 44 tests pass. Updating design.md to
  match.

  Read 1 file (ctrl+o to expand)

● Update(openspec/changes/speedy-shipping/design.md)
  ⎿  Added 2 lines, removed 2 lines
      19  
      20  ### Line items become a closed hierarchy
      21  ```
      22 -public abstract record LineItem(decimal Cost)            // pri
         -vate protected constructor                                     
      22 +public abstract record LineItem      // private protected LineI
         +tem(decimal cost); internal abstract void SealHierarchy();     
      23  public sealed record ParcelLineItem(Parcel Parcel, ParcelSize S
          ize, decimal Cost) : LineItem(Cost)
      24  public sealed record SpeedyShippingLineItem(decimal Cost) : Lin
          eItem(Cost)
      25  ```
      26  - Speedy is listed as a separate item in the same `Items` list,
           so `Quote` keeps its shape and `Total` stays the sum of `Items
          `.
      27 -- The base constructor is `private protected`, so only this ass
         -embly can add item kinds and callers can rely on there being ex
         -actly two.                                                     
      27 +- Only this assembly can add item kinds, so callers can rely on
         + there being exactly two. A `private protected` constructor is 
         +not enough on its own: records always get a `protected` copy co
         +nstructor, and another assembly can derive through it (checked 
         +with a probe project). An `internal abstract` member closes tha
         +t gap, because a type outside the assembly can't implement it a
         +nd fails with CS0534.                                          
      28  - The name `LineItem` stays on the base so that code reading on
          ly `Cost` keeps compiling. The parcel-specific fields move to `
          ParcelLineItem`.
      29  - Alternatives considered: a separate `Quote.SpeedyShipping` me
          mber (simpler for callers, but less literal to "listed as a sep
          arate item"), and a separate list of extras. The user chose the
           hierarchy.
      30  

● Bash(sed -i 's/^- \[ \] 4\.1 /- [x] 4.1 /'
      openspec/changes/speedy-shipping/tasks.md
      sed -i 's/^- \[x\] 1\.1 Make `LineItem` an abstract record with `Cost` and
      a `pr…)
  ⎿  a46f405 Close the line-item hierarchy to other assemblies
     30ea2b7 Validate the cart's shipping option
     26bdd9e Add speedy shipping as a separate quote line item
     … +4 lines (ctrl+o to expand)
  ⎿  Updated openspec/changes/speedy-shipping/tasks.md (+2 -2)
      2  
      3  ## 1. Split line items into a closed hierarchy (refactor, no beh
         aviour change)
      4  
      5 -- [x] 1.1 Make `LineItem` an abstract record with `Cost` and a `
        -private protected` constructor, add `sealed record ParcelLineIte
        -m(Parcel, ParcelSize, Cost) : LineItem`, and make `ParcelPricing
        -.Price` return `ParcelLineItem`. Update the existing tests (`Car
        -tTests`, `ParcelCostTests`, `ParcelSizeTests`) to use `ParcelLin
        -eItem` where they read `Size` or `Parcel`. Verify that `docker c
        -ompose run --rm tests` passes with no other test changes and zer
        -o warnings.                                                     
      5 +- [x] 1.1 Make `LineItem` an abstract record with `Cost` and a `
        +private protected` constructor (closed with an `internal abstrac
        +t` member in 4.1, see design.md), add `sealed record ParcelLineI
        +tem(Parcel, ParcelSize, Cost) : LineItem`, and make `ParcelPrici
        +ng.Price` return `ParcelLineItem`. Update the existing tests (`C
        +artTests`, `ParcelCostTests`, `ParcelSizeTests`) to use `ParcelL
        +ineItem` where they read `Size` or `Parcel`. Verify that `docker
        + compose run --rm tests` passes with no other test changes and z
        +ero warnings.                                                   
      6  - [x] 1.2 Commit the refactor.
      7  
      8  ## 2. Speedy shipping pricing (test first, starting with the bri
         ef's example)
     ...
      37  
      38  ## 4. Final checks
      39  
      40 -- [ ] 4.1 Check the public surface: it should be only `Cart`, `
         -Parcel`, `ParcelSize`, `Shipping`, `LineItem`, `ParcelLineItem`
         -, `SpeedyShippingLineItem` and `Quote`. Check that every scenar
         -io in `specs/parcel-pricing/spec.md` has a matching test. Verif
         -y that `docker compose run --rm tests` passes with zero warning
         -s.                                                             
      40 +- [x] 4.1 Check the public surface: it should be only `Cart`, `
         +Parcel`, `ParcelSize`, `Shipping`, `LineItem`, `ParcelLineItem`
         +, `SpeedyShippingLineItem` and `Quote`. Check that every scenar
         +io in `specs/parcel-pricing/spec.md` has a matching test. Verif
         +y that `docker compose run --rm tests` passes with zero warning
         +s.                                                             
      41  
      42  ## Workflow follow-up
      43  

● Speedy shipping is built and all 9 tasks in speedy-shipping are done. The full
  test suite passes in Docker: 44 tests, zero warnings.

  What callers see

  var cart = new Cart { Shipping = Shipping.Speedy };
  cart.AddParcel(...);            // one of each size
  cart.GetQuote();
  // Items: Parcel Small $3 | Medium $8 | Large $15 | ExtraLarge $25 |
  SpeedyShippingLineItem $51
  // Total: $102

  - Speedy pricing: your $51 / $102 example was the first test, and it failed
    before I wrote any code. The speedy item costs the same as all the parcels
    together and comes after them, and parcel prices don't change. Standard
    shipping has no speedy item, and an empty cart with speedy gets a $0 speedy
    item.
  - Choosing shipping: Cart.Shipping starts as Standard. It can be switched to
    Speedy and back, before or after adding parcels, and quotes already taken
    don't change. Setting a value like (Shipping)42 throws
    ArgumentOutOfRangeException and keeps the current setting.
  - Breaking change: existing tests that read Size or Parcel from a line item
    now use the new ParcelLineItem type.
  - Assumptions: six new entries in docs/assumptions.md, each committed with the
    code that depends on it.

  One problem with the design, now fixed. The design relied on a private
  protected constructor to stop outside code adding new kinds of line item. I
  built a small separate project to test this, and it could still create its own
  line item, because C# records always get a protected copy constructor. I
  added an internal abstract member in LineItem.cs (src/CourierKata/LineItem.cs)
  that outside code can't implement, and the same test project now fails to
  compile. design.md has been updated to match.

  The tests for task groups 2 and 3, other than the invalid-value check, passed
  as soon as I wrote them. The code from the first test already handled those
  cases.

  Commits

  a46f405 Close the line-item hierarchy to other assemblies
  30ea2b7 Validate the cart's shipping option
  26bdd9e Add speedy shipping as a separate quote line item
  31a7b54 Split LineItem into a closed line-item hierarchy
  4351976 Propose speedy shipping change

  Following CLAUDE.md, these commits have no Claude co-author line, although the
  earlier 48f692b commit has one.

  The change hasn't been archived yet. Run /opsx:archive when you're happy with
  it; that updates the main parcel-pricing spec and makes the commit CLAUDE.md
  asks for.

❯ /opsx:archive

● Using change: speedy-shipping (override with /opsx:archive <other>).

  Ran 1 shell command (ctrl+o to expand)

● All tasks and artifacts are done; syncing would add 2 requirements to the main
  parcel-pricing spec and modify 2 others, each getting new wording and a
  scenario. · summary

● User answered Claude's questions:
  ⎿  · The speedy-shipping changes aren't in the main parcel-pricing spec yet: 2
     new requirements and 2 modified ones, each modified one gaining a
     scenario. Sync them into the main spec before archiving? → Sync now
     (Recommended)

  Read 1 file, ran 1 shell command (ctrl+o to expand)

● Update(openspec/specs/parcel-pricing/spec.md)
  ⎿  Added 5 lines, removed 1 line
      67  - **THEN** the quote contains two Small line items
      68  
      69  ### Requirement: Quote lists each parcel with its size and cost
      70 -A quote SHALL contain one line item per parcel in the cart, in 
         -the order the parcels were added. Each line item MUST show the 
         -parcel, its size and its cost.                                 
      70 +A quote SHALL contain one parcel line item per parcel in the ca
         +rt, in the order the parcels were added. Each parcel line item 
         +MUST show the parcel, its size and its cost. When speedy shippi
         +ng is selected, its line item MUST come after all the parcel li
         +ne items.                                                      
      71  
      72  #### Scenario: Line items follow insertion order
      73  - **WHEN** an XL, a Small and a Medium parcel are added in that
           order
      74  - **THEN** the quote's line items are XL $25, Small $3, Medium 
          $8 in that order
      75  
      76 +#### Scenario: Speedy shipping item comes last                 
      77 +- **WHEN** a Medium and a Small parcel are added in that order 
         +with Speedy shipping                                           
      78 +- **THEN** the quote's line items are Medium $8, Small $3, spee
         +dy shipping $11 in that order                                  
      79 +                                                               
      80  ### Requirement: Quote includes the total cost
      81  A quote SHALL include a total equal to the sum of its line-item
           costs.
      82  

● Update(openspec/specs/parcel-pricing/spec.md)
  ⎿  Added 43 lines, removed 1 line
      89  - **THEN** the quote has no line items and a total of $0
      90  
      91  ### Requirement: Requesting a quote does not change the cart
      92 -Requesting a quote SHALL NOT modify the cart. Later quotes MUST
         - reflect any parcels added since.                              
      92 +Requesting a quote SHALL NOT modify the cart. Later quotes MUST
         + reflect any parcels added and any shipping option changed sinc
         +e.                                                             
      93  
      94  #### Scenario: Quoting twice
      95  - **WHEN** a quote is requested twice for the same cart with no
           changes in between
     ...
       98  #### Scenario: Adding after a quote
       99  - **WHEN** a quote is requested, then another parcel is added,
            then a second quote is requested
      100  - **THEN** the second quote includes the new parcel and the fi
           rst quote is unchanged
      101 +                                                              
      102 +#### Scenario: Changing shipping after a quote                
      103 +- **WHEN** a quote is requested with Standard shipping, then S
          +peedy is selected, then a second quote is requested           
      104 +- **THEN** the second quote includes the speedy shipping line 
          +item and the first quote is unchanged                         
      105 +                                                              
      106 +### Requirement: Shipping option is chosen on the cart        
      107 +A cart SHALL have a shipping option of either Standard or Spee
          +dy, which is Standard by default. The shipping option MAY be c
          +hanged at any time, in either direction, and the cart MUST rep
          +ort its current option. Setting an option that is neither Stan
          +dard nor Speedy MUST fail with an out-of-range error. The ship
          +ping option and the parcels can be set in any order without ch
          +anging the quote.                                             
      108 +                                                              
      109 +#### Scenario: Default is Standard                            
      110 +- **WHEN** a new cart is created                              
      111 +- **THEN** its shipping option is Standard                    
      112 +                                                              
      113 +#### Scenario: Switching back to Standard removes speedy shipp
          +ing                                                           
      114 +- **WHEN** a cart's shipping option is set to Speedy and then 
          +back to Standard, and a quote is requested                    
      115 +- **THEN** the quote has no speedy shipping line item and its 
          +total is the sum of the parcel costs                          
      116 +                                                              
      117 +#### Scenario: Undefined shipping option is rejected          
      118 +- **WHEN** a cart's shipping option is set to a value that is 
          +neither Standard nor Speedy                                   
      119 +- **THEN** the change fails with an out-of-range error and the
          + shipping option is unchanged                                 
      120 +                                                              
      121 +#### Scenario: Selecting Speedy before adding parcels         
      122 +- **WHEN** Speedy is selected on an empty cart and then a Smal
          +l and an XL parcel are added                                  
      123 +- **THEN** the quote's speedy shipping line item costs $28 and
          + the total is $56                                             
      124 +                                                              
      125 +### Requirement: Speedy shipping doubles the order cost       
      126 +When the shipping option is Speedy, the quote SHALL include ex
          +actly one speedy shipping line item that costs the sum of the 
          +parcel line-item costs. Each parcel line item MUST cost the sa
          +me as it would with Standard shipping. When the shipping optio
          +n is Standard, the quote MUST NOT include a speedy shipping li
          +ne item.                                                      
      127 +                                                              
      128 +#### Scenario: Speedy shipping on one parcel of each size     
      129 +- **WHEN** a Small, a Medium, a Large and an XL parcel are add
          +ed, Speedy is selected, and a quote is requested              
      130 +- **THEN** the parcel line items cost $3, $8, $15 and $25, the
          + speedy shipping line item costs $51, and the total is $102   
      131 +                                                              
      132 +#### Scenario: Standard shipping has no speedy item           
      133 +- **WHEN** a Small parcel is added with Standard shipping and 
          +a quote is requested                                          
      134 +- **THEN** the quote has only the parcel line item and its tot
          +al is $3                                                      
      135 +                                                              
      136 +#### Scenario: Parcel costs are unaffected by speedy shipping 
      137 +- **WHEN** the same parcels are quoted once with Standard and 
          +once with Speedy shipping                                     
      138 +- **THEN** the parcel line items in both quotes are equal     
      139 +                                                              
      140 +#### Scenario: Speedy shipping on an empty cart               
      141 +- **WHEN** Speedy is selected on a cart with no parcels and a 
          +quote is requested                                            
      142 +- **THEN** the quote has a single speedy shipping line item co
          +sting $0 and a total of $0                                    

  Ran 1 shell command (ctrl+o to expand)

● The sync is verified: all four changed requirements match the change, the
  Purpose section is untouched, and strict validation passes. I'm archiving and
  committing next. · summary

● Bash(T=openspec/changes/archive/2026-11-11-speedy-shipping; test -e $T && echo
      EXISTS || (mv openspec/changes/speedy-shipping $T && git add -A openspec
      && git commit…)
  ⎿  bd7ca18 Archive speedy shipping change and sync parcel-pricing spec
     []
  ⎿  Created
     openspec/changes/archive/2026-11-11-speedy-shipping/proposal.md (+32
     -0)
       1 +# Proposal                                                     
       2 +                                                               
       3 +## Why                                                         
       4 +                                                               
       5 +Logistics improvements let us deliver parcels faster, and custo
         +mers will pay more for that. Customers need a way to choose fas
         +ter delivery for an order, and the quote must show what that ch
         +oice costs.                                                    
       6 +                                                               
       7 +## What Changes                                                
       8 +                                                               
       9 +- Add a shipping choice to the cart: Standard (the default) or 
         +Speedy. The customer can change it at any time before requestin
         +g a quote, including switching back to Standard.               
      10 +- Speedy shipping doubles the cost of the order. It appears in 
         +the quote as its own line item, after the parcel line items, co
         +sting the sum of the parcel costs. The quote total is still the
         + sum of all line items, so it doubles.                         
      11 +- Parcel line items keep their normal cost whether or not Speed
         +y is selected.                                                 
      12 +- With Standard shipping, the quote has no speedy line item. Wi
         +th Speedy and an empty cart, the quote has a $0 speedy line ite
         +m.                                                             
      13 +- **BREAKING**: The quote's line items are no longer all parcel
         + items. `LineItem` becomes a base type with a parcel item kind 
         +and a speedy shipping item kind. Callers that read a line item'
         +s parcel or size must first check that it is a parcel item.    
      14 +- Record the new interpretations in `docs/assumptions.md`.     
      15 +                                                               
      16 +Out of scope: weight, discounts, other shipping speeds, and how
         + speedy shipping combines with rules added in later steps.     
      17 +                                                               
      18 +## Capabilities                                                
      19 +                                                               
      20 +### New Capabilities                                           
      21 +                                                               
      22 +None.                                                          
      23 +                                                               
      24 +### Modified Capabilities                                      
      25 +- `parcel-pricing`: the quote can include a speedy shipping lin
         +e item after the parcel items. A new requirement covers choosin
         +g the shipping option on the cart, and another covers how speed
         +y shipping is priced. The snapshot requirement gains a scenario
         + for changing the shipping option after a quote.               
      26 +                                                               
      27 +## Impact                                                      
      28 +                                                               
      29 +- Public API: adds a `Shipping` enum and a `Cart.Shipping` prop
         +erty. `LineItem` becomes an abstract base type with `ParcelLine
         +Item` and `SpeedyShippingLineItem` under it. `Quote`'s shape is
         + unchanged.                                                    
      30 +- Internal: `ParcelPricing` returns parcel line items. `Cart.Ge
         +tQuote` appends the speedy item when Speedy is selected.       
      31 +- Tests: existing tests that read `Size` or `Parcel` from a lin
         +e item change to use `ParcelLineItem`.                         
      32 +- Docs: new entries in `docs/assumptions.md`.                  
  ⎿  Created
     openspec/changes/archive/2026-11-11-speedy-shipping/.openspec.yaml (+2
     -0)
      1 +schema: spec-driven                                             
      2 +created: 2026-11-11                                             
  ⎿  Created openspec/changes/archive/2026-11-11-speedy-shipping/tasks.md
     (+45 -0)
       1 +# Tasks                                                        
       2 +                                                               
       3 +## 1. Split line items into a closed hierarchy (refactor, no be
         +haviour change)                                                
       4 +                                                               
       5 +- [x] 1.1 Make `LineItem` an abstract record with `Cost` and a 
         +`private protected` constructor (closed with an `internal abstr
         +act` member in 4.1, see design.md), add `sealed record ParcelLi
         +neItem(Parcel, ParcelSize, Cost) : LineItem`, and make `ParcelP
         +ricing.Price` return `ParcelLineItem`. Update the existing test
         +s (`CartTests`, `ParcelCostTests`, `ParcelSizeTests`) to use `P
         +arcelLineItem` where they read `Size` or `Parcel`. Verify that 
         +`docker compose run --rm tests` passes with no other test chang
         +es and zero warnings.                                          
       6 +- [x] 1.2 Commit the refactor.                                 
       7 +                                                               
       8 +## 2. Speedy shipping pricing (test first, starting with the br
         +ief's example)                                                 
       9 +                                                               
      10 +- [x] 2.1 Write a failing test: one parcel of each size with `c
         +art.Shipping = Shipping.Speedy` gives parcel items of $3, $8, $
         +15 and $25, then a `SpeedyShippingLineItem` of $51, and a total
         + of $102. Add the `Shipping` enum, the `Cart.Shipping` property
         + (default `Standard`), `SpeedyShippingLineItem`, and the speedy
         + item in `Cart.GetQuote()` to make it pass. Verify with `docker
         + compose run --rm tests`.                                      
      11 +- [x] 2.2 Add tests for these cases, implementing anything stil
         +l missing:                                                     
      12 +  - standard shipping has no speedy item (Small only: total $3)
      13 +  - parcel items are equal under Standard and Speedy           
      14 +  - the speedy item comes last (Medium, Small with Speedy: $8, 
         +$3, speedy $11)                                                
      15 +  - an empty cart with Speedy has a single $0 speedy item and a
         + $0 total                                                      
      16 +                                                               
      17 +  Verify with `docker compose run --rm tests`.                 
      18 +- [x] 2.3 Add these assumptions to `docs/assumptions.md`:      
      19 +  - the speedy charge equals the parcel subtotal               
      20 +  - the speedy item comes last                                 
      21 +  - standard shipping shows no speedy item                     
      22 +  - an empty cart with Speedy shows a $0 speedy item           
      23 +                                                               
      24 +  Commit them with the code.                                   
      25 +                                                               
      26 +## 3. Shipping option on the cart                              
      27 +                                                               
      28 +- [x] 3.1 Add tests for these cases, implementing anything stil
         +l missing:                                                     
      29 +  - a new cart defaults to `Standard`                          
      30 +  - switching from Speedy back to Standard removes the speedy i
         +tem                                                            
      31 +  - selecting Speedy before adding parcels (Small and XL: speed
         +y $28, total $56)                                              
      32 +  - changing shipping after a quote leaves the earlier quote un
         +changed                                                        
      33 +                                                               
      34 +  Verify with `docker compose run --rm tests`.                 
      35 +- [x] 3.2 Write a failing test that setting `(Shipping)42` thro
         +ws `ArgumentOutOfRangeException` and leaves `Shipping` unchange
         +d. Implement the `Enum.IsDefined` guard in the setter. Verify w
         +ith `docker compose run --rm tests`.                           
      36 +- [x] 3.3 Add assumptions for choosing shipping on the cart (it
         + can be changed back) and for rejecting undefined `Shipping` va
         +lues. Commit them with the code.                               
      37 +                                                               
      38 +## 4. Final checks                                             
      39 +                                                               
      40 +- [x] 4.1 Check the public surface: it should be only `Cart`, `
         +Parcel`, `ParcelSize`, `Shipping`, `LineItem`, `ParcelLineItem`
         +, `SpeedyShippingLineItem` and `Quote`. Check that every scenar
         +io in `specs/parcel-pricing/spec.md` has a matching test. Verif
         +y that `docker compose run --rm tests` passes with zero warning
         +s.                                                             
     … 5 more lines (ctrl+o to expand)
  ⎿  Created openspec/changes/archive/2026-11-11-speedy-shipping/design.md
     (+66 -0)
       1 +# Design                                                       
       2 +                                                               
       3 +## Context                                                     
       4 +                                                               
       5 +See proposal.md for the motivation. Today `LineItem(Parcel, Siz
         +e, Cost)` is a sealed record that only describes parcels, and `
         +Quote(Items, Total)` totals the costs of its items. `ParcelPric
         +ing` (internal) prices one parcel, and `Cart.GetQuote()` maps p
         +arcels to line items. Speedy shipping is the first line item th
         +at isn't a parcel, so the line-item model has to change.       
       6 +                                                               
       7 +## Goals / Non-Goals                                           
       8 +                                                               
       9 +**Goals:**                                                     
      10 +- Speedy shipping appears as its own line item, and the total s
         +tays "the sum of the line items".                              
      11 +- Parcel pricing code does not know about speedy shipping.     
      12 +- A closed set of line-item kinds that callers can match on exh
         +austively.                                                     
      13 +                                                               
      14 +**Non-Goals:**                                                 
      15 +- A general list of surcharges or discounts, or a pricing pipel
         +ine. Later steps (weight, discounts) will add these when they n
         +eed them.                                                      
      16 +- Setting the order in which order-level rules apply. Today spe
         +edy doubles the parcel subtotal and nothing else.              
      17 +                                                               
      18 +## Decisions                                                   
      19 +                                                               
      20 +### Line items become a closed hierarchy                       
      21 +```                                                            
      22 +public abstract record LineItem      // private protected LineI
         +tem(decimal cost); internal abstract void SealHierarchy();     
      23 +public sealed record ParcelLineItem(Parcel Parcel, ParcelSize S
         +ize, decimal Cost) : LineItem(Cost)                            
      24 +public sealed record SpeedyShippingLineItem(decimal Cost) : Lin
         +eItem(Cost)                                                    
      25 +```                                                            
      26 +- Speedy is listed as a separate item in the same `Items` list,
         + so `Quote` keeps its shape and `Total` stays the sum of `Items
         +`.                                                             
      27 +- Only this assembly can add item kinds, so callers can rely on
         + there being exactly two. A `private protected` constructor is 
         +not enough on its own: records always get a `protected` copy co
         +nstructor, and another assembly can derive through it (checked 
         +with a probe project). An `internal abstract` member closes tha
         +t gap, because a type outside the assembly can't implement it a
         +nd fails with CS0534.                                          
      28 +- The name `LineItem` stays on the base so that code reading on
         +ly `Cost` keeps compiling. The parcel-specific fields move to `
         +ParcelLineItem`.                                               
      29 +- Alternatives considered: a separate `Quote.SpeedyShipping` me
         +mber (simpler for callers, but less literal to "listed as a sep
         +arate item"), and a separate list of extras. The user chose the
         + hierarchy.                                                    
      30 +                                                               
      31 +### Shipping option lives on the cart                          
      32 +```                                                            
      33 +public enum Shipping { Standard, Speedy }                      
      34 +public sealed class Cart { public Shipping Shipping { get; set;
         + } = Shipping.Standard; ... }                                  
      35 +```                                                            
      36 +- Shipping is part of the order, alongside the parcels. One set
         +table property handles selecting, deselecting and reading back 
         +the choice.                                                    
      37 +- The setter rejects undefined enum values (`Enum.IsDefined`) w
         +ith `ArgumentOutOfRangeException` and leaves the current value 
         +as it was. `Parcel` rejects invalid dimensions in the same way.
      38 +- The enum is named `Shipping`, the same as the property. This 
         +is the usual C# "Color Color" pattern and resolves cleanly.    
      39 +- Alternatives considered: a `GetQuote(Shipping)` parameter, se
         +lect and deselect methods, and a `SelectShipping(Shipping)` met
         +hod. The user chose the property.                              
      40 +                                                               
     … 26 more lines (ctrl+o to expand)
  ⎿  Created openspec/changes/archive/2026-11-11-speedy-shipping/specs/parc
     el-pricing/spec.md (+69 -0)
       1 +# Spec Delta                                                   
       2 +                                                               
       3 +## ADDED Requirements                                          
       4 +                                                               
       5 +### Requirement: Shipping option is chosen on the cart         
       6 +A cart SHALL have a shipping option of either Standard or Speed
         +y, which is Standard by default. The shipping option MAY be cha
         +nged at any time, in either direction, and the cart MUST report
         + its current option. Setting an option that is neither Standard
         + nor Speedy MUST fail with an out-of-range error. The shipping 
         +option and the parcels can be set in any order without changing
         + the quote.                                                    
       7 +                                                               
       8 +#### Scenario: Default is Standard                             
       9 +- **WHEN** a new cart is created                               
      10 +- **THEN** its shipping option is Standard                     
      11 +                                                               
      12 +#### Scenario: Switching back to Standard removes speedy shippi
         +ng                                                             
      13 +- **WHEN** a cart's shipping option is set to Speedy and then b
         +ack to Standard, and a quote is requested                      
      14 +- **THEN** the quote has no speedy shipping line item and its t
         +otal is the sum of the parcel costs                            
      15 +                                                               
      16 +#### Scenario: Undefined shipping option is rejected           
      17 +- **WHEN** a cart's shipping option is set to a value that is n
         +either Standard nor Speedy                                     
      18 +- **THEN** the change fails with an out-of-range error and the 
         +shipping option is unchanged                                   
      19 +                                                               
      20 +#### Scenario: Selecting Speedy before adding parcels          
      21 +- **WHEN** Speedy is selected on an empty cart and then a Small
         + and an XL parcel are added                                    
      22 +- **THEN** the quote's speedy shipping line item costs $28 and 
         +the total is $56                                               
      23 +                                                               
      24 +### Requirement: Speedy shipping doubles the order cost        
      25 +When the shipping option is Speedy, the quote SHALL include exa
         +ctly one speedy shipping line item that costs the sum of the pa
         +rcel line-item costs. Each parcel line item MUST cost the same 
         +as it would with Standard shipping. When the shipping option is
         + Standard, the quote MUST NOT include a speedy shipping line it
         +em.                                                            
      26 +                                                               
      27 +#### Scenario: Speedy shipping on one parcel of each size      
      28 +- **WHEN** a Small, a Medium, a Large and an XL parcel are adde
         +d, Speedy is selected, and a quote is requested                
      29 +- **THEN** the parcel line items cost $3, $8, $15 and $25, the 
         +speedy shipping line item costs $51, and the total is $102     
      30 +                                                               
      31 +#### Scenario: Standard shipping has no speedy item            
      32 +- **WHEN** a Small parcel is added with Standard shipping and a
         + quote is requested                                            
      33 +- **THEN** the quote has only the parcel line item and its tota
         +l is $3                                                        
      34 +                                                               
      35 +#### Scenario: Parcel costs are unaffected by speedy shipping  
      36 +- **WHEN** the same parcels are quoted once with Standard and o
         +nce with Speedy shipping                                       
      37 +- **THEN** the parcel line items in both quotes are equal      
      38 +                                                               
      39 +#### Scenario: Speedy shipping on an empty cart                
      40 +- **WHEN** Speedy is selected on a cart with no parcels and a q
         +uote is requested                                              
     … 29 more lines (ctrl+o to expand)
  ⎿  … 5 more files changed

● I've added the speedy-shipping requirements to the main spec, archived the
  change and committed both.

  - Archived to: openspec/changes/archive/2026-11-11-speedy-shipping/
  - Main spec: openspec/specs/parcel-pricing/spec.md has two new requirements:
    "Shipping option is chosen on the cart" and "Speedy shipping doubles the
    order cost". Two existing requirements were updated, each with one new
    scenario: "Quote lists each parcel…" now says speedy comes last, and
    "Requesting a quote does not change the cart" now covers changing the
    shipping option. I checked that every changed requirement appears in the
    main spec exactly as written in the change, and the spec passes openspec
    validate --strict.
  - Commit: bd7ca18 Archive speedy shipping change and sync parcel-pricing spec.
    The working tree is clean and there are no active changes left.
  - No warnings: all 4 planning artifacts and all 9 tasks were complete.

  The spec's Purpose line still says "producing a quote with per-parcel costs
  and a total" and doesn't mention shipping options. The archive process leaves
  an existing Purpose untouched, so I didn't change it. If you want it updated,
  that's a one-line edit to the main spec.

❯ update it

  Ran 1 shell command (ctrl+o to expand)

● Update(openspec/specs/parcel-pricing/spec.md)
  ⎿  Added 1 line, removed 1 line
      2  
      3  ## Purpose
      4  
      5 -Prices a cart of parcels for delivery by classifying each parcel
        - by its size and producing a quote with per-parcel costs and a t
        -otal.                                                           
      5 +Prices a cart of parcels for delivery by classifying each parcel
        + by its size and producing a quote with per-parcel costs, any ch
        +arge for the chosen shipping option, and a total.               
      6  
      7  ## Requirements
      8  

  Ran 1 shell command (ctrl+o to expand)

● I updated the Purpose line in openspec/specs/parcel-pricing/spec.md to mention
  the shipping charge:

  ▎ Prices a cart of parcels for delivery by classifying each parcel by its size
  ▎ and producing a quote with per-parcel costs, any charge for the chosen
  ▎ shipping option, and a total.

  I said "the chosen shipping option" rather than naming speedy shipping, so the
  line won't need rewording if another shipping speed is added later. The spec
  passes openspec validate --strict, and the change is committed as 0e44454
  Mention shipping charges in parcel-pricing spec purpose. The working tree is
  clean.

❯ add, commit

  Ran 1 shell command (ctrl+o to expand)

● The Purpose change is already committed as 0e44454 Mention shipping charges in
  parcel-pricing spec purpose. I committed it right after the edit, and the
  working tree is clean, so there's nothing left to add.

❯ make a new branch for these changes, move master back to
  48f692b344e71edd33e7bb8608b2b70effd11776
  then squash and merge the new branch into master

  Ran 1 shell command (ctrl+o to expand)

● There's no remote and the tree is clean. Following the existing
  parcel-size-pricing naming, I'll call the branch speedy-shipping.
  Ran 1 shell command (ctrl+o to expand)

● master now has all the speedy shipping work as one commit on top of 48f692b:

  9cb886b Add speedy shipping          <- master
  48f692b Add parcel pricing by size

  - New branch: speedy-shipping holds the original 7 commits, from "Propose
    speedy shipping change" to "Mention shipping charges in parcel-pricing spec
    purpose".
  - Same content: the files on master exactly match the tip of speedy-shipping.
    Tests passed on that version, so I didn't run them again.
  - Commit message: it summarises the change: the Shipping option, the speedy
    line item, the closed line-item types, and the updates to assumptions, spec
    and archive. Per CLAUDE.md, it has no Claude attribution line.
  - Nothing pushed: the repo has no remote, so moving master back didn't rewrite
    anything shared.

  The working tree is clean. I haven't deleted the speedy-shipping branch. Since
  the squash, Git no longer counts it as merged, so removing it needs git
  branch -D speedy-shipping.