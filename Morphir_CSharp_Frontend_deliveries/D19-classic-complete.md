# D19 — Complete v3 Classic model and serializer

- status: planned
- required-context: [master](../Morphir_CSharp_Frontend_delivery_plan.md) + this file + prerequisite evidence
- prerequisites: [D18.4](D18_4-feature-closure.md)
- outcome: Every v3 distribution-reachable model/codec case
- ownership: Classic/Json completeness inventory; independent codec host fixtures
- fixture-root: `fixtures/D19/`; test trait: `Delivery=D19`

## Implementation

- [ ] CurrentSupportedExample update: phase marker/coverage refresh + full cumulative verification; no new business-source feature from model-only nodes
- [ ] Complete every remaining Classic algebra case; generic type/value attributes; typed aliases
- [ ] Explicit codecs for tags/dictionaries/scalars/unit/visibility/docs/dependencies
- [ ] Independent Elm IR/codec output for source-inexpressible cases; full versioned distribution comparisons
- [ ] Host sample: ExtensibleRecord(row) with Unit value field; ModelFixture owner in both languages
- [ ] Replace sample JsonArray construction with actual Classic-node construction in implementation tests; no invented API implied
- [ ] Host JSON/codec APIs outside admitted business-source catalog; no frontend translation of host fixture

## Model increment

- Full inventory below, including cases not emitted by admitted source subset

| Model family | Required cases |
| --- | --- |
| Identity and wrappers | Name, Path, FQName, access control, documentation, unit attributes, versioned distribution |
| Distribution | Library, dependency package specifications, package definition |
| Packages and modules | Definitions and specifications; their type/value maps, docs and visibility |
| Type expressions | Variable, Reference, Tuple, Record, ExtensibleRecord, Function, Unit |
| Type definitions | TypeAliasDefinition, CustomTypeDefinition; constructors and named arguments |
| Type specifications | TypeAliasSpecification, OpaqueTypeSpecification, CustomTypeSpecification, DerivedTypeSpecification and its details |
| Literals | BoolLiteral, CharLiteral, StringLiteral, WholeNumberLiteral, FloatLiteral, DecimalLiteral |
| Value expressions | Literal, Constructor, Tuple, List, Record, Variable, Reference, Field, FieldFunction, Apply, Lambda, LetDefinition, LetRecursion, Destructure, IfThenElse, PatternMatch, UpdateRecord, Unit |
| Patterns | WildcardPattern, AsPattern, TuplePattern, ConstructorPattern, EmptyListPattern, HeadTailPattern, LiteralPattern, UnitPattern |
| Values | Specifications, definitions, named inputs, output types, body and attributes |

## Fixture contract

- package: `Example`; namespace: `Example.D19`
- source data paths: —
- member modules: `Example.D19.ModelFixture`
- constraints: Original host C# snippet: wire-shape specification only; production test must exercise Classic model
- fixture role: host serializer/codec; excluded from business frontend

## Acceptance

- [ ] Expected host JSON: exact structure below; model serializer vs independent Elm codec
- [ ] Every inventory case: C# representation + codec fixture + complete-distribution inclusion
- [ ] No model-only case enables excluded source feature; no normalization/metadata loss

## Rejections

- [ ] Missing reachable algebra/codec case; speculative model API dependency; host fixture passed to business frontend
- [ ] Default dictionary JSON serialization; dropped tags/access/docs/attributes

## Code examples

### C# — host serializer fixture

```csharp
namespace Example.D19;

public static class ModelFixture
{
    public static string Encode() =>
        new System.Text.Json.Nodes.JsonArray(
            "ExtensibleRecord",
            new System.Text.Json.Nodes.JsonObject(),
            new System.Text.Json.Nodes.JsonArray("row"),
            new System.Text.Json.Nodes.JsonArray(
                new System.Text.Json.Nodes.JsonArray(
                    new System.Text.Json.Nodes.JsonArray("value"),
                    new System.Text.Json.Nodes.JsonArray(
                        "Unit", new System.Text.Json.Nodes.JsonObject()))))
            .ToJsonString();
}
```

### Elm — Example/D19/ModelFixture.elm (host codec fixture)

```elm
module Example.D19.ModelFixture exposing (encode)

import Json.Encode as Encode
import Morphir.IR.Type as Type
import Morphir.IR.Type.Codec as TypeCodec

encode : String
encode =
    Type.ExtensibleRecord () [ "row" ]
        [ { name = [ "value" ], tpe = Type.Unit () } ]
        |> TypeCodec.encodeType (\() -> Encode.object [])
        |> Encode.encode 0
```

### Expected JSON

```json
["ExtensibleRecord",{},["row"],[[["value"],["Unit",{}]]]]
```
