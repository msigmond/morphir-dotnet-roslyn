# Models and codecs

The Classic algebra and standard v3 codec remain available and Roslyn-free.
Independent model/codec fixtures remain frozen and active.
The C# frontend now adds the explicit [Morphir.CSharp.Source v1 contract](../source-structure.md).
It is not standard Classic v3 output.

| Model | Coverage |
| --- | --- |
| Classic names, owners, access, docs, libraries, dependencies, types, values, patterns | Existing independent model and JSON tests |
| SourceValue: kind, properties, ordered children | AuditMatrixTests; source schema serialization |
| SourceNumberLiteral and CharLiteral | Wide and character source cases; explicit literal codec |
| SourceSyntax, SourceLocation, SourceDocument | Complete selected declaration/token parity; source spelling, bindings, types, source order |
| Source v1 envelope | Explicit format/version assertions; CLI parity and atomic writes |
| Plain Classic v3 envelope | Existing independent model fixture comparisons |

Source operations retain assignments, returns, blocks, casts, indices, calls, constructors, nullable syntax, and declaration contexts.
They encode structure and do not define CLR execution.
The typed projection uses SDK values where a simple mapping fits. Source metadata retains distinctions lost by that projection.
No mutable runtime store, compatibility algorithm, or synthesized body is added.

D19 retains complete model/codec closure. Current source support does not claim the full upstream v3 algebra.
Source v1 consumers must check the format before reading the extended distribution.
