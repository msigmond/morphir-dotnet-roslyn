using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Morphir.IR.Classic;
using V = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

internal sealed partial class SourceTranslation
{
    private readonly Dictionary<string, IMethodSymbol> genericLocalDefinitions = [];
    private readonly Dictionary<string, HashSet<string>> localCallOwners = [];
    private bool HasLocalBodyContext(string symbol, HashSet<string> visited) => visited.Add(symbol) &&
        localCallOwners.TryGetValue(symbol, out var owners) && owners.Any(owner => owner == "" || HasLocalBodyContext(owner, visited));
    private readonly Dictionary<string, List<ITypeSymbol>> genericLocalArguments = [];
    private static string TypeParameterKey(ITypeParameterSymbol parameter) => Id(parameter.ContainingSymbol) + ":" + parameter.Ordinal;

    private Dictionary<string, IrType<Unit>> LocalBodyTypeArguments(CSharpOperation<Unit, IrType<Unit>> local)
    {
        var arguments = new Dictionary<string, IrType<Unit>>();
        if (!genericLocalDefinitions.TryGetValue(Property(local, "symbol"), out var method)) return arguments;
        if (!HasLocalBodyContext(Property(local, "symbol"), []))
            ClassicFailure("An unused generic local function needs another Elm body-inference mapping.");
        foreach (var parameter in method.TypeParameters)
            arguments[Name.From(parameter.Name).Key] = ResolveLocalType(parameter, []) ?? Type(parameter);
        return arguments;
    }

    private IrType<Unit>? ResolveLocalType(ITypeSymbol symbol, HashSet<string> path)
    {
        if (symbol is ITypeParameterSymbol parameter && genericLocalArguments.TryGetValue(TypeParameterKey(parameter), out var instances))
        {
            if (!path.Add(TypeParameterKey(parameter))) return null;
            IrType<Unit>? result = null;
            foreach (var instance in instances.Where(t => !SymbolEqualityComparer.Default.Equals(t, parameter)))
            {
                var type = ResolveLocalType(instance, path);
                if (type is null) continue;
                if (result is not null && Shape(result) != Shape(type))
                    ClassicFailure("The pinned Elm compiler requires one body type for a local generic function.");
                result = type;
            }
            path.Remove(TypeParameterKey(parameter));
            return result;
        }
        var substitutions = new Dictionary<string, IrType<Unit>>();
        void Visit(ITypeSymbol type)
        {
            if (type is ITypeParameterSymbol p && genericLocalArguments.ContainsKey(TypeParameterKey(p)))
            {
                if (ResolveLocalType(p, path) is { } resolved) substitutions[Name.From(p.Name).Key] = resolved;
            }
            else if (type is INamedTypeSymbol named) foreach (var argument in SourceTypeArguments(named, null)) Visit(argument.Symbol);
            else if (type is IArrayTypeSymbol array) Visit(array.ElementType);
        }
        Visit(symbol);
        return Substitute(Type(symbol), substitutions);
    }

    private V SubstituteSourceTypes(V value, IReadOnlyDictionary<string, IrType<Unit>> arguments)
    {
        if (arguments.Count == 0) return value;
        var type = Substitute(value.Attributes, arguments);
        V Child(V child) => SubstituteSourceTypes(child, arguments);
        return value switch
        {
            CSharpOperation<Unit, IrType<Unit>> source => source with { Attributes = type, Children = source.Children.Select(Child).ToArray() },
            VariableValue<Unit, IrType<Unit>> variable => variable with { Attributes = type },
            LiteralValue<Unit, IrType<Unit>> literal => literal with { Attributes = type },
            ReferenceValue<Unit, IrType<Unit>> reference => reference with { Attributes = type },
            ConstructorValue<Unit, IrType<Unit>> constructor => constructor with { Attributes = type },
            UnitValue<Unit, IrType<Unit>> unit => unit with { Attributes = type },
            ApplyValue<Unit, IrType<Unit>> apply => apply with { Attributes = type, Function = Child(apply.Function), Argument = Child(apply.Argument) },
            FieldValue<Unit, IrType<Unit>> field => field with { Attributes = type, Subject = Child(field.Subject) },
            IfValue<Unit, IrType<Unit>> branch => branch with { Attributes = type, Condition = Child(branch.Condition), Then = Child(branch.Then), Else = Child(branch.Else) },
            TupleValue<Unit, IrType<Unit>> tuple => tuple with { Attributes = type, Elements = tuple.Elements.Select(Child).ToArray() },
            ListValue<Unit, IrType<Unit>> list => list with { Attributes = type, Elements = list.Elements.Select(Child).ToArray() },
            _ => ClassicFailure("This generic local body needs another typed expression mapping.")
        };
    }

    private static string SourceOwner(INamedTypeSymbol type) =>
        (type.ContainingType is { } parent ? SourceOwner(parent) : type.ContainingNamespace.ToDisplayString()) + "." + type.Name;

    private static IEnumerable<ITypeParameterSymbol> TypeParameters(INamedTypeSymbol type) =>
        (type.ContainingType is { } parent ? TypeParameters(parent) : []).Concat(type.TypeParameters);

    private static IEnumerable<(ITypeSymbol Symbol, TypeSyntax? Syntax)> SourceTypeArguments(INamedTypeSymbol type, TypeSyntax? syntax) =>
        (type.ContainingType is { } parent ? SourceTypeArguments(parent, (syntax as QualifiedNameSyntax)?.Left) : [])
        .Concat(type.TypeArguments.Select((argument, i) => (argument, GenericArguments(syntax).ElementAtOrDefault(i))));

    private static bool GenericMember(IMethodSymbol method) => method.Arity != 0 || TypeParameters(method.ContainingType).Any();

    private static bool UsesTypeParameter(ITypeSymbol type, ITypeParameterSymbol parameter) =>
        SymbolEqualityComparer.Default.Equals(type, parameter) || type switch
        {
            IArrayTypeSymbol array => UsesTypeParameter(array.ElementType, parameter),
            INamedTypeSymbol named => SourceTypeArguments(named, null).Any(t => UsesTypeParameter(t.Symbol, parameter)),
            _ => false
        };

    private static void BindTypeArguments(IrType<Unit> declaredType, IrType<Unit> actual, Dictionary<string, IrType<Unit>> arguments)
    {
        if (declaredType is VariableType<Unit> variable)
        {
            arguments.TryAdd(variable.Name.Key, actual);
        }
        else if (declaredType is ReferenceType<Unit> reference && actual is ReferenceType<Unit> instance && reference.Name.Package.Key == instance.Name.Package.Key && reference.Name.Module.Key == instance.Name.Module.Key && reference.Name.Local.Key == instance.Name.Local.Key)
        {
            for (var i = 0; i < Math.Min(reference.Arguments.Count, instance.Arguments.Count); i++)
                BindTypeArguments(reference.Arguments[i], instance.Arguments[i], arguments);
        }
        else if (declaredType is TupleType<Unit> tuple && actual is TupleType<Unit> elements)
        {
            for (var i = 0; i < Math.Min(tuple.Elements.Count, elements.Elements.Count); i++)
                BindTypeArguments(tuple.Elements[i], elements.Elements[i], arguments);
        }
        else if (declaredType is FunctionType<Unit> function && actual is FunctionType<Unit> target)
        {
            BindTypeArguments(function.Argument, target.Argument, arguments);
            BindTypeArguments(function.Result, target.Result, arguments);
        }
    }

    private static IrType<Unit> Substitute(IrType<Unit> type, IReadOnlyDictionary<string, IrType<Unit>> arguments) => type switch
    {
        VariableType<Unit> variable => arguments.GetValueOrDefault(variable.Name.Key, variable),
        ReferenceType<Unit> reference => reference with { Arguments = reference.Arguments.Select(t => Substitute(t, arguments)).ToArray() },
        TupleType<Unit> tuple => tuple with { Elements = tuple.Elements.Select(t => Substitute(t, arguments)).ToArray() },
        FunctionType<Unit> function => function with { Argument = Substitute(function.Argument, arguments), Result = Substitute(function.Result, arguments) },
        RecordType<Unit> record => record with { Fields = record.Fields.Select(f => new Entry<Name, IrType<Unit>>(f.Key, Substitute(f.Value, arguments))).ToArray() },
        _ => type
    };
}
