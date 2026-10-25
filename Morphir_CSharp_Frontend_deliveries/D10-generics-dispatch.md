# D10 — Generics, interfaces, and inheritance

- status: planned
- contract: [source structure](../docs/source-structure.md)
- prerequisites: [D9](D09-user-data.md)
- evidence: [migration state](../docs/deliveries/source-structure-state.md)

## Work

- Represent type parameters, arguments, constraints, variance, base lists, and interface members.
- Keep abstract, virtual, override, base access, casts, and type tests.
- Keep the resolved declared call. Do not enumerate runtime alternatives or generate dispatch bodies.

## Acceptance

- Open interfaces and generic declarations preserve structure without runtime closure proof.
- All source definitions retain their owners and declaration metadata.
- Add independent source/IR assertions and cumulative sample coverage.
- Run affected delivery tests, catalog checks, and repository checks.
- Keep toolchain pins, selected-source errors, source locations, and atomic output.

The C# examples below are feature inputs. The Elm examples show the former runtime mapping.
They are historical context and are not the expected source v1 output.

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
