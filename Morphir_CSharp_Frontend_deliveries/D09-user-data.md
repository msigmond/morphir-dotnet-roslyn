# D9 — Classes, structs, records, nested data

- status: planned
- required-context: [master](../Morphir_CSharp_Frontend_delivery_plan.md) + this file + prerequisite evidence
- prerequisites: [D7](D07-calls.md), [D8](D08-enums-tuples.md)
- outcome: User-defined data forms as values; local alias-preserving updates
- ownership: Data layout/construction/update; alias/ownership tracking; equality
- fixture-root: `fixtures/D9/`; test trait: `Delivery=D9`

## Implementation

- [ ] CurrentSupportedExample update: class/struct/record/nested data, constructors/properties/operators/value equality, owned alias updates
- [ ] Classes/structs/record classes/record structs; nested types including private; fields/properties; constructors/initializers; record updates
- [ ] Custom value equality/conversions/operators via resolved pure implementations
- [ ] Value-data identity policy; recursive declarations + finite acyclic values; cyclic graph rejection
- [ ] Local owned object updates reflected through all local aliases; input reachability retained through new wrappers
- [ ] Initialization/getter/constructor purity; hidden effects rejected

## Model increment

- UpdateRecord; remaining required type definitions/specifications

## Fixture contract

- package: `Example`; namespace: `Example.D9`
- source data paths: `Example.D9.Counter`
- member modules: `Example.D9.Rules`
- constraints: Mutable declaration alone admissible; operation/ownership contract decisive

## Acceptance

- [ ] Counter build/local alias update → 2 through either alias
- [ ] Each data form: construction/read/local update/value-equality fixture
- [ ] Nested private type: enclosing-type module + correct local name/Private wrapper
- [ ] Input mutation via alias/new wrapper/helper → rejection; identity observations/cyclic graphs → rejection
- [ ] Recursive type declaration + finite acyclic value → success

## Rejections

- [ ] ReferenceEquals/reference-identity equality; cyclic graph
- [ ] Input/global/external mutation; impure initialization/getter/operator

## Code examples

### C#

```csharp
namespace Example.D9;

public sealed class Counter
{
    public int Value;
}

public static class Rules
{
    public static int BuildAndUpdate()
    {
        var counter = new Counter { Value = 1 };
        var alias = counter;
        alias.Value = 2;
        return counter.Value;
    }
}
```

### Elm — Example/D9.elm

```elm
module Example.D9 exposing (Counter)

type alias Counter =
    { value : Int }
```

### Elm — Example/D9/Rules.elm

```elm
module Example.D9.Rules exposing (buildAndUpdate)

import Example.D9 exposing (Counter)

buildAndUpdate : Int
buildAndUpdate =
    let
        counterBefore : Counter
        counterBefore =
            { value = 1 }

        counterAfter =
            { counterBefore | value = 2 }
    in
    counterAfter.value
```
