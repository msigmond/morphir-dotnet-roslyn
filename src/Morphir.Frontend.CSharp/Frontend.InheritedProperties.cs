using Microsoft.CodeAnalysis;
using Morphir.IR.Classic;
using TypedValue = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        private static bool BaseProperty(IPropertySymbol property) => DataPropertyDeclaration(property) && (!property.IsStatic || StaticInterfaceProperty(property)) && new[] { property.GetMethod, property.SetMethod }.Where(method => method is not null).All(method => method!.GetAttributes().Length == 0 && method.GetReturnTypeAttributes().Length == 0 && !method.ReturnsByRef && !method.ReturnsByRefReadonly && method.Parameters.All(parameter => parameter.RefKind == RefKind.None && parameter.GetAttributes().Length == 0));
        private static bool BaseStorageProperty(ISymbol member) => member is IPropertySymbol property && BaseProperty(property) && DataPropertyStorage(property);
        private static bool BasePropertyMember(ISymbol member) => member is IPropertySymbol property && BaseProperty(property) || member is IMethodSymbol { AssociatedSymbol: IPropertySymbol associated } && BaseProperty(associated) || member is IFieldSymbol { IsImplicitlyDeclared: true, AssociatedSymbol: IPropertySymbol storage } && BaseProperty(storage);
        private bool InheritedStorageMember(ISymbol member) => InheritedField(member) || member is IPropertySymbol { IsStatic: false } property && BaseStorageProperty(property) && ValueInterfaceType(property.ContainingType);
        private TypedValue TransformClassStorage(INamedTypeSymbol contract, TypedValue receiver, BindingEnvironment environment, Location location, Func<DataOrigin, BindingEnvironment, Func<BindingEnvironment, TypedValue>, TypedValue> transform)
        {
            return MatchInterface(receiver, contract, (alternative, payload) =>
            {
                var state = Copy(environment); var layout = DispatchStorageLayout(alternative); var origin = new DataOrigin(++nextDataCell);
                var fields = layout.Slots.ToDictionary(slot => Name.From(slot.Name).Key, slot => (TypedValue)new FieldValue<Unit, IrType<Unit>>(slot.Representation ?? Type(slot.Type, location), DataPayload(layout, payload), Name.From(slot.Name)));
                state.Heap[origin.Cell] = new(alternative, payload, fields, ++nextDataVersion, Layout: layout);
                return transform(origin, state, after => DemandPresent(InterfaceValue(contract, alternative, DemandPresent(ReadDataOrigin(origin, after, location), location)), location));
            });
        }
    }
}
