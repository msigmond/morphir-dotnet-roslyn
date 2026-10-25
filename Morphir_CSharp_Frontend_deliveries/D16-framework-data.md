# D16 — Framework data structure

- status: planned
- contract: [source structure](../docs/source-structure.md)
- prerequisites: [D6](D06-decimal.md), [D11](D11-collections.md)
- evidence: [migration state](../docs/deliveries/source-structure-state.md)

## Work

- Represent the fixed date, time, duration, and Guid types.
- Keep constructors, casts, literals where available, properties, and named member calls.
- Do not simulate calendars, offsets, cultures, tick ranges, or parsing.

## Acceptance

- Framework values have named types and source operations.
- Source checks remain visible; no hidden runtime validation is added.
- Add independent source/IR assertions and cumulative sample coverage.
- Run affected delivery tests, catalog checks, and repository checks.
- Keep toolchain pins, selected-source errors, source locations, and atomic output.

The C# examples below are feature inputs. The Elm examples show the former runtime mapping.
They are historical context and are not the expected source v1 output.

## Historical examples

### C#

```csharp
namespace Example.D16;

public static class Rules
{
    public static bool YearEnd(System.DateOnly date) =>
        date.Month == 12 && date.Day == 31;

    public static bool Early(System.TimeOnly time) => time.Hour < 6;

    public static bool In2026(System.DateTime timestamp) =>
        timestamp.Year == 2026;

    public static bool ZeroOffset(System.DateTimeOffset timestamp) =>
        timestamp.Offset == System.TimeSpan.Zero;

    public static bool ZeroDuration(System.TimeSpan duration) =>
        duration.Ticks == 0;

    public static bool EmptyId(System.Guid id) => id == System.Guid.Empty;
}
```

### Elm — Example/Compat/DotNetData.elm

```elm
module Example.Compat.DotNetData exposing
    ( ClockParts, DateParts, DateTimeParts, Guid(..), OffsetTimestamp
    , TickBits(..), TimestampKind(..)
    )

type alias DateParts =
    { year : Int, month : Int, day : Int }

type alias ClockParts =
    { hour : Int, minute : Int, second : Int, fractionTicks : Int }

type TimestampKind
    = Unspecified
    | Utc
    | Local

type alias DateTimeParts =
    { date : DateParts, time : ClockParts, kind : TimestampKind }

type alias OffsetTimestamp =
    { local : DateTimeParts, offsetMinutes : Int }

type TickBits
    = TickBits Int Int Int Int

type Guid
    = Guid (List Int)
```

### Elm — Example/D16/Rules.elm

```elm
module Example.D16.Rules exposing
    ( early, emptyId, in2026, yearEnd, zeroDuration, zeroOffset )

import Example.Compat.DotNetData exposing
    ( ClockParts, DateParts, DateTimeParts, Guid(..), OffsetTimestamp, TickBits(..) )

yearEnd : DateParts -> Bool
yearEnd date =
    date.month == 12 && date.day == 31

early : ClockParts -> Bool
early time =
    time.hour < 6

in2026 : DateTimeParts -> Bool
in2026 timestamp =
    timestamp.date.year == 2026

zeroOffset : OffsetTimestamp -> Bool
zeroOffset timestamp =
    timestamp.offsetMinutes == 0

zeroDuration : TickBits -> Bool
zeroDuration (TickBits a b c d) =
    a == 0 && b == 0 && c == 0 && d == 0

emptyId : Guid -> Bool
emptyId (Guid bytes) =
    bytes == List.repeat 16 0
```
