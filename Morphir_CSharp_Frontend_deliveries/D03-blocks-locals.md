# D3 — Blocks, locals, and statements

- status: exact Elm parity revalidation required; historical Source-mode pass on 2026-10-24
- contract: [exact Elm parity](../docs/elm-parity.md)
- prerequisites: [D2](D02-project-inputs.md)
- evidence: [current acceptance gaps](../docs/elm-parity.md#existing-gaps); earlier migration evidence is historical

## Work

Scope below is a feature inventory. Confirm its standard Elm mapping before implementation.
No Source v1 tag or metadata envelope can satisfy this delivery.

- Keep blocks, declarations without initializers, assignments, empty statements, and returns.
- Keep if/else branches, including constant and unreachable branches.
- Keep short-circuit operators. Do not copy following statements into branches.

## Acceptance

- Define and test the agreed standard Elm mapping for each admitted assignment form.
- No SSA versions, discard bindings, or constant folding enter the output.
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
