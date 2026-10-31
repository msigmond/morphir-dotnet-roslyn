using Morphir.IR.Classic;
using V = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

internal sealed partial class SourceTranslation
{
    private V ClassicTupleField(CSharpOperation<Unit, IrType<Unit>> field)
    {
        var subject = ClassicValue(field.Children.Single());
        var index = int.Parse(Property(field, "index"), System.Globalization.CultureInfo.InvariantCulture);
        if (subject.Attributes is not TupleType<Unit> { Elements.Count: 2 }) return ClassicFailure("This tuple field needs an approved extraction mapping.");
        return Sdk("Tuple", index == 0 ? "first" : "second", field.Attributes, subject);
    }
    private V ClassicDeconstruction(CSharpOperation<Unit, IrType<Unit>> declaration, V subject, Func<V> continuation)
    {
        var added = new List<string>();
        Pattern<IrType<Unit>> Pattern(V designation, IrType<Unit> type)
        {
            if (designation is not CSharpOperation<Unit, IrType<Unit>> source) return Fail<Pattern<IrType<Unit>>>("MCS4001", "Missing tuple designation.", classicLocation);
            if (source.Kind == "DesignationTuple" && type is TupleType<Unit> tuple && tuple.Elements.Count == source.Children.Count)
                return new TuplePattern<IrType<Unit>>(type, source.Children.Select((d, i) => Pattern(d, tuple.Elements[i])).ToArray());
            if (source.Kind == "Discard") return new WildcardPattern<IrType<Unit>>(type);
            if (source.Kind != "Designation") return Fail<Pattern<IrType<Unit>>>("MCS4001", "Unmapped tuple designation.", classicLocation);
            var name = Name.From(Property(source, "name"));
            if (!classicBindings.Add(name.Key)) return Fail<Pattern<IrType<Unit>>>("MCS4001", "Tuple bindings collide after normalization.", classicLocation);
            added.Add(name.Key);
            return new AsPattern<IrType<Unit>>(type, new WildcardPattern<IrType<Unit>>(type), name);
        }
        try
        {
            var pattern = Pattern(declaration.Children.Single(), subject.Attributes);
            var body = continuation();
            return new DestructureValue<Unit, IrType<Unit>>(body.Attributes, pattern, subject, body);
        }
        finally { foreach (var name in added) classicBindings.Remove(name); }
    }
}
