# D7 — Calls, references, and arguments

- status: exact Elm parity revalidation required; historical Source-mode pass on 2026-10-24
- contract: [exact Elm parity](../docs/elm-parity.md)
- prerequisites: [D2](D02-project-inputs.md), [D3](D03-blocks-locals.md), [D4](D04-maybe.md), [D5](D05-numerics.md), [D6](D06-decimal.md)
- evidence: [current acceptance gaps](../docs/elm-parity.md#existing-gaps); earlier migration evidence is historical

## Work

Scope below is a feature inventory. Confirm its standard Elm mapping before implementation.
No Source v1 tag or metadata envelope can satisfy this delivery.

- Keep resolved targets, receivers, written argument names and order, and declaration defaults.
- Keep expanded params arguments as written. Keep explicit array syntax only when the source contains it.
- Keep local functions in scope, captures, delegate calls, and recursive references.
- Keep external calls without pure/total assertions. Validate configured target identities and signatures.

## Acceptance

- A call stays a call. No callee body, receiver snapshot, lambda adapter, omitted default, or params allocation is inserted.
- Params reads and delegate invocation need no bounds, presence, or effect proof.
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
