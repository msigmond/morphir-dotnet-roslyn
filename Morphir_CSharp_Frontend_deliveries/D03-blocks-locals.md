# D3 — Blocks, locals, and returned branches

- status: passed under the approved Elm profile
- contract: [exact Elm parity](../docs/elm-parity.md)
- prerequisites: [D2](D02-project-inputs.md)
- evidence: [current gates](../docs/deliveries/elm-parity-state.md); [approved mappings](../docs/reviews/d1-d9-elm-mapping-decisions.md)

## Approved scope

Initialized locals, scope, returned if/else branches, early returns, nested/constant/unreachable branches, conditional expressions, and checked context.

## Boundary

Mutation and unrepresentable statements after a return remain rejected. No continuation is duplicated.
Unmapped forms fail at the source. They produce no partial output.
No Source v1 output or custom source tag is accepted.

## Acceptance

- Every feature needs paired C#/Elm inputs and full JSON equality against `morphir-elm make`.
- Ignore only whitespace/object-key order. No package, name, type, or operation normalization.
- Keep the fixed SDK/reference/Roslyn versions, declared types, namespace selection, and atomic output.
- Run restore, build, the full suite, the D1–D9 catalog gate, and repository checks.
- Use the [case registry](../fixtures/elm-parity/cases.json) and executable rejection inputs.

The examples below are historical. They do not replace current compiler parity evidence.

## Historical examples

### C#

```csharp
namespace Example.D3;

public static class Rules
{
    public static int Category(int age, bool active)
    {
        int category = 0;
        if (active)
        {
            category = 1;
            if (age >= 18)
                category = 2;
        }
        return category;
    }
}
```

### Elm — Example/D3/Rules.elm

```elm
module Example.D3.Rules exposing (category)

category : Int -> Bool -> Int
category age active =
    let
        initialCategory =
            0
    in
    if active then
        let
            activeCategory =
                1
        in
        if age >= 18 then
            2
        else
            activeCategory
    else
        initialCategory
```
