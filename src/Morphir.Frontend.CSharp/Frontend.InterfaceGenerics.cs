using Microsoft.CodeAnalysis;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        // A parametric union can carry a runtime alternative only when every
        // payload parameter is determined by the contract's own arguments.
        // Nonuniform implementations need a separate resolved specialization.
        private INamedTypeSymbol UniformInterfaceAlternative(INamedTypeSymbol candidate, INamedTypeSymbol contract)
        {
            var implementations = ImplementedRuntimeContracts(candidate, contract).ToArray();
            if (implementations.Length != 1) return Fail<INamedTypeSymbol>("MCS3112", "Multiple constructed contracts require resolved interface specialization.", Location(candidate));
            var actual = SourceTypeArguments(implementations[0]).ToArray(); var parameters = SourceTypeParameters(candidate).ToArray(); var formal = SourceTypeParameters(contract).ToArray();
            if (parameters.Length != formal.Length || actual.Length != formal.Length || actual.Any(argument => argument is not ITypeParameterSymbol) || parameters.Any(parameter => actual.Count(argument => SymbolEqualityComparer.Default.Equals(parameter, argument)) != 1))
                return Fail<INamedTypeSymbol>("MCS3110", "Nonuniform generic implementation alternatives require resolved representation specialization.", Location(candidate));
            var substitutions = new Dictionary<ITypeParameterSymbol, ITypeSymbol>(SymbolEqualityComparer.Default);
            foreach (var pair in actual.Zip(formal)) substitutions[(ITypeParameterSymbol)pair.First] = pair.Second;
            return (INamedTypeSymbol)SubstituteType(candidate, substitutions);
        }
        private INamedTypeSymbol[] ValueAlternatives(INamedTypeSymbol contract)
        {
            var substitutions = TypeSubstitutions(contract);
            return valueInterfaces[SymbolId(contract)].Alternatives.Select(alternative => (INamedTypeSymbol)SubstituteType(alternative, substitutions)).ToArray();
        }
        private static string GenericDispatchTypeKey(ITypeSymbol type) => type is ITypeParameterSymbol parameter ? GenericParameterIdentity(parameter) : type is IArrayTypeSymbol array ? "array:" + array.Rank + ":" + array.IsSZArray + ":" + GenericDispatchTypeKey(array.ElementType) : type is INamedTypeSymbol named ? SymbolId(named) + "<" + string.Join(";", SourceTypeArguments(named).Select(GenericDispatchTypeKey)) + ">" : type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        private string RuntimeTypeKey(ITypeSymbol type)
        {
            if (GenericArgument(type, out var actual, out var scope)) return WithGenericScope(scope, () => RuntimeTypeKey(actual));
            if (type is ITypeParameterSymbol parameter) return GenericParameterIdentity(parameter);
            if (type is IArrayTypeSymbol array) return "array:" + array.Rank + ":" + array.IsSZArray + ":" + RuntimeTypeKey(array.ElementType);
            return type is INamedTypeSymbol named ? SymbolId(named) + "<" + string.Join(";", SourceTypeArguments(named).Select(RuntimeTypeKey)) + ">" : type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }
        private bool SameRuntimeType(ITypeSymbol left, ITypeSymbol right) => RuntimeTypeKey(left) == RuntimeTypeKey(right);
        private IMethodSymbol SubstituteDispatchMethod(IMethodSymbol method, IReadOnlyDictionary<ITypeParameterSymbol, ITypeSymbol> substitutions)
        {
            var owner = (INamedTypeSymbol)SubstituteType(method.ContainingType, substitutions);
            var member = DispatchMember(owner, method);
            return member.Arity == 0 ? member : member.Construct(method.TypeArguments.Select(argument => SubstituteType(argument, substitutions)).ToArray());
        }
        private static IMethodSymbol DispatchMember(INamedTypeSymbol owner, IMethodSymbol method) => method.AssociatedSymbol is IPropertySymbol property
            ? owner.GetMembers(property.Name).OfType<IPropertySymbol>().Where(member => SymbolId(member) == SymbolId(property)).Select(member => method.MethodKind == MethodKind.PropertySet ? member.SetMethod : member.GetMethod).Single()!
            : owner.GetMembers(method.Name).OfType<IMethodSymbol>().Single(member => SymbolId(member) == SymbolId(method));
        private IMethodSymbol ResolveDispatchMethod(IMethodSymbol method)
        {
            var member = DispatchMember((INamedTypeSymbol)ResolveRuntimeType(method.ContainingType), method);
            return member.Arity == 0 ? member : member.Construct(method.TypeArguments.Select(ResolveRuntimeType).ToArray());
        }
        private IEnumerable<IMethodSymbol> InterfaceTargets(IMethodSymbol method) => InterfaceDispatchMethod(method) ? ValueAlternatives(method.ContainingType).Select(alternative => ValueImplementation(alternative, method)!) : [];
    }
}
