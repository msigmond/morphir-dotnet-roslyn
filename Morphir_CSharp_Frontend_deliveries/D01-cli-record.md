# D1 — CLI, record data, first typed distribution

- status: planned
- required-context: [master](../Morphir_CSharp_Frontend_delivery_plan.md) + this file + prerequisite evidence
- prerequisites: —
- outcome: CLI → typed Classic model → v3 JSON; record eligibility + known-present string + addition
- ownership: Solution; Classic/Json/core/CLI projects; reference harness; first fixtures
- fixture-root: `fixtures/D1/`; test trait: `Delivery=D1`

## Implementation

- [ ] Existing bootstrap docs/license reconciliation; README/AGENTS extended with actual D1 commands; license provenance preserved
- [ ] Repository scaffolding; master project layout; compatible exact SDK/Roslyn/package pins; .NET 10/C# 14; explicit x64 validation
- [ ] Root LICENSE mirrored from finos/morphir-dotnet + upstream NOTICE; source commit/URLs/file hashes; preserved license/copyright text
- [ ] README: actual setup/build/test/CLI commands, layout, support/exclusions, cumulative sample, license/plan links
- [ ] AGENTS: canonical repository workflow/commands/contracts/gates; CLAUDE: `@AGENTS.md` + newline only
- [ ] CurrentSupportedExample project/class: D1 record/construction/field reads, int/bool, short circuit/comparison/addition, string literal; initial language→example coverage map
- [ ] CLI inputs: explicit source files, package name, namespace selectors, output path; core API: Compilation + settings → typed IR + diagnostics
- [ ] Namespace discovery: descendants; unused/nonpublic declarations; overlapping selectors; outside-source dependency rejection; Public/Private wrappers
- [ ] Shared symbol table: namespace/enclosing-type data definitions; class/type member modules; canonical Name/Path conversion; qualified references
- [ ] Semantic lowering: primitive literals; int addition/comparison; Boolean short circuit; operand order; readonly record struct construction/reads
- [ ] Prototype addition regression ([archived inputs](../docs/references/prototype-addition/PROVENANCE.md)): JsonNode translation replaced by model-backed translation; independent Elm fixture
- [ ] Initial validations: actual checked syntax; checked semantic operations; async/await; pointer/ref-like forms; source-located stable diagnostics
- [ ] Deterministic explicit codecs; complete structural comparator; duplicate-property rejection; atomic successful writes
- [ ] Reference tooling: pinned compiler/source; per-fixture compiler mode/provenance; no automatic golden refresh during validation

## Model increment

- Name/Path/FQName; access/docs/unit; Library/dependency containers; package/module definitions; Reference/Function/Record types; aliases; typed definitions; literals/variables/references/application/fields/record construction/IfThenElse

## Fixture contract

- package: `Example`; namespace: `Example.D1`
- source data paths: `Example.D1.Customer`
- member modules: `Example.D1.Rules`
- constraints: D1 domain: value receiver, int/bool fields, known-present string literal; nullable/string data fields → D4; broad numeric semantics → D5

## Acceptance

- [ ] Cumulative sample builds/transpiles; complete parity/behavior; initial supported forms mapped; README commands verified; AGENTS/CLAUDE/license contracts satisfied
- [ ] Example.D1.Customer: type in Example.D1; no Customer.Customer; functions in Example.D1.Rules
- [ ] Eligible: active age 18 → true; inactive → false; EligibleLabel → "eligible"
- [ ] Record construction + bounded addition: full-distribution parity; pre-D5 admitted domain restricted by proven numeric semantics
- [ ] Package-prefix regression: same sample under package Sample, namespace Sample.Business; Customer → Sample.Business.Customer; Rules → Sample.Business.Rules
- [ ] Unused internal record struct + private static function: emitted owner paths; Private access; public counterparts → Public
- [ ] Selectors: Sample.Business.Tax included; Sample.BusinessExtra excluded; overlap → single declaration
- [ ] Required outside record namespace: failure; explicit selector → success + inclusion of its unused admitted declarations
- [ ] Changed names/operands/fields → corresponding IR changes; compilation/validation failure → no new success artifact

## Rejections

- [ ] checked expression/block/operator declaration, including safe constants and unreachable code
- [ ] Compilation-enabled checked operations; comments/strings/@checked excluded from syntax rejection
- [ ] Async/await; pointers/ref-like forms; unsupported selected forms
- [ ] Missing package name; incompatible 32-bit target; required unselected source dependency

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
