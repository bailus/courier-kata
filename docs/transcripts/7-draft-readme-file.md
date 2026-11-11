
❯ /opsx:explore draft a README.md file
how to run the tests using docker compose
folder structure
link to docs/assumptions.md
simplified explanation of openspec, with a link to the openspec site
structure of the git repository: commits have been squashed to one for each
step, starting with "Add parcel pricing by size" as Step 1. intermediate
commits can be found in undeleted branches:
 - step 1: parcel-size-pricing
 - step 2: speedy-shipping
 - step 3: overweight-parcel-charge
 - step 4: heavy-parcel
 - step 5: multi-parcel-discounts

  Read 1 file, ran 1 shell command (ctrl+o to expand)

● I looked through the repo: compose file, layout, public API,
  docs/assumptions.md, the OpenSpec specs and archive, and the git graph. Below
  is a draft for you to react to. I haven't written any files.

  What the git history actually looks like

  master
    |
    * edca233 Add multi-parcel discounts ...... Step 5   (squash)
    |  \
    |   * f643b8e..5ceb663  ................... branch: multi-parcel-discounts
  (6 commits)
    |
    * 16c16fd Add heavy parcel rate ........... Step 4
    |  \__ 7c44ac1..9aeded2 ................... branch: heavy-parcel (5)
    |
    * 3dc32f3 Add overweight charge ........... Step 3
    |  \__ 8cdbb28..30cbf19 ................... branch: overweight-parcel-charge
  (4)
    |
    * 9cb886b Add speedy shipping ............. Step 2
    |  \__ 4351976..0e44454 ................... branch: speedy-shipping (7)
    |
    * 48f692b Add parcel pricing by size ...... Step 1
    |  \__ f69cb30..55c0c3c ................... branch: parcel-size-pricing (7)
    |
    * a205672 / 72a2d39 / bc9c4a5 / 7f4d14a ... setup (CLAUDE.md, docker,
  OpenSpec)

  I noticed three things the README should handle:

  1. There are 4 setup commits before Step 1. I put them in as "Step 0 / setup"
     so readers aren't confused by them.
  2. Each step branch starts from the previous step's squashed commit, not from
     the previous branch's tip. So git log master..speedy-shipping shows exactly
     that step's working commits. That command is worth giving readers.
  3. The default branch is master, not main. The draft says master.

  Draft README.md

  # Courier Kata

  A C# class library that prices parcel deliveries. You put parcels in a `Cart`,
  choose how fast to ship them, and get back a `Quote` with one line item per
  parcel, any multi-parcel discounts, an optional speedy-shipping charge, and a
  total.

  It is a library only: the public C# API is the whole interface. There is no
  CLI, web API or UI.

  ```csharp
  var cart = new Cart { Shipping = Shipping.Speedy };
  cart.AddParcel(new Parcel(Length: 20, Width: 15, Height: 5, Weight: 1.5m));
  cart.AddParcel(new Parcel(Length: 60, Width: 40, Height: 30, Weight: 8m));

  Quote quote = cart.GetQuote();
  // quote.Items: parcels, then discounts, then speedy shipping
  // quote.Total: the total cost in dollars
  ```

  ## Running the tests

  You don't need a local .NET SDK. The tests run in a .NET 10 SDK container:

  ```bash
  docker compose run --rm tests
  ```

  The source is mounted read-only. Build output and restored NuGet packages go
  to
  named Docker volumes, so nothing is written to your checkout and later runs
  are
  faster.

  ## Folder structure

  ```
  CourierKata.sln
  Directory.Build.props     # shared settings: net10.0, nullable, warnings as
  errors
  docker-compose.yml        # the `tests` service
  src/CourierKata/          # the library
  tests/CourierKata.Tests/  # xUnit tests
  docs/assumptions.md       # decisions made where the brief was unclear
  openspec/
    specs/                  # current requirements, one folder per capability
    changes/archive/        # each step's proposal, design, spec changes and
  tasks
  .claude/                  # Claude Code commands and skills for the OpenSpec
  workflow
  ```

  ## Assumptions

  Where the brief was ambiguous (for example, which size a parcel is when a
  dimension is exactly 10 cm), the interpretation chosen and the reasons for it
  are recorded in [docs/assumptions.md](docs/assumptions.md).

  ## OpenSpec

  This repo uses [OpenSpec](https://openspec.dev/) to write down what the code
  should do before writing the code. In short:

  - **Specs** (`openspec/specs/`) describe how the library currently behaves, as
    requirements with example scenarios. There are two: `parcel-pricing` and
    `parcel-discounts`.
  - **Changes** describe new work. Each one has a proposal (why), a design
  (how),
    spec deltas (what changes in the requirements) and a task list.
  - When a change is finished it is **archived**. Its spec deltas are merged
  into
    `openspec/specs/`, and the change moves to `openspec/changes/archive/`.

  Each step of the kata was one OpenSpec change. To see how a step was planned,
  read its folder in `openspec/changes/archive/`.

  ## Git history

  `master` has one squashed commit for each step of the kata, after a few setup
  commits:

  | Step | Commit on `master`                     | Branch with the working
  commits |
  |------|----------------------------------------|-----------------------------
  ----|
  | 0    | setup (CLAUDE.md, Docker, OpenSpec)    | none
     |
  | 1    | Add parcel pricing by size             | `parcel-size-pricing`
     |
  | 2    | Add speedy shipping                    | `speedy-shipping`
     |
  | 3    | Add overweight charge per parcel size  | `overweight-parcel-charge`
     |
  | 4    | Add heavy parcel rate                  | `heavy-parcel`
     |
  | 5    | Add multi-parcel discounts             | `multi-parcel-discounts`
     |

  Each step branch starts from the previous step's squashed commit on `master`,
  so you can list the working commits for one step with:

  ```bash
  git log --oneline master..speedy-shipping
  ```

  The working commits show the test-first progression: the proposal, then each
  behaviour with its tests, then archiving the change.

  Choices I made that you may want to change

  - Usage snippet. You didn't ask for one, but a library README without one
    feels empty. The member names are real (Cart, Shipping, AddParcel, GetQuote,
    Quote.Items/Total). I deliberately didn't show a total, so it can't go
    stale.
  - OpenSpec link. I used https://openspec.dev/. I'm fairly sure that's the
    project site, but I haven't checked it from here. The alternative is the
    GitHub repo, github.com/Fission-AI/OpenSpec.
  - "Test-first progression" line. This is my reading of the branch commit
    messages. Cut it if it promises more than the branches show.
  - .claude/ row. Some people would rather leave tooling out of a README. I kept
    it because the step branches reference that workflow.

  If you're happy with this (or after your edits), should I write it to
  README.md at the repo root and commit it on master as "Add README"? Yes or no?