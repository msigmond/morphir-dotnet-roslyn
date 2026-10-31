# D1 — CLI, source data, and identities

- status: passed under the approved Elm profile
- contract: [exact Elm parity](../docs/elm-parity.md)
- prerequisites: —
- evidence: [current gates](../docs/deliveries/elm-parity-state.md); [approved mappings](../docs/reviews/d1-d9-elm-mapping-decisions.md)

## Approved scope

Source selection, record aliases/construction, fields, literals, access flags, documentation, source owners, and approved overload suffixes.

## Boundary

Invalid binding, selection gaps, and normalized identity collisions remain errors.
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

### C# — Customer.cs

```csharp
namespace Example.D1;

public readonly record struct Customer(int Age, bool Active);
```

### C# — Rules.cs

```csharp
namespace Example.D1;

public static class Rules
{
    public static bool Eligible(Customer customer) =>
        customer.Active && customer.Age >= 18;

    public static string EligibleLabel() => "eligible";
}
```

### Elm — Example/D1.elm

```elm
module Example.D1 exposing (Customer)

type alias Customer =
    { age : Int, active : Bool }
```

### Elm — Example/D1/Rules.elm

```elm
module Example.D1.Rules exposing (eligible, eligibleLabel)

import Example.D1 exposing (Customer)

eligible : Customer -> Bool
eligible customer =
    if customer.active then
        customer.age >= 18
    else
        False

eligibleLabel : String
eligibleLabel =
    "eligible"
```
