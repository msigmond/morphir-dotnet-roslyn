using Microsoft.CodeAnalysis;
using Morphir.IR.Classic;
using TypedValue = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        private static bool AutomaticSetter(IMethodSymbol method) => method.MethodKind == MethodKind.PropertySet && !method.IsAbstract && !AccessorBody(method) && method.AssociatedSymbol is IPropertySymbol property && DataPropertyStorage(property);
        private void RegisterDispatchSetters(INamedTypeSymbol[] selected)
        {
            void Register(IMethodSymbol setter)
            {
                if (!identities.ContainsKey(SymbolId(setter)))
                {
                    AddSymbol(setter, SourceOwnerName(setter.ContainingType), "Set" + setter.AssociatedSymbol!.MetadataName + SignatureSuffix(setter));
                    RegisterLocalMethods(setter);
                }
                dataSetters[SymbolId(setter)] = setter;
            }
            foreach (var type in selected)
                foreach (var property in type.GetMembers().OfType<IPropertySymbol>().Where(DataPropertyDeclaration))
                    if (property.SetMethod is { IsStatic: false } setter && (type.TypeKind == TypeKind.Interface || setter.IsVirtual || setter.IsOverride || setter.IsAbstract)) Register(setter);
            foreach (var contract in selected.Where(type => type.TypeKind == TypeKind.Interface))
                foreach (var property in contract.GetMembers().OfType<IPropertySymbol>().Where(property => InstanceInterfaceProperty(property) && property.SetMethod is not null))
                    foreach (var candidate in selected.Where(type => !type.IsAbstract))
                        foreach (var implemented in candidate.AllInterfaces.Where(type => SymbolId(type) == SymbolId(contract)))
                        {
                            var member = implemented.GetMembers(property.Name).OfType<IPropertySymbol>().Single(item => SymbolId(item) == SymbolId(property));
                            if (candidate.FindImplementationForInterfaceMember(member) is IPropertySymbol { SetMethod: { } setter } && Selected(setter.ContainingType)) Register(setter);
                        }
        }
        private DataLayout DispatchStorageLayout(INamedTypeSymbol type) => DataType(type) ? ResolvedDataLayout(type) : new(type, records[SymbolId(type)].Select(parameter => type.GetMembers(parameter.Name).OfType<IPropertySymbol>().Single()).Select(property => new DataSlot(property, property.Type)).ToArray(), CaptureGenericScope());
        private Input<Unit, IrType<Unit>>[] SetterInputs(IMethodSymbol setter, Location location)
        {
            var receiver = ReceiverIrType(setter, location);
            return DefaultWitnessInputs(setter).Concat([new Input<Unit, IrType<Unit>>(ReceiverName(setter), receiver, receiver)]).Concat(setter.Parameters.Select(parameter => new Input<Unit, IrType<Unit>>(Name.From(parameter.Name), Type(parameter.Type, location), Type(parameter.Type, location)))).ToArray();
        }
        private void EmitSetterDispatch(IMethodSymbol setter)
        {
            var key = SymbolId(setter); var location = Location(setter)!;
            foreach (var alternative in ValueAlternatives(setter.ContainingType))
            {
                var target = ValueImplementation(alternative, setter)!;
                EmitDataSetter(target, InterfaceDispatchMethod(target));
            }
            currentMethod = key; calls.TryAdd(key, []); PrepareBindings(setter); PrepareDefaultWitnesses(setter);
            var inputs = SetterInputs(setter, location); var arguments = setter.Parameters.Select(parameter => ReadBinding(parameter, location)).ToArray();
            var body = TransformClassStorage(setter.ContainingType, currentReceiver!, bindings, location, (origin, state, finish) =>
            {
                var target = ValueImplementation(state.Heap[origin.Cell].Type, setter)!;
                calls[key].Add((InterfaceDispatchMethod(target) ? InterfaceDefaultKey(target) : SymbolId(target), location));
                TypedValue Bind(int index, BindingEnvironment next) => index == arguments.Length ? RunDataSetter(target, origin, next, finish) : BindValue(target.Parameters[index].OriginalDefinition, target.Parameters[index].Type, arguments[index], next, after => Bind(index + 1, after), location);
                return Bind(0, state);
            });
            var id = Identity(setter); EnsureModule(id.Module, Visibility(setter.ContainingType));
            modules[id.Module.Key].Values.Add(new(id.Local, new(Visibility(setter), new(Documentation(setter.AssociatedSymbol!), new(inputs, body.Attributes, body)))));
        }
        private void EmitAutomaticSetter(IMethodSymbol setter, bool interfaceBody)
        {
            var key = interfaceBody ? InterfaceDefaultKey(setter) : SymbolId(setter); var location = Location(setter)!;
            currentMethod = key; calls.TryAdd(key, []); PrepareBindings(setter); PrepareDefaultWitnesses(setter);
            var inputs = SetterInputs(setter, location); TypedValue body;
            if (ValueInterfaceType(setter.ContainingType)) body = TransformClassStorage(setter.ContainingType, currentReceiver!, bindings, location, (origin, state, finish) => RunDataSetter(setter, origin, state, finish));
            else
            {
                var origin = new DataOrigin(++nextDataCell); bindings.Heap[origin.Cell] = new(setter.ContainingType, currentReceiver!, null, ++nextDataVersion, Layout: DispatchStorageLayout(setter.ContainingType));
                body = RunDataSetter(setter, origin, bindings, after => DemandPresent(ReadDataOrigin(origin, after, location), location));
            }
            var id = interfaceBody ? InterfaceDefaultIdentity(setter) : Identity(setter); EnsureModule(id.Module, Visibility(setter.ContainingType));
            modules[id.Module.Key].Values.Add(new(id.Local, new(interfaceBody ? Access.Private : Visibility(setter), new(Documentation(setter.AssociatedSymbol!), new(inputs, body.Attributes, body)))));
        }
    }
}
