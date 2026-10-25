# D17 — Object boxing and type-catalog closure

- status: planned
- required-context: [master](../Morphir_CSharp_Frontend_delivery_plan.md) + this file + prerequisite evidence
- prerequisites: [D8](D08-enums-tuples.md), [D9](D09-user-data.md), [D10](D10-generics-dispatch.md), [D11](D11-collections.md), [D12](D12-patterns.md), [D13](D13-closures.md), [D14](D14-recursion.md), [D15](D15-loops-local-state.md), [D16](D16-framework-data.md)
- outcome: Working construction/use for every accepted type entry
- ownership: Tagged object data; remaining type catalog support/fixtures
- fixture-root: `fixtures/D17/`; test trait: `Delivery=D17`

## Implementation

- [ ] CurrentSupportedExample update: boxing/type tests/casts + remaining admitted type forms; complete current type coverage
- [ ] object boxing/heterogeneous values; pure type tests/casts; complete admitted tagged variants
- [ ] Remaining language/user-defined/catalog types, delegates, object/dynamic data and finite acyclic recursive values
- [ ] Dynamic invocation deferred to D18.3; no reference identity/cyclic graphs
- [ ] Per-entry actual frontend lowering + paired runnable fixtures; no placeholder-only completion
- [ ] Pointer/ref-like declaration/use rejection; runtime-handle families outside catalog

## Model increment

- Tagged data for complete admitted runtime type set; finite recursive data

## Fixture contract

- package: `Example`; namespace: `Example.D17`
- source data paths: —
- member modules: `Example.D17.Rules`
- constraints: Type closure separate from remaining function-form closure D18.4

## Acceptance

- [ ] AgeOrZero: boxed 18 → 18; boxed false/null → 0
- [ ] Displayed BoxedInt/BoxedBool representation extended to full admitted set
- [ ] Every accepted fixed-version type catalog entry: passing construction/use fixture; no pending type entry

## Rejections

- [ ] Pointer/ref-like data; identity; cyclic graph; implicit inclusion of runtime handles
- [ ] Opaque placeholder/catalog row as substitute for working support

## Code examples

### C#

```csharp
namespace Example.D17;

public static class Rules
{
    public static int AgeOrZero(object? value) =>
        value is int age ? age : 0;
}
```

### Elm — Example/Compat/Object.elm

```elm
module Example.Compat.Object exposing (Boxed(..))

type Boxed
    = BoxedInt Int
    | BoxedBool Bool
```

### Elm — Example/D17/Rules.elm

```elm
module Example.D17.Rules exposing (ageOrZero)

import Example.Compat.Object exposing (Boxed(..))

ageOrZero : Maybe Boxed -> Int
ageOrZero possibleValue =
    case possibleValue of
        Just (BoxedInt age) ->
            age

        _ ->
            0
```
