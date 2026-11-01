using System.Text.RegularExpressions;

namespace Morphir.IR.Classic;

public readonly record struct Unit;
public sealed record Name(IReadOnlyList<string> Words)
{
    // Matches Morphir.IR.Name.fromString (acronyms are individual capitals).
    public static Name From(string text) => new(Regex.Matches(text, "[a-zA-Z][a-z]*|[0-9]+")
        .Select(m => m.Value.ToLowerInvariant()).ToArray());
    public string Key => string.Join("\0", Words);
}
public sealed record Path(IReadOnlyList<Name> Segments)
{
    public static Path From(string text) => new(text.Split('.', StringSplitOptions.RemoveEmptyEntries).Select(Name.From).ToArray());
    public string Key => string.Join("\u0001", Segments.Select(n => n.Key));
}
public sealed record FQName(Path Package, Path Module, Name Local);
public enum Access { Public, Private }
public sealed record AccessControlled<T>(Access Access, T Value);
public sealed record Documented<T>(string Doc, T Value);
public sealed record Entry<K, V>(K Key, V Value);

public abstract record IrType<A>(A Attributes);
public sealed record TupleType<A>(A Attributes, IReadOnlyList<IrType<A>> Elements) : IrType<A>(Attributes);
public sealed record UnitType<A>(A Attributes) : IrType<A>(Attributes);
public sealed record ReferenceType<A>(A Attributes, FQName Name, IReadOnlyList<IrType<A>> Arguments) : IrType<A>(Attributes);
public sealed record FunctionType<A>(A Attributes, IrType<A> Argument, IrType<A> Result) : IrType<A>(Attributes);
public sealed record RecordType<A>(A Attributes, IReadOnlyList<Entry<Name, IrType<A>>> Fields) : IrType<A>(Attributes);
public sealed record VariableType<A>(A Attributes, Name Name) : IrType<A>(Attributes);
public abstract record TypeDefinition<A>(IReadOnlyList<Name> Parameters);
public sealed record AliasDefinition<A>(IReadOnlyList<Name> Parameters, IrType<A> Body) : TypeDefinition<A>(Parameters);
public sealed record CustomDefinition<A>(IReadOnlyList<Name> Parameters, AccessControlled<IReadOnlyList<Entry<Name, IReadOnlyList<Entry<Name, IrType<A>>>>>> Constructors) : TypeDefinition<A>(Parameters);
public abstract record TypeSpecification<A>(IReadOnlyList<Name> Parameters);
public sealed record AliasSpecification<A>(IReadOnlyList<Name> Parameters, IrType<A> Body) : TypeSpecification<A>(Parameters);
public sealed record CustomSpecification<A>(IReadOnlyList<Name> Parameters, IReadOnlyList<Entry<Name, IReadOnlyList<Entry<Name, IrType<A>>>>> Constructors) : TypeSpecification<A>(Parameters);
public sealed record OpaqueSpecification<A>(IReadOnlyList<Name> Parameters) : TypeSpecification<A>(Parameters);

public abstract record Literal;
public sealed record BoolLiteral(bool Value) : Literal;
public sealed record IntLiteral(long Value) : Literal;
public sealed record StringLiteral(string Value) : Literal;
public sealed record CharLiteral(string Value) : Literal;
public sealed record FloatLiteral(double Value) : Literal;
public sealed record DecimalLiteral(string Value) : Literal;
public abstract record Value<TA, VA>(VA Attributes);
public sealed record TupleValue<TA, VA>(VA Attributes, IReadOnlyList<Value<TA, VA>> Elements) : Value<TA, VA>(Attributes);
public sealed record UnitValue<TA, VA>(VA Attributes) : Value<TA, VA>(Attributes);
public sealed record LambdaValue<TA, VA>(VA Attributes, Pattern<VA> Argument, Value<TA, VA> Body) : Value<TA, VA>(Attributes);
public sealed record LiteralValue<TA, VA>(VA Attributes, Literal Literal) : Value<TA, VA>(Attributes);
public sealed record VariableValue<TA, VA>(VA Attributes, Name Name) : Value<TA, VA>(Attributes);
public sealed record ReferenceValue<TA, VA>(VA Attributes, FQName Name) : Value<TA, VA>(Attributes);
public sealed record ApplyValue<TA, VA>(VA Attributes, Value<TA, VA> Function, Value<TA, VA> Argument) : Value<TA, VA>(Attributes);
public sealed record FieldValue<TA, VA>(VA Attributes, Value<TA, VA> Subject, Name Field) : Value<TA, VA>(Attributes);
public sealed record UpdateRecordValue<TA, VA>(VA Attributes, Value<TA, VA> Subject, IReadOnlyList<Entry<Name, Value<TA, VA>>> Fields) : Value<TA, VA>(Attributes);
public sealed record RecordValue<TA, VA>(VA Attributes, IReadOnlyList<Entry<Name, Value<TA, VA>>> Fields) : Value<TA, VA>(Attributes);
public sealed record IfValue<TA, VA>(VA Attributes, Value<TA, VA> Condition, Value<TA, VA> Then, Value<TA, VA> Else) : Value<TA, VA>(Attributes);
public sealed record LetValue<TA, VA>(VA Attributes, Name Name, ValueDefinition<TA, VA> Definition, Value<TA, VA> Body) : Value<TA, VA>(Attributes);
public sealed record LetRecursionValue<TA, VA>(VA Attributes, IReadOnlyList<Entry<Name, ValueDefinition<TA, VA>>> Definitions, Value<TA, VA> Body) : Value<TA, VA>(Attributes);
public sealed record DestructureValue<TA, VA>(VA Attributes, Pattern<VA> Pattern, Value<TA, VA> Subject, Value<TA, VA> Body) : Value<TA, VA>(Attributes);
public sealed record ConstructorValue<TA, VA>(VA Attributes, FQName Name) : Value<TA, VA>(Attributes);
public abstract record Pattern<VA>(VA Attributes);
public sealed record LiteralPattern<VA>(VA Attributes, Literal Literal) : Pattern<VA>(Attributes);
public sealed record TuplePattern<VA>(VA Attributes, IReadOnlyList<Pattern<VA>> Elements) : Pattern<VA>(Attributes);
public sealed record UnitPattern<VA>(VA Attributes) : Pattern<VA>(Attributes);
public sealed record WildcardPattern<VA>(VA Attributes) : Pattern<VA>(Attributes);
public sealed record AsPattern<VA>(VA Attributes, Pattern<VA> Pattern, Name Name) : Pattern<VA>(Attributes);
public sealed record ConstructorPattern<VA>(VA Attributes, FQName Name, IReadOnlyList<Pattern<VA>> Arguments) : Pattern<VA>(Attributes);
public sealed record MatchCase<TA, VA>(Pattern<VA> Pattern, Value<TA, VA> Body);
public sealed record MatchValue<TA, VA>(VA Attributes, Value<TA, VA> Subject, IReadOnlyList<MatchCase<TA, VA>> Cases) : Value<TA, VA>(Attributes);
public sealed record EmptyListPattern<VA>(VA Attributes) : Pattern<VA>(Attributes);
public sealed record HeadTailPattern<VA>(VA Attributes, Pattern<VA> Head, Pattern<VA> Tail) : Pattern<VA>(Attributes);
public sealed record ListValue<TA, VA>(VA Attributes, IReadOnlyList<Value<TA, VA>> Elements) : Value<TA, VA>(Attributes);
public sealed record Input<TA, VA>(Name Name, VA Attributes, IrType<TA> Type);
public sealed record ValueDefinition<TA, VA>(IReadOnlyList<Input<TA, VA>> Inputs, IrType<TA> Output, Value<TA, VA> Body);
public sealed record ValueSpecification<TA>(IReadOnlyList<Entry<Name, IrType<TA>>> Inputs, IrType<TA> Output);
public sealed record ModuleDefinition<TA, VA>(
    IReadOnlyList<Entry<Name, AccessControlled<Documented<TypeDefinition<TA>>>>> Types,
    IReadOnlyList<Entry<Name, AccessControlled<Documented<ValueDefinition<TA, VA>>>>> Values,
    string? Doc = null);
public sealed record ModuleSpecification<TA>(
    IReadOnlyList<Entry<Name, Documented<TypeSpecification<TA>>>> Types,
    IReadOnlyList<Entry<Name, Documented<ValueSpecification<TA>>>> Values,
    string? Doc = null);
public sealed record PackageSpecification<TA>(IReadOnlyList<Entry<Path, ModuleSpecification<TA>>> Modules);
public sealed record PackageDefinition<TA, VA>(IReadOnlyList<Entry<Path, AccessControlled<ModuleDefinition<TA, VA>>>> Modules);
public sealed record Library<TA, VA>(Path Package, IReadOnlyList<Entry<Path, PackageSpecification<TA>>> Dependencies, PackageDefinition<TA, VA> Definition);

public static class Types
{
    public static readonly Path Sdk = Path.From("Morphir.SDK");
    public static IrType<Unit> Reference(string module, string local) => new ReferenceType<Unit>(default, new(Sdk, Path.From(module), Name.From(local)), []);
    public static IrType<Unit> Unit => new UnitType<Unit>(default);
    public static IrType<Unit> Int => Reference("Basics", "Int");
    public static IrType<Unit> Decimal => Reference("Decimal", "Decimal");
    public static IrType<Unit> Float => Reference("Basics", "Float");
    public static IrType<Unit> List(IrType<Unit> element) => new ReferenceType<Unit>(default, new(Sdk, Path.From("List"), Name.From("List")), [element]);
    public static IrType<Unit> Bool => Reference("Basics", "Bool");
    public static IrType<Unit> String => Reference("String", "String");
    public static IrType<Unit> Maybe(IrType<Unit> element) => new ReferenceType<Unit>(default, new(Sdk, Path.From("Maybe"), Name.From("Maybe")), [element]);
    public static IrType<Unit> Function(IrType<Unit> arg, IrType<Unit> result) => new FunctionType<Unit>(default, arg, result);
}

public static class IdentityOrder
{
    // Elm compares nested lists lexicographically: segment boundaries sort before
    // additional words in that same segment. A flattened path key cannot do this.
    public static IComparer<Path> Paths { get; } = Comparer<Path>.Create((left, right) =>
    {
        if (ReferenceEquals(left, right)) return 0;
        if (left is null) return -1;
        if (right is null) return 1;
        for (int i = 0; i < Math.Min(left.Segments.Count, right.Segments.Count); i++)
        {
            int order = StringComparer.Ordinal.Compare(left.Segments[i].Key, right.Segments[i].Key);
            if (order != 0) return order;
        }
        return left.Segments.Count.CompareTo(right.Segments.Count);
    });
}
