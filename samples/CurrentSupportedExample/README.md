# Current supported example

Use [ClassicLoan.cs](ClassicLoan.cs). It contains the current D1–D13 and D11 follow-up example.
Use [CurrentSupportedExample.csproj](CurrentSupportedExample.csproj) to load it.
The project also runs the [source generator](../CurrentSupportedGenerator/ThresholdGenerator.cs).
The other C# files in this folder are historical inputs. The project excludes them.

The example includes generic data and calls, nullable operations, Decimal division, local functions,
overloads, default arguments, params lists, enums, tuples, constructors, record updates,
List/Dict/Set construction and nullable-result indexing,
returned switches with literal, enum, nullable, and list patterns,
plus captured lambdas and collection filter, map, and seeded fold calls.
It shows selected supported forms. See the [unsupported feature guide](../../docs/unsupported-language-features.md) for limits.

Run this command from the repository root after build:

```bash
dotnet run --project src/Morphir.Frontend.CSharp.Cli --no-build -- --package Example --namespace Example.CurrentSupported --project samples/CurrentSupportedExample/CurrentSupportedExample.csproj --framework net10.0 --output artifacts/current-sample.json
```

The [Elm source](../../fixtures/elm-parity/current-sample/elm/src/Example/CurrentSupported.elm)
and its [functions](../../fixtures/elm-parity/current-sample/elm/src/Example/CurrentSupported/ClassicLoanCalculations.elm)
form the independent reference.
Tests compare the complete JSON and run the pinned Elm compiler again.
See the [reference record](reference.json) and [current evidence](../../docs/deliveries/D11-follow-up-elm-parity.md).
