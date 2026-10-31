# Classic output

Standard Morphir Classic v3 is the CLI and core API default, as requested on 2026-10-31.
The [exact Elm parity contract](elm-parity.md) controls acceptance.
The existing Source v1 mode was unrequested work. It is not an alternative delivery target.
No command silently switches formats or mixes Source nodes into Classic JSON.

The default file has only `formatVersion: 3` and `distribution`.
It has no C# source tree, Source tags, source assembly IDs, or numeric compatibility modules.

## Current mappings

| C# structure | Classic structure |
| --- | --- |
| Block with initialized locals and a final value return | Nested LetDefinition and the return expression |
| Ordinary arithmetic and field reads | Direct SDK applications and Field |
| Source function call with complete positional arguments | Reference and ordered Apply nodes |
| Positional record creation | Alias Constructor and ordered Apply nodes |
| Parentheses, null-forgiving, checked/unchecked context | The enclosed expression; no CLR check or wrapper |
| Int/Float negation or Boolean not | One SDK operation |

Keep source local names, declared types, visibility, package, and module owners.
References to source methods remain fully qualified. A same-named local does not make a global call recursive.
Primary record constructors are represented by alias constructors. Do not export separate `New...` functions or empty owner modules.
Object creation has the constructed data type. A constructor symbol's `void` return type is not the expression type.

Classic mode has an explicit mapping subset. Assignment, statement branches, optional/named/expanded calls, general construction,
nullable operations, implicit conversions between different IR types, instance initializers, recursive aliases,
intrinsic C# types, and other unmapped forms report `MCS4001` at the source definition.
No output is produced on failure; an existing output file remains unchanged.
Legacy Source-mode support does not establish delivery acceptance. D10–D21 remain planned.
These are representation gaps, not runtime safety restrictions. No presence, range, nonzero, purity, or ownership proof is required.

## Existing supplemental comparison

This normalized comparison predates the exact-parity rule. It does not pass that gate.
New feature tests must compare complete compiler JSON without these normalizations.

The [business fixture](../fixtures/classic-business-logic/provenance.json) preserves the supplied C# files and corrected Elm project/output.
Expected JSON came from the Elm compiler, not the C# frontend.
The legacy test helper compares the complete distributions after three explicit equivalences:

1. Place package and module segments at the requested package boundary.
2. Rename local binding variables consistently by lexical binding position.
3. Resolve retained Elm local inference variables from their typed initializer attributes.

No module, type, function, input, output, field, access flag, operator, literal, or call is discarded by the comparison.
Negative checks prove that changed operators and visibility still fail.
The C# names `marketValue` and `dailyFee` remain in actual output.
The Elm local names `marketVal` and `dailyF` remain in the frozen reference.

The frozen fixture Elm configuration uses package `Sigmond`; its module is `Buslog.Model`.
The original C# command selects package `Sigmond.Buslog`; its module is `Model`.
Both display `Sigmond.Buslog.Model.Loan`, but their encoded package/module split differs.
Use `--package Sigmond` for the same package split as that Elm file.
The original `--package Sigmond.Buslog` command remains valid and retains that requested package.

## Sample

After build, from the repository root:

```bash
dotnet run --project src/Morphir.Frontend.CSharp.Cli --no-build -- --package Example --namespace Example.CurrentSupported --source samples/CurrentSupportedExample/ClassicLoan.cs --output artifacts/classic-loan.json
```

The full cumulative sample includes unmapped Classic forms and is not an accepted Classic delivery.
See [validation evidence](deliveries/classic-output-state.md).
