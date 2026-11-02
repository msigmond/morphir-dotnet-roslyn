# D18.3 — Dynamic syntax

- status: planned
- contract: [exact Elm parity](../docs/elm-parity.md)
- prerequisites: [D18.2](D18_2-lazy-iterators.md), [D7](D07-calls.md), [D10](D10-generics-dispatch.md), [D17](D17-type-closure.md)
- evidence: [exact-parity profile](../docs/deliveries/elm-parity-state.md); [remaining scope](../docs/elm-parity.md#existing-gaps)

## Prerequisite profile

Use the [D1–D13 and D11 follow-up Elm profile](../docs/deliveries/D11-follow-up-elm-parity.md) and all earlier exact-parity gates.
Each new mapping needs independent Elm input and complete JSON equality. Historical Source-mode passes do not count.

## Work

Scope below is a feature inventory. Confirm its standard Elm mapping before implementation.
No Source v1 tag or metadata envelope can satisfy this delivery.

- Keep dynamic declarations, operations, and written calls with compiler binding facts where available.
- Ask for the intended Elm counterpart for deferred binding before implementation.
- Do not infer runtime target sets, execute a binder, or generate dispatch.

## Acceptance

- An agreed standard Elm mapping and exact-parity test are required before support.
- Invalid C# still rejects.
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
