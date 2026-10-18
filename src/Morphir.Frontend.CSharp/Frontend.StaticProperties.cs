using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Morphir.IR.Classic;
using TypedValue = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        private static bool StaticInterfaceProperty(IPropertySymbol property) => property.IsStatic && !property.IsIndexer && property.GetMethod is not null && property.SetMethod is null && DataPropertyDeclaration(property);
        private static bool InstanceInterfaceProperty(IPropertySymbol property) => !property.IsStatic && DataPropertyDeclaration(property);
        private static bool AdmittedInterfaceMember(ISymbol member)
        {
            if (member is IPropertySymbol property) return (StaticInterfaceProperty(property) || InstanceInterfaceProperty(property)) && new[] { property.GetMethod, property.SetMethod }.Where(accessor => accessor is not null).All(accessor => AdmittedInterfaceMember(accessor!));
            if (member is not IMethodSymbol method || method.MethodKind is not (MethodKind.Ordinary or MethodKind.ExplicitInterfaceImplementation or MethodKind.PropertyGet or MethodKind.PropertySet) || method.MethodKind is (MethodKind.PropertyGet or MethodKind.PropertySet) && (method.AssociatedSymbol is not IPropertySymbol associated || !(StaticInterfaceProperty(associated) || InstanceInterfaceProperty(associated)))) return false;
            return method.GetAttributes().Length == 0 && method.GetReturnTypeAttributes().Length == 0 && !method.DeclaringSyntaxReferences.Any(reference => reference.GetSyntax() is MethodDeclarationSyntax { AttributeLists.Count: > 0 }) && !method.ReturnsByRef && !method.ReturnsByRefReadonly && method.Parameters.All(parameter => parameter.RefKind == RefKind.None && parameter.GetAttributes().Length == 0);
        }
        private static IEnumerable<IMethodSymbol> InterfaceMethods(INamedTypeSymbol type) => type.GetMembers().OfType<IMethodSymbol>().Where(method => method.MethodKind is not (MethodKind.Constructor or MethodKind.PropertySet) && (type.TypeKind != TypeKind.Class || method.AssociatedSymbol is not IPropertySymbol || method.MethodKind == MethodKind.PropertyGet && (AccessorBody(method) || method.IsAbstract || method.IsVirtual || method.IsOverride))).Concat(type.GetMembers().OfType<IPropertySymbol>().Where(property => StaticInterfaceProperty(property) || type.TypeKind == TypeKind.Interface && InstanceInterfaceProperty(property)).Select(property => property.GetMethod).OfType<IMethodSymbol>()).DistinctBy(SymbolId);
        private void RegisterStaticInterfaceProperties(INamedTypeSymbol[] selected)
        {
            void Register(IMethodSymbol getter)
            {
                if (!identities.ContainsKey(SymbolId(getter)))
                {
                    AddSymbol(getter, SourceOwnerName(getter.ContainingType), "Get" + getter.AssociatedSymbol!.MetadataName + SignatureSuffix(getter));
                    RegisterLocalMethods(getter);
                }
                dataGetters[SymbolId(getter)] = getter;
            }
            RegisterDispatchSetters(selected);
            foreach (var type in selected.Where(type => type.TypeKind == TypeKind.Class && DispatchContractType(type))) foreach (var property in type.GetMembers().OfType<IPropertySymbol>().Where(property => BaseProperty(property) && property.GetMethod is not null && (AccessorBody(property.GetMethod) || property.GetMethod.IsAbstract || property.GetMethod.IsVirtual || property.GetMethod.IsOverride))) Register(property.GetMethod!);
            foreach (var type in selected.Where(type => !type.IsAbstract)) foreach (var property in type.GetMembers().OfType<IPropertySymbol>().Where(property => DataPropertyDeclaration(property) && property.GetMethod is { IsOverride: true })) Register(property.GetMethod!);
            foreach (var contract in selected.Where(type => type.TypeKind == TypeKind.Interface)) foreach (var property in contract.GetMembers().OfType<IPropertySymbol>().Where(property => StaticInterfaceProperty(property) || InstanceInterfaceProperty(property)))
                {
                    if (property.GetMethod is { } contractGetter) Register(contractGetter);
                    foreach (var candidate in selected.Where(type => !type.IsAbstract)) foreach (var implemented in candidate.AllInterfaces.Where(type => SymbolId(type) == SymbolId(contract)))
                        {
                            var member = implemented.GetMembers(property.Name).OfType<IPropertySymbol>().Single(item => SymbolId(item) == SymbolId(property));
                            if (candidate.FindImplementationForInterfaceMember(member) is IPropertySymbol { GetMethod: { } getter } && Selected(getter.ContainingType)) Register(getter);
                        }
                }
        }
        private bool GenericStaticProperty(IPropertyReferenceOperation property) => property.ConstrainedToType is ITypeParameterSymbol && property.Property.GetMethod is { IsStatic: true } && ValueInterfaceType(property.Property.ContainingType);
        private TypedValue StaticDispatchProperty(IPropertyReferenceOperation property)
        {
            var method = property.Property.GetMethod!; var location = property.Syntax.GetLocation(); ValidateStaticDispatchResult(property.Type!, location);
            calls[currentMethod].Add((SymbolId(method), location)); var function = DispatchWitness(property.ConstrainedToType!, method, location);
            return new ApplyValue<Unit, IrType<Unit>>(((FunctionType<Unit>)function.Attributes).Result, function, new UnitValue<Unit, IrType<Unit>>(Types.Unit));
        }
    }
}
