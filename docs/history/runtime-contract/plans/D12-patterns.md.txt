# D12 — Complete pattern matching

- status: planned
- required-context: [master](../Morphir_CSharp_Frontend_delivery_plan.md) + this file + prerequisite evidence
- prerequisites: [D8](D08-enums-tuples.md), [D9](D09-user-data.md), [D10](D10-generics-dispatch.md), [D11](D11-collections.md)
- outcome: Ordered pattern decisions over business data
- ownership: Pattern/guard lowering; exhaustiveness/failure analysis
- fixture-root: `fixtures/D12/`; test trait: `Delivery=D12`

## Implementation

- [ ] CurrentSupportedExample update: all admitted switch/pattern/guard forms, ordered matching, safe exhaustive domains
- [ ] Switch statements/expressions; guards; constant/type/property/positional/relational/logical/list patterns
- [ ] Remaining admitted C# 14 pattern forms from catalog
- [ ] First-match + guard evaluation order; nonexhaustive failure contract

## Model increment

- All pattern cases; Destructure; PatternMatch

## Fixture contract

- package: `Example`; namespace: `Example.D12`
- source data paths: —
- member modules: `Example.D12.Rules`
- constraints: Exhaustiveness proof tied to admitted runtime domain

## Acceptance

- [ ] Category: 17 → 0; 18 → 1; 65 → 2
- [ ] Nested data/list patterns; guard order; unmatched input; complete distribution parity
- [ ] Potentially unmatched switch: proven exhaustive admitted domain or explicit handled failure after D18.1

## Rejections

- [ ] Unproved unhandled match failure; reordered guards/side effects; unsafe dereference

## Code examples

### C#

```csharp
namespace Example.D12;

public static class Rules
{
    public static int Category(int age) => age switch
    {
        >= 18 and < 65 => 1,
        >= 65 => 2,
        _ => 0
    };
}
```

### Elm — Example/D12/Rules.elm

```elm
module Example.D12.Rules exposing (category)

category : Int -> Int
category age =
    if age >= 18 && age < 65 then
        1
    else if age >= 65 then
        2
    else
        0
```
