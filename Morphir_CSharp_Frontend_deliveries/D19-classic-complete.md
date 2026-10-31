# D19 — Complete models and codecs

- status: planned
- contract: [exact Elm parity](../docs/elm-parity.md)
- prerequisites: [D18.4](D18_4-feature-closure.md)
- evidence: [exact-parity profile](../docs/deliveries/elm-parity-state.md); [remaining scope](../docs/elm-parity.md#existing-gaps)

## Prerequisite profile

Use the [D1–D9 Elm profile](../docs/deliveries/elm-parity-state.md) and all earlier exact-parity gates.
Each new mapping needs independent Elm input and complete JSON equality. Historical Source-mode passes do not count.

## Work

Scope below is a feature inventory. Confirm its standard Elm mapping before implementation.
No Source v1 tag or metadata envelope can satisfy this delivery.

- Complete the pinned Classic algebra/codec inventory required by the frontend.
- Emit only the requested standard Morphir IR contract.
- Keep the removed Source v1 output out of the release; do not develop a second schema.

## Acceptance

- Frontend feature references come from `morphir-elm make`; codec tests are supplemental.
- No automatic reference refresh occurs during validation.
- Every feature needs paired C#/Elm inputs and full JSON equality against `morphir-elm make`.
- Ignore only whitespace/object-key order. No package, name, type, or operation normalization.
- Ask the user if the Elm counterpart is unclear. No unrequested output or feature is permitted.
- Run affected delivery tests, catalog checks, and repository checks.
- Keep toolchain pins, selected-source errors, source locations, and atomic output.

The C# examples below are feature inputs. The Elm examples show the former runtime mapping.
They are historical context. New acceptance references must come from `morphir-elm make`.

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
