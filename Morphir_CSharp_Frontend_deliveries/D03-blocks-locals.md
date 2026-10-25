# D3 — Blocks, locals, and statements

- status: passed under the source contract; revalidated on 2026-10-24
- contract: [source structure](../docs/source-structure.md)
- prerequisites: [D2](D02-project-inputs.md)
- evidence: [migration state](../docs/deliveries/source-structure-state.md)

## Work

- Keep blocks, declarations without initializers, assignments, empty statements, and returns.
- Keep if/else branches, including constant and unreachable branches.
- Keep short-circuit operators. Do not copy following statements into branches.

## Acceptance

- A written assignment has one Assignment node.
- No SSA versions, discard bindings, or constant folding enter the output.
- Add independent source/IR assertions and cumulative sample coverage.
- Run affected delivery tests, catalog checks, and repository checks.
- Keep toolchain pins, selected-source errors, source locations, and atomic output.

The C# examples below are feature inputs. The Elm examples show the former runtime mapping.
They are historical context and are not the expected source v1 output.

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
