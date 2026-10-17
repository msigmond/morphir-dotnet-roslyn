using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Morphir.Frontend.CSharp;
using Morphir.IR.Classic;
using Morphir.IR.Json;
using V = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Tests;

internal static class Support
{
    public static string Root
    {
        get
        {
            var d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d is not null && !File.Exists(System.IO.Path.Combine(d.FullName, "Morphir.CSharp.slnx"))) d = d.Parent;
            return d?.FullName ?? throw new InvalidOperationException("Repository root not found.");
        }
    }
    public static string CurrentSampleReference
    {
        get
        {
            using var document = System.Text.Json.JsonDocument.Parse(Read("samples/CurrentSupportedExample/reference.json"));
            return document.RootElement.GetProperty("expected").GetString()!;
        }
    }
    public static string Read(string file) => File.ReadAllText(System.IO.Path.Combine(Root, file));
    public static CSharpCompilation Compile(string code, bool check = false, Platform platform = Platform.X64) => SourceCompilation.Create([("Case.cs", code)], check, platform);
    public static FrontendResult Convert(string code, string package = "Example", string[]? selectors = null, bool check = false, Platform platform = Platform.X64) => Morphir.Frontend.CSharp.Frontend.Convert(Compile(code, check, platform), new(package, selectors ?? ["Example.D1"]));
    public static Library<Unit, IrType<Unit>> Success(FrontendResult r)
    {
        Assert.True(r.Success, string.Join("\n", r.Diagnostics)); return r.Distribution!;
    }
    public static string Json(string code, string package = "Example", params string[] selectors) => Codec.Encode(Success(Convert(code, package, selectors.Length == 0 ? null : selectors)));
    public static void Parity(string expectedFile, string code, string package = "Example", params string[] selectors)
    {
        var d = StructuralComparison.Compare(Read(expectedFile), Json(code, package, selectors)); Assert.True(d is null, d?.ToString());
    }
    public static Assembly Emit(string code) => Emit(Compile(code));
    public static Assembly Emit(CSharpCompilation compilation)
    {
        using var bytes = new MemoryStream(); var emit = compilation.Emit(bytes);
        Assert.True(emit.Success, string.Join("\n", emit.Diagnostics));
        bytes.Position = 0;
        return new AssemblyLoadContext(Guid.NewGuid().ToString(), true).LoadFromStream(bytes);
    }
    public sealed record MaybeData(bool Present, object? Value);
    public static MaybeData Maybe(object? value) => new(value is not null, value);
    private static object Match(MatchValue<Unit, IrType<Unit>> match, IReadOnlyDictionary<string, object> variables, Library<Unit, IrType<Unit>>? library)
    {
        var subject = (MaybeData)Evaluate(match.Subject, variables, library);
        foreach (var branch in match.Cases)
        {
            var constructor = (ConstructorPattern<IrType<Unit>>)branch.Pattern;
            if ((constructor.Name.Local.Key == "just") != subject.Present) continue;
            var bound = new Dictionary<string, object>(variables);
            if (subject.Present && constructor.Arguments.Single() is AsPattern<IrType<Unit>> variable) bound[variable.Name.Key] = subject.Value!;
            return Evaluate(branch.Body, bound, library);
        }
        throw new InvalidOperationException("Non-exhaustive Maybe match");
    }
    // Small independent interpreter used only for C#/IR behavioral cross-checks.
    public static object Evaluate(V v, IReadOnlyDictionary<string, object> vars, Library<Unit, IrType<Unit>>? library = null) => v switch
    {
        ConstructorValue<Unit, IrType<Unit>> constructor => constructor.Name.Local.Key == "just" ? (Func<object, object>)(value => Maybe(value)) : Maybe(null),
        MatchValue<Unit, IrType<Unit>> match => Match(match, vars, library),
        LiteralValue<Unit, IrType<Unit>> { Literal: IntLiteral i } => checked((int)i.Value),
        LiteralValue<Unit, IrType<Unit>> { Literal: BoolLiteral b } => b.Value,
        LiteralValue<Unit, IrType<Unit>> { Literal: StringLiteral s } => s.Value,
        VariableValue<Unit, IrType<Unit>> x => vars[x.Name.Key],
        FieldValue<Unit, IrType<Unit>> x => ((IReadOnlyDictionary<string, object>)Evaluate(x.Subject, vars, library))[x.Field.Key],
        RecordValue<Unit, IrType<Unit>> x => x.Fields.ToDictionary(e => e.Key.Key, e => Evaluate(e.Value, vars, library)),
        LetValue<Unit, IrType<Unit>> x => Evaluate(x.Body, new Dictionary<string, object>(vars) { [x.Name.Key] = Evaluate(x.Definition.Body, vars, library) }, library),
        IfValue<Unit, IrType<Unit>> x => Evaluate((bool)Evaluate(x.Condition, vars, library) ? x.Then : x.Else, vars, library),
        ReferenceValue<Unit, IrType<Unit>> x => Resolve(x.Name, library),
        ApplyValue<Unit, IrType<Unit>> x => ((Func<object, object>)Evaluate(x.Function, vars, library))(Evaluate(x.Argument, vars, library)),
        _ => throw new NotSupportedException(v.ToString())
    };
    private static object Resolve(FQName name, Library<Unit, IrType<Unit>>? library)
    {
        if (name.Package.Key == Types.Sdk.Key)
            return name.Module.Key == Morphir.IR.Classic.Path.From("String").Key && name.Local.Key == "length" ? (Func<object, object>)(value => ((string)value).Length) : name.Local.Key == "not" ? (Func<object, object>)(value => !(bool)value)
                : (Func<object, object>)(left => (Func<object, object>)(right => Binary(name.Local.Key, left, right)));
        var definition = Lookup.Value(library!, name) ?? throw new InvalidOperationException($"Missing definition {name}");
        object Bind(int index, Dictionary<string, object> values) => index == definition.Inputs.Count
            ? Evaluate(definition.Body, values, library)
            : (Func<object, object>)(argument => Bind(index + 1, new(values) { [definition.Inputs[index].Name.Key] = argument }));
        return Bind(0, []);
    }
    private static object Binary(string op, object a, object b) => op switch
    {
        "add" => checked((int)a + (int)b),
        "greater\0than\0or\0equal" => (int)a >= (int)b,
        "greater\0than" => (int)a > (int)b,
        "less\0than\0or\0equal" => (int)a <= (int)b,
        "less\0than" => (int)a < (int)b,
        "equal" => Equals(a, b),
        "not\0equal" => !Equals(a, b),
        "and" => (bool)a & (bool)b,
        "or" => (bool)a | (bool)b,
        "xor" => (bool)a ^ (bool)b,
        _ => throw new NotSupportedException(op)
    };
    public static object Call(Library<Unit, IrType<Unit>> lib, string module, string method, params object[] args)
    {
        var m = lib.Definition.Modules.Single(e => e.Key.Key == Morphir.IR.Classic.Path.From(module).Key).Value.Value;
        var def = m.Values.Single(e => e.Key.Key == Name.From(method).Key).Value.Value.Value;
        return Evaluate(def.Body, def.Inputs.Select((input, i) => (input.Name.Key, Value: args[i])).ToDictionary(x => x.Key, x => x.Value), lib);
    }
}
