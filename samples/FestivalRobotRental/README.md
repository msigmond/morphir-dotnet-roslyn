# Festival robot rental

[BusinessLogic.cs](BusinessLogic.cs) is the single supported business example.
It contains all data, inputs, and rules. It needs no generated business source.

A fictional event company rents singing and moving robots. Start with `RentalBusiness.Demo()`.
Then read `Plan`, followed by the numbered rule sections.

The plan reviews each order, rejects cancelled or invalid orders, applies promotions,
calculates rental and delivery charges, totals the quotes, and plans cartons and loyalty credit.
The policies and prices are fictional demo inputs.

| Demo result | Value |
| --- | --- |
| Orders reviewed | 5 |
| Orders accepted | 2 |
| Robots planned | 3 |
| Cartons needed | 2 |
| Total charge | 460 |
| First accepted order | ORD-001 |
| Capacity use | 0.75 |
| Projected loyalty credit | 6.05 |

## Feature coverage

| Approved business feature family | Functions or data in the file |
| --- | --- |
| Records, fields, plain references, enums | RentalOrder, RentalQuote, RobotCatalog, RentalState |
| Constructors, primary parameters, getters, operators | RentalPolicy, DeliveryRule, RentalQuote.Amount, ChargeTotal |
| Blocks, locals, branches, early returns | Demo, Plan, ReviewOrder |
| Int/Float operations, casts, scalar SDK calls | CartonsNeeded, CapacityUse, CapQuantity |
| Decimal literals, arithmetic, comparison, rounding, nullable division | QuoteOrder, Divide, RoundedAmount |
| Null, default, coalesce, conditional access, lifted arithmetic | NoOrder, OptionalQuantity, UnfilledQuantity, QuoteOrder |
| Generic data and methods | Review, Assess, ReviewedItem |
| Named/default/params calls and overloads | Demo, CreatePolicy, CombineQuantities, CapQuantity |
| Value/reference tuples and deconstruction | DispatchQuantity, CombinedDays, DispatchDecision |
| Record updates and equality | Confirm, SameOrder |
| List, array, immutable/read-only copies, Dict, Set, spreads, reads | AddOrder, Snapshot, ReadOnlyQuotes, PromotionRates, AllowedModels |
| Nullable-result indexing and list patterns | QuoteAt, PromotionFor, FirstOrder |
| Enum, String, Char, tuple, and returned switch patterns | StatePriority, ServiceDays, DeliveryMultiplier, DispatchQuantity |
| Filter, map, seeded fold, chained calls, FindAll, ConvertAll | Plan, SettlementAmounts |
| Captures, anonymous methods, delegates, bound methods, local recursion | QuoteWith, BelowLimit, PolicyCheck, CheckOrder, ProjectCredit |

This file covers the approved business feature families. The [parity registry](../../fixtures/elm-parity/cases.json)
keeps the exhaustive API, overload, carrier, and rejection cases.
See [supported mappings](../../docs/classic-output.md) and [unsupported features](../../docs/unsupported-language-features.md).
Project loading, generators, references, and compiler options have separate adapter fixtures.

## Generate IR

Run from the repository root after build. For the single file:

```bash
dotnet run --project src/Morphir.Frontend.CSharp.Cli --no-build -- --package Example --namespace Example.FestivalRobotRental --source samples/FestivalRobotRental/BusinessLogic.cs --output artifacts/robot-rental.json
```

The same file also has a project:

```bash
dotnet run --project src/Morphir.Frontend.CSharp.Cli --no-build -- --package Example --namespace Example.FestivalRobotRental --project samples/FestivalRobotRental/FestivalRobotRental.csproj --framework net10.0 --output artifacts/robot-rental-project.json
```

Both commands must match the complete independent [Elm reference](../../fixtures/elm-parity/festival-robot-rental/elm/src/Example/FestivalRobotRental/RentalBusiness.elm).
Elm requires one file per module; the C# demo remains one file.
See the [reference record](reference.json) and [validation](../../docs/reviews/single-file-business-demo.md).
