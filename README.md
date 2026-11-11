# Courier Kata

A C# class library that prices parcel deliveries. You put parcels in a `Cart`,
choose how fast to ship them, and get back a `Quote` with one line item per
parcel, any multi-parcel discounts, an optional speedy-shipping charge, and a
total.

```csharp
var cart = new Cart { Shipping = Shipping.Speedy };
cart.AddParcel(new Parcel(Length: 20, Width: 15, Height: 5, Weight: 1.5m));
cart.AddParcel(new Parcel(Length: 60, Width: 40, Height: 30, Weight: 8m));

Quote quote = cart.GetQuote();
// quote.Items: parcels, then discounts, then speedy shipping
// quote.Total: the total cost in dollars
```

## Running the tests

Run it locally, ideally using .NET 10 SDK (I haven't tested this):
```bash
dotnet test
```

I ran it in a docker container:
```bash
docker compose run --rm tests
```

## Folder structure

```
src/CourierKata/          # the library
tests/CourierKata.Tests/  # xUnit tests
```

## Git History

`master` has one squashed commit for each step of the kata, after a few setup
commits:

| Step | Commit on `master`                     | Branch with the working commits |
|------|----------------------------------------|---------------------------------|
| 0    | setup (CLAUDE.md, Docker, OpenSpec)    | none                            |
| 1    | Add parcel pricing by size             | `parcel-size-pricing`           |
| 2    | Add speedy shipping                    | `speedy-shipping`               |
| 3    | Add overweight charge per parcel size  | `overweight-parcel-charge`      |
| 4    | Add heavy parcel rate                  | `heavy-parcel`                  |
| 5    | Add multi-parcel discounts             | `multi-parcel-discounts`        |

Each step branch starts from the previous step's squashed commit on `master`.

## Development Methodology

I used [OpenSpec](https://openspec.dev/) AI-assisted spec-driven development:

- **Specs** (`openspec/specs/`) describe how the library currently behaves, as
  requirements with example scenarios. There are two: `parcel-pricing` and
  `parcel-discounts`.
- **Changes** describe new work. Each one has a proposal (why), a design (how),
  spec deltas (what changes in the requirements) and a task list.
- When a change is finished it is **archived**. Its spec deltas are merged into
  `openspec/specs/`, and the change moves to `openspec/changes/archive/`.

Each step of the kata was one OpenSpec change, written using a test-first approach.

Transcripts from the AI-assisted development process are available in the `docs/transcripts/` directory.

## Assumptions

Where the brief was ambiguous, assumptions were made and recorded in [docs/assumptions.md](docs/assumptions.md). A human-edited version of the assumptions is in [docs/assumptions-reviewed.md](docs/assumptions-reviewed.md).

### Key assumptions
- Each part of a kilogram over the limit is charged as a whole kilogram. The same rule applies to heavy-rate parcels.
- Heavy parcels aren't a parcel type; they are a rate applied to parcels based on weight. Size category is determined by the parcel's dimensions alone.
- Each item in the output records base cost, overweight charge, and any heavy rate applied. Not just the individual parcel's total cost.
- There is no maximum size or weight limit.

## Reflection

### The process
- The spec-driven approach worked well, probably because the requirements were clear up front.
- This is more code and tests than I could realistically read and understand within the time limit, so I relied mainly on high-level analysis during the exploration phase of each step. I wouldn't feel comfortable deploying this to production without a more thorough review.
- I should have reviewed the tests and assumptions.md at each step and removed unnecessary ones, instead of letting them pile up.

### The robot
- [Claude has ingested this problem and solutions to it](docs/transcripts/00-investigation.jpg). This might make it more effective than it would be on a new problem.
- Given the above, a faster, less capable model might have sped things up without much loss in quality.

### The solution
- The design uses more classes than a problem this small strictly needs, but the classes map to the domain reasonably well. I read "well structured" as calling for this, though a simpler design could probably have met that requirement too.
- Nothing is configurable. A real-world version would need it.
- Some better documentation would be nice.
