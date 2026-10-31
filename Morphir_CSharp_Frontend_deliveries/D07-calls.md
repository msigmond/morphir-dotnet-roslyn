# D7 — Calls and lexical functions

- status: passed under the approved Elm profile
- contract: [exact Elm parity](../docs/elm-parity.md)
- prerequisites: [D2](D02-project-inputs.md), [D3](D03-blocks-locals.md), [D4](D04-maybe.md), [D5](D05-numerics.md), [D6](D06-decimal.md)
- evidence: [current gates](../docs/deliveries/elm-parity-state.md); [approved mappings](../docs/reviews/d1-d9-elm-mapping-decisions.md)

## Approved scope

Resolved targets use signature suffixes for overloads. Calls use parameter order, declared omitted defaults, and List for params packs. Method references, bound receivers, Func/Action, delegate calls, lexical captures, local binding graphs, forward declarations, and self/mutual recursion use standard nodes. Exact configured metadata methods/getters need no pure/total assertion.

## Boundary

No body inlining, receiver snapshot, Unit adapter, or indexing fallback is added. Params indexing, Elm name shadowing, and unmapped signatures remain rejected. Later closure combinations keep their delivery gates.
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
