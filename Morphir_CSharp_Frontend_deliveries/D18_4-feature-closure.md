# D18.4 — Feature and rejection closure

- status: planned
- contract: [source structure](../docs/source-structure.md)
- prerequisites: [D18.1](D18_1-caught-failures.md), [D18.2](D18_2-lazy-iterators.md), [D18.3](D18_3-dynamic.md)
- evidence: [migration state](../docs/deliveries/source-structure-state.md)

## Work

- Audit all source syntax and Roslyn binding forms against the fixed catalog.
- Implement every admitted source mapping and list each actual representation gap.
- Keep excluded async, pointer/ref-like, resource, and query forms explicit until separately mapped.
- Remove obsolete runtime-safety exclusions from all current catalogs.

## Acceptance

- No selected declaration is silently lost.
- Pending mappings fail with source locations. Runtime safety, identity, effects, and cycles never substitute for a missing structural mapping.
- Add independent source/IR assertions and cumulative sample coverage.
- Run affected delivery tests, catalog checks, and repository checks.
- Keep toolchain pins, selected-source errors, source locations, and atomic output.

The C# examples below are feature inputs. The Elm examples show the former runtime mapping.
They are historical context and are not the expected source v1 output.

## Historical examples

### C# — accepted regression

```csharp
namespace Example.D18_4;

public static class Rules
{
    public static bool Adult(int age) => age >= 18;
}
```

### Elm — Example/D18_4/Rules.elm

```elm
module Example.D18_4.Rules exposing (adult)

adult : Int -> Bool
adult age =
    age >= 18
```

### C# — rejected cases

```csharp
// Rejected input. Do not include this block in the accepted fixture project.
namespace Example.Rejected;

public static class Rules
{
    public static int CheckedIncrement(int value) => checked(value + 1);

    public static async System.Threading.Tasks.Task<int> Delayed(
        System.Threading.Tasks.Task<int> value) => await value;

    public static int UnhandledDivide(int value, int divisor) => value / divisor;
}
```
