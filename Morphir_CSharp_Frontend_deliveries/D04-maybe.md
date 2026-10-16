# D4 — Missing values and M1 presence proofs

- status: planned
- required-context: [master](../Morphir_CSharp_Frontend_delivery_plan.md) + this file + prerequisite evidence
- prerequisites: [D3](D03-blocks-locals.md)
- outcome: Nullable fields, explicit defaults, proven-safe extraction
- ownership: Null representation; flow facts; Maybe lowering; safety diagnostics
- fixture-root: `fixtures/D4/`; test trait: `Delivery=D4`

## Implementation

- [ ] CurrentSupportedExample update: nullable data, null tests/coalescing/conditional access/lifted forms, guarded extraction
- [ ] Nullable values + reference slots → Maybe; reference fields/array elements/default initialization included
- [ ] Null literals/tests; conditional access; coalescing; lifted-operation C# semantics
- [ ] Construction/domain/flow invariants for absent cases; annotations alone insufficient
- [ ] Presence proof at every .Value/dereference; invalidation after relevant assignment, alias update, call
- [ ] Null-forgiving !: no added proof; no invented defaults, Nothing substitution, error wrappers or hidden exception fallback

## Model increment

- Constructors; constructor patterns; PatternMatch; Maybe references

## Fixture contract

- package: `Example`; namespace: `Example.D4`
- source data paths: `Example.D4.Customer`
- member modules: `Example.D4.Rules`
- constraints: M1 safety mandatory; no exception-based bypass

## Acceptance

- [ ] Customer type → Example.D4.Customer; no repeated type-name segment
- [ ] Missing age → false; missing region → explicit "unknown" source default
- [ ] Guarded HasValue/.Value → success; null propagation + explicit fallback → paired Maybe parity
- [ ] Invalidated guard after reassignment/call/alias write → rejection
- [ ] Non-nullable annotated slot with possible default null → Maybe; invariant-proven present slot → permitted extraction

## Rejections

- [ ] Unguarded .Value; possible null dereference; annotation/! without proof
- [ ] Unsafe missing access even inside catch

## Code examples

### C#

```csharp
namespace Example.D4;

public readonly record struct Customer(int? Age, string? Region);

public static class Rules
{
    public static bool Adult(Customer customer)
    {
        if (!customer.Age.HasValue)
            return false;
        return customer.Age.Value >= 18;
    }

    public static string RegionOrDefault(Customer customer) =>
        customer.Region ?? "unknown";
}
```

### Elm — Example/D4.elm

```elm
module Example.D4 exposing (Customer)

type alias Customer =
    { age : Maybe Int, region : Maybe String }
```

### Elm — Example/D4/Rules.elm

```elm
module Example.D4.Rules exposing (adult, regionOrDefault)

import Example.D4 exposing (Customer)

adult : Customer -> Bool
adult customer =
    case customer.age of
        Nothing ->
            False

        Just age ->
            age >= 18

regionOrDefault : Customer -> String
regionOrDefault customer =
    case customer.region of
        Nothing ->
            "unknown"

        Just region ->
            region
```
