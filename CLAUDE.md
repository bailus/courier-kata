# CLAUDE.md

Guidance for Claude Code when working in this repository.

## What this project is

A C# **class library** (the "courier kata") meant to be used by other code.

- Build **only** the library and its tests.
- Do **not** add a CLI, console app, HTTP/web API, UI, or any other way to interact with it. The public C# API is the only interface.

## Ground rules

1. **Well-structured code.** Keep the public API small and deliberate. Use clear domain types (records, enums, value objects) rather than loose primitives. Each class does one thing. Keep implementation details `internal`.
2. **Tests for everything.** Every behaviour gets tests. Prefer test-first: write a failing test, make it pass, then refactor. Tests describe behaviour, not implementation.
3. **Record assumptions.** Whenever a requirement is unclear and you pick an interpretation, add an entry to [docs/assumptions.md](docs/assumptions.md) in the same commit as the code that depends on it. Each entry says what was unclear, what you assumed, and why. Create the file if it does not exist.
4. **Small, well-structured commits.** Commit as you go, making sure to commit after each openspec change has been archived. Code, its tests, and any related assumptions go in the same commit. All tests must pass before each commit. Use an imperative subject line of 72 characters or fewer, and add a body when the reason for the change isn't obvious. Don't add "Co-authored by Claude" or reference Claude in commit messages.

## Suggested layout

```
CourierKata.sln
src/CourierKata/            # the library
tests/CourierKata.Tests/    # unit tests (xUnit)
docs/assumptions.md         # running log of assumptions
openspec/                   # OpenSpec specs and change proposals
```

## Commands

No local .NET SDK. Run the tests in Docker instead:

```bash
docker compose run --rm tests
```

Use the current LTS .NET SDK, and enable nullable reference types and warnings-as-errors in the projects.

## Spec workflow

This repo uses [OpenSpec](openspec/) (`/opsx:*` commands). Requirements live in `openspec/specs/`, and proposed changes in `openspec/changes/`. When implementing a change, follow its `tasks.md` and keep the specs in sync with what was actually built.
