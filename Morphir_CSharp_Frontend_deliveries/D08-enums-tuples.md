# D8 — Enum and tuple source structure

- status: passed under the source contract; revalidated on 2026-10-24
- contract: [source structure](../docs/source-structure.md)
- prerequisites: [D5](D05-numerics.md), [D7](D07-calls.md)
- evidence: [migration state](../docs/deliveries/source-structure-state.md)

## Work

- Keep enum member initializer expressions, aliases, flags operations, and underlying type metadata.
- Keep tuple family, labels, casts, member reads, factory calls, equality, deconstruction, and writes.
- Do not add enum wrappers, tuple extraction matches, hidden Rest creation, or element equality algorithms.

## Acceptance

- Tuple and enum operators remain single source operations.
- Explicit nullable reference tuples alone receive Maybe.
- Add independent source/IR assertions and cumulative sample coverage.
- Run affected delivery tests, catalog checks, and repository checks.
- Keep toolchain pins, selected-source errors, source locations, and atomic output.

The C# examples below are feature inputs. The Elm examples show the former runtime mapping.
They are historical context and are not the expected source v1 output.

## Historical examples

### C#

```csharp
namespace Example.D8;

[System.Flags]
public enum Access : int
{
    None = 0,
    Read = 1,
    Write = 2,
    All = 3
}

public static class Rules
{
    public static (int Code, bool IsReadOnly) Describe(Access access) =>
        ((int)access, access == Access.Read);
}
```

### Elm — Example/D8.elm

```elm
module Example.D8 exposing (Access(..))

type Access
    = Access Int
```

### Elm — Example/D8/Access.elm

```elm
module Example.D8.Access exposing (all, none, read, write)

import Example.D8 exposing (Access(..))

none : Access
none =
    Access 0

read : Access
read =
    Access 1

write : Access
write =
    Access 2

all : Access
all =
    Access 3
```

### Elm — Example/D8/Rules.elm

```elm
module Example.D8.Rules exposing (describe)

import Example.D8 exposing (Access(..))
import Example.D8.Access as Access

describe : Access -> ( Int, Bool )
describe access =
    case access of
        Access code ->
            ( code, access == Access.read )
```
