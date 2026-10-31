# D1 — CLI, source data, typed output

- status: partial exact parity; remaining mappings pending; historical Source-mode pass is not acceptance
- contract: [exact Elm parity](../docs/elm-parity.md)
- prerequisites: —
- evidence: [exact-parity profile](../docs/deliveries/elm-parity-state.md); [remaining scope](../docs/elm-parity.md#existing-gaps)

## Work

Scope below is a feature inventory. Confirm its standard Elm mapping before implementation.
No Source v1 tag or metadata envelope can satisfy this delivery.

- Keep selected namespace and enclosing-type owners, access, and documentation.
- Keep literals, fields, ordinary arithmetic, and explicit source syntax.
- Emit standard Classic v3 matching the paired Elm output. Source v1 is not a delivery target.

## Acceptance

- The Loan and integer products contain two reads and one multiplication.
- Invalid C#, unresolved source selection, and identity collisions fail with locations.
- Every feature needs paired C#/Elm inputs and full JSON equality against `morphir-elm make`.
- Ignore only whitespace/object-key order. No package, name, type, or operation normalization.
- Ask the user if the Elm counterpart is unclear. No unrequested output or feature is permitted.
- Run affected delivery tests, catalog checks, and repository checks.
- Keep toolchain pins, selected-source errors, source locations, and atomic output.

The C# examples below are feature inputs. The Elm examples show the former runtime mapping.
They are historical context. New acceptance references must come from `morphir-elm make`.

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
