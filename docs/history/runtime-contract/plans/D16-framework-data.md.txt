# D16 — Dates, times, durations, identifiers

- status: planned
- required-context: [master](../Morphir_CSharp_Frontend_delivery_plan.md) + this file + prerequisite evidence
- prerequisites: [D6](D06-decimal.md), [D11](D11-collections.md)
- outcome: Deterministic rules across accepted framework data families
- ownership: Framework mappings/helpers; culture/offset/precision contracts
- fixture-root: `fixtures/D16/`; test trait: `Delivery=D16`

## Implementation

- [ ] CurrentSupportedExample update: deterministic dates/times/durations/Guid operations with delivered culture/offset/precision contracts
- [ ] System.DateOnly/TimeOnly/DateTime/DateTimeOffset/TimeSpan/Guid
- [ ] Complete fields/domains; offsets; precision; comparison; parsing/formatting/culture; supported pure members
- [ ] SDK semantic validation; compatibility representation where required
- [ ] No clock/randomness/machine-zone lookup/resources; further wrappers only via explicit catalog change

## Model increment

- SDK types only with verified equivalence; otherwise complete custom data/helpers

## Fixture contract

- package: `Example`; namespace: `Example.D16`
- source data paths: —
- member modules: `Example.D16.Rules`
- constraints: Value representation support ≠ every member pure; DateTime.Now excluded

## Acceptance

- [ ] Rules functions retained in Example.D16.Rules; framework representations under Example.Compat.DotNetData
- [ ] fractionTicks: 100 ns fraction; DateTime kind retained; offsetMinutes retained
- [ ] TickBits: four 16-bit words, signed 64-bit tick pattern; Guid: exactly 16 bytes + documented byte order
- [ ] At least one fixture per family; precision/offset/range/culture boundaries; parsing/formatting failure contracts
- [ ] Sample encodings illustrative complete-field forms for shown operations; final public mapping API recorded with evidence

## Rejections

- [ ] Clock/randomness/machine-zone/resource reads; unsupported culture semantics; unsafe parse/conversion
- [ ] Unselected additional framework family

## Code examples

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
