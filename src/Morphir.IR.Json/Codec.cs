using System.Text;
using System.Text.Json;
using Morphir.IR.Classic;
using IrPath = Morphir.IR.Classic.Path;

namespace Morphir.IR.Json;

public static class Codec
{
    private static object[] Arr(params object?[] items) => items!;
    private static object Name(Name n) => n.Words;
    private static object Path(IrPath p) => p.Segments.Select(Name).ToArray();
    private static object Fq(FQName q) => Arr(Path(q.Package), Path(q.Module), Name(q.Local));
    private static object Unit(Unit _) => new Dictionary<string, object>();
    private static object Access<T>(AccessControlled<T> a, Func<T, object> encode) => new { access = a.Access.ToString(), value = encode(a.Value) };
    private static object Doc<T>(Documented<T> d, Func<T, object> encode) => new { doc = d.Doc, value = encode(d.Value) };
    private static object Entries<K, V>(IReadOnlyList<Entry<K, V>> list, Func<K, object> key, Func<V, object> val) => list.Select(e => Arr(key(e.Key), val(e.Value))).ToArray();
    public static object Type<A>(IrType<A> t, Func<A, object> attr) => t switch
    {
        UnitType<A> u => Arr("Unit", attr(u.Attributes)),
        VariableType<A> v => Arr("Variable", attr(v.Attributes), Name(v.Name)),
        ReferenceType<A> r => Arr("Reference", attr(r.Attributes), Fq(r.Name), r.Arguments.Select(a => Type(a, attr)).ToArray()),
        FunctionType<A> f => Arr("Function", attr(f.Attributes), Type(f.Argument, attr), Type(f.Result, attr)),
        RecordType<A> r => Arr("Record", attr(r.Attributes), r.Fields.Select(f => new { name = Name(f.Key), tpe = Type(f.Value, attr) }).ToArray()),
        _ => throw new NotSupportedException(t.GetType().Name)
    };
    private static object Alias<A>(AliasDefinition<A> d, Func<A, object> attr, bool spec = false) => Arr(spec ? "TypeAliasSpecification" : "TypeAliasDefinition", d.Parameters.Select(Name).ToArray(), Type(d.Body, attr));
    private static object Literal(Literal l) => l switch
    {
        DecimalLiteral d => Arr("DecimalLiteral", d.Value),
        FloatLiteral f when double.IsFinite(f.Value) => Arr("FloatLiteral", f.Value),
        BoolLiteral b => Arr("BoolLiteral", b.Value),
        IntLiteral i => Arr("WholeNumberLiteral", i.Value),
        StringLiteral s => Arr("StringLiteral", s.Value),
        _ => throw new NotSupportedException(l.GetType().Name)
    };
    private static object Pattern<VA>(Pattern<VA> pattern, Func<VA, object> attr) => pattern switch
    {
        UnitPattern<VA> p => Arr("UnitPattern", attr(p.Attributes)),
        EmptyListPattern<VA> p => Arr("EmptyListPattern", attr(p.Attributes)),
        HeadTailPattern<VA> p => Arr("HeadTailPattern", attr(p.Attributes), Pattern(p.Head, attr), Pattern(p.Tail, attr)),
        WildcardPattern<VA> p => Arr("WildcardPattern", attr(p.Attributes)),
        AsPattern<VA> p => Arr("AsPattern", attr(p.Attributes), Pattern(p.Pattern, attr), Name(p.Name)),
        ConstructorPattern<VA> p => Arr("ConstructorPattern", attr(p.Attributes), Fq(p.Name), p.Arguments.Select(a => Pattern(a, attr)).ToArray()),
        _ => throw new NotSupportedException(pattern.GetType().Name)
    };
    public static object Value<TA, VA>(Value<TA, VA> v, Func<TA, object> ta, Func<VA, object> va)
    {
        object V(Value<TA, VA> x) => Value(x, ta, va);
        var a = va(v.Attributes);
        return v switch
        {
            UnitValue<TA, VA> x => Arr("Unit", a),
            LambdaValue<TA, VA> x => Arr("Lambda", a, Pattern(x.Argument, va), V(x.Body)),
            ListValue<TA, VA> x => Arr("List", a, x.Elements.Select(V).ToArray()),
            ConstructorValue<TA, VA> x => Arr("Constructor", a, Fq(x.Name)),
            MatchValue<TA, VA> x => Arr("PatternMatch", a, V(x.Subject), x.Cases.Select(c => Arr(Pattern(c.Pattern, va), V(c.Body))).ToArray()),
            LiteralValue<TA, VA> l => Arr("Literal", a, Literal(l.Literal)),
            VariableValue<TA, VA> x => Arr("Variable", a, Name(x.Name)),
            ReferenceValue<TA, VA> x => Arr("Reference", a, Fq(x.Name)),
            ApplyValue<TA, VA> x => Arr("Apply", a, V(x.Function), V(x.Argument)),
            FieldValue<TA, VA> x => Arr("Field", a, V(x.Subject), Name(x.Field)),
            RecordValue<TA, VA> x => Arr("Record", a, Entries(x.Fields, Name, V)),
            LetValue<TA, VA> x => Arr("LetDefinition", a, Name(x.Name), Definition(x.Definition, ta, va), V(x.Body)),
            IfValue<TA, VA> x => Arr("IfThenElse", a, V(x.Condition), V(x.Then), V(x.Else)),
            _ => throw new NotSupportedException(v.GetType().Name)
        };
    }
    private static object Definition<TA, VA>(ValueDefinition<TA, VA> d, Func<TA, object> ta, Func<VA, object> va) => new
    {
        inputTypes = d.Inputs.Select(i => Arr(Name(i.Name), va(i.Attributes), Type(i.Type, ta))).ToArray(),
        outputType = Type(d.Output, ta),
        body = Value(d.Body, ta, va)
    };
    private static object Module<TA, VA>(ModuleDefinition<TA, VA> m, Func<TA, object> ta, Func<VA, object> va) => new
    {
        types = Entries(m.Types, Name, x => Access(x, d => Doc(d, a => Alias(a, ta)))),
        values = Entries(m.Values, Name, x => Access(x, d => Doc(d, v => Definition(v, ta, va)))),
        doc = m.Doc
    };
    private static object Specification<TA>(PackageSpecification<TA> p, Func<TA, object> ta) => new
    {
        modules = Entries(p.Modules, Path, m => new
        {
            types = Entries(m.Types, Name, d => Doc(d, a => Alias(a, ta, true))),
            values = Entries(m.Values, Name, d => Doc(d, v => new { inputs = Entries(v.Inputs, Name, t => Type(t, ta)), output = Type(v.Output, ta) })),
            doc = m.Doc
        })
    };
    public static string Encode(Library<Unit, IrType<Unit>> lib) => JsonSerializer.Serialize(new
    {
        formatVersion = 3,
        distribution = Arr("Library", Path(lib.Package), Entries(lib.Dependencies, Path, p => Specification(p, Unit)), new
        {
            modules = Entries(lib.Definition.Modules, Path, m => Access(m, v => Module(v, Unit, t => Type(t, Unit))))
        })
    });

    public static void WriteAtomic(string path, Library<Unit, IrType<Unit>> lib)
    {
        var bytes = new UTF8Encoding(false).GetBytes(Encode(lib));
        var full = System.IO.Path.GetFullPath(path);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
        var tmp = full + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllBytes(tmp, bytes); File.Move(tmp, full, true); }
        finally { if (File.Exists(tmp)) File.Delete(tmp); }
    }
}
