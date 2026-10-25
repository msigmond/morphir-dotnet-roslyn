# D7 — Calls, references, and arguments

- status: passed under the source contract; revalidated on 2026-10-24
- contract: [source structure](../docs/source-structure.md)
- prerequisites: [D2](D02-project-inputs.md), [D3](D03-blocks-locals.md), [D4](D04-maybe.md), [D5](D05-numerics.md), [D6](D06-decimal.md)
- evidence: [migration state](../docs/deliveries/source-structure-state.md)

## Work

- Keep resolved targets, receivers, written argument names and order, and declaration defaults.
- Keep expanded params arguments as written. Keep explicit array syntax only when the source contains it.
- Keep local functions in scope, captures, delegate calls, and recursive references.
- Keep external calls without pure/total assertions. Validate configured target identities and signatures.

## Acceptance

- A call stays a call. No callee body, receiver snapshot, lambda adapter, omitted default, or params allocation is inserted.
- Params reads and delegate invocation need no bounds, presence, or effect proof.
- Add independent source/IR assertions and cumulative sample coverage.
- Run affected delivery tests, catalog checks, and repository checks.
- Keep toolchain pins, selected-source errors, source locations, and atomic output.

The C# examples below are feature inputs. The Elm examples show the former runtime mapping.
They are historical context and are not the expected source v1 output.

## Historical examples

### C#

```csharp
namespace Example.D7;

public static class Rules
{
    private static bool Meets(int age, int minimum = 18) =>
        age >= minimum;

    private static bool Meets(bool approved) => approved;

    public static bool Eligible(int age) =>
        Meets(minimum: 18, age: age);

    public static bool Approved(bool approved) => Meets(approved);

    public static bool DefaultEligible(int age) => Meets(age);
}
```

### Elm — Example/D7/Rules.elm

```elm
module Example.D7.Rules exposing (approved, defaultEligible, eligible)

meetsIntInt : Int -> Int -> Bool
meetsIntInt age minimum =
    age >= minimum

meetsBool : Bool -> Bool
meetsBool approved =
    approved

eligible : Int -> Bool
eligible age =
    meetsIntInt age 18

approved : Bool -> Bool
approved value =
    meetsBool value

defaultEligible : Int -> Bool
defaultEligible age =
    meetsIntInt age 18
```
