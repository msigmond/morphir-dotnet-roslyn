# D19 — Complete models and codecs

- status: planned
- contract: [source structure](../docs/source-structure.md)
- prerequisites: [D18.4](D18_4-feature-closure.md)
- evidence: [migration state](../docs/deliveries/source-structure-state.md)

## Work

- Complete the source-operation schema and its explicit codecs.
- Complete the separately identified pinned Classic algebra/codec inventory.
- Validate source v1 and Classic v3 as different wire contracts.
- Add explicit format/schema migration rules. Do not label source extension tags as standard Classic v3.

## Acceptance

- Independent complete references cover each schema and source operation.
- No automatic reference refresh occurs during validation.
- Add independent source/IR assertions and cumulative sample coverage.
- Run affected delivery tests, catalog checks, and repository checks.
- Keep toolchain pins, selected-source errors, source locations, and atomic output.

The C# examples below are feature inputs. The Elm examples show the former runtime mapping.
They are historical context and are not the expected source v1 output.

## Historical examples

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
