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
    public static Assembly Emit(string code)
    {
        using var bytes = new MemoryStream(); var emit = Compile(code).Emit(bytes);
        Assert.True(emit.Success, string.Join("\n", emit.Diagnostics));
        bytes.Position = 0;
        return new AssemblyLoadContext(Guid.NewGuid().ToString(), true).LoadFromStream(bytes);
    }
    // Small independent interpreter used only for C#/IR behavioral cross-checks.
    public static object Evaluate(V v, IReadOnlyDictionary<string, object> vars, Library<Unit, IrType<Unit>>? library = null) => v switch
    {
        LiteralValue<Unit, IrType<Unit>> { Literal: IntLiteral i } => checked((int)i.Value),
        LiteralValue<Unit, IrType<Unit>> { Literal: BoolLiteral b } => b.Value,
        LiteralValue<Unit, IrType<Unit>> { Literal: StringLiteral s } => s.Value,
        VariableValue<Unit, IrType<Unit>> x => vars[x.Name.Key],
        FieldValue<Unit, IrType<Unit>> x => ((IReadOnlyDictionary<string, object>)Evaluate(x.Subject, vars, library))[x.Field.Key],
        RecordValue<Unit, IrType<Unit>> x => x.Fields.ToDictionary(e => e.Key.Key, e => Evaluate(e.Value, vars, library)),
        IfValue<Unit, IrType<Unit>> x => Evaluate((bool)Evaluate(x.Condition, vars, library) ? x.Then : x.Else, vars, library),
        ReferenceValue<Unit, IrType<Unit>> x => Resolve(x.Name, library),
        ApplyValue<Unit, IrType<Unit>> x => ((Func<object, object>)Evaluate(x.Function, vars, library))(Evaluate(x.Argument, vars, library)),
        _ => throw new NotSupportedException(v.ToString())
    };
    private static object Resolve(FQName name, Library<Unit, IrType<Unit>>? library)
    {
        if (name.Package.Key == Types.Sdk.Key) return (Func<object, object>)(left => (Func<object, object>)(right => Binary(name.Local.Key, (int)left, (int)right)));
        var definition = Lookup.Value(library!, name) ?? throw new InvalidOperationException($"Missing definition {name}");
        object Bind(int index, Dictionary<string, object> values) => index == definition.Inputs.Count
            ? Evaluate(definition.Body, values, library)
            : (Func<object, object>)(argument => Bind(index + 1, new(values) { [definition.Inputs[index].Name.Key] = argument }));
        return Bind(0, []);
    }
    private static object Binary(string op, int a, int b) => op switch
    {
        "add" => checked(a + b),
        "greater\0than\0or\0equal" => a >= b,
        "greater\0than" => a > b,
        "less\0than\0or\0equal" => a <= b,
        "less\0than" => a < b,
        "equal" => a == b,
        "not\0equal" => a != b,
        _ => throw new NotSupportedException(op)
    };
    public static object Call(Library<Unit, IrType<Unit>> lib, string module, string method, params object[] args)
    {
        var m = lib.Definition.Modules.Single(e => e.Key.Key == Morphir.IR.Classic.Path.From(module).Key).Value.Value;
        var def = m.Values.Single(e => e.Key.Key == Name.From(method).Key).Value.Value.Value;
        return Evaluate(def.Body, def.Inputs.Select((input, i) => (input.Name.Key, Value: args[i])).ToDictionary(x => x.Key, x => x.Value), lib);
    }
}
