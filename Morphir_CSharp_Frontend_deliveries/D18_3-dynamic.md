# D18.3 — Dynamic syntax

- status: planned
- contract: [source structure](../docs/source-structure.md)
- prerequisites: [D18.2](D18_2-lazy-iterators.md), [D7](D07-calls.md), [D10](D10-generics-dispatch.md), [D17](D17-type-closure.md)
- evidence: [migration state](../docs/deliveries/source-structure-state.md)

## Work

- Keep dynamic declarations, operations, and written calls with compiler binding facts where available.
- Define an explicit source node for deferred binding.
- Do not infer runtime target sets, execute a binder, or generate dispatch.

## Acceptance

- Unresolved runtime dispatch is representable as explicit dynamic source structure.
- Invalid C# still rejects.
- Add independent source/IR assertions and cumulative sample coverage.
- Run affected delivery tests, catalog checks, and repository checks.
- Keep toolchain pins, selected-source errors, source locations, and atomic output.

The C# examples below are feature inputs. The Elm examples show the former runtime mapping.
They are historical context and are not the expected source v1 output.

## Historical examples

### C#

```csharp
namespace Example.D18_3;

public readonly record struct MinimumAge(int Minimum)
{
    public bool Allows(int age) => age >= Minimum;
}

public static class Rules
{
    public static bool Adult(int age)
    {
        dynamic rule = new MinimumAge(18);
        return rule.Allows(age);
    }
}
```

### Elm — Example/D18_3.elm

```elm
module Example.D18_3 exposing (MinimumAge)

type alias MinimumAge =
    { minimum : Int }
```

### Elm — Example/D18_3/MinimumAge.elm

```elm
module Example.D18_3.MinimumAge exposing (allows)

import Example.D18_3 exposing (MinimumAge)

allows : MinimumAge -> Int -> Bool
allows this age =
    age >= this.minimum
```

### Elm — Example/D18_3/Rules.elm

```elm
module Example.D18_3.Rules exposing (adult)

import Example.D18_3 exposing (MinimumAge)
import Example.D18_3.MinimumAge as MinimumAge

adult : Int -> Bool
adult age =
    let
        rule : MinimumAge
        rule =
            { minimum = 18 }
    in
    MinimumAge.allows rule age
```
