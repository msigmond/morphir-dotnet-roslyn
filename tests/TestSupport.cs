using System.Numerics;
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
    public static FrontendResult Convert(string code, string package = "Example", string[]? selectors = null, bool check = false, Platform platform = Platform.X64) => Morphir.Frontend.CSharp.Frontend.Convert(Compile(code, check, platform), new(package, selectors ?? ["Example.D1"], OutputFormat: FrontendOutputFormat.Source));
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
    public sealed record CustomData(FQName Constructor, IReadOnlyList<object> Arguments);
    public static CustomData Enum(string module, string type, object value) => new(new(Morphir.IR.Classic.Path.From("Example"), Morphir.IR.Classic.Path.From(module), Name.From(type)), [value]);
    public sealed record MaybeData(bool Present, object? Value);
    public static MaybeData Maybe(object? value) => new(value is not null, value);
    public static object Integer(BigInteger value)
    {
        var magnitude = BigInteger.Abs(value); var digits = new List<object>();
        while (magnitude != 0) { digits.Add((int)(magnitude & 65535)); magnitude >>= 16; }
        return new Dictionary<string, object> { { "negative", value.Sign < 0 }, { "digits", digits.ToArray() } };
    }
    public static BigInteger IntegerResult(object value)
    {
        var record = (IReadOnlyDictionary<string, object>)value; var digits = (IReadOnlyList<object>)record["digits"]; BigInteger result = 0;
        for (int i = digits.Count - 1; i >= 0; i--) result = (result << 16) + Whole(digits[i]);
        return (bool)record["negative"] ? -result : result;
    }
    private static bool MatchPattern(Pattern<IrType<Unit>> pattern, object subject, Dictionary<string, object> variables)
    {
        switch (pattern)
        {
            case UnitPattern<IrType<Unit>>: return subject is Unit;
            case WildcardPattern<IrType<Unit>>: return true;
            case AsPattern<IrType<Unit>> bound: if (!MatchPattern(bound.Pattern, subject, variables)) return false; variables[bound.Name.Key] = subject; return true;
            case EmptyListPattern<IrType<Unit>>: return ((IReadOnlyList<object>)subject).Count == 0;
            case HeadTailPattern<IrType<Unit>> list:
                var values = (IReadOnlyList<object>)subject;
                return values.Count > 0 && MatchPattern(list.Head, values[0], variables) && MatchPattern(list.Tail, values.Skip(1).ToArray(), variables);
            case TuplePattern<IrType<Unit>> tuple:
                return subject is IReadOnlyList<object> elements && elements.Count == tuple.Elements.Count && tuple.Elements.Zip(elements).All(pair => MatchPattern(pair.First, pair.Second, variables));
            case ConstructorPattern<IrType<Unit>> constructor when constructor.Name.Package.Key != Types.Sdk.Key:
                return subject is CustomData data && data.Constructor.Package.Key == constructor.Name.Package.Key && data.Constructor.Module.Key == constructor.Name.Module.Key && data.Constructor.Local.Key == constructor.Name.Local.Key && data.Arguments.Count == constructor.Arguments.Count && constructor.Arguments.Zip(data.Arguments).All(pair => MatchPattern(pair.First, pair.Second, variables));
            case ConstructorPattern<IrType<Unit>> constructor:
                var maybe = (MaybeData)subject;
                return (constructor.Name.Local.Key == "just") == maybe.Present && (!maybe.Present || MatchPattern(constructor.Arguments.Single(), maybe.Value!, variables));
            default: throw new NotSupportedException(pattern.ToString());
        }
    }
    private static object Match(MatchValue<Unit, IrType<Unit>> match, IReadOnlyDictionary<string, object> variables, Library<Unit, IrType<Unit>>? library)
    {
        var subject = Evaluate(match.Subject, variables, library);
        foreach (var branch in match.Cases) { var bound = new Dictionary<string, object>(variables); if (MatchPattern(branch.Pattern, subject, bound)) return Evaluate(branch.Body, bound, library); }
        throw new InvalidOperationException("Non-exhaustive match");
    }
    // Small independent interpreter used only for C#/IR behavioral cross-checks.
    public static object Evaluate(V v, IReadOnlyDictionary<string, object> vars, Library<Unit, IrType<Unit>>? library = null)
    {
        while (true)
        {
            if (v is IfValue<Unit, IrType<Unit>> decision) { v = (bool)Evaluate(decision.Condition, vars, library) ? decision.Then : decision.Else; continue; }
            if (v is LetValue<Unit, IrType<Unit>> binding) { vars = new Dictionary<string, object>(vars) { [binding.Name.Key] = Evaluate(binding.Definition.Body, vars, library) }; v = binding.Body; continue; }
            if (v is MatchValue<Unit, IrType<Unit>> match)
            {
                var subject = Evaluate(match.Subject, vars, library); bool found = false;
                foreach (var branch in match.Cases) { var bound = new Dictionary<string, object>(vars); if (!MatchPattern(branch.Pattern, subject, bound)) continue; v = branch.Body; vars = bound; found = true; break; }
                if (!found) throw new InvalidOperationException("Non-exhaustive match"); continue;
            }
            if (library is not null)
            {
                var arguments = new List<V>(); var root = v;
                while (root is ApplyValue<Unit, IrType<Unit>> apply) { arguments.Insert(0, apply.Argument); root = apply.Function; }
                if (root is ReferenceValue<Unit, IrType<Unit>> reference && reference.Name.Package.Key != Types.Sdk.Key && Lookup.Value(library, reference.Name) is { } definition && definition.Inputs.Count == arguments.Count)
                {
                    var evaluated = arguments.Select(a => Evaluate(a, vars, library)).ToArray();
                    vars = definition.Inputs.Select((input, i) => (input.Name.Key, Value: evaluated[i])).ToDictionary(pair => pair.Key, pair => pair.Value); v = definition.Body; continue;
                }
            }
            return v switch
            {
                ConstructorValue<Unit, IrType<Unit>> constructor when constructor.Name.Package.Key != Types.Sdk.Key => (Func<object, object>)(argument => new CustomData(constructor.Name, [argument])),
                TupleValue<Unit, IrType<Unit>> tuple => tuple.Elements.Select(element => Evaluate(element, vars, library)).ToArray(),
                ConstructorValue<Unit, IrType<Unit>> constructor => constructor.Name.Local.Key == "just" ? (Func<object, object>)(value => Maybe(value)) : Maybe(null),
                MatchValue<Unit, IrType<Unit>> matched => Match(matched, vars, library),
                LiteralValue<Unit, IrType<Unit>> { Literal: IntLiteral i } => Narrow(i.Value),
                LiteralValue<Unit, IrType<Unit>> { Literal: FloatLiteral f } => f.Value,
                LiteralValue<Unit, IrType<Unit>> { Literal: DecimalLiteral d } => decimal.Parse(d.Value, System.Globalization.CultureInfo.InvariantCulture),
                UnitValue<Unit, IrType<Unit>> => default(Unit),
                LambdaValue<Unit, IrType<Unit>> lambda => (Func<object, object>)(argument => { var bound = new Dictionary<string, object>(vars); if (!MatchPattern(lambda.Argument, argument, bound)) throw new InvalidOperationException("Function argument pattern did not match"); return Evaluate(lambda.Body, bound, library); }),
                ListValue<Unit, IrType<Unit>> list => list.Elements.Select(e => Evaluate(e, vars, library)).ToArray(),
                LiteralValue<Unit, IrType<Unit>> { Literal: BoolLiteral b } => b.Value,
                LiteralValue<Unit, IrType<Unit>> { Literal: StringLiteral s } => s.Value,
                VariableValue<Unit, IrType<Unit>> x => vars[x.Name.Key],
                FieldValue<Unit, IrType<Unit>> x => ((IReadOnlyDictionary<string, object>)Evaluate(x.Subject, vars, library))[x.Field.Key],
                UpdateRecordValue<Unit, IrType<Unit>> update => UpdateRecord(update, vars, library),
                RecordValue<Unit, IrType<Unit>> x => x.Fields.ToDictionary(e => e.Key.Key, e => Evaluate(e.Value, vars, library)),
                LetValue<Unit, IrType<Unit>> x => Evaluate(x.Body, new Dictionary<string, object>(vars) { [x.Name.Key] = Evaluate(x.Definition.Body, vars, library) }, library),
                IfValue<Unit, IrType<Unit>> x => Evaluate((bool)Evaluate(x.Condition, vars, library) ? x.Then : x.Else, vars, library),
                ReferenceValue<Unit, IrType<Unit>> x => Resolve(x.Name, library),
                ApplyValue<Unit, IrType<Unit>> x => ((Func<object, object>)Evaluate(x.Function, vars, library))(Evaluate(x.Argument, vars, library)),
                _ => throw new NotSupportedException(v.ToString())
            };
        }
    }
    private static readonly AsyncLocal<Func<FQName, object?>?> ExternalValues = new();
    public static object WithExternalValues(Func<FQName, object?> resolver, Func<object> run)
    {
        var previous = ExternalValues.Value; ExternalValues.Value = resolver;
        try { return run(); } finally { ExternalValues.Value = previous; }
    }
    private static object Resolve(FQName name, Library<Unit, IrType<Unit>>? library)
    {
        if (name.Package.Key == Types.Sdk.Key)
        {
            if (name.Module.Key == Morphir.IR.Classic.Path.From("Decimal").Key)
            {
                object DecimalUnary(string operation, object value) => operation switch
                {
                    "from\0int" => (decimal)Whole(value),
                    "from\0string" => decimal.TryParse((string)value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var parsed) ? Maybe(parsed) : Maybe(null),
                    "negate" => -(decimal)value,
                    "abs" => Math.Abs((decimal)value),
                    "to\0string" => ((decimal)value).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    _ => throw new NotSupportedException(operation)
                };
                if (name.Local.Key == "zero") return 0m;
                if (name.Local.Key == "one") return 1m;
                if (name.Local.Key == "minus\0one") return -1m;
                if (name.Local.Key is "from\0int" or "from\0string" or "negate" or "abs" or "to\0string") return (Func<object, object>)(v => DecimalUnary(name.Local.Key, v));
                return (Func<object, object>)(a => (Func<object, object>)(b => name.Local.Key switch { "add" => (decimal)a + (decimal)b, "sub" => (decimal)a - (decimal)b, "mul" => (decimal)a * (decimal)b, _ => throw new NotSupportedException(name.Local.Key) }));
            }
            if (name.Module.Key == Morphir.IR.Classic.Path.From("List").Key) return name.Local.Key switch
            {
                "cons" => (Func<object, object>)(head => (Func<object, object>)(tail => new[] { head }.Concat((IEnumerable<object>)tail).ToArray())),
                "length" => (Func<object, object>)(value => ((IReadOnlyList<object>)value).Count),
                "drop" => (Func<object, object>)(count => (Func<object, object>)(value => ((IReadOnlyList<object>)value).Skip((int)count).ToArray())),
                "head" => (Func<object, object>)(value => ((IReadOnlyList<object>)value) is { Count: > 0 } list ? Maybe(list[0]) : Maybe(null)),
                _ => throw new NotSupportedException("List." + name.Local.Key)
            };
            if (name.Module.Key == Morphir.IR.Classic.Path.From("String").Key && name.Local.Key == "length") return (Func<object, object>)(value => ((string)value).Length);
            Func<object, object>? unary = name.Local.Key switch
            {
                "not" => value => !(bool)value,
                "negate" => value => value is double d ? -d : Narrow(-Whole(value)),
                "abs" => value => value is double d ? Math.Abs(d) : Narrow(BigInteger.Abs(Whole(value))),
                "to\0float" => value => (double)Whole(value),
                "floor" => value => Narrow(new BigInteger(Math.Floor((double)value))),
                "ceiling" => value => Narrow(new BigInteger(Math.Ceiling((double)value))),
                "truncate" => value => Narrow(new BigInteger((double)value)),
                "is\0na\0n" => value => double.IsNaN((double)value),
                "is\0infinite" => value => double.IsInfinity((double)value),
                _ => null
            };
            return unary is not null ? unary : (Func<object, object>)(left => (Func<object, object>)(right => Binary(name.Local.Key, left, right)));
        }
        if (library is not null && name.Package.Key != library.Package.Key && ExternalValues.Value?.Invoke(name) is { } external) return external;
        var definition = Lookup.Value(library!, name) ?? throw new InvalidOperationException($"Missing definition {name}");
        object Bind(int index, Dictionary<string, object> values) => index == definition.Inputs.Count
            ? Evaluate(definition.Body, values, library)
            : (Func<object, object>)(argument => Bind(index + 1, new(values) { [definition.Inputs[index].Name.Key] = argument }));
        return Bind(0, []);
    }
    private static BigInteger Whole(object value) => value switch { BigInteger i => i, int i => i, long i => i, uint i => i, ulong i => i, _ => throw new InvalidOperationException("Not an integer " + value) };
    private static object Narrow(BigInteger value) => value >= int.MinValue && value <= int.MaxValue ? (object)(int)value : value;
    private static double Real(object value) => value is double d ? d : (double)Whole(value);
    private static object UpdateRecord(UpdateRecordValue<Unit, IrType<Unit>> update, IReadOnlyDictionary<string, object> variables, Library<Unit, IrType<Unit>>? library)
    {
        var record = new Dictionary<string, object>((IReadOnlyDictionary<string, object>)Evaluate(update.Subject, variables, library));
        foreach (var field in update.Fields) record[field.Key.Key] = Evaluate(field.Value, variables, library);
        return record;
    }
    private static bool Equal(object a, object b)
    {
        if (a is CustomData ca && b is CustomData cb) return ca.Constructor.Package.Key == cb.Constructor.Package.Key && ca.Constructor.Module.Key == cb.Constructor.Module.Key && ca.Constructor.Local.Key == cb.Constructor.Local.Key && ca.Arguments.Count == cb.Arguments.Count && ca.Arguments.Zip(cb.Arguments).All(pair => Equal(pair.First, pair.Second));
        if (a is IReadOnlyList<object> aa && b is IReadOnlyList<object> bb) return aa.Count == bb.Count && aa.Zip(bb).All(pair => Equal(pair.First, pair.Second));
        if (a is IReadOnlyDictionary<string, object> ad && b is IReadOnlyDictionary<string, object> bd) return ad.Count == bd.Count && ad.All(pair => bd.TryGetValue(pair.Key, out var other) && Equal(pair.Value, other));
        if (a is double || b is double) return Real(a) == Real(b);
        if (a is int or BigInteger && b is int or BigInteger) return Whole(a) == Whole(b);
        return Equals(a, b);
    }
    private static object Binary(string op, object a, object b)
    {
        bool real = a is double || b is double;
        return op switch
        {
            "add" => real ? Real(a) + Real(b) : Narrow(Whole(a) + Whole(b)),
            "subtract" => real ? Real(a) - Real(b) : Narrow(Whole(a) - Whole(b)),
            "multiply" => real ? Real(a) * Real(b) : Narrow(Whole(a) * Whole(b)),
            "integer\0divide" => Narrow(Whole(a) / Whole(b)),
            "divide" => Real(a) / Real(b),
            "power" => real ? Math.Pow(Real(a), Real(b)) : Narrow(BigInteger.Pow(Whole(a), (int)Whole(b))),
            "mod\0by" => Narrow((Whole(b) % Whole(a) + BigInteger.Abs(Whole(a))) % Whole(a)),
            "remainder\0by" => Narrow(Whole(b) % Whole(a)),
            "greater\0than\0or\0equal" => real ? Real(a) >= Real(b) : Whole(a) >= Whole(b),
            "greater\0than" => real ? Real(a) > Real(b) : Whole(a) > Whole(b),
            "less\0than\0or\0equal" => real ? Real(a) <= Real(b) : Whole(a) <= Whole(b),
            "less\0than" => real ? Real(a) < Real(b) : Whole(a) < Whole(b),
            "equal" => Equal(a, b),
            "not\0equal" => !Equal(a, b),
            "max" => Real(a) >= Real(b) ? a : b,
            "min" => Real(a) <= Real(b) ? a : b,
            "and" => (bool)a && (bool)b,
            "or" => (bool)a || (bool)b,
            "xor" => (bool)a ^ (bool)b,
            _ => throw new NotSupportedException(op)
        };
    }
    public static object Call(Library<Unit, IrType<Unit>> lib, string module, string method, params object[] args)
    {
        var m = lib.Definition.Modules.Single(e => e.Key.Key == Morphir.IR.Classic.Path.From(module).Key).Value.Value;
        var def = m.Values.Single(e => e.Key.Key == Name.From(method).Key).Value.Value.Value;
        return Evaluate(def.Body, def.Inputs.Select((input, i) => (input.Name.Key, Value: args[i])).ToDictionary(x => x.Key, x => x.Value), lib);
    }
}
