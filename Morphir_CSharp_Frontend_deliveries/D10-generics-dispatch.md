# D10 — Generics, interfaces, and inheritance

- status: planned
- contract: [exact Elm parity](../docs/elm-parity.md)
- prerequisites: [D9](D09-user-data.md)
- evidence: [exact-parity profile](../docs/deliveries/elm-parity-state.md); [remaining scope](../docs/elm-parity.md#existing-gaps)

## Work

Scope below is a feature inventory. Confirm its standard Elm mapping before implementation.
No Source v1 tag or metadata envelope can satisfy this delivery.

- Represent type parameters, arguments, constraints, variance, base lists, and interface members.
- Keep abstract, virtual, override, base access, casts, and type tests.
- Keep the resolved declared call. Do not enumerate runtime alternatives or generate dispatch bodies.

## Acceptance

- Open interfaces and generic declarations preserve structure without runtime closure proof.
- Names, owners, access, types, and operations match the paired Elm output.
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
namespace Example.D10;

public readonly record struct Box<T>(T Value);

public interface IRule
{
    bool Allows(int age);
}

public readonly record struct MinimumAge(int Minimum) : IRule
{
    public bool Allows(int age) => age >= Minimum;
}

public readonly record struct MaximumAge(int Maximum) : IRule
{
    public bool Allows(int age) => age <= Maximum;
}

public static class Rules
{
    public static Box<T> Wrap<T>(T value) => new(value);

    public static bool Test(IRule? rule, int age)
    {
        if (rule is null)
            return false;
        return rule.Allows(age);
    }
}
```

### Elm — Example/D10.elm

```elm
module Example.D10 exposing (Box, IRule(..), MaximumAge, MinimumAge)

type alias Box t =
    { value : t }

type alias MinimumAge =
    { minimum : Int }

type alias MaximumAge =
    { maximum : Int }

type IRule
    = MinimumAgeRule MinimumAge
    | MaximumAgeRule MaximumAge
```

### Elm — Example/D10/MinimumAge.elm

```elm
module Example.D10.MinimumAge exposing (allows)

import Example.D10 exposing (MinimumAge)

allows : MinimumAge -> Int -> Bool
allows this age =
    age >= this.minimum
```

### Elm — Example/D10/MaximumAge.elm

```elm
module Example.D10.MaximumAge exposing (allows)

import Example.D10 exposing (MaximumAge)

allows : MaximumAge -> Int -> Bool
allows this age =
    age <= this.maximum
```

### Elm — Example/D10/IRule.elm

```elm
module Example.D10.IRule exposing (allows)

import Example.D10 exposing (IRule(..))
import Example.D10.MaximumAge as MaximumAge
import Example.D10.MinimumAge as MinimumAge

allows : IRule -> Int -> Bool
allows this age =
    case this of
        MinimumAgeRule rule ->
            MinimumAge.allows rule age

        MaximumAgeRule rule ->
            MaximumAge.allows rule age
```

### Elm — Example/D10/Rules.elm

```elm
module Example.D10.Rules exposing (test, wrap)

import Example.D10 exposing (Box, IRule)
import Example.D10.IRule as IRule

wrap : t -> Box t
wrap value =
    { value = value }

test : Maybe IRule -> Int -> Bool
test possibleRule age =
    case possibleRule of
        Nothing ->
            False

        Just rule ->
            IRule.allows rule age
```
