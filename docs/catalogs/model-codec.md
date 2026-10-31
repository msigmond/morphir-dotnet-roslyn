# Models and codecs

The public model and codec use standard Classic v3 only and remain Roslyn-free.
Source v1 envelope, syntax-document, Source operation, and source-number types are removed.
The C# frontend's staging operations are internal and must be mapped or rejected.
Unknown model nodes fail serialization.

Frontend acceptance uses complete JSON equality against `morphir-elm make`, including all model attributes.
Independent historical model/codec references remain frozen and active as supplemental codec tests.
They do not replace frontend compiler parity.
D19 retains the remaining standard model/codec inventory. No second output schema is planned.
See [current evidence](../deliveries/elm-parity-state.md).
