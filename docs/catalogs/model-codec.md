# Models and codecs

The public model and codec use standard Classic v3 only and remain Roslyn-free.
Source v1 envelope, syntax-document, Source operation, and source-number types are removed.
The C# frontend's staging operations are internal and must be mapped or rejected.
Unknown model nodes fail serialization.
Standard LetRecursion and Destructure nodes are now exercised by complete frontend-to-Elm parity cases.

Frontend acceptance uses complete JSON equality against `morphir-elm make`, including all model attributes.
Independent historical model/codec references remain frozen and active as supplemental codec tests.
They do not replace frontend compiler parity.
D19 retains the remaining standard model/codec inventory. No second output schema is planned.
See [current evidence](../deliveries/D11-follow-up-elm-parity.md).

D10 exercises existing alias parameters, Variable types, and instantiated references through frontend parity.
The Classic v3 schema and codec need no extension.

D11 uses the existing List, Tuple, Reference, and Apply nodes. No codec extension is required.

D12 adds the standard LiteralPattern node. Complete frontend-to-Elm cases test its attributes and literal payloads.
The Classic v3 envelope stays unchanged.

D13 and the D11 follow-up use existing Lambda, Function, Reference, and Apply nodes. No schema or codec extension is required.
