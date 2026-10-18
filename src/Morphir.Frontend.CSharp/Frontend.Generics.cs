using Microsoft.CodeAnalysis;
using Morphir.IR.Classic;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        private Dictionary<ITypeParameterSymbol, ITypeSymbol> activeGenericTypes = new(SymbolEqualityComparer.Default);
        private ITypeSymbol ResolveGenericParameter(ITypeSymbol type) => GenericArgument(type, out var argument, out var scope) ? WithGenericScope(scope, () => ResolveGenericParameter(argument)) : type;
        private static Dictionary<ITypeParameterSymbol, ITypeSymbol> TypeSubstitutions(INamedTypeSymbol type)
        {
            var result = new Dictionary<ITypeParameterSymbol, ITypeSymbol>(SymbolEqualityComparer.Default);
            foreach (var pair in SourceTypeParameters(type.OriginalDefinition).Zip(SourceTypeArguments(type))) result[pair.First] = pair.Second;
            return result;
        }
        private static Dictionary<ITypeParameterSymbol, ITypeSymbol> TypeSubstitutions(IMethodSymbol method)
        {
            var result = TypeSubstitutions(method.ContainingType);
            foreach (var pair in method.OriginalDefinition.TypeParameters.Zip(method.TypeArguments)) result[pair.First] = pair.Second;
            return result;
        }
        private ITypeSymbol SubstituteType(ITypeSymbol type, IReadOnlyDictionary<ITypeParameterSymbol, ITypeSymbol> substitutions)
        {
            // Simultaneous substitution: replacements already belong to the
            // caller. Substituting again would capture a free caller parameter.
            if (type is ITypeParameterSymbol parameter) return substitutions.TryGetValue(parameter, out var argument) ? argument : type;
            if (type is IArrayTypeSymbol array) return compilation.CreateArrayTypeSymbol(SubstituteType(array.ElementType, substitutions), array.Rank).WithNullableAnnotation(array.NullableAnnotation);
            if (type is not INamedTypeSymbol named || named.TypeArguments.Length == 0 && named.ContainingType is null) return type;
            var owner = named.ContainingType is { } containing ? (INamedTypeSymbol)SubstituteType(containing, substitutions) : null;
            var definition = owner is null ? named.ConstructedFrom : owner.GetTypeMembers(named.Name, named.Arity).Single();
            var result = named.Arity == 0 ? definition : definition.Construct(named.TypeArguments.Select(argument => SubstituteType(argument, substitutions)).ToArray()); return result.WithNullableAnnotation(named.NullableAnnotation);
        }
        private Dictionary<ITypeParameterSymbol, ITypeSymbol> GenericContext(IMethodSymbol method, GenericScope? caller = null)
        {
            caller ??= CaptureGenericScope(); var context = new Dictionary<ITypeParameterSymbol, ITypeSymbol>(activeGenericTypes, SymbolEqualityComparer.Default); var scopes = new Dictionary<ITypeParameterSymbol, GenericScope>(activeGenericScopes, SymbolEqualityComparer.Default);
            foreach (var pair in SourceTypeParameters(method.OriginalDefinition.ContainingType).Zip(SourceTypeArguments(method.ContainingType)).Concat(method.OriginalDefinition.TypeParameters.Zip(method.TypeArguments)))
                if (!SymbolEqualityComparer.Default.Equals(pair.First, pair.Second)) { context[pair.First] = pair.Second; scopes[pair.First] = caller; }
            activeGenericScopes = scopes; return context;
        }
        private static string SourceTypeName(INamedTypeSymbol type) => type.ContainingSymbol is INamespaceOrTypeSymbol owner && owner.GetTypeMembers(type.Name).Select(member => member.Arity).Distinct().Count() > 1 ? type.Name + "__Arity" + type.Arity : type.Name;
        private static string SourceOwnerName(INamedTypeSymbol type) => (type.ContainingType is { } parent ? SourceOwnerName(parent) : type.ContainingNamespace.ToDisplayString()) + "." + SourceTypeName(type);
        private static IEnumerable<ITypeParameterSymbol> SourceTypeParameters(INamedTypeSymbol type) => (type.ContainingType is { } parent ? SourceTypeParameters(parent) : Enumerable.Empty<ITypeParameterSymbol>()).Concat(type.OriginalDefinition.TypeParameters);
        private static IEnumerable<ITypeSymbol> SourceTypeArguments(INamedTypeSymbol type) => (type.ContainingType is { } parent ? SourceTypeArguments(parent) : Enumerable.Empty<ITypeSymbol>()).Concat(type.TypeArguments);
        private static Name TypeParameterName(ITypeParameterSymbol parameter)
        {
            var method = parameter.ContainingSymbol as IMethodSymbol; var type = parameter.ContainingType;
            var methods = new List<IMethodSymbol>();
            for (var current = method; current is not null; current = current.ContainingSymbol as IMethodSymbol) methods.Insert(0, current);
            var scope = SourceTypeParameters(type).Concat(methods.SelectMany(current => current.TypeParameters)).ToArray(); var simple = Name.From(parameter.Name);
            if (scope.Count(candidate => Name.From(candidate.Name).Key == simple.Key) <= 1) return simple;
            var depth = 0; for (var parent = type.ContainingType; parent is not null; parent = parent.ContainingType) depth++;
            return Name.From((method is null ? "type" + depth : "method" + methods.Count) + "_" + parameter.Name + "_" + parameter.Ordinal);
        }
        private void ValidateOrdinaryGenerics(IEnumerable<ITypeParameterSymbol> parameters)
        {
            foreach (var parameter in parameters)
            {
                if (parameter.Variance != VarianceKind.None || parameter.AllowsRefLikeType || parameter.GetAttributes().Length != 0)
                    Fail<object>("MCS3101", "Variance/ref-like generic parameters require their explicit representation gate.", Location(parameter));
                // Roslyn checks constraint satisfaction at every constructed source use.
                // Constraints on a representation-parametric body need no runtime witness.
                // Operations such as default(T), new T() and constrained dispatch keep
                // separate gates until a concrete witness is available.
                foreach (var constraint in parameter.ConstraintTypes)
                    if (constraint.Locations.Any(location => location.IsInSource) && !identities.ContainsKey(SymbolId(constraint)))
                        Fail<object>("MCS2201", $"Constraint type {constraint} is not selected; add its namespace explicitly.", Location(parameter));
            }
        }
        private IrType<Unit> GenericTypeReference(INamedTypeSymbol type) => new ReferenceType<Unit>(default, Identity(type), SourceTypeArguments(type).Select(argument => Type(argument, Location(type))).ToArray());
        private IrType<Unit> InstantiateCallableOutput(IMethodSymbol method, IrType<Unit> output, Location location)
        {
            var definition = method.OriginalDefinition; var substitutions = new Dictionary<string, IrType<Unit>>();
            foreach (var pair in SourceTypeParameters(definition.ContainingType).Zip(SourceTypeArguments(method.ContainingType))) substitutions[TypeParameterName(pair.First).Key] = Type(pair.Second, location);
            foreach (var pair in definition.TypeParameters.Zip(method.TypeArguments)) substitutions[TypeParameterName(pair.First).Key] = Type(pair.Second, location);
            IrType<Unit> Replace(IrType<Unit> type) => type switch
            {
                VariableType<Unit> variable when substitutions.TryGetValue(variable.Name.Key, out var replacement) => replacement,
                ReferenceType<Unit> reference => reference with { Arguments = reference.Arguments.Select(Replace).ToArray() },
                FunctionType<Unit> function => function with { Argument = Replace(function.Argument), Result = Replace(function.Result) },
                TupleType<Unit> tuple => tuple with { Elements = tuple.Elements.Select(Replace).ToArray() },
                RecordType<Unit> record => record with { Fields = record.Fields.Select(field => new Entry<Name, IrType<Unit>>(field.Key, Replace(field.Value))).ToArray() },
                _ => type
            };
            return Replace(output);
        }
    }
}
