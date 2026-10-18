using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Morphir.IR.Classic;
using TypedValue = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        private bool MutableInterfaceBox(INamedTypeSymbol type) => type.IsValueType && !type.IsReadOnly && type.AllInterfaces.SelectMany(contract => contract.GetMembers().OfType<IPropertySymbol>()).Any(property => property.SetMethod is { IsInitOnly: false } setter && dataSetters.ContainsKey(SymbolId(setter)));
        private bool OwnedBoxConversion(IOperation operation) => operation is IConversionOperation conversion && ValueInterfaceConversion(conversion) && (NullableElement(conversion.Operand.Type) ?? conversion.Operand.Type) is INamedTypeSymbol source && MutableInterfaceBox(source);
        private TypedValue SnapshotBoxValue(INamedTypeSymbol type, TypedValue value, BindingEnvironment environment, Location location)
        {
            var layout = DispatchStorageLayout(type); var saved = bindings; bindings = environment;
            try
            {
                var fields = layout.Slots.Select(slot =>
                {
                    var name = Name.From(slot.Name); var known = KnownDataField(value, name.Key);
                    return new Entry<Name, TypedValue>(name, known ?? new FieldValue<Unit, IrType<Unit>>(slot.Representation ?? Type(slot.Type, location), DataPayload(layout, value), name));
                }).OrderBy(field => field.Key.Key, StringComparer.Ordinal).ToArray();
                return PackData(layout, new RecordValue<Unit, IrType<Unit>>(customDataTypes.Contains(SymbolId(type)) ? DataPayloadType(layout) : Type(type, location), fields));
            }
            finally { bindings = saved; }
        }
        private TypedValue? InterfaceBoxExpression(IOperation operation, BindingEnvironment environment, Func<TypedValue, BindingEnvironment, TypedValue> continuation)
        {
            if (operation is not IConversionOperation conversion || !OwnedBoxConversion(conversion)) return null;
            var source = (INamedTypeSymbol)(NullableElement(conversion.Operand.Type) ?? conversion.Operand.Type)!; var contract = (INamedTypeSymbol)conversion.Type!; var location = operation.Syntax.GetLocation();
            return Expression(conversion.Operand, environment, (value, state) =>
            {
                TypedValue Box(TypedValue payload, BindingEnvironment next)
                {
                    payload = SnapshotBoxValue(source, payload, next, location); var layout = DispatchStorageLayout(source); var origin = new DataOrigin(++nextDataCell);
                    var fields = layout.Slots.ToDictionary(slot => Name.From(slot.Name).Key, slot => KnownDataField(payload, Name.From(slot.Name).Key)!);
                    next.Heap[origin.Cell] = new(source, payload, fields, ++nextDataVersion, Layout: layout, Boxed: true); dataOrigins[payload] = origin;
                    provedDataOperations.Add(operation); return continuation(InterfaceValue(contract, source, payload), next);
                }
                return MaybeElement(value.Attributes) is null ? Box(value, state) : MatchMaybe(value, () => { provedDataOperations.Add(operation); return continuation(Nothing(Type(contract, location)), Copy(state)); }, payload => Box(payload, Copy(state)));
            });
        }
    }
}
