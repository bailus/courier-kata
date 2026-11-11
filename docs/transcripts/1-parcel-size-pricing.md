
❯ /opsx:explore The initial implementation just needs to calculate cost based
on a parcelʼs size. For
each size category there is a fixed delivery cost
Small parcel: all dimensions < 10 cm, $3
Medium parcel: all dimensions < 50 cm, $8
Large parcel: all dimensions < 100 cm, $15
XL parcel: any dimension 100cm, $25

Input should be a `Cart` class with `addParcel` method.
Output should be a collection of items with their individual cost and type, as
well as the total cost.

In all circumstances the cheapest option for sending each parcel should be
selected.

Start with tests for each parcel size

  Ran 1 shell command (ctrl+o to expand)

● No code or specs exist yet: no openspec/changes, no openspec/specs, and no
  src/. There's a docker-compose.yml that runs dotnet test on the .NET 10 SDK.
  So this is the first change and it sets the shape of the library.

  The problem

    Cart.AddParcel(10, 20, 5)  --+
    Cart.AddParcel( 3,  3, 3)  --+-->  [ classify each parcel ] --> [ price ]
  --> Quote
    Cart.AddParcel(120, 5, 5)  --+         (largest dimension)       (fixed)
                                                                        |
                       +------------------------------------------------+
                       v
             Quote
             +-------------------------------------+
             | Medium   10x20x5     $8             |
             | Small     3x3x3      $3             |
             | XL      120x5x5     $25             |
             +-------------------------------------+
             | Total               $36             |
             +-------------------------------------+

  The rules depend only on the largest dimension. "All dimensions < 10" means
  the same as "max < 10", so the brackets are:

    max dim:  0 ----- 10 ----------- 50 ----------- 100 ----------->
              | Small |    Medium    |     Large     |      XL
              |  $3   |      $8      |      $15      |      $25
             (<10)          (<50)           (<100)        (>=100)

  What the brief leaves open

  These would become entries in docs/assumptions.md. My suggested default is
  given for each:

  #: 1
  Unclear: The XL rule says "any dimension 100cm" with no operator
  Suggested default: >= 100
  Why: It's the only reading that leaves no gaps between brackets
  ────────────────────────────────────────
  #: 2
  Unclear: A value exactly on a boundary (10, 50)
  Suggested default: Goes up a bracket: 10 is Medium
  Why: Follows from the strict < in the brief
  ────────────────────────────────────────
  #: 3
  Unclear: How many dimensions, and what type
  Suggested default: 3 dimensions (L, W, H) as decimal cm
  Why: Real parcels have fractional sizes
  ────────────────────────────────────────
  #: 4
  Unclear: Zero, negative or non-finite dimensions
  Suggested default: Throw ArgumentOutOfRangeException
  Why: A parcel can't be 0 cm, and failing early beats a $3 quote
  ────────────────────────────────────────
  #: 5
  Unclear: addParcel naming
  Suggested default: AddParcel
  Why: C# convention; the brief's camelCase reads as language-neutral
  ────────────────────────────────────────
  #: 6
  Unclear: Money
  Suggested default: decimal, single implied currency (USD)
  Why: No multi-currency requirement, so a Money type is YAGNI for now
  ────────────────────────────────────────
  #: 7
  Unclear: Empty cart
  Suggested default: Empty item list, total $0
  Why: It's the natural identity, and a cart with nothing in it isn't an error
  ────────────────────────────────────────
  #: 8
  Unclear: Line order
  Suggested default: Same order as added
  Why: Predictable, and easy to test
  ────────────────────────────────────────
  #: 9
  Unclear: "Cheapest option"
  Suggested default: The smallest bracket that fits
  Why: With fixed prices that rise with size, smallest = cheapest

  A possible public API

  public sealed class Cart
      void AddParcel(Parcel parcel)          // or AddParcel(l, w, h)?
      Quote GetQuote()                       // name TBD: Checkout? Calculate?

  public sealed record Parcel(decimal Length, decimal Width, decimal Height)
  public enum ParcelSize { Small, Medium, Large, ExtraLarge }
  public sealed record LineItem(Parcel Parcel, ParcelSize Size, decimal Cost)
  public sealed record Quote(IReadOnlyList<LineItem> Items, decimal Total)

  internal static class ParcelClassifier / PriceList   // the rules, hidden

  Some smaller points to settle as we go:
  - Does GetQuote() read the cart or empty it? I'd make it a pure read that you
    can call again and get the same answer.
  - Should Parcel be a value object that validates in its constructor? Then an
    invalid parcel can't exist, and the Cart never has to check.
  - What tests come first? You asked for one test per size. I'd use [Theory]
    rows at each boundary (9.99 / 10, 49.99 / 50, 99.99 / 100), not just one
    value per size, because the boundaries are where the bugs hide.

  The question that matters most

  "In all circumstances the cheapest option… should be selected" says more than
  step 1 needs. Your brief only covers this step, but in the usual form of this
  kata, later steps add weight limits, a "heavy parcel" type, speedy shipping
  and discounts. Once weight arrives, "smallest bracket that fits" may no longer
  be the cheapest. For example, a heavy small parcel can cost less as a "heavy
  parcel" than as Small plus overweight charges.

  So my one question is: should this first change be strictly step 1, or should
  it leave room for those later rules?

  I recommend strictly step 1. Map the largest dimension to a size and look up a
  fixed price. Record assumption #9 so the "cheapest" rule is visible and can
  be revisited. The internal classifier is easy to rework later, and the public
  types above wouldn't need to change. Building the "evaluate every option, take
  the minimum" machinery now would be guessing at requirements you don't have
  yet.

  When you've settled this and anything in the table you want to change,
  /opsx:propose will turn it into a change with the specs, tasks and
  assumptions.

❯ add an assumption that very large parcels aren't rejected
  strictly step 1
  /opsx:propose

● Skill(opsx:propose)
  ⎿  Successfully loaded skill · 1 tool allowed

● Bash(openspec context --json && openspec new change "parcel-size-pricing" &&
      openspec status --change "parcel-size-pricing" --json && openspec
      instructions proposal…)
  ⎿  {
       "root": {
         "path": "/home/sam/workspace/courier-kata",
     … +143 lines (ctrl+o to expand)
  ⎿  Created openspec/changes/parcel-size-pricing/.openspec.yaml (+2 -0)
      1 +schema: spec-driven                                             
      2 +created: 2026-11-11                                             

● Write(openspec/changes/parcel-size-pricing/proposal.md)
  ⎿  Wrote 31 lines to openspec/changes/parcel-size-pricing/proposal.md
       1 # Proposal
       2
       3 ## Why
       4
       5 The courier kata needs its first behaviour: pricing a set of par
         cels by size. Nothing exists yet, so this change also establishe
         s the library, its test project, and the public API shape that l
         ater steps (weight, speedy shipping, discounts) will build on.
       6
       7 ## What Changes
     … +24 lines (ctrl+o to expand)

  Ran 1 shell command (ctrl+o to expand)

● Writing the parcel-pricing spec.

● Write(openspec/changes/parcel-size-pricing/specs/parcel-pricing/spec.md)
  ⎿  Wrote 96 lines to
     openspec/changes/parcel-size-pricing/specs/parcel-pricing/spec.md
       1 # Spec Delta
       2
       3 ## Purpose
       4
       5 Prices a cart of parcels for delivery by classifying each parcel
          by its size and producing a quote with per-parcel costs and a t
         otal.
       6
       7 ## ADDED Requirements
       8
     … +88 lines (ctrl+o to expand)

● Write(openspec/changes/parcel-size-pricing/design.md)
  ⎿  Wrote 54 lines to openspec/changes/parcel-size-pricing/design.md
       1 # Design
       2
       3 ## Context
       4
       5 The repository has no code yet; this change creates the solution
          and sets the public API. See proposal.md for scope. The project
          conventions require a C# class library with no other interface,
          small public surface, `internal` implementation details, nullab
         le reference types and warnings as errors. Tests run locally wit
         h `dotnet test` or in Docker via `docker compose run --rm tests`
     … +50 lines (~4 KB) (ctrl+o to expand)

  Ran 1 shell command (ctrl+o to expand)

● Write(openspec/changes/parcel-size-pricing/tasks.md)
  ⎿  Wrote 43 lines to openspec/changes/parcel-size-pricing/tasks.md
      1 # Tasks
      2
      3 ## 1. Scaffolding
      4
      5 - [ ] 1.1 Create `CourierKata.sln`, `src/CourierKata` (net10.0 cl
        ass library) and `tests/CourierKata.Tests` (xUnit, referencing th
        e library), with a root `Directory.Build.props` enabling nullable
        , warnings as errors and implicit usings; verify `dotnet build` s
        ucceeds with zero warnings and `dotnet test` runs (no tests yet)
      6 - [ ] 1.2 Create `docs/assumptions.md` with a short header explai
     … +38 lines (~3 KB) (ctrl+o to expand)

  Ran 1 shell command (ctrl+o to expand)

● I've created the change parcel-size-pricing in
  openspec/changes/parcel-size-pricing/. It covers strictly step 1, and it
  passes openspec validate --strict. No code has been written yet.

  Artifacts
  - proposal.md (openspec/changes/parcel-size-pricing/proposal.md): sets up the
    library and test project, and adds pricing by size through Cart. Weight,
    speedy shipping, discounts and currencies are listed as out of scope.
  - specs/parcel-pricing/spec.md
    (openspec/changes/parcel-size-pricing/specs/parcel-pricing/spec.md): 7
    requirements. The rules:
    - Each parcel is classified by its largest dimension, and a value exactly on
      a boundary goes up a bracket (10 cm is Medium).
    - Each size has a fixed price.
    - Line items stay in the order parcels were added, and an empty cart totals
      $0.
    - Getting a quote doesn't change the cart.
    - A dimension of zero or less is rejected.
  - design.md (openspec/changes/parcel-size-pricing/design.md): describes the
    public types, keeps the pricing rules internal, and deliberately leaves out
    any "compare every option" logic.
  - tasks.md (openspec/changes/parcel-size-pricing/tasks.md): 6 groups. After
    scaffolding, group 2 writes the tests first, one size at a time (Small,
    Medium, Large, XL), checking just below and exactly on each boundary. Each
    group adds its entries to docs/assumptions.md in the same commit as its
    code.

  Your additions
  - No upper size limit: this is in the spec as the scenario "Very large parcel
    is accepted". A 10,000 cm parcel is created and priced as XL. Task 2.7
    records it as an assumption.
  - Strictly step 1: "cheapest" is treated as the smallest size that fits. That
    interpretation is recorded as an assumption in task 3.2.

  Choices I made that differ from the brief
  - The method is AddParcel rather than addParcel, following C# naming.
  - The XL size is called ParcelSize.ExtraLarge in code.
  - Dimensions and money both use decimal. It has no NaN or infinity, so
    rejecting non-positive values is the only check needed.

  When you're ready, run /opsx:apply.

❯ /opsx:apply

  Ran 2 shell commands (ctrl+o to expand)

● Using change: parcel-size-pricing. There's no local .NET SDK, so I'll run the
  SDK 10 image through Docker with a writable mount as my user, and work on a
  feature branch.

  Ran 2 shell commands (ctrl+o to expand)

● Bash(cd /home/sam/workspace/courier-kata; S=/tmp/claude-1000/-home-sam-workspa
      ce-courier-kata/a3f60f4d-a8a5-46bb-b08c-b6cd787b3a1b/scratchpad; $S/dn sh
      -c 'dotnet ne…)
  ⎿  The template "xUnit Test Project" was created successfully.

     Processing post-creation actions...
     … +49 lines (ctrl+o to expand)
  ⎿  Created CourierKata.sln (+56 -0)
       1 +                                                               
       2 +Microsoft Visual Studio Solution File, Format Version 12.00    
       3 +# Visual Studio Version 17                                     
       4 +VisualStudioVersion = 17.0.31903.59                            
       5 +MinimumVisualStudioVersion = 10.0.40219.1                      
       6 +Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "src", "src
         +", "{827E0CD3-B72D-47B6-A68D-7590B98EB39B}"                    
       7 +EndProject                                                     
       8 +Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "CourierKat
         +a", "src\CourierKata\CourierKata.csproj", "{E36BDC4A-04A4-4FB8-
         +B4A0-F0F86AC866C6}"                                            
       9 +EndProject                                                     
      10 +Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "tests", "t
         +ests", "{0AB3BF05-4346-4AA6-1389-037BE0695223}"                
      11 +EndProject                                                     
      12 +Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "CourierKat
         +a.Tests", "tests\CourierKata.Tests\CourierKata.Tests.csproj", "
         +{DAEC5AA2-1366-4F0F-B0D7-A8D967F386F7}"                        
      13 +EndProject                                                     
      14 +Global                                                         
      15 +        GlobalSection(SolutionConfigurationPlatforms) = preSolu
         +tion                                                           
      16 +                Debug|Any CPU = Debug|Any CPU                  
      17 +                Debug|x64 = Debug|x64                          
      18 +                Debug|x86 = Debug|x86                          
      19 +                Release|Any CPU = Release|Any CPU              
      20 +                Release|x64 = Release|x64                      
      21 +                Release|x86 = Release|x86                      
      22 +        EndGlobalSection                                       
      23 +        GlobalSection(ProjectConfigurationPlatforms) = postSolu
         +tion                                                           
      24 +                {E36BDC4A-04A4-4FB8-B4A0-F0F86AC866C6}.Debug|An
         +y CPU.ActiveCfg = Debug|Any CPU                                
      25 +                {E36BDC4A-04A4-4FB8-B4A0-F0F86AC866C6}.Debug|An
         +y CPU.Build.0 = Debug|Any CPU                                  
      26 +                {E36BDC4A-04A4-4FB8-B4A0-F0F86AC866C6}.Debug|x6
         +4.ActiveCfg = Debug|Any CPU                                    
      27 +                {E36BDC4A-04A4-4FB8-B4A0-F0F86AC866C6}.Debug|x6
         +4.Build.0 = Debug|Any CPU                                      
      28 +                {E36BDC4A-04A4-4FB8-B4A0-F0F86AC866C6}.Debug|x8
         +6.ActiveCfg = Debug|Any CPU                                    
      29 +                {E36BDC4A-04A4-4FB8-B4A0-F0F86AC866C6}.Debug|x8
         +6.Build.0 = Debug|Any CPU                                      
      30 +                {E36BDC4A-04A4-4FB8-B4A0-F0F86AC866C6}.Release|
         +Any CPU.ActiveCfg = Release|Any CPU                            
      31 +                {E36BDC4A-04A4-4FB8-B4A0-F0F86AC866C6}.Release|
         +Any CPU.Build.0 = Release|Any CPU                              
      32 +                {E36BDC4A-04A4-4FB8-B4A0-F0F86AC866C6}.Release|
         +x64.ActiveCfg = Release|Any CPU                                
      33 +                {E36BDC4A-04A4-4FB8-B4A0-F0F86AC866C6}.Release|
         +x64.Build.0 = Release|Any CPU                                  
      34 +                {E36BDC4A-04A4-4FB8-B4A0-F0F86AC866C6}.Release|
         +x86.ActiveCfg = Release|Any CPU                                
      35 +                {E36BDC4A-04A4-4FB8-B4A0-F0F86AC866C6}.Release|
         +x86.Build.0 = Release|Any CPU                                  
      36 +                {DAEC5AA2-1366-4F0F-B0D7-A8D967F386F7}.Debug|An
         +y CPU.ActiveCfg = Debug|Any CPU                                
      37 +                {DAEC5AA2-1366-4F0F-B0D7-A8D967F386F7}.Debug|An
         +y CPU.Build.0 = Debug|Any CPU                                  
      38 +                {DAEC5AA2-1366-4F0F-B0D7-A8D967F386F7}.Debug|x6
         +4.ActiveCfg = Debug|Any CPU                                    
      39 +                {DAEC5AA2-1366-4F0F-B0D7-A8D967F386F7}.Debug|x6
         +4.Build.0 = Debug|Any CPU                                      
      40 +                {DAEC5AA2-1366-4F0F-B0D7-A8D967F386F7}.Debug|x8
         +6.ActiveCfg = Debug|Any CPU                                    
     … 16 more lines (ctrl+o to expand)
  ⎿  Created src/CourierKata/Class1.cs (+6 -0)
      1 +namespace CourierKata;                                          
      2 +                                                                
      3 +public class Class1                                             
      4 +{                                                               
      5 +                                                                
      6 +}                                                               
  ⎿  Created src/CourierKata/CourierKata.csproj (+9 -0)
      1 +<Project Sdk="Microsoft.NET.Sdk">                               
      2 +                                                                
      3 +  <PropertyGroup>                                               
      4 +    <TargetFramework>net10.0</TargetFramework>                  
      5 +    <ImplicitUsings>enable</ImplicitUsings>                     
      6 +    <Nullable>enable</Nullable>                                 
      7 +  </PropertyGroup>                                              
      8 +                                                                
      9 +</Project>                                                      
  ⎿  Created tests/CourierKata.Tests/CourierKata.Tests.csproj (+25 -0)
       1 +<Project Sdk="Microsoft.NET.Sdk">                              
       2 +                                                               
       3 +  <PropertyGroup>                                              
       4 +    <TargetFramework>net10.0</TargetFramework>                 
       5 +    <ImplicitUsings>enable</ImplicitUsings>                    
       6 +    <Nullable>enable</Nullable>                                
       7 +    <IsPackable>false</IsPackable>                             
       8 +  </PropertyGroup>                                             
       9 +                                                               
      10 +  <ItemGroup>                                                  
      11 +    <PackageReference Include="coverlet.collector" Version="6.0
         +.4" />                                                         
      12 +    <PackageReference Include="Microsoft.NET.Test.Sdk" Version=
         +"17.14.1" />                                                   
      13 +    <PackageReference Include="xunit" Version="2.9.3" />       
      14 +    <PackageReference Include="xunit.runner.visualstudio" Versi
         +on="3.1.4" />                                                  
      15 +  </ItemGroup>                                                 
      16 +                                                               
      17 +  <ItemGroup>                                                  
      18 +    <Using Include="Xunit" />                                  
      19 +  </ItemGroup>                                                 
      20 +                                                               
      21 +  <ItemGroup>                                                  
      22 +    <ProjectReference Include="..\..\src\CourierKata\CourierKat
         +a.csproj" />                                                   
      23 +  </ItemGroup>                                                 
      24 +                                                               
      25 +</Project>                                                     
      26   No newline at end of file
  ⎿  Created tests/CourierKata.Tests/UnitTest1.cs (+10 -0)
       1 +namespace CourierKata.Tests;                                   
       2 +                                                               
       3 +public class UnitTest1                                         
       4 +{                                                              
       5 +    [Fact]                                                     
       6 +    public void Test1()                                        
       7 +    {                                                          
       8 +                                                               
       9 +    }                                                          
      10 +}                                                              
  ⎿  … 1 more file changed

● Bash(rm src/CourierKata/Class1.cs tests/CourierKata.Tests/UnitTest1.cs && cat
      > Directory.Build.props <<'EOF'
      <Project>…)
  ⎿  <Project Sdk="Microsoft.NET.Sdk">

       <PropertyGroup>
     … +18 lines (ctrl+o to expand)
  ⎿  Created Directory.Build.props (+11 -0)
       1 +<Project>                                                      
       2 +                                                               
       3 +  <PropertyGroup>                                              
       4 +    <TargetFramework>net10.0</TargetFramework>                 
       5 +    <LangVersion>latest</LangVersion>                          
       6 +    <ImplicitUsings>enable</ImplicitUsings>                    
       7 +    <Nullable>enable</Nullable>                                
       8 +    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>        
       9 +  </PropertyGroup>                                             
      10 +                                                               
      11 +</Project>                                                     
  ⎿  Created docs/assumptions.md (+5 -0)
      1 +# Assumptions                                                   
      2 +                                                                
      3 +A running log of decisions made where the requirements were uncl
        +ear. Each entry                                                 
      4 +records what was unclear, what was assumed, and why. Entries are
        + added in the                                                   
      5 +same commit as the code that depends on them.                   
  ⎿  Deleted src/CourierKata/Class1.cs (+0 -6)
      1 -namespace CourierKata;                                          
      2 -                                                                
      3 -public class Class1                                             
      4 -{                                                               
      5 -                                                                
      6 -}                                                               
  ⎿  Updated src/CourierKata/CourierKata.csproj (+1 -7)
      1 -<Project Sdk="Microsoft.NET.Sdk">                               
      2 -                                                                
      3 -  <PropertyGroup>                                               
      4 -    <TargetFramework>net10.0</TargetFramework>                  
      5 -    <ImplicitUsings>enable</ImplicitUsings>                     
      6 -    <Nullable>enable</Nullable>                                 
      7 -  </PropertyGroup>                                              
      1 +<Project Sdk="Microsoft.NET.Sdk">                               
      2  
      3  </Project>
  ⎿  Updated tests/CourierKata.Tests/CourierKata.Tests.csproj (+1 -4)
      1 -<Project Sdk="Microsoft.NET.Sdk">                               
      1 +<Project Sdk="Microsoft.NET.Sdk">                               
      2  
      3    <PropertyGroup>
      4 -    <TargetFramework>net10.0</TargetFramework>                  
      5 -    <ImplicitUsings>enable</ImplicitUsings>                     
      6 -    <Nullable>enable</Nullable>                                 
      4      <IsPackable>false</IsPackable>
      5    </PropertyGroup>
      6  
  ⎿  … 1 more file changed

● Bash(sed -i 's/^- \[ \] 1\.\([123]\) /- [x] 1.\1 /'
      openspec/changes/parcel-size-pricing/tasks.md && git add -A && git status
      --short && git commit -q -F - <<'EOF'
      S…)
  ⎿  A  .gitignore
     A  CourierKata.sln
     A  Directory.Build.props
     … +9 lines (ctrl+o to expand)
  ⎿  Updated openspec/changes/parcel-size-pricing/tasks.md (+3 -3)
       2  
       3  ## 1. Scaffolding
       4  
       5 -- [ ] 1.1 Create `CourierKata.sln`, `src/CourierKata` (net10.0 
         -class library) and `tests/CourierKata.Tests` (xUnit, referencin
         -g the library), with a root `Directory.Build.props` enabling nu
         -llable, warnings as errors and implicit usings; verify `dotnet 
         -build` succeeds with zero warnings and `dotnet test` runs (no t
         -ests yet)                                                      
       6 -- [ ] 1.2 Create `docs/assumptions.md` with a short header expl
         -aining its purpose; verify the file exists                     
       7 -- [ ] 1.3 Commit the scaffold after `dotnet format` reports no 
         -changes                                                        
       5 +- [x] 1.1 Create `CourierKata.sln`, `src/CourierKata` (net10.0 
         +class library) and `tests/CourierKata.Tests` (xUnit, referencin
         +g the library), with a root `Directory.Build.props` enabling nu
         +llable, warnings as errors and implicit usings; verify `dotnet 
         +build` succeeds with zero warnings and `dotnet test` runs (no t
         +ests yet)                                                      
       6 +- [x] 1.2 Create `docs/assumptions.md` with a short header expl
         +aining its purpose; verify the file exists                     
       7 +- [x] 1.3 Commit the scaffold after `dotnet format` reports no 
         +changes                                                        
       8  
       9  ## 2. Size classification (tests first, one size at a time)
      10  

● Scaffold committed. Now task 2.1, test first:

● Write(tests/CourierKata.Tests/ParcelSizeTests.cs)
  ⎿  Wrote 26 lines to tests/CourierKata.Tests/ParcelSizeTests.cs
       1 namespace CourierKata.Tests;
       2
       3 public class ParcelSizeTests
       4 {
       5     [Theory]
       6     [MemberData(nameof(SmallParcels))]
       7     public void Parcel_with_all_dimensions_under_10cm_is_small(d
         ecimal length, decimal width, decimal height)
       8     {
       9         Assert.Equal(ParcelSize.Small, SizeOf(length, width, hei
     … +18 lines (ctrl+o to expand)

  Ran 1 shell command (ctrl+o to expand)

● Red. Minimal implementation:

● Bash(cd src/CourierKata && cat > ParcelSize.cs <<'EOF'
      namespace CourierKata;…)
  ⎿  Passed!  - Failed:     0, Passed:     3, Skipped:     0, Total:     3,
      Duration: 16 ms - CourierKata.Tests.dll (net10.0)
  ⎿  Created src/CourierKata/ParcelSize.cs (+7 -0)
      1 +namespace CourierKata;                                          
      2 +                                                                
      3 +/// <summary>The size category a parcel is charged at.</summary>
      4 +public enum ParcelSize                                          
      5 +{                                                               
      6 +    Small,                                                      
      7 +}                                                               
  ⎿  Created src/CourierKata/Parcel.cs (+4 -0)
      1 +namespace CourierKata;                                          
      2 +                                                                
      3 +/// <summary>A parcel to deliver, with its dimensions in centime
        +tres.</summary>                                                 
      4 +public sealed record Parcel(decimal Length, decimal Width, decim
        +al Height);                                                     
  ⎿  Created src/CourierKata/LineItem.cs (+4 -0)
      1 +namespace CourierKata;                                          
      2 +                                                                
      3 +/// <summary>One parcel in a quote, with the size it is charged 
        +at.</summary>                                                   
      4 +public sealed record LineItem(Parcel Parcel, ParcelSize Size);  
  ⎿  Created src/CourierKata/Quote.cs (+4 -0)
      1 +namespace CourierKata;                                          
      2 +                                                                
      3 +/// <summary>The priced contents of a cart.</summary>           
      4 +public sealed record Quote(IReadOnlyList<LineItem> Items);      
  ⎿  Created src/CourierKata/Cart.cs (+18 -0)
       1 +namespace CourierKata;                                         
       2 +                                                               
       3 +/// <summary>Collects parcels to be sent and prices them.</summ
         +ary>                                                           
       4 +public sealed class Cart                                       
       5 +{                                                              
       6 +    private readonly List<Parcel> _parcels = [];               
       7 +                                                               
       8 +    /// <summary>Adds a parcel to the cart.</summary>          
       9 +    public void AddParcel(Parcel parcel)                       
      10 +    {                                                          
      11 +        ArgumentNullException.ThrowIfNull(parcel);             
      12 +        _parcels.Add(parcel);                                  
      13 +    }                                                          
      14 +                                                               
      15 +    /// <summary>Prices every parcel in the cart.</summary>    
      16 +    public Quote GetQuote() =>                                 
      17 +        new(_parcels.Select(parcel => new LineItem(parcel, Parc
         +elSize.Small)).ToList());                                      
      18 +}                                                              
● Green. Task 2.2, Medium:

● Update(tests/CourierKata.Tests/ParcelSizeTests.cs)
  ⎿  Added 14 lines
      16          { 9.99m, 9.99m, 9.99m },
      17      };
      18  
      19 +    [Theory]                                                   
      20 +    [MemberData(nameof(MediumParcels))]                        
      21 +    public void Parcel_with_largest_dimension_from_10cm_to_unde
         +r_50cm_is_medium(decimal length, decimal width, decimal height)
      22 +    {                                                          
      23 +        Assert.Equal(ParcelSize.Medium, SizeOf(length, width, h
         +eight));                                                       
      24 +    }                                                          
      25 +                                                               
      26 +    public static TheoryData<decimal, decimal, decimal> MediumP
         +arcels => new()                                                
      27 +    {                                                          
      28 +        { 10m, 5m, 5m },                                       
      29 +        { 49.99m, 5m, 5m },                                    
      30 +        { 49.99m, 49.99m, 49.99m },                            
      31 +    };                                                         
      32 +                                                               
      33      private static ParcelSize SizeOf(decimal length, decimal wi
          dth, decimal height)
      34      {
      35          var cart = new Cart();

  Ran 1 shell command (ctrl+o to expand)

● Bash(cd /home/sam/workspace/courier-kata/src/CourierKata && cat >
      ParcelSize.cs <<'EOF'
      namespace CourierKata;…)
  ⎿  Passed!  - Failed:     0, Passed:     6, Skipped:     0, Total:     6,
      Duration: 17 ms - CourierKata.Tests.dll (net10.0)
  ⎿  Updated src/CourierKata/ParcelSize.cs (+1 -0)
      4  public enum ParcelSize
      5  {
      6      Small,
      7 +    Medium,                                                     
      8  }
  ⎿  Updated src/CourierKata/Cart.cs (+7 -1)
      14  
      15      /// <summary>Prices every parcel in the cart.</summary>
      16      public Quote GetQuote() =>
      17 -        new(_parcels.Select(parcel => new LineItem(parcel, Parc
         -elSize.Small)).ToList());                                      
      17 +        new(_parcels.Select(parcel => new LineItem(parcel, Clas
         +sify(parcel))).ToList());                                      
      18 +                                                               
      19 +    private static ParcelSize Classify(Parcel parcel)          
      20 +    {                                                          
      21 +        var largest = Math.Max(parcel.Length, Math.Max(parcel.W
         +idth, parcel.Height));                                         
      22 +        return largest < 10m ? ParcelSize.Small : ParcelSize.Me
         +dium;                                                          
      23 +    }                                                          
      24  }

● Task 2.3, Large:

● Update(tests/CourierKata.Tests/ParcelSizeTests.cs)
  ⎿  Added 14 lines
      30          { 49.99m, 49.99m, 49.99m },
      31      };
      32  
      33 +    [Theory]                                                   
      34 +    [MemberData(nameof(LargeParcels))]                         
      35 +    public void Parcel_with_largest_dimension_from_50cm_to_unde
         +r_100cm_is_large(decimal length, decimal width, decimal height)
      36 +    {                                                          
      37 +        Assert.Equal(ParcelSize.Large, SizeOf(length, width, he
         +ight));                                                        
      38 +    }                                                          
      39 +                                                               
      40 +    public static TheoryData<decimal, decimal, decimal> LargePa
         +rcels => new()                                                 
      41 +    {                                                          
      42 +        { 50m, 5m, 5m },                                       
      43 +        { 99.99m, 5m, 5m },                                    
      44 +        { 99.99m, 99.99m, 99.99m },                            
      45 +    };                                                         
      46 +                                                               
      47      private static ParcelSize SizeOf(decimal length, decimal wi
          dth, decimal height)
      48      {
      49          var cart = new Cart();

  Ran 1 shell command (ctrl+o to expand)

● Bash(cd /home/sam/workspace/courier-kata/src/CourierKata && sed -i 's/^
      Medium,$/    Medium,\n    Large,/' ParcelSize.cs && python3 - <<'EOF'
      p='Cart.cs'…)
  ⎿  Passed!  - Failed:     0, Passed:     9, Skipped:     0, Total:     9,
      Duration: 17 ms - CourierKata.Tests.dll (net10.0)
  ⎿  Updated src/CourierKata/ParcelSize.cs (+1 -0)
      5  {
      6      Small,
      7      Medium,
      8 +    Large,                                                      
      9  }
  ⎿  Updated src/CourierKata/Cart.cs (+6 -1)
      19      private static ParcelSize Classify(Parcel parcel)
      20      {
      21          var largest = Math.Max(parcel.Length, Math.Max(parcel.W
          idth, parcel.Height));
      22 -        return largest < 10m ? ParcelSize.Small : ParcelSize.Me
         -dium;                                                          
      22 +        return largest switch                                  
      23 +        {                                                      
      24 +            < 10m => ParcelSize.Small,                         
      25 +            < 50m => ParcelSize.Medium,                        
      26 +            _ => ParcelSize.Large,                             
      27 +        };                                                     
      28      }
      29  }

● Task 2.4, XL:

● Update(tests/CourierKata.Tests/ParcelSizeTests.cs)
  ⎿  Added 14 lines
      44          { 99.99m, 99.99m, 99.99m },
      45      };
      46  
      47 +    [Theory]                                                   
      48 +    [MemberData(nameof(ExtraLargeParcels))]                    
      49 +    public void Parcel_with_any_dimension_of_100cm_or_more_is_e
         +xtra_large(decimal length, decimal width, decimal height)      
      50 +    {                                                          
      51 +        Assert.Equal(ParcelSize.ExtraLarge, SizeOf(length, widt
         +h, height));                                                   
      52 +    }                                                          
      53 +                                                               
      54 +    public static TheoryData<decimal, decimal, decimal> ExtraLa
         +rgeParcels => new()                                            
      55 +    {                                                          
      56 +        { 100m, 5m, 5m },                                      
      57 +        { 100m, 100m, 100m },                                  
      58 +        { 10_000m, 5m, 5m },                                   
      59 +    };                                                         
      60 +                                                               
      61      private static ParcelSize SizeOf(decimal length, decimal wi
          dth, decimal height)
      62      {
      63          var cart = new Cart();

  Ran 1 shell command (ctrl+o to expand)

● Bash(cd /home/sam/workspace/courier-kata/src/CourierKata && sed -i 's/^
      Large,$/    Large,\n    ExtraLarge,/' ParcelSize.cs && sed -i 's/
        _ => ParcelSi…)
  ⎿  Passed!  - Failed:     0, Passed:    12, Skipped:     0, Total:    12,
      Duration: 36 ms - CourierKata.Tests.dll (net10.0)
  ⎿  Updated src/CourierKata/ParcelSize.cs (+1 -0)
       6      Small,
       7      Medium,
       8      Large,
       9 +    ExtraLarge,                                                
      10  }
  ⎿  Updated src/CourierKata/Cart.cs (+2 -1)
      23          {
      24              < 10m => ParcelSize.Small,
      25              < 50m => ParcelSize.Medium,
      26 -            _ => ParcelSize.Large,                             
      26 +            < 100m => ParcelSize.Large,                        
      27 +            _ => ParcelSize.ExtraLarge,                        
      28          };
      29      }
      30  }

● Task 2.5, orientation:

● Update(tests/CourierKata.Tests/ParcelSizeTests.cs)
  ⎿  Added 9 lines
      58          { 10_000m, 5m, 5m },
      59      };
      60  
      61 +    [Theory]                                                   
      62 +    [InlineData(60, 5, 5)]                                     
      63 +    [InlineData(5, 60, 5)]                                     
      64 +    [InlineData(5, 5, 60)]                                     
      65 +    public void Largest_dimension_decides_the_size_whichever_po
         +sition_it_is_in(int length, int width, int height)             
      66 +    {                                                          
      67 +        Assert.Equal(ParcelSize.Large, SizeOf(length, width, he
         +ight));                                                        
      68 +    }                                                          
      69 +                                                               
      70      private static ParcelSize SizeOf(decimal length, decimal wi
          dth, decimal height)
      71      {
      72          var cart = new Cart();

  Ran 1 shell command (ctrl+o to expand)

● The orientation theory passed straight away. That's expected, because
  classification already uses the maximum dimension. Task 2.6 is the refactor
  into an internal class:

● Bash(cd /home/sam/workspace/courier-kata/src/CourierKata && cat >
      ParcelPricing.cs <<'EOF'
      namespace CourierKata;…)
  ⎿  Passed!  - Failed:     0, Passed:    15, Skipped:     0, Total:    15,
      Duration: 39 ms - CourierKata.Tests.dll (net10.0)
     ?? src/CourierKata/Cart.cs
     … +6 lines (ctrl+o to expand)
  ⎿  Created src/CourierKata/ParcelPricing.cs (+18 -0)
       1 +namespace CourierKata;                                         
       2 +                                                               
       3 +/// <summary>The rules that decide what size a parcel is charge
         +d at.</summary>                                                
       4 +internal static class ParcelPricing                            
       5 +{                                                              
       6 +    public static ParcelSize Classify(Parcel parcel)           
       7 +    {                                                          
       8 +        var largest = Math.Max(parcel.Length, Math.Max(parcel.W
         +idth, parcel.Height));                                         
       9 +                                                               
      10 +        return largest switch                                  
      11 +        {                                                      
      12 +            < 10m => ParcelSize.Small,                         
      13 +            < 50m => ParcelSize.Medium,                        
      14 +            < 100m => ParcelSize.Large,                        
      15 +            _ => ParcelSize.ExtraLarge,                        
      16 +        };                                                     
      17 +    }                                                          
      18 +}                                                              
  ⎿  Updated src/CourierKata/Cart.cs (+1 -13)
      14  
      15      /// <summary>Prices every parcel in the cart.</summary>
      16      public Quote GetQuote() =>
      17 -        new(_parcels.Select(parcel => new LineItem(parcel, Clas
         -sify(parcel))).ToList());                                      
      18 -                                                               
      19 -    private static ParcelSize Classify(Parcel parcel)          
      20 -    {                                                          
      21 -        var largest = Math.Max(parcel.Length, Math.Max(parcel.W
         -idth, parcel.Height));                                         
      22 -        return largest switch                                  
      23 -        {                                                      
      24 -            < 10m => ParcelSize.Small,                         
      25 -            < 50m => ParcelSize.Medium,                        
      26 -            < 100m => ParcelSize.Large,                        
      27 -            _ => ParcelSize.ExtraLarge,                        
      28 -        };                                                     
      29 -    }                                                          
      17 +        new(_parcels.Select(parcel => new LineItem(parcel, Parc
         +elPricing.Classify(parcel))).ToList());                        
      18  }

● Task 2.7 adds the assumptions and commits:

● Bash(cat >> docs/assumptions.md <<'EOF'…)
  ⎿  f94999c Classify parcels by size
  ⎿  Updated docs/assumptions.md (+44 -0)
       3  A running log of decisions made where the requirements were unc
          lear. Each entry
       4  records what was unclear, what was assumed, and why. Entries ar
          e added in the
       5  same commit as the code that depends on them.
       6 +                                                               
       7 +## Parcel size classification                                  
       8 +                                                               
       9 +### XL threshold is 100 cm or more                             
      10 +                                                               
      11 +- **Unclear:** The brief says "XL parcel: any dimension 100cm" 
         +with no comparison.                                            
      12 +- **Assumed:** A parcel is XL when its largest dimension is `>=
         + 100` cm.                                                      
      13 +- **Why:** The other brackets use a strict `<`, so `>= 100` is 
         +the only reading                                               
      14 +  that leaves no gaps between brackets.                        
      15 +                                                               
      16 +### Boundary values belong to the larger size                  
      17 +                                                               
      18 +- **Unclear:** Which size a parcel takes when a dimension is ex
         +actly 10 or 50 cm.                                             
      19 +- **Assumed:** It goes up a bracket: 10 cm is Medium, 50 cm is 
         +Large.                                                         
      20 +- **Why:** This follows directly from the strict `<` in "all di
         +mensions < 10 cm".                                             
      21 +                                                               
      22 +### Size is decided by the largest dimension                   
      23 +                                                               
      24 +- **Unclear:** How "all dimensions" and "any dimension" combine
         + across three                                                  
      25 +  measurements.                                                
      26 +- **Assumed:** Only the largest of the three dimensions matters
         +; which position                                               
      27 +  it is in (length, width or height) does not.                 
      28 +- **Why:** "All dimensions < N" is equivalent to "largest dimen
         +sion < N".                                                     
      29 +                                                               
      30 +### Three decimal dimensions in centimetres                    
      31 +                                                               
      32 +- **Unclear:** How many dimensions a parcel has, and whether th
         +ey can be                                                      
      33 +  fractional.                                                  
      34 +- **Assumed:** A parcel has length, width and height as `decima
         +l` centimetres.                                                
      35 +- **Why:** Real parcels have three dimensions and fractional si
         +zes; `decimal`                                                 
      36 +  makes boundary comparisons such as 9.99 vs 10 exact.         
      37 +                                                               
      38 +### No upper size limit                                        
      39 +                                                               
      40 +- **Unclear:** Whether very large parcels should be rejected.  
      41 +- **Assumed:** There is no maximum. Any parcel 100 cm or more i
         +s priced as XL.                                                
      42 +- **Why:** The brief gives no limit, and XL is open-ended ("any
         + dimension 100cm").                                            
     … 7 more lines (ctrl+o to expand)
  ⎿  Updated openspec/changes/parcel-size-pricing/tasks.md (+7 -7)
       8  
       9  ## 2. Size classification (tests first, one size at a time)
      10  
      11 -- [ ] 2.1 Write a failing `[Theory]` for Small (e.g. 1x1x1, 9.9
         -9x5x5) that adds a `Parcel` to a `Cart` and asserts the quote's
         - single line item has `ParcelSize.Small`; add the minimal `Parc
         -el`, `ParcelSize`, `LineItem`, `Quote` and `Cart` to make it pa
         -ss; verify with `dotnet test`                                  
      12 -- [ ] 2.2 Write a failing `[Theory]` for Medium (10x5x5 and 49.
         -99x5x5) and extend classification to pass; verify with `dotnet 
         -test`                                                          
      13 -- [ ] 2.3 Write a failing `[Theory]` for Large (50x5x5 and 99.9
         -9x5x5) and extend classification to pass; verify with `dotnet t
         -est`                                                           
      14 -- [ ] 2.4 Write a failing `[Theory]` for XL (100x5x5 and 10,000
         -x5x5) and extend classification to pass; verify with `dotnet te
         -st`                                                            
      15 -- [ ] 2.5 Add a `[Theory]` that the largest dimension decides r
         -egardless of position (60x5x5, 5x60x5, 5x5x60 are all Large); v
         -erify it passes                                                
      16 -- [ ] 2.6 Refactor classification into an internal pricing clas
         -s (design.md, Pricing internals) with no change to tests; verif
         -y `dotnet test` passes                                         
      17 -- [ ] 2.7 Add assumptions for the XL `>= 100` boundary, boundar
         -y values going up a bracket, three decimal cm dimensions, no up
         -per size limit, and `AddParcel` naming to `docs/assumptions.md`
         -; commit with the code                                         
      11 +- [x] 2.1 Write a failing `[Theory]` for Small (e.g. 1x1x1, 9.9
         +9x5x5) that adds a `Parcel` to a `Cart` and asserts the quote's
         + single line item has `ParcelSize.Small`; add the minimal `Parc
         +el`, `ParcelSize`, `LineItem`, `Quote` and `Cart` to make it pa
         +ss; verify with `dotnet test`                                  
      12 +- [x] 2.2 Write a failing `[Theory]` for Medium (10x5x5 and 49.
         +99x5x5) and extend classification to pass; verify with `dotnet 
         +test`                                                          
      13 +- [x] 2.3 Write a failing `[Theory]` for Large (50x5x5 and 99.9
         +9x5x5) and extend classification to pass; verify with `dotnet t
         +est`                                                           
      14 +- [x] 2.4 Write a failing `[Theory]` for XL (100x5x5 and 10,000
         +x5x5) and extend classification to pass; verify with `dotnet te
         +st`                                                            
      15 +- [x] 2.5 Add a `[Theory]` that the largest dimension decides r
         +egardless of position (60x5x5, 5x60x5, 5x5x60 are all Large); v
         +erify it passes                                                
      16 +- [x] 2.6 Refactor classification into an internal pricing clas
         +s (design.md, Pricing internals) with no change to tests; verif
         +y `dotnet test` passes                                         
      17 +- [x] 2.7 Add assumptions for the XL `>= 100` boundary, boundar
         +y values going up a bracket, three decimal cm dimensions, no up
         +per size limit, and `AddParcel` naming to `docs/assumptions.md`
         +; commit with the code                                         
      18  
      19  ## 3. Cost per size
      20  

● Task 3.1, cost per size, test first:

● Write(tests/CourierKata.Tests/ParcelCostTests.cs)
  ⎿  Wrote 20 lines to tests/CourierKata.Tests/ParcelCostTests.cs
       1 namespace CourierKata.Tests;
       2
       3 public class ParcelCostTests
       4 {
       5     [Theory]
       6     [InlineData(5, ParcelSize.Small, 3)]
       7     [InlineData(20, ParcelSize.Medium, 8)]
       8     [InlineData(70, ParcelSize.Large, 15)]
       9     [InlineData(150, ParcelSize.ExtraLarge, 25)]
      10     public void Each_size_has_a_fixed_cost(int largestDimension,
     … +11 lines (ctrl+o to expand)

  Ran 1 shell command (ctrl+o to expand)

● Bash(cd /home/sam/workspace/courier-kata/src/CourierKata && cat > LineItem.cs
      <<'EOF'
      namespace CourierKata;…)
  ⎿  namespace CourierKata;

     /// <summary>The rules that decide what size a parcel is charged at an
     … +35 lines (ctrl+o to expand)
  ⎿  Updated src/CourierKata/LineItem.cs (+2 -2)
      1  namespace CourierKata;
      2  
      3 -/// <summary>One parcel in a quote, with the size it is charged 
        -at.</summary>                                                   
      4 -public sealed record LineItem(Parcel Parcel, ParcelSize Size);  
      3 +/// <summary>One parcel in a quote, with the size it is charged 
        +at and its cost in dollars.</summary>                           
      4 +public sealed record LineItem(Parcel Parcel, ParcelSize Size, de
        +cimal Cost);                                                    
  ⎿  Updated src/CourierKata/ParcelPricing.cs (+16 -1)
      1  namespace CourierKata;
      2  
      3 -/// <summary>The rules that decide what size a parcel is charged
        - at.</summary>                                                  
      3 +/// <summary>The rules that decide what size a parcel is charged
        + at and what it costs.</summary>                                
      4  internal static class ParcelPricing
      5  {
      6      public static ParcelSize Classify(Parcel parcel)
     ...
      15              _ => ParcelSize.ExtraLarge,
      16          };
      17      }
      18 +                                                               
      19 +    public static decimal PriceOf(ParcelSize size) => size swit
         +ch                                                             
      20 +    {                                                          
      21 +        ParcelSize.Small => 3m,                                
      22 +        ParcelSize.Medium => 8m,                               
      23 +        ParcelSize.Large => 15m,                               
      24 +        ParcelSize.ExtraLarge => 25m,                          
      25 +        _ => throw new ArgumentOutOfRangeException(nameof(size)
         +, size, "Unknown parcel size."),                               
      26 +    };                                                         
      27 +                                                               
      28 +    public static LineItem Price(Parcel parcel)                
      29 +    {                                                          
      30 +        var size = Classify(parcel);                           
      31 +        return new LineItem(parcel, size, PriceOf(size));      
      32 +    }                                                          
      33  }
  ⎿  Updated src/CourierKata/Cart.cs (+1 -1)
      14  
      15      /// <summary>Prices every parcel in the cart.</summary>
      16      public Quote GetQuote() =>
      17 -        new(_parcels.Select(parcel => new LineItem(parcel, Parc
         -elPricing.Classify(parcel))).ToList());                        
      17 +        new(_parcels.Select(ParcelPricing.Price).ToList());    
      18  }

● Bash(sed -i 's/    public static ParcelSize Classify/    private static
      ParcelSize Classify/; s/    public static decimal PriceOf/    private
      static decimal PriceOf/…)
  ⎿  Passed!  - Failed:     0, Passed:    19, Skipped:     0, Total:    19,
      Duration: 40 ms - CourierKata.Tests.dll (net10.0)
     FORMAT_OK
     13b2e5e Price each parcel by its size
  ⎿  Updated src/CourierKata/ParcelPricing.cs (+2 -2)
      3  /// <summary>The rules that decide what size a parcel is charged
          at and what it costs.</summary>
      4  internal static class ParcelPricing
      5  {
      6 -    public static ParcelSize Classify(Parcel parcel)            
      6 +    private static ParcelSize Classify(Parcel parcel)           
      7      {
      8          var largest = Math.Max(parcel.Length, Math.Max(parcel.Wi
         dth, parcel.Height));
      9  
     ...
      16          };
      17      }
      18  
      19 -    public static decimal PriceOf(ParcelSize size) => size swit
         -ch                                                             
      19 +    private static decimal PriceOf(ParcelSize size) => size swi
         +tch                                                            
      20      {
      21          ParcelSize.Small => 3m,
      22          ParcelSize.Medium => 8m,
  ⎿  Updated docs/assumptions.md (+19 -0)
      47  - **Assumed:** The method is `Cart.AddParcel`.
      48  - **Why:** PascalCase is the C# convention for public methods; 
          the brief's
      49    camelCase reads as language-neutral.
      50 +                                                               
      51 +## Pricing                                                     
      52 +                                                               
      53 +### Costs are plain decimal dollars                            
      54 +                                                               
      55 +- **Unclear:** What currency costs are in and how money should 
         +be represented.                                                
      56 +- **Assumed:** Costs are `decimal` amounts in US dollars, with 
         +no currency type.                                              
      57 +- **Why:** The brief only uses `$` and has no multi-currency re
         +quirement, so a                                                
      58 +  `Money` type would add nothing yet. `decimal` keeps amounts e
         +xact.                                                          
      59 +                                                               
      60 +### Cheapest option is the smallest size that fits             
      61 +                                                               
      62 +- **Unclear:** "In all circumstances the cheapest option for se
         +nding each parcel                                              
      63 +  should be selected" implies a choice between options.        
      64 +- **Assumed:** With size-only pricing, the only option is the s
         +mallest size the                                               
      65 +  parcel fits, which is also the cheapest because prices rise w
         +ith size. No                                                   
      66 +  general "compare every option" logic is built yet.           
      67 +- **Why:** This change covers step 1 only. A selector between c
         +ompeting options                                               
      68 +  belongs with the later rule (such as weight) that makes it ne
         +cessary.                                                       
  ⎿  Updated openspec/changes/parcel-size-pricing/tasks.md (+2 -2)
      18  
      19  ## 3. Cost per size
      20  
      21 -- [ ] 3.1 Write a failing `[Theory]` asserting line-item costs 
         -Small $3, Medium $8, Large $15, XL $25, then implement the pric
         -e lookup; verify with `dotnet test`                            
      22 -- [ ] 3.2 Add assumptions for decimal USD with no currency type
         - and "cheapest = smallest fitting size" for this step; commit w
         -ith the code                                                   
      21 +- [x] 3.1 Write a failing `[Theory]` asserting line-item costs 
         +Small $3, Medium $8, Large $15, XL $25, then implement the pric
         +e lookup; verify with `dotnet test`                            
      22 +- [x] 3.2 Add assumptions for decimal USD with no currency type
         + and "cheapest = smallest fitting size" for this step; commit w
         +ith the code                                                   
      23  
      24  ## 4. Parcel validation
      25  

● Task 4.1, validation, test first:

● Write(tests/CourierKata.Tests/ParcelTests.cs)
  ⎿  Wrote 27 lines to tests/CourierKata.Tests/ParcelTests.cs
       1 namespace CourierKata.Tests;
       2
       3 public class ParcelTests
       4 {
       5     [Theory]
       6     [InlineData(0, 5, 5, "Length")]
       7     [InlineData(-1, 5, 5, "Length")]
       8     [InlineData(5, 0, 5, "Width")]
       9     [InlineData(5, -5, 5, "Width")]
      10     [InlineData(5, 5, 0, "Height")]
     … +17 lines (ctrl+o to expand)

  Ran 1 shell command (ctrl+o to expand)
● Bash(cat > src/CourierKata/Parcel.cs <<'EOF'
      namespace CourierKata;…)
  ⎿  Passed!  - Failed:     0, Passed:    26, Skipped:     0, Total:    26,
      Duration: 39 ms - CourierKata.Tests.dll (net10.0)
     FORMAT_OK
  ⎿  Updated src/CourierKata/Parcel.cs (+18 -1)
       1  namespace CourierKata;
       2  
       3  /// <summary>A parcel to deliver, with its dimensions in centim
          etres.</summary>
       4 -public sealed record Parcel(decimal Length, decimal Width, deci
         -mal Height);                                                   
       4 +/// <exception cref="ArgumentOutOfRangeException">A dimension i
         +s zero or negative.</exception>                                
       5 +public sealed record Parcel(decimal Length, decimal Width, deci
         +mal Height)                                                    
       6 +{                                                              
       7 +    /// <summary>Length in centimetres.</summary>              
       8 +    public decimal Length { get; } = Positive(Length, nameof(Le
         +ngth));                                                        
       9 +                                                               
      10 +    /// <summary>Width in centimetres.</summary>               
      11 +    public decimal Width { get; } = Positive(Width, nameof(Widt
         +h));                                                           
      12 +                                                               
      13 +    /// <summary>Height in centimetres.</summary>              
      14 +    public decimal Height { get; } = Positive(Height, nameof(He
         +ight));                                                        
      15 +                                                               
      16 +    private static decimal Positive(decimal value, string dimen
         +sion)                                                          
      17 +    {                                                          
      18 +        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value
         +, dimension);                                                  
      19 +        return value;                                          
      20 +    }                                                          
      21 +}                                                              

● Bash(cat >> docs/assumptions.md <<'EOF'…)
  ⎿  ab0751a Reject parcels with zero or negative dimensions
  ⎿  Updated docs/assumptions.md (+13 -0)
      66    general "compare every option" logic is built yet.
      67  - **Why:** This change covers step 1 only. A selector between c
          ompeting options
      68    belongs with the later rule (such as weight) that makes it ne
          cessary.
      69 +                                                               
      70 +## Validation                                                  
      71 +                                                               
      72 +### Zero or negative dimensions are rejected                   
      73 +                                                               
      74 +- **Unclear:** The brief does not say what happens with a dimen
         +sion of zero or                                                
      75 +  less.                                                        
      76 +- **Assumed:** Creating a `Parcel` with any dimension that is z
         +ero or negative                                                
      77 +  throws `ArgumentOutOfRangeException` naming that dimension.  
      78 +- **Why:** A physical parcel can't measure 0 cm or less, and fa
         +iling early is                                                 
      79 +  better than quoting $3 for nonsense input. Validating in the 
         +`Parcel`                                                       
      80 +  constructor means an invalid parcel can never reach a `Cart`.
         + `decimal` has no                                              
      81 +  NaN or infinity, so no other checks are needed.              
  ⎿  Updated openspec/changes/parcel-size-pricing/tasks.md (+2 -2)
      23  
      24  ## 4. Parcel validation
      25  
      26 -- [ ] 4.1 Write failing tests that zero and negative values for
         - each of length, width and height throw `ArgumentOutOfRangeExce
         -ption` whose `ParamName` names the dimension; implement validat
         -ion in `Parcel`; verify with `dotnet test`                     
      27 -- [ ] 4.2 Add the assumption that zero or negative dimensions a
         -re rejected; commit with the code                              
      26 +- [x] 4.1 Write failing tests that zero and negative values for
         + each of length, width and height throw `ArgumentOutOfRangeExce
         +ption` whose `ParamName` names the dimension; implement validat
         +ion in `Parcel`; verify with `dotnet test`                     
      27 +- [x] 4.2 Add the assumption that zero or negative dimensions a
         +re rejected; commit with the code                              
      28  
      29  ## 5. Cart and quote behaviour
      30  

● Group 5 tests, cart and quote behaviour:

● Write(tests/CourierKata.Tests/CartTests.cs)
  ⎿  Wrote 105 lines to tests/CourierKata.Tests/CartTests.cs
       1 namespace CourierKata.Tests;
       2
       3 public class CartTests
       4 {
       5     private static readonly Parcel Small = new(5m, 5m, 5m);
       6     private static readonly Parcel Medium = new(20m, 5m, 5m);
       7     private static readonly Parcel Large = new(70m, 5m, 5m);
       8     private static readonly Parcel ExtraLarge = new(150m, 5m, 5m
         );
       9
     … +96 lines (ctrl+o to expand)

  Ran 1 shell command (ctrl+o to expand)

● Bash(cat > src/CourierKata/Quote.cs <<'EOF'
      namespace CourierKata;…)
  ⎿  Passed!  - Failed:     0, Passed:    34, Skipped:     0, Total:    34,
      Duration: 42 ms - CourierKata.Tests.dll (net10.0)
     FORMAT_OK
  ⎿  Updated src/CourierKata/Quote.cs (+2 -2)
      1  namespace CourierKata;
      2  
      3 -/// <summary>The priced contents of a cart.</summary>           
      4 -public sealed record Quote(IReadOnlyList<LineItem> Items);      
      3 +/// <summary>The priced contents of a cart: one line item per pa
        +rcel and the total cost in dollars.</summary>                   
      4 +public sealed record Quote(IReadOnlyList<LineItem> Items, decima
        +l Total);                                                       
  ⎿  Updated src/CourierKata/Cart.cs (+5 -2)
      13      }
      14  
      15      /// <summary>Prices every parcel in the cart.</summary>
      16 -    public Quote GetQuote() =>                                 
      17 -        new(_parcels.Select(ParcelPricing.Price).ToList());    
      16 +    public Quote GetQuote()                                    
      17 +    {                                                          
      18 +        var items = _parcels.Select(ParcelPricing.Price).ToList
         +().AsReadOnly();                                               
      19 +        return new Quote(items, items.Sum(item => item.Cost)); 
      20 +    }                                                          
      21  }

● Bash(cat >> docs/assumptions.md <<'EOF'…)
  ⎿  c63ec0f Quote a cart with ordered line items and a total
  ⎿  Updated docs/assumptions.md (+23 -0)
       79    better than quoting $3 for nonsense input. Validating in the
            `Parcel`
       80    constructor means an invalid parcel can never reach a `Cart`
           . `decimal` has no
       81    NaN or infinity, so no other checks are needed.
       82 +                                                              
       83 +## Cart and quote                                             
       84 +                                                              
       85 +### Line items keep insertion order                           
       86 +                                                              
       87 +- **Unclear:** The brief asks for "a collection of items" with
          +out an order.                                                 
       88 +- **Assumed:** A quote lists one line item per parcel in the o
          +rder the parcels                                              
       89 +  were added, including repeats of the same parcel.           
       90 +- **Why:** It's predictable for callers and easy to match item
          +s back to input.                                              
       91 +                                                              
       92 +### Empty cart costs nothing                                  
       93 +                                                              
       94 +- **Unclear:** What a quote for an empty cart should be.      
       95 +- **Assumed:** It has no line items and a total of $0; it is n
          +ot an error.                                                  
       96 +- **Why:** Zero is the natural total of nothing, and an empty 
          +cart is a valid                                               
       97 +  state before parcels are added.                             
       98 +                                                              
       99 +### Getting a quote does not change the cart                  
      100 +                                                              
      101 +- **Unclear:** Whether producing the output "checks out" the c
          +art.                                                          
      102 +- **Assumed:** `GetQuote()` is a read. It can be called repeat
          +edly, and each                                                
      103 +  quote is an independent snapshot that later `AddParcel` call
          +s don't change.                                               
      104 +- **Why:** The brief describes calculating a cost, not complet
          +ing a purchase.                                               
  ⎿  Updated openspec/changes/parcel-size-pricing/tasks.md (+4 -4)
      28  
      29  ## 5. Cart and quote behaviour
      30  
      31 -- [ ] 5.1 Write failing tests for line items in insertion order
         - (XL, Small, Medium) and for adding the same parcel twice; impl
         -ement; verify with `dotnet test`                               
      32 -- [ ] 5.2 Write failing tests for the total (one of each size =
         - $51) and the empty cart (no items, $0); implement; verify with
         - `dotnet test`                                                 
      33 -- [ ] 5.3 Write failing tests that quoting twice gives equal re
         -sults and that a quote taken before adding a parcel is unchange
         -d afterwards; implement (copy items into a read-only list); ver
         -ify with `dotnet test`                                         
      34 -- [ ] 5.4 Add assumptions for empty cart = $0 and insertion ord
         -er; commit with the code                                       
      31 +- [x] 5.1 Write failing tests for line items in insertion order
         + (XL, Small, Medium) and for adding the same parcel twice; impl
         +ement; verify with `dotnet test`                               
      32 +- [x] 5.2 Write failing tests for the total (one of each size =
         + $51) and the empty cart (no items, $0); implement; verify with
         + `dotnet test`                                                 
      33 +- [x] 5.3 Write failing tests that quoting twice gives equal re
         +sults and that a quote taken before adding a parcel is unchange
         +d afterwards; implement (copy items into a read-only list); ver
         +ify with `dotnet test`                                         
      34 +- [x] 5.4 Add assumptions for empty cart = $0 and insertion ord
         +er; commit with the code                                       
      35  
      36  ## 6. Final checks
      37  

● Now task 6.1, the final checks:

  Ran 1 shell command (ctrl+o to expand)

● Bash(cd /home/sam/workspace/courier-kata; sed -i 's/^- \[ \] 6\.1 /- [x] 6.1
      /' openspec/changes/parcel-size-pricing/tasks.md && git commit -q -am
      "Mark parcel-size-…)
  ⎿  45985ab Mark parcel-size-pricing final checks complete
     c63ec0f Quote a cart with ordered line items and a total
     ab0751a Reject parcels with zero or negative dimensions
     … +4 lines (ctrl+o to expand)
  ⎿  Updated openspec/changes/parcel-size-pricing/tasks.md (+1 -1)
      35  
      36  ## 6. Final checks
      37  
      38 -- [ ] 6.1 Confirm the public surface is only `Cart`, `Parcel`, 
         -`ParcelSize`, `LineItem` and `Quote` (everything else `internal
         -`), and that every spec scenario in `specs/parcel-pricing/spec.
         -md` has a matching test; verify `dotnet build` has zero warning
         -s, `dotnet test` passes, `docker compose run --rm tests` passes
         -, and `dotnet format --verify-no-changes` succeeds             
      38 +- [x] 6.1 Confirm the public surface is only `Cart`, `Parcel`, 
         +`ParcelSize`, `LineItem` and `Quote` (everything else `internal
         +`), and that every spec scenario in `specs/parcel-pricing/spec.
         +md` has a matching test; verify `dotnet build` has zero warning
         +s, `dotnet test` passes, `docker compose run --rm tests` passes
         +, and `dotnet format --verify-no-changes` succeeds             
      39  
      40  ## Workflow follow-up
      41  

● Implementation complete: parcel-size-pricing

  All 19 tasks are done. The 34 tests pass both through my Docker wrapper and
  with docker compose run --rm tests. The build has zero warnings and dotnet
  format --verify-no-changes is clean.

  There's no .NET SDK on this machine, so I ran every dotnet command in the
  sdk:10.0 image as your user. That way no root-owned bin//obj/ files end up in
  the repo, and the .gitignore covers them.

  Commits, on a new branch parcel-size-pricing cut from master:
  f69cb30 Scaffold library and test projects
  f94999c Classify parcels by size
  13b2e5e Price each parcel by its size
  ab0751a Reject parcels with zero or negative dimensions
  c63ec0f Quote a cart with ordered line items and a total
  45985ab Mark parcel-size-pricing final checks complete

  What was built (in src/CourierKata/):
  - Public types: Cart (AddParcel, GetQuote), Parcel, ParcelSize, LineItem,
    Quote.
  - Internal rules: all classification and pricing sit in one internal class,
    ParcelPricing.cs (src/CourierKata/ParcelPricing.cs).
  - Tests first, one size at a time: each size was its own failing-then-passing
    [Theory], with cases just below and exactly on 10, 50 and 100. They're in
    ParcelSizeTests.cs (tests/CourierKata.Tests/ParcelSizeTests.cs).
  - Assumptions: docs/assumptions.md has 12 entries, each added in the commit
    that depends on it. One of them is your "very large parcels aren't
    rejected", under "No upper size limit".

  Things that differ slightly from the plan or are worth knowing:
  - Parcel can't be copied with with. Its properties are get-only so validation
    can't be bypassed, which means parcel with { Length = ... } won't compile.
  - One extra test beyond the spec: AddParcel(null) throws
    ArgumentNullException.
  - The orientation test passed as soon as I wrote it. Classification already
    used the largest dimension, so it acts as a regression check rather than
    having driven any code.
  - .sln, not .slnx. .NET 10 creates .slnx by default; I forced .sln to match
    the CourierKata.sln name in CLAUDE.md.

  Next, review the branch, then run /opsx:archive. That moves the spec into
  openspec/specs/parcel-pricing/ and needs its own commit.

❯ /opsx:archive

  Ran 1 shell command (ctrl+o to expand)

● Using change: parcel-size-pricing (to archive a different one, run
  /opsx:archive <other>). All 4 artifacts are done and all 19 tasks are
  complete.

● User answered Claude's questions:
  ⎿  · The parcel-size-pricing change has a delta spec for a new capability,
     parcel-pricing, with 7 ADDED requirements and no MODIFIED/REMOVED ones.
     openspec/specs/parcel-pricing/spec.md doesn't exist yet and would be
     created from it. Sync before archiving? → Sync now (Recommended)

  Ran 1 shell command (ctrl+o to expand)

● Bash(cd /home/sam/workspace/courier-kata; mkdir -p
      openspec/specs/parcel-pricing && python3 - <<'EOF'
      src='openspec/changes/parcel-size-pricing/specs/parcel-pricing/…)
  ⎿  # parcel-pricing Specification

     ## Purpose
     … +17 lines (ctrl+o to expand)
  ⎿  Created openspec/specs/parcel-pricing/spec.md (+96 -0)
       1 +# parcel-pricing Specification                                 
       2 +                                                               
       3 +## Purpose                                                     
       4 +                                                               
       5 +Prices a cart of parcels for delivery by classifying each parce
         +l by its size and producing a quote with per-parcel costs and a
         + total.                                                        
       6 +                                                               
       7 +## Requirements                                                
       8 +                                                               
       9 +### Requirement: Parcel dimensions are validated               
      10 +A parcel SHALL have three dimensions (length, width, height) in
         + centimetres. Each dimension MUST be a positive, finite number.
         + Creating a parcel with an invalid dimension MUST fail with an 
         +error that identifies the invalid dimension. There is no upper 
         +limit on a dimension.                                          
      11 +                                                               
      12 +#### Scenario: Zero dimension is rejected                      
      13 +- **WHEN** a parcel is created with a width of 0 cm            
      14 +- **THEN** creation fails with an out-of-range error naming the
         + width                                                         
      15 +                                                               
      16 +#### Scenario: Negative dimension is rejected                  
      17 +- **WHEN** a parcel is created with a height of -5 cm          
      18 +- **THEN** creation fails with an out-of-range error naming the
         + height                                                        
      19 +                                                               
      20 +#### Scenario: Very large parcel is accepted                   
      21 +- **WHEN** a parcel is created with a length of 10,000 cm      
      22 +- **THEN** the parcel is created successfully                  
      23 +                                                               
      24 +### Requirement: Parcels are classified by their largest dimens
         +ion                                                            
      25 +Each parcel SHALL be classified into exactly one size by its la
         +rgest dimension: Small when it is under 10 cm, Medium when unde
         +r 50 cm, Large when under 100 cm, and XL when 100 cm or more. A
         + dimension exactly on a boundary belongs to the larger size. Th
         +e orientation of the dimensions MUST NOT affect the size.      
      26 +                                                               
      27 +#### Scenario: Small parcel                                    
      28 +- **WHEN** a parcel measuring 9.99 x 5 x 5 cm is quoted        
      29 +- **THEN** its size is Small                                   
      30 +                                                               
      31 +#### Scenario: Medium parcel at the lower boundary             
      32 +- **WHEN** a parcel measuring 10 x 5 x 5 cm is quoted          
      33 +- **THEN** its size is Medium                                  
      34 +                                                               
      35 +#### Scenario: Medium parcel below the upper boundary          
      36 +- **WHEN** a parcel measuring 49.99 x 5 x 5 cm is quoted       
      37 +- **THEN** its size is Medium                                  
      38 +                                                               
      39 +#### Scenario: Large parcel at the lower boundary              
      40 +- **WHEN** a parcel measuring 50 x 5 x 5 cm is quoted          
     … 56 more lines (ctrl+o to expand)

● The sync is verified: all 7 requirements and 17 scenarios are present, no
  delta headers remain, and strict validation passes. Now archiving and
  committing:

● Bash(cd /home/sam/workspace/courier-kata;
      T=openspec/changes/archive/2026-11-11-parcel-size-pricing; mkdir -p
      openspec/changes/archive && test ! -e $T && mv openspec…)
  ⎿  .
     ..
     design.md
     … +6 lines (ctrl+o to expand)
  ⎿  Created
     openspec/changes/archive/2026-11-11-parcel-size-pricing/proposal.md
     (+31 -0)
       1 +# Proposal                                                     
       2 +                                                               
       3 +## Why                                                         
       4 +                                                               
       5 +The courier kata needs its first behaviour: pricing a set of pa
         +rcels by size. Nothing exists yet, so this change also establis
         +hes the library, its test project, and the public API shape tha
         +t later steps (weight, speedy shipping, discounts) will build o
         +n.                                                             
       6 +                                                               
       7 +## What Changes                                                
       8 +                                                               
       9 +- Create the solution, `src/CourierKata` class library and `tes
         +ts/CourierKata.Tests` xUnit project (current LTS .NET, nullable
         + enabled, warnings as errors).                                 
      10 +- Add a `Cart` that accepts parcels through `AddParcel` and pro
         +duces a quote.                                                 
      11 +- Classify each parcel by its largest dimension into Small (< 1
         +0 cm, $3), Medium (< 50 cm, $8), Large (< 100 cm, $15) or XL (>
         += 100 cm, $25).                                                
      12 +- Produce a quote listing each parcel with its size and cost, i
         +n the order added, plus the total.                             
      13 +- Reject parcels with zero, negative or non-finite dimensions. 
         +Very large parcels are accepted and priced as XL.              
      14 +- Create `docs/assumptions.md` recording the interpretations ma
         +de where the brief is unclear.                                 
      15 +                                                               
      16 +Out of scope: weight, speedy shipping, discounts, multiple curr
         +encies, and any interface other than the C# API.               
      17 +                                                               
      18 +## Capabilities                                                
      19 +                                                               
      20 +### New Capabilities                                           
      21 +- `parcel-pricing`: classifying parcels by size, pricing them, 
         +and producing a quote for a cart of parcels.                   
      22 +                                                               
      23 +### Modified Capabilities                                      
      24 +                                                               
      25 +None.                                                          
      26 +                                                               
      27 +## Impact                                                      
      28 +                                                               
      29 +- New code only: `CourierKata.sln`, `src/CourierKata/`, `tests/
         +CourierKata.Tests/`, `docs/assumptions.md`.                    
      30 +- New public API: `Cart`, `Parcel`, `ParcelSize`, `LineItem`, `
         +Quote`.                                                        
      31 +- Dependencies: xUnit and the .NET test SDK in the test project
         + only.                                                         
  ⎿  Created
     openspec/changes/archive/2026-11-11-parcel-size-pricing/.openspec.yaml
     (+2 -0)
      1 +schema: spec-driven                                             
      2 +created: 2026-11-11                                             
  ⎿  Created
     openspec/changes/archive/2026-11-11-parcel-size-pricing/tasks.md (+43
     -0)
       1 +# Tasks                                                        
       2 +                                                               
       3 +## 1. Scaffolding                                              
       4 +                                                               
       5 +- [x] 1.1 Create `CourierKata.sln`, `src/CourierKata` (net10.0 
         +class library) and `tests/CourierKata.Tests` (xUnit, referencin
         +g the library), with a root `Directory.Build.props` enabling nu
         +llable, warnings as errors and implicit usings; verify `dotnet 
         +build` succeeds with zero warnings and `dotnet test` runs (no t
         +ests yet)                                                      
       6 +- [x] 1.2 Create `docs/assumptions.md` with a short header expl
         +aining its purpose; verify the file exists                     
       7 +- [x] 1.3 Commit the scaffold after `dotnet format` reports no 
         +changes                                                        
       8 +                                                               
       9 +## 2. Size classification (tests first, one size at a time)    
      10 +                                                               
      11 +- [x] 2.1 Write a failing `[Theory]` for Small (e.g. 1x1x1, 9.9
         +9x5x5) that adds a `Parcel` to a `Cart` and asserts the quote's
         + single line item has `ParcelSize.Small`; add the minimal `Parc
         +el`, `ParcelSize`, `LineItem`, `Quote` and `Cart` to make it pa
         +ss; verify with `dotnet test`                                  
      12 +- [x] 2.2 Write a failing `[Theory]` for Medium (10x5x5 and 49.
         +99x5x5) and extend classification to pass; verify with `dotnet 
         +test`                                                          
      13 +- [x] 2.3 Write a failing `[Theory]` for Large (50x5x5 and 99.9
         +9x5x5) and extend classification to pass; verify with `dotnet t
         +est`                                                           
      14 +- [x] 2.4 Write a failing `[Theory]` for XL (100x5x5 and 10,000
         +x5x5) and extend classification to pass; verify with `dotnet te
         +st`                                                            
      15 +- [x] 2.5 Add a `[Theory]` that the largest dimension decides r
         +egardless of position (60x5x5, 5x60x5, 5x5x60 are all Large); v
         +erify it passes                                                
      16 +- [x] 2.6 Refactor classification into an internal pricing clas
         +s (design.md, Pricing internals) with no change to tests; verif
         +y `dotnet test` passes                                         
      17 +- [x] 2.7 Add assumptions for the XL `>= 100` boundary, boundar
         +y values going up a bracket, three decimal cm dimensions, no up
         +per size limit, and `AddParcel` naming to `docs/assumptions.md`
         +; commit with the code                                         
      18 +                                                               
      19 +## 3. Cost per size                                            
      20 +                                                               
      21 +- [x] 3.1 Write a failing `[Theory]` asserting line-item costs 
         +Small $3, Medium $8, Large $15, XL $25, then implement the pric
         +e lookup; verify with `dotnet test`                            
      22 +- [x] 3.2 Add assumptions for decimal USD with no currency type
         + and "cheapest = smallest fitting size" for this step; commit w
         +ith the code                                                   
      23 +                                                               
      24 +## 4. Parcel validation                                        
      25 +                                                               
      26 +- [x] 4.1 Write failing tests that zero and negative values for
         + each of length, width and height throw `ArgumentOutOfRangeExce
         +ption` whose `ParamName` names the dimension; implement validat
         +ion in `Parcel`; verify with `dotnet test`                     
      27 +- [x] 4.2 Add the assumption that zero or negative dimensions a
         +re rejected; commit with the code                              
      28 +                                                               
      29 +## 5. Cart and quote behaviour                                 
      30 +                                                               
      31 +- [x] 5.1 Write failing tests for line items in insertion order
         + (XL, Small, Medium) and for adding the same parcel twice; impl
         +ement; verify with `dotnet test`                               
      32 +- [x] 5.2 Write failing tests for the total (one of each size =
         + $51) and the empty cart (no items, $0); implement; verify with
         + `dotnet test`                                                 
      33 +- [x] 5.3 Write failing tests that quoting twice gives equal re
         +sults and that a quote taken before adding a parcel is unchange
         +d afterwards; implement (copy items into a read-only list); ver
         +ify with `dotnet test`                                         
      34 +- [x] 5.4 Add assumptions for empty cart = $0 and insertion ord
         +er; commit with the code                                       
      35 +                                                               
      36 +## 6. Final checks                                             
      37 +                                                               
      38 +- [x] 6.1 Confirm the public surface is only `Cart`, `Parcel`, 
         +`ParcelSize`, `LineItem` and `Quote` (everything else `internal
         +`), and that every spec scenario in `specs/parcel-pricing/spec.
         +md` has a matching test; verify `dotnet build` has zero warning
         +s, `dotnet test` passes, `docker compose run --rm tests` passes
         +, and `dotnet format --verify-no-changes` succeeds             
      39 +                                                               
      40 +## Workflow follow-up                                          
     … 3 more lines (ctrl+o to expand)
  ⎿  Created
     openspec/changes/archive/2026-11-11-parcel-size-pricing/design.md (+54
     -0)
       1 +# Design                                                       
       2 +                                                               
       3 +## Context                                                     
       4 +                                                               
       5 +The repository has no code yet; this change creates the solutio
         +n and sets the public API. See proposal.md for scope. The proje
         +ct conventions require a C# class library with no other interfa
         +ce, small public surface, `internal` implementation details, nu
         +llable reference types and warnings as errors. Tests run locall
         +y with `dotnet test` or in Docker via `docker compose run --rm 
         +tests` (.NET 10 SDK image).                                    
       6 +                                                               
       7 +## Goals / Non-Goals                                           
       8 +                                                               
       9 +**Goals:**                                                     
      10 +- A minimal public API that later kata steps can extend without
         + breaking callers.                                             
      11 +- Pricing rules kept in one internal place so they can change i
         +ndependently of `Cart`.                                        
      12 +                                                               
      13 +**Non-Goals:**                                                 
      14 +- Any "evaluate every option, pick the cheapest" machinery. Wit
         +h fixed prices rising by size, the smallest fitting size is the
         + cheapest. A general selector waits until a later step (e.g. we
         +ight) makes it necessary.                                      
      15 +- A `Money` type or currency handling.                         
      16 +                                                               
      17 +## Decisions                                                   
      18 +                                                               
      19 +### Project layout and tooling                                 
      20 +`CourierKata.sln`, `src/CourierKata` (net10.0 class library) an
         +d `tests/CourierKata.Tests` (xUnit). A `Directory.Build.props` 
         +at the root sets `Nullable`, `TreatWarningsAsErrors`, `Implicit
         +Usings` and `LangVersion` once for both projects. The test proj
         +ect uses `InternalsVisibleTo` only if internal tests prove nece
         +ssary; tests should go through the public API by default.      
      21 +                                                               
      22 +### Public types                                               
      23 +```                                                            
      24 +public sealed record Parcel(decimal Length, decimal Width, deci
         +mal Height)  // validates in constructor                       
      25 +public enum ParcelSize { Small, Medium, Large, ExtraLarge }    
      26 +public sealed record LineItem(Parcel Parcel, ParcelSize Size, d
         +ecimal Cost)                                                   
      27 +public sealed record Quote(IReadOnlyList<LineItem> Items, decim
         +al Total)                                                      
      28 +public sealed class Cart { void AddParcel(Parcel parcel); Quote
         + GetQuote(); }                                                 
      29 +```                                                            
      30 +- `Parcel` as a validating record (value object): an invalid pa
         +rcel cannot exist, so `Cart` never re-checks. Alternative: `Add
         +Parcel(l, w, h)` with primitives. Rejected because it spreads v
         +alidation and loses the domain type in `LineItem`.             
      31 +- `decimal` for both centimetres and dollars: exact boundary co
         +mparisons (9.99 vs 10) and exact money. `double` was rejected b
         +ecause of rounding at boundaries. Since `decimal` has no NaN or
         + infinity, "non-finite" can't happen, so validation only needs 
         +`> 0`.                                                         
      32 +- `ExtraLarge` rather than `XL` as the enum name, for C# naming
         +. The spec's "XL" maps to it.                                  
      33 +- `AddParcel` in PascalCase, following C# convention rather tha
         +n the brief's `addParcel`.                                     
      34 +- `GetQuote()` is a pure read that returns a new immutable `Quo
         +te` (items copied into a read-only list). It returns a new valu
         +e rather than exposing internal state, so earlier quotes stay u
         +nchanged when the cart changes.                                
      35 +- `Quote.Total` is computed by the `Quote` creator (the interna
         +l pricing code), not supplied by callers. Because `Quote` is a 
         +public record, a caller could build one with an inconsistent to
         +tal. That's acceptable for a value-carrying output type; if nee
         +ded later, a private constructor plus factory can lock it down.
      36 +                                                               
      37 +### Pricing internals                                          
      38 +An `internal static class PriceList` (or similar) holds:       
      39 +- `ParcelSize Classify(Parcel)`: uses `Math.Max` of the three d
         +imensions against ordered thresholds 10 / 50 / 100.            
      40 +- `decimal PriceOf(ParcelSize)`: a switch expression.          
     … 14 more lines (ctrl+o to expand)
  ⎿  Created openspec/changes/archive/2026-11-11-parcel-size-pricing/specs/
     parcel-pricing/spec.md (+96 -0)
       1 +# Spec Delta                                                   
       2 +                                                               
       3 +## Purpose                                                     
       4 +                                                               
       5 +Prices a cart of parcels for delivery by classifying each parce
         +l by its size and producing a quote with per-parcel costs and a
         + total.                                                        
       6 +                                                               
       7 +## ADDED Requirements                                          
       8 +                                                               
       9 +### Requirement: Parcel dimensions are validated               
      10 +A parcel SHALL have three dimensions (length, width, height) in
         + centimetres. Each dimension MUST be a positive, finite number.
         + Creating a parcel with an invalid dimension MUST fail with an 
         +error that identifies the invalid dimension. There is no upper 
         +limit on a dimension.                                          
      11 +                                                               
      12 +#### Scenario: Zero dimension is rejected                      
      13 +- **WHEN** a parcel is created with a width of 0 cm            
      14 +- **THEN** creation fails with an out-of-range error naming the
         + width                                                         
      15 +                                                               
      16 +#### Scenario: Negative dimension is rejected                  
      17 +- **WHEN** a parcel is created with a height of -5 cm          
      18 +- **THEN** creation fails with an out-of-range error naming the
         + height                                                        
      19 +                                                               
      20 +#### Scenario: Very large parcel is accepted                   
      21 +- **WHEN** a parcel is created with a length of 10,000 cm      
      22 +- **THEN** the parcel is created successfully                  
      23 +                                                               
      24 +### Requirement: Parcels are classified by their largest dimens
         +ion                                                            
      25 +Each parcel SHALL be classified into exactly one size by its la
         +rgest dimension: Small when it is under 10 cm, Medium when unde
         +r 50 cm, Large when under 100 cm, and XL when 100 cm or more. A
         + dimension exactly on a boundary belongs to the larger size. Th
         +e orientation of the dimensions MUST NOT affect the size.      
      26 +                                                               
      27 +#### Scenario: Small parcel                                    
      28 +- **WHEN** a parcel measuring 9.99 x 5 x 5 cm is quoted        
      29 +- **THEN** its size is Small                                   
      30 +                                                               
      31 +#### Scenario: Medium parcel at the lower boundary             
      32 +- **WHEN** a parcel measuring 10 x 5 x 5 cm is quoted          
      33 +- **THEN** its size is Medium                                  
      34 +                                                               
      35 +#### Scenario: Medium parcel below the upper boundary          
      36 +- **WHEN** a parcel measuring 49.99 x 5 x 5 cm is quoted       
      37 +- **THEN** its size is Medium                                  
      38 +                                                               
      39 +#### Scenario: Large parcel at the lower boundary              
      40 +- **WHEN** a parcel measuring 50 x 5 x 5 cm is quoted          
     … 56 more lines (ctrl+o to expand)
  ⎿  … 5 more files changed

● I've archived parcel-size-pricing, which used the spec-driven schema.

  - Spec: I created the main spec at openspec/specs/parcel-pricing/spec.md from
    the change's spec. It has all 7 requirements and 17 scenarios, and passes
    openspec validate --strict.
  - Archive: the change is now in
    openspec/changes/archive/2026-11-11-parcel-size-pricing/, with
    .openspec.yaml kept. All 4 artifacts and all 19 tasks were complete, so
    there were no warnings.
  - Commit: the move and the new spec are committed as 55c0c3c Archive
    parcel-size-pricing change. The working tree is clean and there are no
    active changes left.

  All 7 commits are on the parcel-size-pricing branch, cut from master. It
  hasn't been merged or pushed; tell me if you want either done.

❯ squash and merge into master

  Merged master, ran 1 shell command (ctrl+o to expand)

● I squash-merged the branch into master as one commit, 48f692b Add parcel
  pricing by size. It sits on top of a205672. The commit body summarises the
  API, the size and price brackets, and where the assumptions and spec live.

  master now has exactly the same files as the branch, and all 34 tests pass on
  it.

  Nothing was pushed, and the parcel-size-pricing branch still exists. Because
  it was squashed, git won't see it as merged, so delete it with git branch -D
  parcel-size-pricing. I can do that, or push master, if you want.