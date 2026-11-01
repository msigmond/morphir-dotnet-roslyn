using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Morphir.IR.Classic;
using V = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;
using IrPath = Morphir.IR.Classic.Path;

namespace Morphir.Frontend.CSharp;

internal sealed partial class SourceTranslation
{
    private string? CollectionModule(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol array) return array.Rank == 1 && array.IsSZArray ? "List" : null;
        if (type is not INamedTypeSymbol named || SourceSymbol(named)) return null;
        return named.ContainingNamespace.ToDisplayString() switch
        {
            "System.Collections.Generic" => named.MetadataName switch
            {
                "List`1" or "IEnumerable`1" or "ICollection`1" or "IList`1" or "IReadOnlyCollection`1" or "IReadOnlyList`1" => "List",
                "Dictionary`2" or "IDictionary`2" or "IReadOnlyDictionary`2" => "Dict",
                "HashSet`1" or "ISet`1" or "IReadOnlySet`1" => "Set",
                _ => null
            },
            "System.Collections.ObjectModel" => named.MetadataName switch { "ReadOnlyCollection`1" => "List", "ReadOnlyDictionary`2" => "Dict", _ => null },
            "System.Collections.Immutable" => named.MetadataName switch
            {
                "ImmutableArray`1" or "ImmutableList`1" => "List", "ImmutableDictionary`2" => "Dict", "ImmutableHashSet`1" => "Set", _ => null
            },
            _ => null
        };
    }
    private static string? CollectionModule(IrType<Unit> type) => type is ReferenceType<Unit> reference && reference.Name.Package.Key == Types.Sdk.Key
        ? new[] { "List", "Dict", "Set" }.FirstOrDefault(module => reference.Name.Module.Key == IrPath.From(module).Key && reference.Name.Local.Key == Name.From(module).Key) : null;
    private static IrType<Unit> CollectionType(string module, params IrType<Unit>[] arguments) => new ReferenceType<Unit>(default, new(Types.Sdk, IrPath.From(module), Name.From(module)), arguments);
    private static V EmptyCollection(IrType<Unit> type) => CollectionModule(type) == "List" ? new ListValue<Unit, IrType<Unit>>(type, []) : Sdk(CollectionModule(type)!, "empty", type);

    private V CollectionExpression(CollectionExpressionSyntax syntax, IrType<Unit> type) => Source(type, "CollectionLiteral", syntax.Elements.Select(element => element switch
    {
        ExpressionElementSyntax item => Source(type, "CollectionElement", [Expression(item.Expression)]),
        SpreadElementSyntax spread => Source(type, "CollectionSpread", [Expression(spread.Expression)]),
        _ => Fail<V>("MCS3101", "This collection element has no approved Elm mapping.", element.GetLocation())
    }).ToArray());

    private V CollectionCreation(SyntaxNode syntax, IMethodSymbol method, IrType<Unit> type, InitializerExpressionSyntax? initializer)
    {
        if (method.Parameters.Length > 1 || method.Parameters.Length == 1 && CollectionModule(method.Parameters[0].Type) is null)
            return Fail<V>("MCS3101", "Collection capacity and comparer constructors have no approved Elm mapping.", syntax.GetLocation());
        var arguments = CallArguments(syntax, method).Select(a => ((CSharpOperation<Unit, IrType<Unit>>)a).Children.Single()).ToList();
        if (initializer is not null)
        {
            if (arguments.Count != 0) return Fail<V>("MCS3101", "A source collection plus an initializer needs another approved mapping.", syntax.GetLocation());
            var elements = initializer.Expressions.Select(item =>
            {
                if (CollectionModule(type) == "Dict")
                {
                    if (item is InitializerExpressionSyntax pair && pair.Expressions.Count == 2)
                        return (V)new TupleValue<Unit, IrType<Unit>>(new TupleType<Unit>(default, ((ReferenceType<Unit>)type).Arguments), pair.Expressions.Select(Expression).ToArray());
                    if (item is AssignmentExpressionSyntax { Left: ImplicitElementAccessSyntax index } assignment && index.ArgumentList.Arguments.Count == 1)
                        return new TupleValue<Unit, IrType<Unit>>(new TupleType<Unit>(default, ((ReferenceType<Unit>)type).Arguments), [Expression(index.ArgumentList.Arguments[0].Expression), Expression(assignment.Right)]);
                    return Fail<V>("MCS3101", "A dictionary initializer requires an explicit key and value.", item.GetLocation());
                }
                return Expression(item);
            }).Select(item => Source(type, "CollectionElement", [item])).ToArray();
            return Source(type, "CollectionLiteral", elements);
        }
        return arguments.Count == 0 ? Source(type, "CollectionLiteral", []) : Source(type, "CollectionCopy", arguments);
    }

    private V? CollectionFactory(InvocationExpressionSyntax syntax, IMethodSymbol method, IrType<Unit> type)
    {
        if (SourceSymbol(method) || external.ContainsKey(Id(method)) || CollectionModule(type) is null || !method.IsStatic) return null;
        if (method.ContainingType.ToDisplayString() == "System.Array" && method.Name == "Empty" && method.Parameters.Length == 0)
            return Source(type, "CollectionLiteral", []);
        if (method.ContainingNamespace.ToDisplayString() != "System.Collections.Immutable" || method.Name is not ("Create" or "CreateRange")) return null;
        var element = ((ReferenceType<Unit>)type).Arguments[0];
        // Expanded factory params are written elements; the compiler's span pack stays internal.
        if (method.Name == "Create" && method.Parameters.Length == 1 && method.Parameters[0].IsParams && method.Parameters[0].Type.IsRefLikeType)
        {
            if (syntax.ArgumentList.Arguments.Any(a => a.NameColon is not null || !a.RefKindKeyword.IsKind(SyntaxKind.None)))
                return Fail<V>("MCS3101", "This factory argument needs a positional element mapping.", syntax.GetLocation());
            var values = syntax.ArgumentList.Arguments.Select(a => Expression(a.Expression)).ToArray();
            if (values.Length == 1 && CollectionModule(values[0].Attributes) == "List" && Shape(((ReferenceType<Unit>)values[0].Attributes).Arguments[0]) == Shape(element))
                return Source(type, "CollectionCopy", values);
            return Source(type, "CollectionLiteral", values.Select(v => Source(type, "CollectionElement", [v])).ToArray());
        }
        var scalarElements = method.Name == "Create" && CollectionModule(type) != "Dict" && method.Parameters.All(p => Shape(Type(p.Type, location: syntax.GetLocation())) == Shape(element));
        if (!scalarElements && method.Parameters.Any(p => CollectionModule(p.Type) is null && !p.IsParams))
            return Fail<V>("MCS3101", "This immutable factory overload has no approved Elm mapping.", syntax.GetLocation());
        var arguments = CallArguments(syntax, method).Select(a => ((CSharpOperation<Unit, IrType<Unit>>)a).Children.Single()).ToArray();
        if (scalarElements) return Source(type, "CollectionLiteral", arguments.Select(a => Source(type, "CollectionElement", [a])).ToArray());
        return arguments.Length switch
        {
            0 => Source(type, "CollectionLiteral", []),
            1 => Source(type, "CollectionCopy", arguments),
            _ => Fail<V>("MCS3101", "This immutable factory needs an approved sequence overload.", syntax.GetLocation())
        };
    }
    private V? CollectionInvocation(InvocationExpressionSyntax syntax, IMethodSymbol method, IrType<Unit> output)
    {
        var module = CollectionModule(method.ContainingType);
        if (module is null || method.IsStatic || syntax.Expression is not (MemberAccessExpressionSyntax or MemberBindingExpressionSyntax)) return null;
        var name = (module, method.Name, method.Parameters.Length) switch
        {
            ("List", "Contains", 1) or ("Set", "Contains", 1) or ("Dict", "ContainsKey", 1) => "member",
            _ => null
        };
        if (name is null) return Fail<V>("MCS3101", "This collection member has no approved read mapping; mutation remains rejected.", syntax.GetLocation());
        var subject = syntax.Expression is MemberAccessExpressionSyntax member ? Expression(member.Expression) : Source(Type(method.ContainingType), "ConditionalReceiver", []);
        var argument = ((CSharpOperation<Unit, IrType<Unit>>)CallArguments(syntax, method).Single()).Children.Single();
        return Source(output, "CollectionRead", [subject, argument], ("member", name));
    }
    private V ClassicCollectionRead(CSharpOperation<Unit, IrType<Unit>> source)
    {
        var subject = ClassicValue(source.Children[0]);
        var module = CollectionModule(subject.Attributes);
        if (module is null) return ClassicFailure("This collection receiver needs an approved nullable mapping.");
        var element = ((ReferenceType<Unit>)subject.Attributes).Arguments[0];
        return Sdk(module, Property(source, "member"), source.Attributes, ClassicExpected(source.Children[1], element), subject);
    }
    private V? CollectionMember(V subject, string name, IrType<Unit> output)
    {
        var module = CollectionModule(subject.Attributes);
        if (module is null) return null;
        var function = name switch
        {
            "Length" or "Count" => module == "List" ? "length" : "size",
            "IsEmpty" => "isEmpty",
            "Keys" when module == "Dict" => "keys",
            "Values" when module == "Dict" => "values",
            _ => null
        };
        return function is null ? null : Sdk(module, function, output, subject);
    }

    private V CollectionSequence(V source)
    {
        var value = ClassicValue(source);
        var module = CollectionModule(value.Attributes);
        if (module == "List") return value;
        if (module == "Set") return Sdk("Set", "toList", Types.List(((ReferenceType<Unit>)value.Attributes).Arguments[0]), value);
        return ClassicFailure("This collection has no approved element sequence mapping.");
    }
    private V ClassicCollectionCopy(CSharpOperation<Unit, IrType<Unit>> source)
    {
        var value = ClassicValue(source.Children.Single());
        if (Shape(value.Attributes) == Shape(source.Attributes)) return value;
        var sequence = CollectionSequence(value);
        return CollectionModule(source.Attributes) == "Set" ? Sdk("Set", "fromList", source.Attributes, sequence)
            : Shape(sequence.Attributes) == Shape(source.Attributes) ? sequence : ClassicFailure("This collection copy requires another approved mapping.");
    }
    private V ClassicCollectionLiteral(CSharpOperation<Unit, IrType<Unit>> source)
    {
        var module = CollectionModule(source.Attributes);
        if (module is null) return ClassicFailure("This collection type has no approved Elm mapping.");
        var arguments = ((ReferenceType<Unit>)source.Attributes).Arguments;
        var element = module == "Dict" ? new TupleType<Unit>(default, arguments) : arguments[0];
        var listType = Types.List(element);
        var segments = new List<V>(); var elements = new List<V>(); var spread = false;
        void Flush() { if (elements.Count != 0) { segments.Add(new ListValue<Unit, IrType<Unit>>(listType, elements.ToArray())); elements.Clear(); } }
        foreach (var child in source.Children.Cast<CSharpOperation<Unit, IrType<Unit>>>())
        {
            if (child.Kind == "CollectionSpread")
            {
                spread = true; Flush(); var sequence = CollectionSequence(child.Children.Single());
                if (Shape(sequence.Attributes) != Shape(listType)) return ClassicFailure("A spread needs the declared element type without another conversion.");
                segments.Add(sequence);
            }
            else elements.Add(ClassicExpected(child.Children.Single(), element));
        }
        V list;
        if (spread) { Flush(); list = Sdk("List", "concat", listType, new ListValue<Unit, IrType<Unit>>(Types.List(listType), segments)); }
        else list = new ListValue<Unit, IrType<Unit>>(listType, elements.ToArray());
        if (source.Children.Count == 0) return EmptyCollection(source.Attributes);
        return module == "List" ? list : Sdk(module, "fromList", source.Attributes, list);
    }
    private V ClassicCollectionIndex(CSharpOperation<Unit, IrType<Unit>> source, IrType<Unit> expected)
    {
        if (source.Children.Count != 2 || MaybeElement(expected) is not { } result)
            return ClassicFailure("Collection indexing requires one index and a declared nullable result.");
        var subject = ClassicValue(source.Children[0]);
        if (subject.Attributes is not ReferenceType<Unit> container) return ClassicFailure("The indexed value has no collection type.");
        var index = ((CSharpOperation<Unit, IrType<Unit>>)source.Children[1]).Children.Single();
        var module = CollectionModule(container);
        if (module == "List" && Shape(result) == Shape(container.Arguments[0]))
            return Sdk("List", "head", expected, Sdk("List", "drop", container, ClassicExpected(index, Types.Int), subject));
        if (module == "Dict" && Shape(result) == Shape(container.Arguments[1]))
            return Sdk("Dict", "get", expected, ClassicExpected(index, container.Arguments[0]), subject);
        return ClassicFailure("This index result needs another declared nullable mapping.");
    }
}
