# D2 project fixture provenance

- SDK 10.0.100; C#14; .NET business/reference target net10.0 / reference pack 10.0.0; Roslyn 5.0.0.
- `Business` references the complete `Shared` source project and an incremental generator project as an analyzer. Generator tooling targets netstandard2.0 (Roslyn compiler-extension requirement); this does not change the frontend/business baseline.
- `threshold.txt` and CompilerVisibleProperty `Threshold` must agree; both are supplied through actual AdditionalFiles/analyzer configuration to the explicit driver pass.
- `D2_FEATURE` controls an unused selected function and `INTERNAL_REFERENCE` changes the referenced type's effective access. Shared's InternalsVisibleTo declaration makes the internal cross-project fixture legal C#; it does not change IR visibility or purity rules.
- Restore: `dotnet restore fixtures/D2/projects/Business/Business.csproj --packages .packages --locked-mode -m:1`.
- Project conversion command is in README. It verifies the real build, loads MSBuild inputs and explicitly runs generators for each source project, preserving compilation references rather than joining source trees or reading DLL bodies.
- Tests copy immutable fixtures into ignored artifacts before changing source/reference/conditional/generator settings; frozen expected distributions are never edited by validation. Actual input identities, text/reference hashes, parse/options/configuration and source-project references are returned by ProjectLoader and recorded in ignored test artifacts.
- Independent ordinary Elm/complete codec source and output hashes: [reference manifest](../expected/provenance.json). No C#-generated JSON is used as expected output.
