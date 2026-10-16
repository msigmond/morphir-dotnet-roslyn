# D2 — Project compilation inputs and coverage catalogs

- status: planned
- required-context: [master](../Morphir_CSharp_Frontend_delivery_plan.md) + this file + prerequisite evidence
- prerequisites: [D1](D01-cli-record.md)
- outcome: Real .csproj + generated/partial source + project references → same core frontend
- ownership: ProjectSystem project; CLI project loading; catalogs; diagnostics; project/generator fixtures
- fixture-root: `fixtures/D2/`; test trait: `Delivery=D2`

## Implementation

- [ ] CurrentSupportedExample update: multifile/partial/generated declarations, project references, namespace/access boundaries; full project-mode sample input
- [ ] Selected target framework; actual parse options, conditional symbols, nullable/overflow settings, references, additional files and generator inputs
- [ ] Complete generated syntax: captured build inputs or explicit equivalent Roslyn generator pass; workspace load alone insufficient
- [ ] Project-reference symbols; explicit namespace boundary; no DLL-body transpilation or purity inferred from successful compilation
- [ ] Partial + generated declarations: merge only same source type; separate containers and qualified references retained
- [ ] Visibility: source legality via Roslyn; complete private-inclusive lookup; public specification filtered independently
- [ ] Independent complete Elm IR/codec fixtures for legal nonpublic cross-container access; actual private wrappers, bodies, qualified references
- [ ] Exhaustive fixed-version language/forms/operations catalog + accepted framework catalog; owner delivery per entry; supported/pending/required-rejection states
- [ ] Stable source-located diagnostic codes; deterministic ordering; project input identity/provenance

## Model increment

- Qualified references; package dependency specifications; public/private definitions/specifications

## Fixture contract

- package: `Example`; selectors: `Example.D2.Business`, `Example.D2.Shared`
- source data paths: —
- member modules: `Example.D2.Business.Rules`, `Example.D2.Shared.AgeRule`
- constraints: Cross-container C# internal/private access: Roslyn legality + Private IR; ordinary Elm source insufficient for some legal combinations

## Acceptance

- [ ] Selectors: Example.D2.Business + Example.D2.Shared; referenced source → AgeRule; configured generator → second Rules partial
- [ ] Rules.Adult private; AgeRule.Adult public; same local name, distinct owners; no container merge
- [ ] GeneratedThreshold unused but emitted; partial Rules → one member module
- [ ] Public cross-module + private same-module source fixtures: full parity
- [ ] Legal internal/nonpublic cross-container fixtures: independent complete codec distribution; Private metadata; references + lookup validated
- [ ] Outside referenced namespace: failure until explicit selection; no automatic inclusion from project reference
- [ ] Changed reference/conditional symbol/generated declaration → correct output change; generator/compiler errors → failure
- [ ] Catalog: every admitted entry assigned through D18.4; pending entries block final acceptance

## Rejections

- [ ] Unselected required source namespace; external call without catalog mapping/Morphir specification
- [ ] Missing generator outputs/inputs; compiler/generator errors; unknown external purity
- [ ] Private promoted to Public; merged unrelated containers; edited reference JSON

## Code examples

### C# — Business/Rules.cs

```csharp
namespace Example.D2.Business;

public static partial class Rules
{
    private static bool Adult(int age) =>
        Example.D2.Shared.AgeRule.Adult(age);

    public static bool Eligible(int age) => Adult(age);
}
```

### C# — Shared/AgeRule.cs

```csharp
namespace Example.D2.Shared;

public static class AgeRule
{
    public static bool Adult(int age) => age >= 18;
}
```

### C# — generated Business/Rules.Generated.cs

```csharp
namespace Example.D2.Business;

public static partial class Rules
{
    public static int GeneratedThreshold() => 18;
}
```

### Elm — Example/D2/Shared/AgeRule.elm

```elm
module Example.D2.Shared.AgeRule exposing (adult)

adult : Int -> Bool
adult age =
    age >= 18
```

### Elm — Example/D2/Business/Rules.elm

```elm
module Example.D2.Business.Rules exposing (eligible, generatedThreshold)

import Example.D2.Shared.AgeRule as AgeRule

adult : Int -> Bool
adult age =
    AgeRule.adult age

eligible : Int -> Bool
eligible age =
    adult age

generatedThreshold : Int
generatedThreshold =
    18
```
