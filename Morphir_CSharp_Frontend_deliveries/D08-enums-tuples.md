# D8 — Enums and tuples

- status: planned
- required-context: [master](../Morphir_CSharp_Frontend_delivery_plan.md) + this file + prerequisite evidence
- prerequisites: [D5](D05-numerics.md), [D7](D07-calls.md)
- outcome: Flags/numeric enum values + multiple business results
- ownership: Enum/tuple mappings; named enum values; deconstruction
- fixture-root: `fixtures/D8/`; test trait: `Delivery=D8`

## Implementation

- [ ] CurrentSupportedExample update: enum domains/aliases/flags/conversions, named constants, tuple forms/deconstruction
- [ ] Explicit enum underlying values; aliases; flags; conversions; unnamed values
- [ ] Enum data type in namespace/enclosing-type module; named values in enum member module
- [ ] Value/reference tuples; element names; deconstruction; respective null/value contracts
- [ ] Flags operations reuse delivered numeric semantics

## Model increment

- Tuple types/values/patterns; required custom definitions/specifications

## Fixture contract

- package: `Example`; namespace: `Example.D8`
- source data paths: `Example.D8.Access`
- member modules: `Example.D8.Access`, `Example.D8.Rules`
- constraints: Enum named constants retained as emitted declarations despite source constant folding

## Acceptance

- [ ] Example.D8.Access type in Example.D8; Access.Read → Example.D8.Access.read; no extra Access segment
- [ ] Read → (1,true); All → (3,false); unnamed 8 → (8,false)
- [ ] Underlying-domain boundaries; aliases/flags/conversions; tuple result/deconstruction parity

## Rejections

- [ ] Closed-named-case assumption dropping unnamed values; wrong numeric domain; unsafe tuple dereference

## Code examples

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
