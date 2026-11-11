
❯ This is a library in C# that can be consumed by other code. Do not
  build a CLI, HTTP, or any other interaction layer.
  Well structured code.
  Tests.
  A record of any assumptions you make as you go, stored in docs/assumptions.md
  Well structured commits as you go.

  write a CLAUDE.md file

  Ran 1 shell command (ctrl+o to expand)

● Write(CLAUDE.md)
  ⎿  Wrote 41 lines to CLAUDE.md
       1 # CLAUDE.md
       2
       3 Guidance for Claude Code when working in this repository.
       4
       5 ## What this project is
       6
       7 A C# **class library** (the "courier kata") meant to be used by
         other code.
       8
       9 - Build **only** the library and its tests.
     … +32 lines (ctrl+o to expand)

  Ran 2 shell commands (ctrl+o to expand)

● I wrote CLAUDE.md and committed it as the first commit on master (7f4d14a).

  What it says:
  - Library only: a C# class library plus its tests. No CLI, HTTP, UI or other
    way to interact with it. The public C# API is the only interface.
  - Code structure: a small public API, clear domain types, and implementation
    details kept internal.
  - Tests: written first where possible, and they test behaviour rather than
    implementation.
  - Assumptions: each one is logged in docs/assumptions.md with what was
    unclear, what was assumed and why. It goes in the same commit as the code
    that depends on it.
  - Commits: one logical change per commit, with code, tests and assumptions
    together. Tests must pass before each commit, and subject lines are short
    and in the imperative.
  - Layout and commands: a suggested src/ and tests/ layout using xUnit, plus
    dotnet build, test and format.
  - OpenSpec: a pointer to the OpenSpec workflow already set up in openspec/.

  Things to know:
  - The .NET SDK isn't installed on this machine. dotnet isn't on the PATH or in
    ~/.dotnet, so nothing can be built or tested until you install it.
  - Not committed: .claude/ and openspec/ are still untracked. Tell me if you
    want them in a separate commit.
  - Branch name: you're on master, but the main branch is set to main. You might
    want to rename the branch before going further.

❯ <ide_opened_file>The user opened the file
  /home/sam/workspace/courier-kata/CLAUDE.md in the IDE. This may or may not be
  related to the current task.</ide_opened_file>

❯ create a docker-compose file to run the tests in a docker container

  Ran 1 shell command (ctrl+o to expand)

● Write(docker-compose.yml)
  ⎿  Wrote 25 lines to docker-compose.yml
       1 # Runs the test suite inside a .NET SDK container, so no local S
         DK is needed.
       2 #
       3 #   docker compose run --rm tests
       4 #
       5 # The source is mounted read-only. Build output (bin/obj) goes t
         o a named
       6 # volume instead of the host checkout, so no root-owned files la
         nd in the repo.
       7 # Restored NuGet packages are kept in a named volume to make lat
     … +19 lines (ctrl+o to expand)

● Now checking it against a throwaway sample solution in the scratchpad.

  Ran 1 shell command (ctrl+o to expand)

● Compose couldn't make a network because the Docker host is out of address
  pools, so I'm switching the tests to the default bridge network, which still
  allows the NuGet access they need. · summary

● Update(docker-compose.yml)
  ⎿  Added 3 lines
      10    tests:
      11      image: mcr.microsoft.com/dotnet/sdk:10.0
      12      working_dir: /src
      13 +    # Use Docker's default bridge instead of creating a per-pro
         +ject network;                                                  
      14 +    # the tests only need outbound access for NuGet restore.   
      15 +    network_mode: bridge                                       
      16      volumes:
      17        - .:/src:ro
      18        - artifacts:/artifacts

  Ran 1 shell command (ctrl+o to expand)

● It works. Next I'll add the command to CLAUDE.md and commit only the compose
  file, without Claude attribution as CLAUDE.md now asks.

● Update(CLAUDE.md)
  ⎿  Added 6 lines
      34  dotnet format          # run before committing
      35  ```
      36  
      37 +No local .NET SDK? Run the tests in Docker instead:            
      38 +                                                               
      39 +```bash                                                        
      40 +docker compose run --rm tests                                  
      41 +```                                                            
      42 +                                                               
      43  Use the current LTS .NET SDK, and enable nullable reference typ
          es and warnings-as-errors in the projects.
      44  
      45  ## Spec workflow

  Ran 1 shell command (ctrl+o to expand)

● I added docker-compose.yml, which runs the tests in a .NET 10 SDK container,
  so you don't need a local SDK. Run it with:

  docker compose run --rm tests

  I tried it on a throwaway xUnit solution in the scratchpad and the test
  passed. The repo has no projects yet, so it will fail until there is a
  solution or project at the root.

  How it's set up:
  - Read-only source: the repo is mounted read-only, and build output (bin/obj)
    goes to a named Docker volume. That stops root-owned files from appearing in
    your checkout.
  - Package cache: downloaded NuGet packages are kept in a second volume, so
    later runs are faster.
  - Default bridge network: the first run failed because this Docker host has
    run out of address space for new networks. I set it to use Docker's default
    network instead, which is enough for downloading packages.

  Commits: the compose file is committed on its own as bc9c4a5, without Claude
  attribution as CLAUDE.md now asks. Your earlier commit 7f4d14a still has a
  Co-Authored-By: Claude line. I can amend it to remove that if you like.

  Not committed: I also added the Docker command to the Commands section of
  CLAUDE.md. That file has your own uncommitted edits, so I left my change
  uncommitted alongside them rather than mixing them into one of my commits.

❯ <ide_opened_file>The user opened the file
  /home/sam/workspace/courier-kata/CLAUDE.md in the IDE. This may or may not be
  related to the current task.</ide_opened_file>

❯ add, commit

  Ran 2 shell commands (ctrl+o to expand)

● I added everything and committed it as two commits, and the working tree is
  now clean:

  - 72a2d39 Update CLAUDE.md commit and test-running guidance: your edits to
    CLAUDE.md and the Docker test command.
  - a205672 Add OpenSpec scaffolding and Claude Code skills: openspec/ and
    .claude/, which hold the OpenSpec skills and /opsx commands. There were no
    local settings files in there.

  The first commit, 7f4d14a, still has a Co-Authored-By: Claude line. I can
  rewrite it to remove that if you want.