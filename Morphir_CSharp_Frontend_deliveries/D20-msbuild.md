# D20 — MSBuild adapter

- status: planned
- required-context: [master](../Morphir_CSharp_Frontend_delivery_plan.md) + this file + prerequisite evidence
- prerequisites: [D19](D19-classic-complete.md), [D2](D02-project-inputs.md)
- outcome: Normal project build → same distribution as CLI
- ownership: MSBuild task/package/targets; shared project inputs; build fixtures
- fixture-root: `fixtures/D20/`; test trait: `Delivery=D20`

## Implementation

- [ ] CurrentSupportedExample update: same cumulative source through CLI + normal MSBuild generation; phase marker/coverage refresh; README/AGENTS build commands
- [ ] Task/package/targets; shared frontend + serializer + ProjectSystem input contract
- [ ] Generation after actual inputs/configured generator outputs available; no assumed live compiler Compilation
- [ ] Task-owned equivalent compilation/generator pass absent supported live handoff
- [ ] Export settings, selected framework/reference versions/helper dependencies
- [ ] Configuration/target-framework-separated outputs; atomic success; failed-conversion stale-output invalidation
- [ ] Incremental inputs/outputs; clean/removal behavior; diagnostic errors fail required generation

## Model increment

- No alternate model or language mapping

## Fixture contract

- package: `Example`; namespace: `Example.D20`
- source data paths: `Example.D20.Customer`
- member modules: `Example.D20.Rules`
- constraints: CLI remains first execution environment; no additional environment selected

## Acceptance

- [ ] Same compilation/settings: CLI JSON == MSBuild JSON
- [ ] Clean/no-change/changed source/changed reference/removed declaration/generator change/failure cases
- [ ] Consuming project normal build without manual generation; sample x64/net10.0/C#14/nullable/unchecked settings
- [ ] Package/target/export public names: implementation choice recorded; no second transpiler

## Rejections

- [ ] Assumed live Compilation; incomplete generators; success artifact on failure; stale output treated as current

## Code examples

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
