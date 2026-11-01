# D10 — Exact Elm acceptance

- Date: 2026-11-01.
- Status: passed under the approved generic profile.
- Prerequisite: [D1–D9 passed profile](elm-parity-state.md).
- Contract: [exact Elm parity](../elm-parity.md).
- User decision: [generic mapping and explicit rejections](../mappings/D10.md).

## Scope

Four new independent Elm projects cover generic aliases, methods, calls, member functions,
constructors, nullable type arguments, delegates, local functions, nested owners, overloads, and visibility.
The cumulative sample adds generic wrapping and reading.
The registry now has 77 C# cases and 59 successful Elm projects.
One separate Elm input confirms the compiler rejection for conflicting local body types.
Thirty-one new bound source cases test D10 rejection boundaries and collisions.
Sixty-two bound member cases test each generic numeric/Decimal conversion without an approved mapping.
The full member/form rejection registry now has 336 cases.

Declarations retain type variables. Resolved calls use instantiated types.
Alias fields substitute the declared type arguments, including outer parameters on nested types.
Owners retain declaration names. Arity collisions fail; no new name suffix is invented.
Constraints, variance, interfaces, inheritance, dispatch, base access, and runtime type tests remain rejected.
No runtime closure, hidden dispatch, helper library, or additional IR format is added.
The existing Classic v3 schema contains all required type nodes.
Historical sources, references, and license bytes remain frozen.

## Commands

Reference generation is a separate operation and uses only the pinned Elm compiler.
Tests never update the reference output.

```bash
node eng/generate-exact-references.mjs --write
dotnet restore Morphir.CSharp.slnx --packages .packages --locked-mode
dotnet build Morphir.CSharp.slnx --no-restore -m:1
dotnet test Morphir.CSharp.slnx --no-build --no-restore -m:1
python3 eng/check-catalogs.py --deliveries D1 D2 D3 D4 D5 D6 D7 D8 D9 D10
python3 eng/check-catalogs.py
python3 eng/check-repository.py
```

## Results

Locked restore passed. Build passed with zero warnings/errors.
The complete run passed 548 tests: 512 frontend, 11 integration, 4 model, and 21 codec.
All 77 C# cases matched fresh output from the 59 independent Elm projects.
The project-mode cumulative sample CLI also matched the complete independent Elm JSON.
The separate Elm input failed with TypeCheckError; the corresponding C# source was explicitly rejected.
The D1–D10 catalog gate has zero pending entries. The global gate retains 1,123 later obligations.
Repository checks passed for plans, links, provenance, frozen history, licenses, and whitespace.

Logs: `/tmp/d10-restore.log`, `/tmp/d10-build.log`, `/tmp/d10-references.log`, `/tmp/d10-final-tests.log`, `/tmp/d10-sample.log`.
D11–D21 remain planned. This result covers the approved mapping and explicit rejection boundaries.
