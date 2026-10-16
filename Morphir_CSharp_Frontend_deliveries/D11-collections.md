# D11 — Arrays and collection data

- status: planned
- required-context: [master](../Morphir_CSharp_Frontend_delivery_plan.md) + this file + prerequisite evidence
- prerequisites: [D4](D04-maybe.md), [D10](D10-generics-dispatch.md)
- outcome: Pure collection rules without LINQ
- ownership: Collection catalog; shape/comparer/order mappings; ownership/enumeration
- fixture-root: `fixtures/D11/`; test trait: `Delivery=D11`

## Implementation

- [ ] CurrentSupportedExample update: arrays/ranks + every accepted collection family/interface/wrapper, pure reads and owned updates; no LINQ
- [ ] Arrays: ranks/shape, construction and element access; accepted lists/dictionaries/sets/queues/stacks
- [ ] Generic mutable: List<T>, Dictionary<TKey,TValue>, HashSet<T>, Queue<T>, Stack<T>
- [ ] Generic interfaces: IEnumerable<T>, ICollection<T>, IList<T>, IDictionary<TKey,TValue>, ISet<T>
- [ ] Read-only interfaces: IReadOnlyCollection<T>, IReadOnlyList<T>, IReadOnlyDictionary<TKey,TValue>, IReadOnlySet<T>
- [ ] ObjectModel wrappers: ReadOnlyCollection<T>, ReadOnlyDictionary<TKey,TValue>
- [ ] Immutable: ImmutableArray/List/Dictionary/HashSet/Queue/Stack; exact System.Collections.Immutable version
- [ ] Construction/reads/enumeration/equality/order/duplicates/comparers/bounds; per-entry mapping and member coverage
- [ ] Bounds/key/construction failure proof; explicit source recovery after D18.1; lazy source production after D18.2
- [ ] Locally owned collection mutation + aliases/enumeration → functional state; input/global/captured mutation rejection
- [ ] Array reference probe: pinned default + fallback modes; core Array mapping absence vs external Morphir dependency distinguished; persist reproducer/evidence

## Model increment

- List/Record expressions; collection SDK/compatibility references

## Fixture contract

- package: `Example`; namespace: `Example.D11`
- source data paths: —
- member modules: `Example.D11.Rules`
- constraints: Array1 example: supported record/list compatibility representation; other ranks/catalog members require independent complete mappings

## Acceptance

- [ ] Displayed array: zero-based rank 1; Array1.length == List.length items invariant by construction
- [ ] Null/empty array → 0; nonempty → first; null list → 0; nonnull list → count
- [ ] Every rank/catalog entry: own parity fixtures; ordering/comparer/duplicate/missing-key/indexing boundaries
- [ ] Local aliases + mutation during enumeration → source behavior or required proven-safe rejection; no default invented for failed access
- [ ] Elm Core Array: no built-in mapping in checked 2.100.0; morphir.json dependencies = Morphir IR files, not Elm package coordinates
- [ ] Alternative Array dependency: verified contract/IR/runtime evidence before adoption; Array1 not an intrinsic IR requirement

## Rejections

- [ ] LINQ calls/query syntax; unsupported noncatalog family
- [ ] Unproved bounds/missing-key/construction safety before handlers; input/external mutation

## Code examples

### C#

```csharp
namespace Example.D11;

public static class Rules
{
    public static int FirstOrZero(int[]? values)
    {
        if (values is null || values.Length == 0)
            return 0;
        return values[0];
    }

    public static int Count(System.Collections.Generic.List<int>? values) =>
        values?.Count ?? 0;
}
```

### Elm — Example/Compat/Array1.elm

```elm
module Example.Compat.Array1 exposing (Array1)

type alias Array1 a =
    { length : Int, items : List a }
```

### Elm — Example/D11/Rules.elm

```elm
module Example.D11.Rules exposing (count, firstOrZero)

import Example.Compat.Array1 exposing (Array1)

firstOrZero : Maybe (Array1 Int) -> Int
firstOrZero possibleArray =
    case possibleArray of
        Nothing ->
            0

        Just array ->
            case array.items of
                [] ->
                    0

                first :: _ ->
                    first

count : Maybe (List Int) -> Int
count possibleList =
    case possibleList of
        Nothing ->
            0

        Just values ->
            List.length values
```
