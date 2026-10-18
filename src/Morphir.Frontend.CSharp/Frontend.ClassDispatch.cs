using Microsoft.CodeAnalysis;
using TypedValue = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        private bool DispatchContractType(INamedTypeSymbol type) => type.TypeKind == TypeKind.Interface || type.TypeKind == TypeKind.Class && !type.IsStatic && !type.IsRecord && (type.IsAbstract || !type.IsSealed && (type.GetMembers().OfType<IMethodSymbol>().Any(method => !method.IsStatic && (method.IsVirtual || method.IsOverride)) || compilations.SelectMany(source => AllTypes(source.Assembly.GlobalNamespace)).Any(candidate => RuntimeContracts(candidate).Any(parent => parent.TypeKind == TypeKind.Class && SymbolId(parent) == SymbolId(type)))));
        private bool ConcreteClassDomain(ITypeSymbol type) => type is INamedTypeSymbol { TypeKind: TypeKind.Class, IsAbstract: false } named && ValueInterfaceType(named);
        private TypedValue ConstructedClassValue(DataLayout data, TypedValue value, Location location) => ConcreteClassDomain(data.Type) ? InterfaceValue(data.Type, data.Type, DemandPresent(value, location)) : value;
        private IEnumerable<INamedTypeSymbol> ImplementedRuntimeContracts(INamedTypeSymbol type, INamedTypeSymbol contract) => (contract.TypeKind == TypeKind.Class && SymbolId(type) == SymbolId(contract) ? new[] { type } : []).Concat(RuntimeContracts(type).Where(parent => SymbolId(parent) == SymbolId(contract)));
        private static IEnumerable<INamedTypeSymbol> RuntimeContracts(INamedTypeSymbol type)
        {
            foreach (var contract in type.AllInterfaces) yield return contract;
            for (var parent = type.BaseType; parent is not null; parent = parent.BaseType) yield return parent;
        }
        private static bool ImplicitBaseConstructor(ISymbol member) => member is IMethodSymbol { IsImplicitlyDeclared: true, MethodKind: MethodKind.Constructor };
        private IMethodSymbol? ClassImplementation(INamedTypeSymbol candidate, IMethodSymbol method)
        {
            var owner = ImplementedRuntimeContracts(candidate, method.ContainingType).FirstOrDefault(type => SameRuntimeType(type, method.ContainingType));
            if (owner is null) return null;
            var declaration = DispatchMember(owner, method);
            IMethodSymbol Construct(IMethodSymbol target) => target.Arity == 0 ? target : target.Construct(method.TypeArguments.ToArray());
            if (!method.IsAbstract && !method.IsVirtual && !method.IsOverride) return Construct(declaration);
            for (var current = candidate; current is not null; current = current.BaseType)
                foreach (var target in (method.AssociatedSymbol is IPropertySymbol property ? current.GetMembers(property.Name).OfType<IPropertySymbol>().Select(member => method.MethodKind == MethodKind.PropertySet ? member.SetMethod : member.GetMethod).OfType<IMethodSymbol>() : current.GetMembers(method.Name).OfType<IMethodSymbol>()))
                    for (var overridden = target; overridden is not null; overridden = overridden.OverriddenMethod)
                        if (SymbolId(overridden) == SymbolId(method)) return target.IsAbstract ? null : Construct(target);
            return null;
        }
        private static bool SourceBaseConstructor(ISymbol member) => member is IMethodSymbol { MethodKind: MethodKind.Constructor, IsStatic: false } constructor && (PrimaryDataConstructor(constructor) || constructor.DeclaringSyntaxReferences.Any(reference => reference.GetSyntax() is Microsoft.CodeAnalysis.CSharp.Syntax.ConstructorDeclarationSyntax));
    }
}
