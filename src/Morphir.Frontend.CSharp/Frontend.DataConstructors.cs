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
        private readonly Dictionary<string, IMethodSymbol> dataConstructors = [];
        private readonly HashSet<string> emittedDataConstructors = [];
        private DataOrigin? currentDataConstructionOrigin;
        private Func<BindingEnvironment, TypedValue>? currentVoidReturn;
        private bool BaseConstructor(IMethodSymbol constructor) => constructor.ContainingType.TypeKind == TypeKind.Class && ValueInterfaceType(constructor.ContainingType);
        private DataLayout ConstructorLayout(INamedTypeSymbol type) => DataType(type) ? ResolvedDataLayout(type) : new(type, type.GetMembers().Where(BaseStorageField).OfType<IFieldSymbol>().Select(field => new DataSlot(field, field.Type)).Concat(type.GetMembers().Where(BaseStorageProperty).OfType<IPropertySymbol>().Select(property => new DataSlot(property, property.Type))).ToArray(), CaptureGenericScope());
        private bool InheritedConstruction(INamedTypeSymbol type) => RuntimeContracts(type).Any(parent => parent.TypeKind == TypeKind.Class && ValueInterfaceType(parent) && (HasDataInitializers(parent) || parent.InstanceConstructors.Any(constructor => !constructor.IsImplicitlyDeclared)));
        private IInvocationOperation? ConstructorInitializer(IMethodSymbol constructor)
        {
            var syntax = constructor.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax()).OfType<ConstructorDeclarationSyntax>().SingleOrDefault();
            if (syntax is not null && Semantic(syntax.SyntaxTree).GetOperation(syntax) is IConstructorBodyOperation { Initializer: IExpressionStatementOperation { Operation: IInvocationOperation initializer } }) return initializer;
            var primary = constructor.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax()).OfType<TypeDeclarationSyntax>().SelectMany(declaration => declaration.BaseList?.Types.OfType<PrimaryConstructorBaseTypeSyntax>() ?? []).SingleOrDefault();
            if (primary is null) return null;
            return Semantic(primary.SyntaxTree).GetOperation(primary) is IInvocationOperation invocation ? invocation : Fail<IInvocationOperation>("MCS3005", "Primary base construction requires its resolved constructor invocation.", primary.GetLocation());
        }
        private IEnumerable<IMethodSymbol> ConstructorTargets(IMethodSymbol method)
        {
            if (method.MethodKind != MethodKind.Constructor) yield break;
            if (ConstructorInitializer(method) is { } initializer)
            {
                if (dataConstructors.ContainsKey(SymbolId(initializer.TargetMethod))) yield return initializer.TargetMethod;
            }
            else if (ImplicitBaseTarget(method) is { } target && dataConstructors.ContainsKey(SymbolId(target))) yield return target;
        }
        private void RegisterDataConstructors(IEnumerable<INamedTypeSymbol> selected)
        {
            foreach (var type in selected.Where(type => DataType(type) || type.TypeKind == TypeKind.Class && ValueInterfaceType(type)))
                foreach (var constructor in type.InstanceConstructors.Where(constructor => PrimaryDataConstructor(constructor) || !constructor.IsImplicitlyDeclared && constructor.DeclaringSyntaxReferences.Any(reference => reference.GetSyntax() is ConstructorDeclarationSyntax) || constructor.IsImplicitlyDeclared && !RecordCopyConstructor(constructor) && type.IsReferenceType && (HasDataInitializers(type) || InheritedConstruction(type))))
                {
                    AddSymbol(constructor, SourceOwnerName(type), "New" + SignatureSuffix(constructor)); dataConstructors[SymbolId(constructor)] = constructor; if (!constructor.IsImplicitlyDeclared && !PrimaryDataConstructor(constructor)) RegisterLocalMethods(constructor);
                }
        }
        private TypedValue DataReceiverValue(Location location)
        {
            if (currentDataConstructionOrigin is not { } origin) return currentReceiver!;
            var value = DemandPresent(ReadDataOrigin(origin, bindings, location), location);
            if (bindings.Heap[origin.Cell].Type.IsReferenceType) dataOrigins[value] = origin;
            if ((dataConstructors.GetValueOrDefault(currentMethod) ?? dataSetters.GetValueOrDefault(currentMethod.EndsWith("::default-body", StringComparison.Ordinal) ? currentMethod[..^"::default-body".Length] : currentMethod)) is { } owner && ValueInterfaceType(owner.ContainingType)) return DemandPresent(InterfaceValue(owner.ContainingType, bindings.Heap[origin.Cell].Type, value), location);
            return value;
        }
        private void EmitDataConstructor(IMethodSymbol constructor)
        {
            var key = SymbolId(constructor); if (emittedDataConstructors.Contains(key)) return;
            if (!emittingMethods.Add(key)) Fail<object>("MCS2203", "Recursive construction/calls require D14.", Location(constructor));
            try
            {
                var syntax = constructor.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax()).OfType<ConstructorDeclarationSyntax>().SingleOrDefault(); var location = syntax?.GetLocation() ?? Location(constructor.ContainingType)!; var root = syntax is null ? null : (IConstructorBodyOperation)Semantic(syntax.SyntaxTree).GetOperation(syntax)!;
                IOperation? dependencies = root ?? (IOperation?)ConstructorInitializer(constructor);
                foreach (var target in ConstructorTargets(constructor)) EmitDataConstructor(target);
                if (dependencies is not null) { EmitBaseDependencies(dependencies); EmitDataOperatorDependencies(dependencies); EmitNewWitnessDependencies(dependencies); }
                var initializers = ConstructorInitializers(constructor).ToArray();
                foreach (var initializer in initializers) { EmitDataOperatorDependencies(initializer.Value); EmitNewWitnessDependencies(initializer.Value); }
                foreach (var call in (dependencies is null ? Enumerable.Empty<IOperation>() : Operations(dependencies)).Concat(initializers.SelectMany(initializer => Operations(initializer.Value))).OfType<IInvocationOperation>())
                {
                    if (dataConstructors.TryGetValue(SymbolId(call.TargetMethod), out var chained)) EmitDataConstructor(chained);
                    else if (!DirectBaseCall(call) && sourceMethods.TryGetValue(SymbolId(call.TargetMethod), out var method)) EmitMethod(method);
                }
                foreach (var creation in (dependencies is null ? Enumerable.Empty<IOperation>() : Operations(dependencies)).Concat(initializers.SelectMany(initializer => Operations(initializer.Value))).OfType<IObjectCreationOperation>()) if (creation.Constructor is not null && dataConstructors.TryGetValue(SymbolId(creation.Constructor), out var dependency)) EmitDataConstructor(dependency);
                foreach (var property in (dependencies is null ? Enumerable.Empty<IOperation>() : Operations(dependencies)).Concat(initializers.SelectMany(initializer => Operations(initializer.Value))).OfType<IPropertyReferenceOperation>().Where(property => DataGetter(property) && !DirectBaseMember(property))) EmitMethod(dataGetters[SymbolId(property.Property.GetMethod!)]);
                foreach (var property in (dependencies is null ? Enumerable.Empty<IOperation>() : Operations(dependencies)).Concat(initializers.SelectMany(initializer => Operations(initializer.Value))).OfType<IPropertyReferenceOperation>().Where(DataSetter)) EmitDataSetter(dataSetters[SymbolId(property.Property.SetMethod!)]);
                if (constructor.Parameters.Any(parameter => parameter.RefKind != RefKind.None || parameter.GetAttributes().Length != 0) || constructor.GetAttributes().Length != 0) Fail<object>("MCS3001", "Constructors require resolved by-value parameters without unmapped attributes.", Location(constructor));
                PrepareBindings(constructor); PrepareDefaultWitnesses(constructor); currentMethod = key; calls.TryAdd(key, []);
                TypedValue body;
                Name? receiverName = null;
                if (BaseConstructor(constructor) && constructor.ContainingType.IsAbstract)
                {
                    receiverName = Fresh("this");
                    var receiver = new VariableValue<Unit, IrType<Unit>>(ValueInterfaceIrType(constructor.ContainingType, true), receiverName);
                    body = TransformClassStorage(constructor.ContainingType, receiver, bindings, location, (origin, state, finish) => RunDataConstructor(constructor, origin, state, finish));
                }
                else
                {
                    var data = dataTypes[SymbolId(constructor.ContainingType)]; var origin = new DataOrigin(++nextDataCell); bindings.Heap[origin.Cell] = NewDataCell(data, location);
                    body = RunDataConstructor(constructor, origin, bindings, state => DemandPresent(ConstructedClassValue(data, ReadDataOrigin(origin, state, location), location), location));
                }
                var inputs = constructor.Parameters.Select(parameter => new Input<Unit, IrType<Unit>>(Name.From(parameter.Name), Type(parameter.Type, Location(parameter)), Type(parameter.Type, Location(parameter)))).ToArray();
                if (receiverName is not null) inputs = new[] { new Input<Unit, IrType<Unit>>(receiverName, ValueInterfaceIrType(constructor.ContainingType, true), ValueInterfaceIrType(constructor.ContainingType, true)) }.Concat(inputs).ToArray();
                inputs = DefaultWitnessInputs(constructor).Concat(inputs).ToArray();
                var id = Identity(constructor); EnsureModule(id.Module, Visibility(constructor.ContainingType));
                modules[id.Module.Key].Values.Add(new(id.Local, new(constructor.IsImplicitlyDeclared ? Access.Private : Visibility(constructor), new(Documentation(constructor), new(inputs, body.Attributes, body))))); emittedDataConstructors.Add(key);
            }
            finally { emittingMethods.Remove(key); }
        }
        private TypedValue RunDataConstructor(IMethodSymbol constructor, DataOrigin origin, BindingEnvironment environment, Func<BindingEnvironment, TypedValue> continuation)
        {
            var key = SymbolId(constructor); var syntax = constructor.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax()).OfType<ConstructorDeclarationSyntax>().SingleOrDefault(); var location = syntax?.GetLocation() ?? Location(constructor.ContainingType)!; var operation = syntax is null ? null : (IConstructorBodyOperation)Semantic(syntax.SyntaxTree).GetOperation(syntax)!;
            var previousScope = CaptureGenericScope(); var witnesses = DefaultWitnessArguments(constructor, location);
            genericDefaultValues = new(previousScope.Defaults, SymbolEqualityComparer.Default); foreach (var pair in GenericDefaults(constructor).Zip(witnesses)) genericDefaultValues[pair.First] = pair.Second;
            genericNewValues = new(previousScope.News, SymbolEqualityComparer.Default); foreach (var pair in GenericNews(constructor).Zip(witnesses.Skip(GenericDefaults(constructor).Length))) genericNewValues[pair.First] = pair.Second;
            genericAbsenceValues = new(previousScope.Absences, SymbolEqualityComparer.Default); foreach (var pair in GenericAbsences(constructor).Zip(witnesses.Skip(GenericDefaults(constructor).Length + GenericNews(constructor).Length))) genericAbsenceValues[pair.First] = pair.Second;
            genericDispatchValues = new(previousScope.Dispatches); foreach (var pair in GenericDispatches(constructor).Zip(witnesses.Skip(GenericDefaults(constructor).Length + GenericNews(constructor).Length + GenericAbsences(constructor).Length))) genericDispatchValues[GenericDispatchKey(pair.First)] = pair.Second;
            genericFactoryValues = FactoryWitnessBindings(constructor, witnesses); activeGenericTypes = GenericContext(constructor, previousScope);
            var previousReceiver = currentReceiver; var previousOrigin = currentDataConstructionOrigin; var previousReturn = currentVoidReturn; var previousMethod = currentMethod; var previousBindings = bindings;
            currentDataConstructionOrigin = origin; currentReceiver = environment.Heap[origin.Cell].Current; currentMethod = key; bindings = environment; calls.TryAdd(key, []);
            TypedValue Finish(BindingEnvironment state)
            {
                var activeScope = CaptureGenericScope(); var receiver = currentReceiver; var method = currentMethod; var activeOrigin = currentDataConstructionOrigin; var activeReturn = currentVoidReturn; var activeBindings = bindings;
                RestoreGenericScope(previousScope); currentReceiver = previousReceiver; currentMethod = previousMethod; currentDataConstructionOrigin = previousOrigin; currentVoidReturn = previousReturn; bindings = previousBindings;
                try { return continuation(state); }
                finally { RestoreGenericScope(activeScope); currentReceiver = receiver; currentMethod = method; currentDataConstructionOrigin = activeOrigin; currentVoidReturn = activeReturn; bindings = activeBindings; }
            }
            currentVoidReturn = Finish;
            try
            {
                var body = operation?.BlockBody ?? operation?.ExpressionBody;
                if (body is not null) ValidateBlockOperations(body);
                var initializerArguments = ConstructorInitializer(constructor)?.Arguments ?? [];
                foreach (var argument in initializerArguments) ValidateBlockOperations(argument.Value);
                TypedValue Body(BindingEnvironment state) => body is null ? Finish(state) : Statements(body.Operations, 0, state, Finish);
                TypedValue BaseBody(BindingEnvironment state)
                {
                    if (ConstructorInitializer(constructor) is { } baseCall)
                    {
                        if (SymbolId(baseCall.TargetMethod.ContainingType) == SymbolId(constructor.ContainingType)) return Body(state);
                        if (baseCall.TargetMethod.ContainingType.SpecialType == SpecialType.System_Object && baseCall.Arguments.Length == 0) return Body(state);
                        if (BaseConstructor(baseCall.TargetMethod))
                        {
                            if (dataConstructors.ContainsKey(SymbolId(baseCall.TargetMethod))) calls[key].Add((SymbolId(baseCall.TargetMethod), baseCall.Syntax.GetLocation()));
                            return BindConstructorArguments(baseCall.Arguments.ToArray(), baseCall.TargetMethod, state, next => RunDataConstructor(baseCall.TargetMethod, origin, next, Body));
                        }
                        return Fail<TypedValue>("MCS3005", "Base construction requires a selected source storage/dispatch representation.", baseCall.Syntax.GetLocation());
                    }
                    if (ImplicitBaseTarget(constructor) is { } implicitBase)
                    {
                        if (dataConstructors.ContainsKey(SymbolId(implicitBase))) calls[key].Add((SymbolId(implicitBase), location));
                        return BindImplicitBaseArguments(implicitBase, state, next => RunDataConstructor(implicitBase, origin, next, Body));
                    }
                    return Body(state);
                }
                TypedValue Initializers(BindingEnvironment state) => RecordCopyShape(constructor) ? Body(state) : RunDataInitializers(constructor, origin, state, BaseBody);
                TypedValue result;
                if (operation?.Initializer is IExpressionStatementOperation { Operation: IInvocationOperation initializer })
                {
                    if (SymbolId(initializer.TargetMethod.ContainingType) == SymbolId(constructor.ContainingType))
                    {
                        if (initializer.TargetMethod.IsImplicitlyDeclared) result = Initializers(environment);
                        else if (dataConstructors.TryGetValue(SymbolId(initializer.TargetMethod), out var target)) { calls[key].Add((SymbolId(target), initializer.Syntax.GetLocation())); result = BindConstructorArguments(initializer.Arguments.ToArray(), initializer.TargetMethod, environment, state => RunDataConstructor(initializer.TargetMethod, origin, state, Body)); }
                        else return Fail<TypedValue>("MCS3001", "Constructor chaining requires a selected supported constructor.", initializer.Syntax.GetLocation());
                    }
                    else result = Initializers(environment);
                }
                else result = InitializePrimaryCaptures(constructor, origin, environment, Initializers);
                if (body is not null) ValidateUnreachedArithmetic(body);
                foreach (var argument in initializerArguments) ValidateUnreachedArithmetic(argument.Value);
                return result;
            }
            finally { RestoreGenericScope(previousScope); currentReceiver = previousReceiver; currentDataConstructionOrigin = previousOrigin; currentVoidReturn = previousReturn; currentMethod = previousMethod; bindings = previousBindings; }
        }
        private TypedValue BindConstructorArguments(IArgumentOperation[] arguments, IMethodSymbol constructor, BindingEnvironment environment, Func<BindingEnvironment, TypedValue> continuation)
        {
            var values = new Dictionary<int, TypedValue>();
            TypedValue Bind(int index, BindingEnvironment state)
            {
                if (index == constructor.Parameters.Length) return continuation(state);
                var parameter = constructor.Parameters[index]; var previous = bindings; bindings = state; TypedValue value;
                try { value = DataArgument(values[index], Location(parameter) ?? Microsoft.CodeAnalysis.Location.None); } finally { bindings = previous; }
                return BindValue(parameter.OriginalDefinition, parameter.Type, value, state, next => Bind(index + 1, next), Location(parameter) ?? Microsoft.CodeAnalysis.Location.None);
            }
            TypedValue Evaluate(int index, BindingEnvironment state) => index == arguments.Length ? Bind(0, state) : Expression(arguments[index].Value, state, (value, next) => { values[arguments[index].Parameter!.Ordinal] = value; return Evaluate(index + 1, next); });
            return Evaluate(0, environment);
        }
        private TypedValue? ConstructUserData(IObjectCreationOperation creation, DataLayout data, BindingEnvironment environment, Func<TypedValue, BindingEnvironment, TypedValue> continuation)
        {
            if (creation.Constructor is null || !dataConstructors.TryGetValue(SymbolId(creation.Constructor), out var constructor)) return null;
            constructor = creation.Constructor;
            var location = creation.Syntax.GetLocation(); calls.TryAdd(currentMethod, []); calls[currentMethod].Add((SymbolId(constructor), location));
            return BindConstructorArguments(creation.Arguments.ToArray(), constructor, environment, state =>
            {
                var origin = new DataOrigin(++nextDataCell); state.Heap[origin.Cell] = NewDataCell(data, location);
                return RunDataConstructor(constructor, origin, state, next =>
                {
                    return RunDataObjectInitializer(creation.Initializer, origin, next, updated => { provedDataOperations.Add(creation); var value = ReadDataOrigin(origin, updated, location); if (data.Type.IsValueType) dataOrigins.Remove(value); return continuation(ConstructedClassValue(data, value, location), updated); });
                });
            });
        }
    }
}
