# D20 — MSBuild adapter

- status: planned
- contract: [exact Elm parity](../docs/elm-parity.md)
- prerequisites: [D19](D19-classic-complete.md), [D2](D02-project-inputs.md)
- evidence: [exact-parity profile](../docs/deliveries/elm-parity-state.md); [remaining scope](../docs/elm-parity.md#existing-gaps)

## Prerequisite profile

Use the [D1–D13 and D11 follow-up Elm profile](../docs/deliveries/D11-follow-up-elm-parity.md) and all earlier exact-parity gates.
Each new mapping needs independent Elm input and complete JSON equality. Historical Source-mode passes do not count.

## Work

Scope below is a feature inventory. Confirm its standard Elm mapping before implementation.
No Source v1 tag or metadata envelope can satisfy this delivery.

- Add the MSBuild task, package, targets, and generated-input handoff.
- Use the same structure converter as the CLI.
- Keep project settings and failures without adding execution semantics.

## Acceptance

- CLI and MSBuild emit the same standard IR, with exact Elm parity for every admitted feature.
- Failed conversion preserves existing output.
- Every feature needs paired C#/Elm inputs and full JSON equality against `morphir-elm make`.
- Ignore only whitespace/object-key order. No package, name, type, or operation normalization.
- Ask the user if the Elm counterpart is unclear. No unrequested output or feature is permitted.
- Run affected delivery tests, catalog checks, and repository checks.
- Keep toolchain pins, selected-source errors, source locations, and atomic output.

The C# examples below are feature inputs. The Elm examples show the former runtime mapping.
They are historical context. New acceptance references must come from `morphir-elm make`.

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
