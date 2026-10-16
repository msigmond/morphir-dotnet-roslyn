# Roslyn C# source frontend: integer-addition concept plan

Historical prototype plan; superseded by [master](../../Morphir_CSharp_Frontend_delivery_plan.md). Original paths, versions and commands: historical only.

## Objective and completion criteria

Implement a small C# source frontend that translates an actual Roslyn semantic operation tree into Morphir IR. Apply it to a runnable C# sample containing the equivalent of:

```elm
module Arithmetic.Add exposing (add)

add : Int -> Int -> Int
add a b =
    a + b
```

Generate the Elm reference independently with Morphir Elm, then compare the **entire versioned distribution** from C# against it. Completion requires structural JSON equality, a runnable sample, meaningful rejection tests, and documented reproduction commands.

This document is an implementation plan. The prototype and its repository fixtures have not been implemented yet. The Elm reference was generated in a temporary directory during planning to verify the JSON contract below.

## Verified starting point and fixed decisions

- Repository: `/home/marton/VSCodeProjects/DotNetMorphir`.
- Existing `DotNetMorphir.csproj`: console application targeting `net10.0`, with nullable reference types and implicit usings enabled.
- Existing `Program.cs`: only the template Hello World program. There are no existing frontend components or tests.
- Available tools at planning time: .NET SDK `10.0.112`, Node `22.22.1`, and npm `9.2.0`.
- Use `Microsoft.CodeAnalysis.CSharp` **4.14.0**, a verified published version. Parse input as C# 13; the fixture needs no newer features.
- Use `System.Text.Json` for construction, serialization, and comparison; no Morphir .NET SDK dependency is required for this concept.
- Use Morphir Elm **2.96.1**, the version resolved by the reference repository's lockfile. Pin it exactly in the new repository's `package.json` and commit `package-lock.json`.
- The globally installed `morphir-elm` is currently `2.100.0`. Use the local pinned CLI explicitly; an unversioned global command is not the reference generator.
- Read one explicitly selected `.cs` source file using `CSharpSyntaxTree` and `CSharpCompilation`. General `.csproj` loading, `MSBuildWorkspace`, multiple source files, and source generators are later work.
- Derive declarations from syntax and symbols; translate expressions from `IOperation`. Do not derive semantics from expression text or a regex.
- Keep this a narrow prototype: namespace, one public static class, one public static method, two by-value `int` parameters, `int` return type, parameter references, and built-in unchecked integer addition.
- Accept both an expression-bodied method and a block containing exactly one `return` of a supported expression. Reject other declarations or statements in the selected file rather than silently dropping them. Ordinary using directives may be ignored as non-emitting syntax.
- Exclude VB, F#, DLL inspection, general arithmetic, literals, locals, calls, mutation, generics, overloaded operators, and arbitrary framework/library dependencies.
- Exact IR parity proves the representation for this fixture. Runtime checks use small, non-overflowing integers; this concept does not establish identical overflow behavior for every C# `int` input across Morphir runtimes. Reject explicitly checked arithmetic in this slice.

## Reference provenance

Use commit `3086f7ad501d13239b474e23baa3d7fc88baf881` throughout. Do not fetch a moving branch to construct the fixture.

- [Elm source](https://github.com/msigmond/morphir-jvm/blob/3086f7ad501d13239b474e23baa3d7fc88baf881/morphir/frontend/test-fixtures/elm/add/src/Arithmetic/Add.elm).
- [Fixture configuration](https://github.com/msigmond/morphir-jvm/blob/3086f7ad501d13239b474e23baa3d7fc88baf881/morphir/frontend/test-fixtures/elm/add/morphir.json): `{"name":"Arithmetic","sourceDirectory":"src"}`.
- [Compiler lockfile](https://github.com/msigmond/morphir-jvm/blob/3086f7ad501d13239b474e23baa3d7fc88baf881/package-lock.json): resolves `morphir-elm` to `2.96.1`.
- [Reference baseline generation](https://github.com/msigmond/morphir-jvm/blob/3086f7ad501d13239b474e23baa3d7fc88baf881/build.mill): invokes the legacy CLI using `make -f`. Here `-f` means **fallback CLI**, not force rebuild.

Commit a local copy of the tiny Elm fixture. Include the commit, upstream links, CLI version, and generation command in `fixtures/elm/add/PROVENANCE.md`. The future frontend must not read this fixture, the expected JSON, or this document at runtime.

## Intended repository layout

```text
DotNetMorphir.csproj                         # Existing project becomes frontend CLI
Program.cs                                  # transpile and compare command dispatch
Frontend/
  RoslynSourceFrontend.cs                    # Parse, bind, validate declarations
  ExpressionTranslator.cs                   # Recursive IOperation translation
  MorphirJson.cs                             # Small, explicit format-v3 constructors
  IrJsonComparer.cs                          # Complete structural comparison + diff
samples/ArithmeticApp/
  ArithmeticApp.csproj                       # Independent net10.0 console sample
  Add.cs                                    # Selected transpilation source
  Program.cs                                # Calls add(2, 3), prints 5
fixtures/elm/add/
  morphir.json
  src/Arithmetic/Add.elm
  PROVENANCE.md
fixtures/expected/add.elm.morphir-ir.json      # Generated and committed Elm golden
tests/DotNetMorphir.Tests/
  DotNetMorphir.Tests.csproj
  AdditionParityTests.cs
  FrontendRejectionTests.cs
  IrJsonComparerTests.cs
scripts/
  generate-elm-baseline.mjs                   # Runs pinned CLI, checks fresh output
  verify-add-parity.mjs                       # Builds, regenerates, compares, tests
package.json
package-lock.json
.gitignore
README.md
out/elm/morphir-ir.json                      # Fresh reference, ignored by Git
out/csharp/morphir-ir.json                   # Actual frontend output, ignored by Git
```

The root SDK project recursively includes `.cs` files. Add `<Compile Remove="samples/**/*.cs;tests/**/*.cs" />` in an `ItemGroup` so the sample and tests are compiled only by their own projects. Ignore all `bin/`, `obj/`, `node_modules/`, and `out/` directories. Keep the generated golden under `fixtures/expected/` tracked.

The test project may reference the existing executable project; it need not introduce a separate production library for this concept. Use a standard xUnit project created by the installed .NET SDK, retain explicit versions of its test packages, and replace the template test with the meaningful tests below.

## Phase 1: create the independent Elm reference

1. Add the copied Elm source and configuration at the paths above. Preserve source content and omit documentation comments so the generated documentation fields remain the verified empty/null values.
2. Add a private root `package.json` with an exact dev dependency `"morphir-elm": "2.96.1"`. Run `npm install` once to create the lockfile; thereafter use `npm ci`.
3. Implement `scripts/generate-elm-baseline.mjs` using built-in Node APIs. Resolve repository paths relative to the script, verify the installed compiler version is `2.96.1`, create `out/elm/`, remove any previous reference output, and run:

   ```bash
   node node_modules/morphir-elm/cli/morphir-elm.js make -f \
     -p fixtures/elm/add -o out/elm/morphir-ir.json
   ```

4. Check the subprocess status, output file existence, and successful JSON parsing. A missing or stale file is a failure even if the CLI reports success. Verify `formatVersion` is `3` and that the distribution contains the one expected module and value.
5. Copy this generated file to `fixtures/expected/add.elm.morphir-ir.json` once. Record provenance; do not manually author or patch the golden to make a later comparison pass.

**Phase exit:** a freshly generated Elm distribution matches the verified JSON contract in this plan. If it differs, first check compiler version, fallback flag, fixture configuration, and source before changing implementation decisions.

## Phase 2: create and run the C# sample

Create `samples/ArithmeticApp/ArithmeticApp.csproj` as an independent console application with target `net10.0`, implicit usings enabled, and `<LangVersion>13.0</LangVersion>`.

`samples/ArithmeticApp/Add.cs` must contain:

```csharp
namespace Arithmetic;

public static class Add
{
    public static int add(int a, int b) => a + b;
}
```

The lowercase method name is intentional: it matches Elm's `add` and avoids declaring a method named exactly like its enclosing C# class.

`samples/ArithmeticApp/Program.cs` calls `Arithmetic.Add.add(2, 3)` and prints the result. Build and run the sample independently; expected output is `5`.

The frontend reads **only `Add.cs`**. The sample's executable entry point is outside the selected input and is not a Morphir value.

**Phase exit:** the sample builds and runs, while the root project does not accidentally compile its entry point.

## Phase 3: implement parsing, binding, and declaration validation

1. Add the pinned Roslyn package to `DotNetMorphir.csproj`.
2. Define a callable frontend API, such as `Transpile(string sourceText, string sourcePath, string packageName)`, returning a JSON tree or a structured failure. Unit tests must call this API without launching a subprocess.
3. Parse using `CSharpSyntaxTree.ParseText`, `CSharpParseOptions(LanguageVersion.CSharp13)`, and the actual input path for diagnostics.
4. Create a `CSharpCompilation` with output kind `DynamicallyLinkedLibrary`, overflow checking disabled, the parsed tree, and `MetadataReference.CreateFromFile(typeof(object).Assembly.Location)`. That reference is sufficient for this intentionally dependency-free source. Arbitrary project references are outside scope.
5. Inspect compilation diagnostics and stop on any `DiagnosticSeverity.Error`. Then obtain the tree's `SemanticModel`.
6. Validate the selected file's complete declaration shape. Use `GetDeclaredSymbol` to obtain the class, method, and parameter symbols. Require the public/static modifiers, exactly two ordinary non-optional parameters, no `ref`/`out`/`in`/`params`, no generic parameters, and `SpecialType.System_Int32` for both parameters and the result. Reject additional classes, methods, fields, properties, constructors, attributes, and executable top-level statements.
7. Extract the expression from `MethodDeclarationSyntax.ExpressionBody.Expression`, or from the sole `ReturnStatementSyntax.Expression` in its block. Call `SemanticModel.GetOperation(expression)`. Reject null or invalid operations.
8. Derive package/module/value names from the supplied package and actual symbols. For this slice require a single-word namespace equal to the supplied single-word package. The class becomes one relative module segment, and the method becomes its value name. Restrict identifiers to simple ASCII words; implement a lowercase single-token `Name` helper and diagnose names needing more sophisticated tokenization. Thus `Arithmetic` becomes `[["arithmetic"]]`, class `Add` becomes `[["add"]]`, and method `add` becomes `["add"]`. Reject names that would create collisions.
9. Report unsupported syntax with input path, one-based line/column, and the unsupported construct or operation kind. Do not silently emit an incomplete distribution.

Keep declaration discovery separate from expression translation. Do not add `MSBuildWorkspace`, analyzers, a source generator, or IL inspection to achieve this fixture.

**Phase exit:** Roslyn identifies an `IBinaryOperation` with two `IParameterReferenceOperation` children for the actual source, and invalid C# or unsupported declarations produce useful errors.

## Phase 4: translate semantic operations and serialize exact IR

Use `Microsoft.CodeAnalysis.Operations` and a recursive expression translator that returns both the Morphir JSON expression and its type. Resolve parameter identity with Roslyn symbols, using `SymbolEqualityComparer.Default`; retain parameter order from `IMethodSymbol.Parameters`.

Supported operation rules:

| Operation | Validation | Translation |
| --- | --- | --- |
| `IParameterReferenceOperation` | Symbol belongs to the method's parameter list; type is `System_Int32` | Typed Morphir `Variable` with that parameter's actual name |
| `IBinaryOperation` | `BinaryOperatorKind.Add`, both operands and result are `System_Int32`, `OperatorMethod == null`, not lifted, and `IsChecked == false` | Typed curried applications of `Morphir.SDK.Basics.add` to the recursively translated operands |
| `IParenthesizedOperation`, if present | Child is supported | Translate child directly |
| Any other operation | — | Explicit unsupported-operation error |

Do not unwrap arbitrary conversions or silently ignore compiler-generated operations. The supplied fixture does not require them.

Implement small JSON constructors in `MorphirJson.cs`. They can return fresh `JsonArray`/`JsonObject` instances. `JsonNode` has a single-parent rule: construct fresh nodes or use `DeepClone()` when reusing a type tree; attaching the same node to several parents will fail.

The exact format-v3 contract, verified against generated Elm output, is:

```text
SDK package path = [["morphir"], ["s", "d", "k"]]
Basics module path = [["basics"]]
int FQName = [SDK package path, Basics module path, ["int"]]
add FQName = [SDK package path, Basics module path, ["add"]]

T = ["Reference", {}, int FQName, []]
Fn(input, output) = ["Function", {}, input, output]
Var(type, name) = ["Variable", type, name]
Ref(type, fqName) = ["Reference", type, fqName]
Apply(resultType, function, argument) = ["Apply", resultType, function, argument]

operator = Ref(Fn(T, Fn(T, T)), add FQName)
partial = Apply(Fn(T, T), operator, Var(T, ["a"]))
body = Apply(T, partial, Var(T, ["b"]))
inputTypes = [[["a"], T, T], [["b"], T, T]]
outputType = T
```

These are explanatory aliases, not literal JSON substitutions. Note the repeated type in each **three-element input tuple**, the typed attributes on **every expression**, the `{}` attributes on type nodes, the uppercase constructor tags, and SDK's `["s","d","k"]` tokenization.

Build the envelope as follows, deriving user names and input types from the symbols:

- `formatVersion`: `3`.
- `distribution`: `["Library", [["arithmetic"]], [], packageDefinition]`; dependencies are empty exactly as emitted by this fixture's Elm compiler.
- `packageDefinition.modules`: one `[modulePath, accessControlledModule]` pair. `modulePath` is `[["add"]]`, relative to the package, not the full namespace/class path.
- Module wrapper: `access: "Public"`; module definition has `types: []`, the one value pair, and `doc: null`.
- Value pair: `[["add"], accessControlledDocumentedDefinition]`.
- Value wrapper: `access: "Public"`; its documented value has `doc: ""` and `value` containing `inputTypes`, `outputType`, and `body`.
- Collections encoded as pair arrays must remain arrays; do not replace them with JSON objects. Preserve parameter and application order.

Writing the result must use the translated tree. It must not copy or consult the golden. The translator must not special-case the literal source text `a + b` or a file called `Add.cs`.

**Phase exit:** the C# output has the complete exact structure in the appendix, including documentation and type attributes.

## Phase 5: implement the CLI and complete-distribution comparison

Replace the Hello World entry point with these two commands:

```bash
dotnet run --project DotNetMorphir.csproj -- transpile \
  --source samples/ArithmeticApp/Add.cs --package Arithmetic \
  --output out/csharp/morphir-ir.json

dotnet run --project DotNetMorphir.csproj -- compare \
  --actual out/csharp/morphir-ir.json \
  --expected out/elm/morphir-ir.json
```

`transpile` validates arguments, reads the selected file, translates it completely, creates the output directory, and writes UTF-8 JSON. A compilation or translation failure must not create or overwrite the target output. Print a short success message after writing.

`compare` parses both files and recursively compares the entire JSON documents:

- Objects: compare the same property names and corresponding values, ignoring property order.
- Arrays: require equal lengths and compare each element in order.
- Scalars: require equal kinds and values.
- Preserve all fields and wrappers. Missing fields differ from explicit `null`; `null`, `{}`, and `[]` differ. Do not strip docs, attributes, access metadata, dependency lists, or format versions, rename either side, sort arrays, or compare only the method body.
- Whitespace and object property ordering are the only irrelevant differences for this fixture.
- On mismatch, report the first JSON path, for example `$.distribution[3].modules[0][1].value...`, and expected/actual values. Return a nonzero exit code.

Use exit code `0` for success, `1` for compilation/translation/comparison or I/O failure, and `2` for invalid CLI usage. A successful comparison prints `PASS: C# and Elm Morphir distributions match.`

**Phase exit:** the two independent generated artifacts match structurally, and altering either artifact causes a useful comparison failure.

## Phase 6: tests, reproducibility, and acceptance

Implement the following focused tests. They must check behavior, not merely repeat serializer constructors.

| Test | Required result |
| --- | --- |
| Translate the real `Add.cs`, then compare with the committed independently generated Elm golden | Complete JSON equality |
| Equivalent `{ return a + b; }` body | Same complete distribution as the expression-bodied fixture |
| Change the source to `b + a` | Variables appear in reversed application order; exact comparison with the original Elm golden fails even though addition is commutative |
| Rename both parameter declarations and their uses to `x`, `y` | Input and variable names are derived from source; original golden comparison fails |
| Invalid C#, `long` parameters, string concatenation, subtraction, checked addition, or an extra statement | Clear compilation or unsupported-feature failure |
| Same JSON with different whitespace/object property order | Comparison succeeds |
| Alter a typed attribute or swap the input array order | Comparison fails at a useful path |
| Remove the module's `doc: null` or add an unexpected property | Comparison fails; no field-stripping normalization |

Also test the independently compiled sample method on `(0, 0)`, `(2, 3)`, and `(-2, 5)`; expected results are `0`, `5`, and `3`. The test project can reference the sample executable project to call its method.

Give the tests a reliable way to locate fixtures: copy the Elm golden and `Add.cs` to the test output directory with explicit `Content` items and `CopyToOutputDirectory`, and use `AppContext.BaseDirectory`. Do not depend on the test runner's current working directory or any absolute developer-specific paths.

Implement `scripts/verify-add-parity.mjs` as a fail-fast sequence using built-in Node subprocess APIs, with repository root as its working directory:

1. Require restored local dependencies and validate the pinned compiler version.
2. Build `DotNetMorphir.csproj` and `samples/ArithmeticApp/ArithmeticApp.csproj`.
3. Run the sample with `--no-build`, require success and output `5`.
4. Regenerate `out/elm/morphir-ir.json` from Elm using the baseline script.
5. Compare the fresh Elm output with `fixtures/expected/add.elm.morphir-ir.json`; this detects a changed fixture/toolchain or stale golden.
6. Run the C# frontend on `Add.cs` to produce `out/csharp/morphir-ir.json`.
7. Compare the complete C# distribution with the **fresh** Elm distribution.
8. Run `dotnet test tests/DotNetMorphir.Tests/DotNetMorphir.Tests.csproj`.
9. Propagate every failure and print one overall success message only after all steps pass.

Document the clean-checkout workflow in `README.md`:

```bash
npm ci
dotnet restore DotNetMorphir.csproj
dotnet restore samples/ArithmeticApp/ArithmeticApp.csproj
dotnet restore tests/DotNetMorphir.Tests/DotNetMorphir.Tests.csproj
node scripts/verify-add-parity.mjs
```

Document prerequisites, both CLI commands, generated output locations, the supported subset, and how to deliberately regenerate/review the golden after an intentional fixture/compiler change. A golden mismatch during ordinary testing must fail; never automatically refresh it in the verification script.

### Final acceptance checklist

- [ ] C# input is parsed and bound by Roslyn; arithmetic is translated from semantic operations.
- [ ] The sample application compiles and prints `5`.
- [ ] The Elm fixture, exact compiler version, lockfile, and reference provenance are committed.
- [ ] Both independently generated complete Morphir distributions are structurally identical.
- [ ] Tests demonstrate source-dependent translation and catch unsupported input and JSON mismatches.
- [ ] Verification runs from a clean checkout without using a global Morphir CLI, remote moving source, temporary planning files, or another checkout's dependencies.
- [ ] The implementation remains within the stated C# subset and makes no claim of general .NET transpilation or unrestricted integer runtime equivalence.
- [ ] Final implementation report states the commands actually run and their results; any failed prerequisite is reported as a failure, not parity success.

## Appendix: exact Elm reference JSON

The following JSON was generated from the pinned fixture by Morphir Elm `2.96.1` using `make -f`. It is included to remove schema ambiguity. Generate the repository's reference independently in Phase 1; do not substitute this appendix for that compiler run.

```json
{
  "formatVersion": 3,
  "distribution": [
    "Library",
    [["arithmetic"]],
    [],
    {
      "modules": [
        [
          [["add"]],
          {
            "access": "Public",
            "value": {
              "types": [],
              "values": [
                [
                  ["add"],
                  {
                    "access": "Public",
                    "value": {
                      "doc": "",
                      "value": {
                        "inputTypes": [
                          [
                            ["a"],
                            ["Reference",{},[[["morphir"],["s","d","k"]],[["basics"]],["int"]],[]],
                            ["Reference",{},[[["morphir"],["s","d","k"]],[["basics"]],["int"]],[]]
                          ],
                          [
                            ["b"],
                            ["Reference",{},[[["morphir"],["s","d","k"]],[["basics"]],["int"]],[]],
                            ["Reference",{},[[["morphir"],["s","d","k"]],[["basics"]],["int"]],[]]
                          ]
                        ],
                        "outputType": ["Reference",{},[[["morphir"],["s","d","k"]],[["basics"]],["int"]],[]],
                        "body": [
                          "Apply",
                          ["Reference",{},[[["morphir"],["s","d","k"]],[["basics"]],["int"]],[]],
                          [
                            "Apply",
                            [
                              "Function",
                              {},
                              ["Reference",{},[[["morphir"],["s","d","k"]],[["basics"]],["int"]],[]],
                              ["Reference",{},[[["morphir"],["s","d","k"]],[["basics"]],["int"]],[]]
                            ],
                            [
                              "Reference",
                              [
                                "Function",
                                {},
                                ["Reference",{},[[["morphir"],["s","d","k"]],[["basics"]],["int"]],[]],
                                [
                                  "Function",
                                  {},
                                  ["Reference",{},[[["morphir"],["s","d","k"]],[["basics"]],["int"]],[]],
                                  ["Reference",{},[[["morphir"],["s","d","k"]],[["basics"]],["int"]],[]]
                                ]
                              ],
                              [[["morphir"],["s","d","k"]],[["basics"]],["add"]]
                            ],
                            ["Variable",["Reference",{},[[["morphir"],["s","d","k"]],[["basics"]],["int"]],[]],["a"]]
                          ],
                          ["Variable",["Reference",{},[[["morphir"],["s","d","k"]],[["basics"]],["int"]],[]],["b"]]
                        ]
                      }
                    }
                  }
                ]
              ],
              "doc": null
            }
          }
        ]
      ]
    }
  ]
}
```

## Roslyn API references

- [Pinned Roslyn package](https://www.nuget.org/packages/Microsoft.CodeAnalysis.CSharp/4.14.0).
- [Syntax model](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/work-with-syntax) and [semantic model](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/work-with-semantics).
- [`IBinaryOperation`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.operations.ibinaryoperation?view=roslyn-dotnet-4.14.0) and [`IParameterReferenceOperation`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.operations.iparameterreferenceoperation?view=roslyn-dotnet-4.14.0).
