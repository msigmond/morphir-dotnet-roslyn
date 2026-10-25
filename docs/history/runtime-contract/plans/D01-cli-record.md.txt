# D1 — CLI, record data, first typed distribution

- status: passed; [execution evidence](../docs/deliveries/D1-state.md)
- required-context: [master](../Morphir_CSharp_Frontend_delivery_plan.md) + this file + prerequisite evidence
- prerequisites: —
- outcome: CLI → typed Classic model → v3 JSON; record eligibility + known-present string + addition
- ownership: Solution; Classic/Json/core/CLI projects; reference harness; first fixtures
- fixture-root: `fixtures/D1/`; test trait: `Delivery=D1`

## Implementation

- [x] Existing bootstrap docs/license reconciliation; README/AGENTS extended with actual D1 commands; license provenance preserved
- [x] Repository scaffolding; master project layout; compatible exact SDK/Roslyn/package pins; .NET 10/C# 14; explicit x64 validation
- [x] Root LICENSE mirrored from finos/morphir-dotnet; root NOTICE bytes preserved; complete upstream NOTICE retained in supplemental reference; source commit/URLs/file hashes and bootstrap discrepancy reconciled in [licensing provenance](../docs/references/LICENSING.md)
- [x] README: actual setup/build/test/CLI commands, layout, support/exclusions, cumulative sample, license/plan links
- [x] AGENTS: canonical repository workflow/commands/contracts/gates; CLAUDE: `@AGENTS.md` + newline only
- [x] CurrentSupportedExample project/class: D1 record/construction/field reads, int/bool, short circuit/comparison/addition, string literal; initial language→example coverage map
- [x] CLI inputs: explicit source files, package name, namespace selectors, output path; core API: Compilation + settings → typed IR + diagnostics
- [x] Namespace discovery: descendants; unused/nonpublic declarations; overlapping selectors; outside-source dependency rejection; Public/Private wrappers
- [x] Shared symbol table: namespace/enclosing-type data definitions; class/type member modules; canonical Name/Path conversion; qualified references
- [x] Semantic lowering: primitive literals; int addition/comparison; Boolean short circuit; operand order; readonly record struct construction/reads
- [x] Prototype addition regression ([archived inputs](../docs/references/prototype-addition/PROVENANCE.md)): model-backed safe specialization with independent Elm fixture; original unbounded addition explicitly rejected under the required pre-D5 safety boundary (see [mapping](../docs/mappings/D1.md))
- [x] Initial validations: actual checked syntax; checked semantic operations; async/await; pointer/ref-like forms; source-located stable diagnostics
- [x] Deterministic explicit codecs; complete structural comparator; duplicate-property rejection; atomic successful writes
- [x] Reference tooling: pinned compiler/source; per-fixture compiler mode/provenance; no automatic golden refresh during validation

## Model increment

- Name/Path/FQName; access/docs/unit; Library/dependency containers; package/module definitions; Reference/Function/Record types; aliases; typed definitions; literals/variables/references/application/fields/record construction/IfThenElse

## Fixture contract

- package: `Example`; namespace: `Example.D1`
- source data paths: `Example.D1.Customer`
- member modules: `Example.D1.Rules`
- constraints: D1 domain: value receiver, int/bool fields, known-present string literal; nullable/string data fields → D4; broad numeric semantics → D5

## Acceptance

- [x] Cumulative sample builds/transpiles; complete parity/behavior; initial supported forms mapped; README commands verified; AGENTS/CLAUDE/license contracts satisfied
- [x] Example.D1.Customer: type in Example.D1; no Customer.Customer; functions in Example.D1.Rules
- [x] Eligible: active age 18 → true; inactive → false; EligibleLabel → "eligible"
- [x] Record construction + bounded addition: full-distribution parity; pre-D5 admitted domain restricted by proven numeric semantics
- [x] Package-prefix regression: same sample under package Sample, namespace Sample.Business; Customer → Sample.Business.Customer; Rules → Sample.Business.Rules
- [x] Unused internal record struct + private static function: emitted owner paths; Private access; public counterparts → Public
- [x] Selectors: Sample.Business.Tax included; Sample.BusinessExtra excluded; overlap → single declaration
- [x] Required outside record namespace: failure; explicit selector → success + inclusion of its unused admitted declarations
- [x] Changed names/operands/fields → corresponding IR changes; compilation/validation failure → no new success artifact

## Rejections

- [x] checked expression/block/operator declaration, including safe constants and unreachable code
- [x] Compilation-enabled checked operations; comments/strings/@checked excluded from syntax rejection
- [x] Async/await; pointers/ref-like forms; unsupported selected forms
- [x] Missing package name; incompatible 32-bit target; required unselected source dependency

## Gate evidence

All D1 gates passed: 60 tests across model/JSON/frontend/integration suites; eight independent complete distributions; compiled C#/IR behavioral boundaries; successful cumulative CLI parity; clean build with zero warnings/errors; repository contracts and protected license bytes checked. D2 onward remains pending. [State and exact commands/results](../docs/deliveries/D1-state.md).

Local aliases/updates, nullable extraction and Decimal operations are not admitted by D1; their positive safety/proof gates remain in their assigned deliveries, with D1 rejection coverage. No reference refresh occurs during validation.

## Code examples

### C# — Customer.cs

```csharp
namespace Example.D1;

public readonly record struct Customer(int Age, bool Active);
```

### C# — Rules.cs

```csharp
namespace Example.D1;

public static class Rules
{
    public static bool Eligible(Customer customer) =>
        customer.Active && customer.Age >= 18;

    public static string EligibleLabel() => "eligible";
}
```

### Elm — Example/D1.elm

```elm
module Example.D1 exposing (Customer)

type alias Customer =
    { age : Int, active : Bool }
```

### Elm — Example/D1/Rules.elm

```elm
module Example.D1.Rules exposing (eligible, eligibleLabel)

import Example.D1 exposing (Customer)

eligible : Customer -> Bool
eligible customer =
    if customer.active then
        customer.age >= 18
    else
        False

eligibleLabel : String
eligibleLabel =
    "eligible"
```
