using Microsoft.CodeAnalysis;
using Morphir.IR.Classic;
using TypedValue = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        private static bool AutomaticGetter(IMethodSymbol method) => method.MethodKind == MethodKind.PropertyGet && !method.IsAbstract && !AccessorBody(method) && method.AssociatedSymbol is IPropertySymbol property && DataPropertyStorage(property);
        private TypedValue ReadAutomaticGetter(IMethodSymbol method, TypedValue receiver, Location location)
        {
            var property = (IPropertySymbol)method.AssociatedSymbol!;
            if (ClassFieldOrigin(receiver) is { } origin && bindings.Heap.TryGetValue(origin.Cell, out var cell)) return ReadStoredMember(property, property.Type, ReadDataOrigin(origin, bindings, location), CellLayout(cell), location);
            if (ValueInterfaceType(method.ContainingType)) return MatchInterface(DemandPresent(receiver, location), method.ContainingType, (alternative, payload) => ReadStoredMember(property, property.Type, payload, ResolvedDataLayout(alternative), location));
            if (DataType(method.ContainingType)) return ReadStoredMember(property, property.Type, receiver, ResolvedDataLayout(method.ContainingType), location);
            var result = new FieldValue<Unit, IrType<Unit>>(Type(property.Type, location), DemandPresent(receiver, location), Name.From(property.Name));
            if (KnownDataField(receiver, Name.From(property.Name).Key) is { } known) dataProjections[result] = known;
            return result;
        }
        private void EmitAutomaticGetter(IMethodSymbol method, bool interfaceBody)
        {
            var key = interfaceBody ? InterfaceDefaultKey(method) : SymbolId(method);
            currentMethod = key; calls.TryAdd(key, []); PrepareBindings(method); PrepareDefaultWitnesses(method);
            var location = Location(method)!; var receiverType = ReceiverIrType(method, location);
            var body = ReadAutomaticGetter(method, currentReceiver!, location);
            var inputs = DefaultWitnessInputs(method).Concat([new Input<Unit, IrType<Unit>>(ReceiverName(method), receiverType, receiverType)]).ToArray();
            methodOutputs[key] = body.Attributes;
            var id = interfaceBody ? InterfaceDefaultIdentity(method) : Identity(method); EnsureModule(id.Module, Visibility(method.ContainingType));
            modules[id.Module.Key].Values.Add(new(id.Local, new(interfaceBody ? Access.Private : Visibility(method), new(Documentation(method.AssociatedSymbol!), new(inputs, body.Attributes, body)))));
        }
    }
}
