using Morphir.IR.Classic;

namespace Morphir.Frontend.CSharp;

// Internal staging nodes. These must be mapped or rejected before the frontend returns a library.
internal sealed record CSharpOperation<TA, VA>(VA Attributes, string Kind,
    IReadOnlyList<Entry<string, string>> Properties, IReadOnlyList<Value<TA, VA>> Children) : Value<TA, VA>(Attributes);
internal sealed record UnmappedNumberLiteral(string Text) : Literal;
