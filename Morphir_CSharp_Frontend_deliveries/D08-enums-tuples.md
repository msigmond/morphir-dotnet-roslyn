# D8 — Enums and tuples

- status: passed under the approved Elm profile
- contract: [exact Elm parity](../docs/elm-parity.md)
- prerequisites: [D5](D05-numerics.md), [D7](D07-calls.md)
- evidence: [current gates](../docs/deliveries/elm-parity-state.md); [approved mappings](../docs/reviews/d1-d9-elm-mapping-decisions.md)

## Approved scope

Source enums use named Int aliases and named constant values. Value/reference tuples use ordered tuples. Two-element projections and written tuple/nested deconstruction use standard SDK and Destructure nodes.

## Boundary

Enum bit operations, empty/single CLR tuple forms, and larger tuple projections remain rejected. CLR width and reference identity are not represented.
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
