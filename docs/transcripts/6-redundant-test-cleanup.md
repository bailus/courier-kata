
❯ review all the tests, suggest tests that can be deleted

  Read 1 file, ran 6 shell commands (ctrl+o to expand)

● I read all 14 test files and checked each test against the code and the specs.
  Twelve tests and two single assertions can go without losing coverage,
  because another test runs the same input with the same or stronger checks. A
  few more are up to you. I haven't changed anything.

  One catch: almost every test here matches a named scenario in openspec/specs/.
  If you delete a test, also delete or merge its scenario. Otherwise the spec
  lists scenarios that no test covers by name.

  Safe to delete (another test already covers it)

  Discounts
  - DiscountCombinationTests.cs:34
    (tests/CourierKata.Tests/DiscountCombinationTests.cs#L34)
    The_same_parcel_added_four_times_counts_four_times:
    Four_small_parcels_get_small_parcel_mania (DiscountTests.cs:8
    (tests/CourierKata.Tests/DiscountTests.cs#L8)) already uses Times(4, Small),
    which is the same instance four times, with the same assertion plus the
    total.
  - DiscountTests.cs:109 (tests/CourierKata.Tests/DiscountTests.cs#L109)
    Parcels_in_a_discount_keep_the_order_they_were_added:
    Cheapest_parcel_is_free_not_the_last_one_added (L52) uses the same cart and
    checks the whole DiscountLineItem. Its equality compares parcels in order,
    which DiscountLineItemTests tests.
  - DiscountTests.cs:141 (tests/CourierKata.Tests/DiscountTests.cs#L141)
    Parcels_in_a_discount_keep_their_cost:
    Discount_is_listed_after_the_parcels... (L95) uses the same cart and checks
    every item against PricedOnTheirOwn, which is exactly "same as with no
    discount".
  - DiscountTests.cs:133 (tests/CourierKata.Tests/DiscountTests.cs#L133)
    Discount_order_does_not_follow_the_size_of_the_saving: this test is also
    weak. It expects $10 then $8, which is descending saving order, so an
    implementation that sorts by saving (largest first) still passes.
    Parcels_grouped_with_others_of_similar_cost (DiscountCombinationTests.cs:8
    (tests/CourierKata.Tests/DiscountCombinationTests.cs#L8)) adds the $8
    parcels first and expects $8 then $10, which is the case that actually
    proves the rule. Either delete this test or swap the input order, and fix
    the spec scenario either way.
  - Single assertion DiscountTests.cs:104
    (tests/CourierKata.Tests/DiscountTests.cs#L104) (Cost == -8): this repeats
    DiscountLineItemTests.Cost_is_minus_the_saving.

  Speedy shipping
  - SpeedyShippingTests.cs:30
    (tests/CourierKata.Tests/SpeedyShippingTests.cs#L30)
    Standard_shipping_has_no_speedy_item: every standard-shipping test that
    checks the full item list covers this, e.g. CartTests.cs:11
    (tests/CourierKata.Tests/CartTests.cs#L11) and ShippingOptionTests.cs:15
    (tests/CourierKata.Tests/ShippingOptionTests.cs#L15).
  - SpeedyShippingTests.cs:39
    (tests/CourierKata.Tests/SpeedyShippingTests.cs#L39)
    Parcel_items_cost_the_same_with_speedy_shipping and L48
    (tests/CourierKata.Tests/SpeedyShippingTests.cs#L48)
    Speedy_shipping_item_comes_after_the_parcel_items: the first test (L11)
    already checks the exact parcel items and that speedy comes last.
  - SpeedyShippingTests.cs:105
    (tests/CourierKata.Tests/SpeedyShippingTests.cs#L105)
    Discounts_are_unaffected_by_speedy_shipping: L85 checks the exact discount
    item under speedy, and it is the same item as in the standard-shipping test.
  - ShippingOptionTests.cs:30
    (tests/CourierKata.Tests/ShippingOptionTests.cs#L30)
    Speedy_can_be_selected_before_adding_parcels: every test in
    SpeedyShippingTests already selects Speedy before adding parcels.
  - HeavyRateTests.cs:67 (tests/CourierKata.Tests/HeavyRateTests.cs#L67) and
    OverweightChargeTests.cs:70
    (tests/CourierKata.Tests/OverweightChargeTests.cs#L70) (speedy doubles
    heavy-rate costs and overweight charges): speedy just sums Cost, whatever
    the rate. The 4 kg Mediums in Speedy_shipping_applies_after_discounts
    already catch a version that leaves out the overweight charge ($30 instead
    of $36). The heavy case can't fail on its own, because its overweight charge
    is $0.

  Pricing and validation
  - OverweightChargeTests.cs:49
    (tests/CourierKata.Tests/OverweightChargeTests.cs#L49)
    Parcel_within_its_limit_shows_a_zero_overweight_charge: CartTests checks the
    same ParcelLineItem(..., 3m, 0m) several times.
  - OverweightChargeTests.cs:60
    (tests/CourierKata.Tests/OverweightChargeTests.cs#L60)
    Heavy_parcel_keeps_the_size_of_its_dimensions:
    Size_rate_is_kept_while_it_is_cheaper (HeavyRateTests.cs:36
    (tests/CourierKata.Tests/HeavyRateTests.cs#L36)) checks a heavier Small
    parcel more strictly.
  - ParcelTests.cs:21 (tests/CourierKata.Tests/ParcelTests.cs#L21)
    Very_large_parcel_is_accepted: the 10,000 cm parcel in the ParcelSizeTests
    extra-large data covers this.
  - ParcelTests.cs:40 (tests/CourierKata.Tests/ParcelTests.cs#L40)
    Fractional_weight_is_accepted: almost every test uses a 0.5 kg parcel.

  Up to you

  - CartTests.cs:55 (tests/CourierKata.Tests/CartTests.cs#L55)
    Quoting_twice_gives_the_same_result: low value, since
    Adding_a_parcel_after_quoting... already covers "quoting doesn't change the
    cart".
  - DiscountTests.cs:17 (tests/CourierKata.Tests/DiscountTests.cs#L17)
    Three_medium_parcels_get_medium_parcel_mania is fully covered by L95, which
    uses the same cart. I'd keep it anyway as the headline example for that
    rule, or merge the two.
  - Leftover single assertions: OverweightChargeTests.cs:13
    (tests/CourierKata.Tests/OverweightChargeTests.cs#L13) repeats the Cost
    check from ParcelLineItemTests. L56
    (tests/CourierKata.Tests/OverweightChargeTests.cs#L56) repeats the
    record-equality check on the line above. The first assertion in
    DiscountTests.cs:76 (tests/CourierKata.Tests/DiscountTests.cs#L76) re-tests
    heavy-rate pricing.

  Keep

  Everything else, notably:
  - DiscountPropertyTests, which checks every grouping on random carts.
  - The tie-break and "large order" tests in DiscountCombinationTests.
  - ParcelSizeTests, ParcelLineItemTests and DiscountLineItemTests, which pin
    the custom equality that most other tests depend on.

  The property test does cover the correctness of many discount example tests,
  but those examples are what the spec scenarios point to, so they're worth
  keeping as documentation.

  If you want, I can make the deletions, update the matching spec scenarios, and
  commit once the tests pass.

❯ <ide_opened_file>The user opened the file
  /home/sam/workspace/courier-kata/README.md in the IDE. This may or may not be
  related to the current task.</ide_opened_file>

❯ do it

  Ran 2 shell commands (ctrl+o to expand)

● Bash(git switch -c trim-redundant-tests && cat >
      /tmp/claude-1000/-home-sam-workspace-courier-kata/d3623e82-f72c-40d4-9326-
      908ac162880d/scratchpad/trim.py <<'EOF'
      im…)
  ⎿  Switched to a new branch 'trim-redundant-tests'
      openspec/specs/parcel-discounts/spec.md            | 12 ------
      openspec/specs/parcel-pricing/spec.md              | 44 -------------
     … +15 lines (ctrl+o to expand)
  ⎿  Updated tests/CourierKata.Tests/DiscountCombinationTests.cs (+0 -10)
  ⎿  Updated tests/CourierKata.Tests/DiscountTests.cs (+0 -29)
  ⎿  Updated tests/CourierKata.Tests/SpeedyShippingTests.cs (+0 -44)
  ⎿  Updated tests/CourierKata.Tests/ShippingOptionTests.cs (+0 -13)
  ⎿  Updated tests/CourierKata.Tests/HeavyRateTests.cs (+0 -16)
  ⎿  … 4 more files changed
  ⎿  (a git step in this command can move the working tree, so these may be
     its changes, not edits) (ctrl+o to expand)
  ⎿  (files that changed while this command ran, by the command or by
     another process; a convenience view, not a review or audit of the
     command)

  Ran 1 shell command (ctrl+o to expand)

● Cleaning up helpers left unused by the deletions.

● Bash(cd tests/CourierKata.Tests && sed -i '/private static readonly Parcel
      ExtraLarge = new(150m, 5m, 5m, 0.5m);/d' ShippingOptionTests.cs && for f
      in HeavyRateTests…)
  ⎿      private static Quote QuoteFor(Parcel parcel)
         {
             var cart = new Cart();
     … +20 lines (ctrl+o to expand)
  ⎿  Updated tests/CourierKata.Tests/ShippingOptionTests.cs (+0 -1)
      3  public class ShippingOptionTests
      4  {
      5      private static readonly Parcel Small = new(5m, 5m, 5m, 0.5m)
         ;
      6 -    private static readonly Parcel ExtraLarge = new(150m, 5m, 5m
        -, 0.5m);                                                        
      6  
      7      [Fact]
      8      public void New_cart_uses_standard_shipping()
  ⎿  Updated tests/CourierKata.Tests/HeavyRateTests.cs (+2 -2)
      63          Assert.Equal(50m, quote.Total);
      64      }
      65  
      66 -    private static Quote QuoteFor(Parcel parcel, Shipping shipp
         -ing = Shipping.Standard)                                       
      66 +    private static Quote QuoteFor(Parcel parcel)               
      67      {
      68 -        var cart = new Cart { Shipping = shipping };           
      68 +        var cart = new Cart();                                 
      69          cart.AddParcel(parcel);
      70          return cart.GetQuote();
      71      }
  ⎿  Updated tests/CourierKata.Tests/OverweightChargeTests.cs (+2 -2)
      45          Assert.Equal(0m, item.OverweightCharge);
      46      }
      47  
      48 -    private static Quote QuoteFor(Parcel parcel, Shipping shipp
         -ing = Shipping.Standard)                                       
      48 +    private static Quote QuoteFor(Parcel parcel)               
      49      {
      50 -        var cart = new Cart { Shipping = shipping };           
      50 +        var cart = new Cart();                                 
      51          cart.AddParcel(parcel);
      52          return cart.GetQuote();
      53      }

  Ran 3 shell commands (ctrl+o to expand)
  ⎿  1 action not run · auto mode's check had no usable answer

● I deleted the 16 redundant tests, removed the matching spec scenarios, and
  committed it all as 1b11eba on a new branch, trim-redundant-tests. You were on
  master, so I branched off it first. All 91 remaining tests pass in Docker,
  and openspec validate --specs passes for both specs.

  Removed:
  - Tests: the 16 from my "safe to delete" list, plus the duplicate Cost == -8
    assertion in DiscountTests.
  - Spec scenarios: 14 (3 in parcel-discounts, 11 in parcel-pricing).
  - Leftovers: an unused ExtraLarge field in ShippingOptionTests. I also took
    the shipping parameter off the QuoteFor helpers in HeavyRateTests and
    OverweightChargeTests, since no remaining test passes Speedy to them.

  One scenario left in place: I kept "Parcels in a discount keep their cost" in
  the spec. It's the only scenario for "Discounts don't change parcel line
  items", and every requirement needs at least one. Its test is gone, but
  Discount_is_listed_after_the_parcels_with_its_parcels_and_saving still checks
  it: it compares each parcel line item with the same parcel quoted alone.

  I didn't touch the "up to you" tests, and your untracked README.md isn't in
  the commit. Nothing is pushed or merged yet.