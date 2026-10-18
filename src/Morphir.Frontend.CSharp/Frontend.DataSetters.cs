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
        private readonly Dictionary<string, IMethodSymbol> dataSetters = [];
        private readonly HashSet<string> emittedDataSetters = [];
        private static bool AccessorBody(IMethodSymbol? method) => method is not null && method.DeclaringSyntaxReferences.Any(reference => reference.GetSyntax() is AccessorDeclarationSyntax { Body: not null } or AccessorDeclarationSyntax { ExpressionBody: not null } or ArrowExpressionClauseSyntax);
        private bool DataSetter(IPropertyReferenceOperation property) => property.Property.SetMethod is { } setter && dataSetters.ContainsKey(SymbolId(setter));
        private void RegisterDataSetters(IEnumerable<INamedTypeSymbol> selected)
        {
            foreach (var type in selected.Where(type => DataType(type) || type.TypeKind == TypeKind.Class && ValueInterfaceType(type)))
                foreach (var property in type.GetMembers().OfType<IPropertySymbol>().Where(DataPropertyDeclaration))
                    if (property.SetMethod is { } setter && AccessorBody(setter) && !dataSetters.ContainsKey(SymbolId(setter)))
                    { AddSymbol(setter, SourceOwnerName(type), "Set" + property.MetadataName + SignatureSuffix(setter)); dataSetters[SymbolId(setter)] = setter; RegisterLocalMethods(setter); }
        }
        private void EmitDataSetter(IMethodSymbol setter, bool interfaceBody = false)
        {
            setter = setter.OriginalDefinition; interfaceBody &= InterfaceDispatchMethod(setter);
            var key = interfaceBody ? InterfaceDefaultKey(setter) : SymbolId(setter); if (emittedDataSetters.Contains(key)) return;
            if (!emittingMethods.Add(key)) Fail<object>("MCS2203", "Recursive setters require D14.", Location(setter));
            try
            {
                if (!interfaceBody && InterfaceDispatchMethod(setter)) { EmitSetterDispatch(setter); emittedDataSetters.Add(key); return; }
                if (AutomaticSetter(setter)) { EmitAutomaticSetter(setter, interfaceBody); emittedDataSetters.Add(key); return; }
                var syntax = setter.DeclaringSyntaxReferences.Single().GetSyntax(); var root = CallableOperation(Semantic(syntax.SyntaxTree), syntax);
                EmitBaseDependencies(root);
                EmitDataOperatorDependencies(root); EmitNewWitnessDependencies(root);
                foreach (var call in Operations(root).OfType<IInvocationOperation>()) if (!DirectBaseCall(call) && sourceMethods.TryGetValue(SymbolId(call.TargetMethod), out var method)) EmitMethod(method);
                foreach (var property in Operations(root).OfType<IPropertyReferenceOperation>())
                {
                    if (DataGetter(property) && !DirectBaseMember(property)) EmitMethod(dataGetters[SymbolId(property.Property.GetMethod!)]);
                    if (DataSetter(property)) EmitDataSetter(dataSetters[SymbolId(property.Property.SetMethod!)], property.Instance?.Syntax is BaseExpressionSyntax);
                }
                foreach (var creation in Operations(root).OfType<IObjectCreationOperation>()) if (creation.Constructor is not null && dataConstructors.TryGetValue(SymbolId(creation.Constructor), out var constructor)) EmitDataConstructor(constructor);
                if (setter.IsStatic || setter.IsAbstract || setter.GetAttributes().Length != 0 || setter.Parameters.Any(parameter => parameter.RefKind != RefKind.None || parameter.GetAttributes().Length != 0)) Fail<object>("MCS3005", "Setter requires a resolved instance body with by-value inputs.", Location(setter));
                PrepareBindings(setter); PrepareDefaultWitnesses(setter); currentMethod = key; calls.TryAdd(key, []);
                var location = syntax.GetLocation(); TypedValue body;
                if (ValueInterfaceType(setter.ContainingType)) body = TransformClassStorage(setter.ContainingType, currentReceiver!, bindings, location, (origin, state, finish) => RunDataSetter(setter, origin, state, finish));
                else
                {
                    var origin = new DataOrigin(++nextDataCell);
                    bindings.Heap[origin.Cell] = new(setter.ContainingType, currentReceiver!, null, ++nextDataVersion, Layout: ResolvedDataLayout(setter.ContainingType));
                    body = RunDataSetter(setter, origin, bindings, state => DemandPresent(ReadDataOrigin(origin, state, location), location));
                }
                var receiverType = ReceiverIrType(setter, location);
                var inputs = new[] { new Input<Unit, IrType<Unit>>(ReceiverName(setter), receiverType, receiverType) }.Concat(setter.Parameters.Select(parameter => new Input<Unit, IrType<Unit>>(Name.From(parameter.Name), Type(parameter.Type, location), Type(parameter.Type, location)))).ToArray();
                inputs = DefaultWitnessInputs(setter).Concat(inputs).ToArray();
                var id = interfaceBody ? InterfaceDefaultIdentity(setter) : Identity(setter); EnsureModule(id.Module, Visibility(setter.ContainingType));
                modules[id.Module.Key].Values.Add(new(id.Local, new(interfaceBody ? Access.Private : Visibility(setter), new(Documentation(setter.AssociatedSymbol!), new(inputs, body.Attributes, body))))); emittedDataSetters.Add(key);
            }
            finally { emittingMethods.Remove(key); }
        }
        private TypedValue RunDataSetter(IMethodSymbol setter, DataOrigin origin, BindingEnvironment environment, Func<BindingEnvironment, TypedValue> continuation)
        {
            if (AutomaticSetter(setter))
            {
                var saved = bindings; bindings = environment; TypedValue value;
                try { value = ReadBinding(setter.Parameters[^1], Location(setter)!); } finally { bindings = saved; }
                WriteDataField(origin, setter.AssociatedSymbol!, value, environment, Location(setter)!); return continuation(environment);
            }
            var syntax = setter.DeclaringSyntaxReferences.Single().GetSyntax(); var root = CallableOperation(Semantic(syntax.SyntaxTree), syntax);
            var body = root switch { IMethodBodyOperation method => method.BlockBody ?? method.ExpressionBody!, IBlockOperation block => block, _ => Fail<IBlockOperation>("MCS3001", "Unsupported setter body.", syntax.GetLocation()) };
            var previousScope = CaptureGenericScope(); var witnesses = DefaultWitnessArguments(setter, syntax.GetLocation());
            genericDefaultValues = new(previousScope.Defaults, SymbolEqualityComparer.Default); foreach (var pair in GenericDefaults(setter).Zip(witnesses)) genericDefaultValues[pair.First] = pair.Second;
            genericNewValues = new(previousScope.News, SymbolEqualityComparer.Default); foreach (var pair in GenericNews(setter).Zip(witnesses.Skip(GenericDefaults(setter).Length))) genericNewValues[pair.First] = pair.Second;
            genericAbsenceValues = new(previousScope.Absences, SymbolEqualityComparer.Default); foreach (var pair in GenericAbsences(setter).Zip(witnesses.Skip(GenericDefaults(setter).Length + GenericNews(setter).Length))) genericAbsenceValues[pair.First] = pair.Second;
            genericDispatchValues = new(previousScope.Dispatches); foreach (var pair in GenericDispatches(setter).Zip(witnesses.Skip(GenericDefaults(setter).Length + GenericNews(setter).Length + GenericAbsences(setter).Length))) genericDispatchValues[GenericDispatchKey(pair.First)] = pair.Second;
            genericFactoryValues = FactoryWitnessBindings(setter, witnesses); activeGenericTypes = GenericContext(setter, previousScope);
            var previousReceiver = currentReceiver; var previousOrigin = currentDataConstructionOrigin; var previousReturn = currentVoidReturn; var previousMethod = currentMethod; var previousBindings = bindings;
            currentReceiver = environment.Heap[origin.Cell].Current; currentDataConstructionOrigin = origin; currentMethod = InterfaceDispatchMethod(setter) ? InterfaceDefaultKey(setter) : SymbolId(setter); bindings = environment; calls.TryAdd(currentMethod, []);
            TypedValue Finish(BindingEnvironment state)
            {
                var activeScope = CaptureGenericScope(); var receiver = currentReceiver; var activeOrigin = currentDataConstructionOrigin; var activeReturn = currentVoidReturn; var method = currentMethod; var activeBindings = bindings;
                RestoreGenericScope(previousScope); currentReceiver = previousReceiver; currentDataConstructionOrigin = previousOrigin; currentVoidReturn = previousReturn; currentMethod = previousMethod; bindings = previousBindings;
                try { return continuation(state); }
                finally { RestoreGenericScope(activeScope); currentReceiver = receiver; currentDataConstructionOrigin = activeOrigin; currentVoidReturn = activeReturn; currentMethod = method; bindings = activeBindings; }
            }
            currentVoidReturn = Finish;
            try { ValidateBlockOperations(body); var result = Statements(body.Operations, 0, environment, Finish); ValidateUnreachedArithmetic(body); return result; }
            finally { RestoreGenericScope(previousScope); currentReceiver = previousReceiver; currentDataConstructionOrigin = previousOrigin; currentVoidReturn = previousReturn; currentMethod = previousMethod; bindings = previousBindings; }
        }
        private TypedValue ApplyDataSetter(IPropertyReferenceOperation property, DataOrigin origin, TypedValue[] indices, TypedValue value, BindingEnvironment environment, Func<BindingEnvironment, TypedValue> continuation)
        {
            var declaration = property.Property.SetMethod!; var location = property.Syntax.GetLocation();
            var setter = property.Instance?.Syntax is BaseExpressionSyntax || !InterfaceDispatchMethod(declaration) ? declaration : ValueImplementation(environment.Heap[origin.Cell].Type, declaration) ?? Fail<IMethodSymbol>("MCS3112", "No acquired setter target exists for this receiver.", location);
            calls.TryAdd(currentMethod, []); calls[currentMethod].Add((InterfaceDispatchMethod(setter) ? InterfaceDefaultKey(setter) : SymbolId(setter), location));
            var arguments = indices.Append(value).ToArray();
            TypedValue Bind(int index, BindingEnvironment state) => index == arguments.Length ? RunDataSetter(setter, origin, state, continuation) : BindValue(setter.Parameters[index].OriginalDefinition, setter.Parameters[index].Type, arguments[index], state, next => Bind(index + 1, next), location);
            return Bind(0, environment);
        }
        private TypedValue? DataSetterExpression(IOperation operation, BindingEnvironment environment, Func<TypedValue, BindingEnvironment, TypedValue> continuation)
        {
            if (operation is not ISimpleAssignmentOperation { Target: IPropertyReferenceOperation property } assignment || !DataSetter(property)) return null;
            var location = assignment.Syntax.GetLocation();
            TypedValue Evaluate(TypedValue? receiver, DataLocation? structLocation, BindingEnvironment state)
            {
                var indices = new TypedValue[property.Arguments.Length];
                TypedValue Arguments(int index, BindingEnvironment next)
                {
                    if (index < property.Arguments.Length) return Expression(property.Arguments[index].Value, next, (value, after) => { indices[property.Arguments[index].Parameter!.Ordinal] = value; return Arguments(index + 1, after); });
                    return Expression(assignment.Value, next, (value, after) =>
                    {
                        var origin = property.Instance is IInstanceReferenceOperation ? currentDataConstructionOrigin : receiver is null ? null : ClassFieldOrigin(receiver);
                        if (structLocation is not null)
                        { origin = new DataOrigin(++nextDataCell); after.Heap[origin.Cell] = new(property.Property.ContainingType, structLocation.Read(after), null, ++nextDataVersion, Layout: ResolvedDataLayout(property.Property.ContainingType)); }
                        if (origin is null || !after.Heap.TryGetValue(origin.Cell, out var cell) || !cell.Mutable) return Fail<TypedValue>("MCS3002", "Setter mutation requires proved local ownership; input/external reachability is immutable.", location);
                        return ApplyDataSetter(property, origin, indices, value, after, updated =>
                        {
                            provedDataOperations.Add(operation); return structLocation is null ? continuation(value, updated) : structLocation.Write(ReadDataOrigin(origin, updated, location), updated, final => continuation(value, final));
                        });
                    });
                }
                return Arguments(0, state);
            }
            return property.Property.ContainingType.IsValueType ? WithDataLocation(property.Instance!, environment, (destination, state) => Evaluate(null, destination, state)) : Expression(property.Instance!, environment, (receiver, state) => Evaluate(receiver, null, state));
        }
        private TypedValue InitializeDataMember(ISimpleAssignmentOperation assignment, DataOrigin origin, TypedValue value, BindingEnvironment state, Func<BindingEnvironment, TypedValue> continuation)
        {
            provedDataOperations.Add(assignment);
            if (assignment.Target is IFieldReferenceOperation tupleField && TupleType(tupleField.Field.ContainingType)) return WithDataLocation(assignment.Target, state, (destination, next) => destination.Write(value, next, continuation));
            if (assignment.Target is IPropertyReferenceOperation property && DataSetter(property)) return ApplyDataSetter(property, origin, [], value, state, continuation);
            if (DataMemberOf(assignment.Target) is not { } member) return Fail<TypedValue>("MCS3001", "Unsupported object initializer member.", assignment.Syntax.GetLocation());
            WriteDataCell(origin, member, Adapt(value, Type(member.Type, assignment.Syntax.GetLocation())), state, assignment.Syntax.GetLocation()); return continuation(state);
        }
    }
}
