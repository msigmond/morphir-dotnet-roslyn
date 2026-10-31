# D1 — CLI, source data, typed output

- status: passed under the source contract; revalidated on 2026-10-24
- contract: [source structure](../docs/source-structure.md)
- prerequisites: —
- evidence: [migration state](../docs/deliveries/source-structure-state.md)

## Work

- Keep selected namespace and enclosing-type owners, access, and documentation.
- Keep literals, fields, ordinary arithmetic, and explicit source syntax.
- Emit standard Classic v3 by default for mapped forms. Select the source v1 extension explicitly. Keep the schemas separate.

## Acceptance

- The Loan and integer products contain two reads and one multiplication.
- Invalid C#, unresolved source selection, and identity collisions fail with locations.
- Add independent source/IR assertions and cumulative sample coverage.
- Run affected delivery tests, catalog checks, and repository checks.
- Keep toolchain pins, selected-source errors, source locations, and atomic output.

The C# examples below are feature inputs. The Elm examples show the former runtime mapping.
They are historical context and are not the expected source v1 output.

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
