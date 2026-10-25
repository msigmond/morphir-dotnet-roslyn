# D20 — MSBuild adapter

- status: planned
- contract: [source structure](../docs/source-structure.md)
- prerequisites: [D19](D19-classic-complete.md), [D2](D02-project-inputs.md)
- evidence: [migration state](../docs/deliveries/source-structure-state.md)

## Work

- Add the MSBuild task, package, targets, and generated-input handoff.
- Use the same structure converter as the CLI.
- Keep project settings and failures without adding execution semantics.

## Acceptance

- CLI and MSBuild agree on declarations, operations, source metadata, and diagnostics.
- Failed conversion preserves existing output.
- Add independent source/IR assertions and cumulative sample coverage.
- Run affected delivery tests, catalog checks, and repository checks.
- Keep toolchain pins, selected-source errors, source locations, and atomic output.

The C# examples below are feature inputs. The Elm examples show the former runtime mapping.
They are historical context and are not the expected source v1 output.

## Historical examples

### C#

```csharp
namespace Example.D20;

public readonly record struct Customer(int Age, bool Active);

public static class Rules
{
    public static bool Eligible(Customer customer) =>
        customer.Active && customer.Age >= 18;
}
```

### Elm — Example/D20.elm

```elm
module Example.D20 exposing (Customer)

type alias Customer =
    { age : Int, active : Bool }
```

### Elm — Example/D20/Rules.elm

```elm
module Example.D20.Rules exposing (eligible)

import Example.D20 exposing (Customer)

eligible : Customer -> Bool
eligible customer =
    if customer.active then
        customer.age >= 18
    else
        False
```

### Fixture project settings

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>14.0</LangVersion>
    <PlatformTarget>x64</PlatformTarget>
    <Nullable>enable</Nullable>
    <CheckForOverflowUnderflow>false</CheckForOverflowUnderflow>
  </PropertyGroup>
</Project>
```
