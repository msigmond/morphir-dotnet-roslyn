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
        TupleType<A> tuple => Arr("Tuple", attr(tuple.Attributes), tuple.Elements.Select(element => Type(element, attr)).ToArray()),
        UnitType<A> u => Arr("Unit", attr(u.Attributes)),
        VariableType<A> v => Arr("Variable", attr(v.Attributes), Name(v.Name)),
        ReferenceType<A> r => Arr("Reference", attr(r.Attributes), Fq(r.Name), r.Arguments.Select(a => Type(a, attr)).ToArray()),
        FunctionType<A> f => Arr("Function", attr(f.Attributes), Type(f.Argument, attr), Type(f.Result, attr)),
        RecordType<A> r => Arr("Record", attr(r.Attributes), r.Fields.Select(f => new { name = Name(f.Key), tpe = Type(f.Value, attr) }).ToArray()),
        _ => throw new NotSupportedException(t.GetType().Name)
    };
    private static object Constructors<A>(IReadOnlyList<Entry<Name, IReadOnlyList<Entry<Name, IrType<A>>>>> constructors, Func<A, object> attr) => Entries(constructors, Name, args => Entries(args, Name, t => Type(t, attr)));
    private static object TypeDefinition<A>(TypeDefinition<A> d, Func<A, object> attr) => d switch
    {
        AliasDefinition<A> alias => Arr("TypeAliasDefinition", alias.Parameters.Select(Name).ToArray(), Type(alias.Body, attr)),
        CustomDefinition<A> custom => Arr("CustomTypeDefinition", custom.Parameters.Select(Name).ToArray(), Access(custom.Constructors, c => Constructors(c, attr))),
        _ => throw new NotSupportedException(d.GetType().Name)
    };
    private static object TypeSpecification<A>(TypeSpecification<A> d, Func<A, object> attr) => d switch
    {
        AliasSpecification<A> alias => Arr("TypeAliasSpecification", alias.Parameters.Select(Name).ToArray(), Type(alias.Body, attr)),
        CustomSpecification<A> custom => Arr("CustomTypeSpecification", custom.Parameters.Select(Name).ToArray(), Constructors(custom.Constructors, attr)),
        OpaqueSpecification<A> opaque => Arr("OpaqueTypeSpecification", opaque.Parameters.Select(Name).ToArray()),
        _ => throw new NotSupportedException(d.GetType().Name)
    };
    private static object Literal(Literal l) => l switch
    {
        SourceNumberLiteral n => Arr("SourceNumberLiteral", n.Text),
        DecimalLiteral d => Arr("DecimalLiteral", d.Value),
        FloatLiteral f when double.IsFinite(f.Value) => Arr("FloatLiteral", f.Value),
        BoolLiteral b => Arr("BoolLiteral", b.Value),
        IntLiteral i => Arr("WholeNumberLiteral", i.Value),
        StringLiteral s => Arr("StringLiteral", s.Value),
        CharLiteral c => Arr("CharLiteral", c.Value),
        _ => throw new NotSupportedException(l.GetType().Name)
    };
    private static object Pattern<VA>(Pattern<VA> pattern, Func<VA, object> attr) => pattern switch
    {
        TuplePattern<VA> tuple => Arr("TuplePattern", attr(tuple.Attributes), tuple.Elements.Select(element => Pattern(element, attr)).ToArray()),
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
            SourceValue<TA, VA> x => Arr("Source", a, x.Kind, Entries(x.Properties, s => s, s => s), x.Children.Select(V).ToArray()),
            TupleValue<TA, VA> tuple => Arr("Tuple", a, tuple.Elements.Select(V).ToArray()),
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
            UpdateRecordValue<TA, VA> x => Arr("UpdateRecord", a, V(x.Subject), Entries(x.Fields, Name, V)),
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
        types = Entries(m.Types, Name, x => Access(x, d => Doc(d, a => TypeDefinition(a, ta)))),
        values = Entries(m.Values, Name, x => Access(x, d => Doc(d, v => Definition(v, ta, va)))),
        doc = m.Doc
    };
    private static object Specification<TA>(PackageSpecification<TA> p, Func<TA, object> ta) => new
    {
        modules = Entries(p.Modules, Path, m => new
        {
            types = Entries(m.Types, Name, d => Doc(d, a => TypeSpecification(a, ta))),
            values = Entries(m.Values, Name, d => Doc(d, v => new { inputs = Entries(v.Inputs, Name, t => Type(t, ta)), output = Type(v.Output, ta) })),
            doc = m.Doc
        })
    };
    private static object Distribution(Library<Unit, IrType<Unit>> lib) =>
        Arr("Library", Path(lib.Package), Entries(lib.Dependencies, Path, p => Specification(p, Unit)), new
        {
            modules = Entries(lib.Definition.Modules, Path, m => Access(m, v => Module(v, Unit, t => Type(t, Unit))))
        });
    private static object Syntax(SourceSyntax syntax) => new
    {
        kind = syntax.Kind, token = syntax.Token, symbol = syntax.Symbol, type = syntax.Type,
        location = new { file = syntax.Location.File, start = syntax.Location.Start, length = syntax.Location.Length,
            line = syntax.Location.Line, column = syntax.Location.Column },
        children = syntax.Children.Select(Syntax).ToArray()
    };
    public static string Encode(Library<Unit, IrType<Unit>> lib)
    {
        if (lib.Source is null && lib.Definition.Modules.SelectMany(m => m.Value.Value.Values).Any(v => Extended(v.Value.Value.Value.Body)))
            throw new NotSupportedException("Source operations require the explicit Morphir.CSharp.Source envelope and source metadata.");
        return lib.Source is { } source ? JsonSerializer.Serialize(new
        {
            format = "Morphir.CSharp.Source", formatVersion = 1, classicModelVersion = 3,
            distribution = Distribution(lib), source = new { declarations = source.Declarations.Select(Syntax).ToArray() }
        }, new JsonSerializerOptions { MaxDepth = int.MaxValue })
        : JsonSerializer.Serialize(new
    {
        formatVersion = 3,
        distribution = Distribution(lib)
    });
    }
    private static bool Extended(Value<Unit, IrType<Unit>> value) => value switch
    {
        SourceValue<Unit, IrType<Unit>> => true,
        LiteralValue<Unit, IrType<Unit>> { Literal: SourceNumberLiteral } => true,
        ApplyValue<Unit, IrType<Unit>> a => Extended(a.Function) || Extended(a.Argument),
        FieldValue<Unit, IrType<Unit>> f => Extended(f.Subject),
        IfValue<Unit, IrType<Unit>> i => Extended(i.Condition) || Extended(i.Then) || Extended(i.Else),
        LetValue<Unit, IrType<Unit>> l => Extended(l.Definition.Body) || Extended(l.Body),
        MatchValue<Unit, IrType<Unit>> m => Extended(m.Subject) || m.Cases.Any(c => Extended(c.Body)),
        LambdaValue<Unit, IrType<Unit>> l => Extended(l.Body),
        RecordValue<Unit, IrType<Unit>> r => r.Fields.Any(f => Extended(f.Value)),
        UpdateRecordValue<Unit, IrType<Unit>> r => Extended(r.Subject) || r.Fields.Any(f => Extended(f.Value)),
        TupleValue<Unit, IrType<Unit>> t => t.Elements.Any(Extended),
        ListValue<Unit, IrType<Unit>> l => l.Elements.Any(Extended),
        _ => false
    };

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
