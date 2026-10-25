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
        private void RegisterDataConstructors(IEnumerable<INamedTypeSymbol> selected)
        {
            foreach (var type in selected.Where(DataType))
                foreach (var constructor in type.InstanceConstructors.Where(constructor => PrimaryDataConstructor(constructor) || !constructor.IsImplicitlyDeclared && constructor.DeclaringSyntaxReferences.Any(reference => reference.GetSyntax() is ConstructorDeclarationSyntax) || constructor.IsImplicitlyDeclared && !RecordCopyConstructor(constructor) && type.IsReferenceType && HasDataInitializers(type)))
                {
                    AddSymbol(constructor, type.ToDisplayString(), "New" + SignatureSuffix(constructor)); dataConstructors[SymbolId(constructor)] = constructor; if (!constructor.IsImplicitlyDeclared && !PrimaryDataConstructor(constructor)) RegisterLocalMethods(constructor);
                }
        }
        private TypedValue DataReceiverValue(Location location)
        {
            if (currentDataConstructionOrigin is not { } origin) return currentReceiver!;
            var value = DemandPresent(ReadDataOrigin(origin, bindings, location), location);
            if (bindings.Heap[origin.Cell].Type.IsReferenceType) dataOrigins[value] = origin;
            return value;
        }
        private void EmitDataConstructor(IMethodSymbol constructor)
        {
            var key = SymbolId(constructor); if (emittedDataConstructors.Contains(key)) return;
            if (!emittingMethods.Add(key)) Fail<object>("MCS2203", "Recursive construction/calls require D14.", Location(constructor));
            try
            {
                var syntax = constructor.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax()).OfType<ConstructorDeclarationSyntax>().SingleOrDefault(); var location = syntax?.GetLocation() ?? Location(constructor.ContainingType)!; var root = syntax is null ? null : (IConstructorBodyOperation)Semantic(syntax.SyntaxTree).GetOperation(syntax)!;
                if (root is not null) EmitDataOperatorDependencies(root); foreach (var initializer in DataInitializers(dataTypes[SymbolId(constructor.ContainingType)])) EmitDataOperatorDependencies(initializer.Value);
                foreach (var call in (root is null ? Enumerable.Empty<IOperation>() : Operations(root)).Concat(DataInitializers(dataTypes[SymbolId(constructor.ContainingType)]).SelectMany(initializer => Operations(initializer.Value))).OfType<IInvocationOperation>())
                {
                    if (dataConstructors.TryGetValue(SymbolId(call.TargetMethod), out var chained)) EmitDataConstructor(chained);
                    else if (sourceMethods.TryGetValue(SymbolId(call.TargetMethod), out var method)) EmitMethod(method);
                }
                foreach (var creation in (root is null ? Enumerable.Empty<IOperation>() : Operations(root)).Concat(DataInitializers(dataTypes[SymbolId(constructor.ContainingType)]).SelectMany(initializer => Operations(initializer.Value))).OfType<IObjectCreationOperation>()) if (creation.Constructor is not null && dataConstructors.TryGetValue(SymbolId(creation.Constructor), out var dependency)) EmitDataConstructor(dependency);
                foreach (var property in (root is null ? Enumerable.Empty<IOperation>() : Operations(root)).Concat(DataInitializers(dataTypes[SymbolId(constructor.ContainingType)]).SelectMany(initializer => Operations(initializer.Value))).OfType<IPropertyReferenceOperation>().Where(DataGetter)) EmitMethod(dataGetters[SymbolId(property.Property.GetMethod!)]);
                foreach (var property in (root is null ? Enumerable.Empty<IOperation>() : Operations(root)).Concat(DataInitializers(dataTypes[SymbolId(constructor.ContainingType)]).SelectMany(initializer => Operations(initializer.Value))).OfType<IPropertyReferenceOperation>().Where(DataSetter)) EmitDataSetter(dataSetters[SymbolId(property.Property.SetMethod!)]);
                if (constructor.Parameters.Any(parameter => parameter.RefKind != RefKind.None || parameter.GetAttributes().Length != 0) || constructor.GetAttributes().Length != 0) Fail<object>("MCS3001", "Constructors require resolved by-value parameters without unmapped attributes.", Location(constructor));
                PrepareBindings(constructor); currentMethod = key; calls.TryAdd(key, []); var data = dataTypes[SymbolId(constructor.ContainingType)];
                var origin = new DataOrigin(++nextDataCell); bindings.Heap[origin.Cell] = NewDataCell(data, location);
                var body = RunDataConstructor(constructor, origin, bindings, state => DemandPresent(ReadDataOrigin(origin, state, location), location));
                var inputs = constructor.Parameters.Select(parameter => new Input<Unit, IrType<Unit>>(Name.From(parameter.Name), Type(parameter.Type, Location(parameter)), Type(parameter.Type, Location(parameter)))).ToArray();
                var id = Identity(constructor); EnsureModule(id.Module, Visibility(constructor.ContainingType));
                modules[id.Module.Key].Values.Add(new(id.Local, new(constructor.IsImplicitlyDeclared ? Access.Private : Visibility(constructor), new(Documentation(constructor), new(inputs, body.Attributes, body))))); emittedDataConstructors.Add(key);
            }
            finally { emittingMethods.Remove(key); }
        }
        private TypedValue RunDataConstructor(IMethodSymbol constructor, DataOrigin origin, BindingEnvironment environment, Func<BindingEnvironment, TypedValue> continuation)
        {
            var key = SymbolId(constructor); var syntax = constructor.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax()).OfType<ConstructorDeclarationSyntax>().SingleOrDefault(); var location = syntax?.GetLocation() ?? Location(constructor.ContainingType)!; var operation = syntax is null ? null : (IConstructorBodyOperation)Semantic(syntax.SyntaxTree).GetOperation(syntax)!;
            var previousReceiver = currentReceiver; var previousOrigin = currentDataConstructionOrigin; var previousReturn = currentVoidReturn; var previousMethod = currentMethod; var previousBindings = bindings;
            currentDataConstructionOrigin = origin; currentReceiver = DataRecord(dataTypes[SymbolId(constructor.ContainingType)], location); currentMethod = key; bindings = environment; calls.TryAdd(key, []);
            TypedValue Finish(BindingEnvironment state)
            {
                var receiver = currentReceiver; var method = currentMethod; var activeOrigin = currentDataConstructionOrigin; var activeReturn = currentVoidReturn; var activeBindings = bindings;
                currentReceiver = previousReceiver; currentMethod = previousMethod; currentDataConstructionOrigin = previousOrigin; currentVoidReturn = previousReturn; bindings = previousBindings;
                try { return continuation(state); }
                finally { currentReceiver = receiver; currentMethod = method; currentDataConstructionOrigin = activeOrigin; currentVoidReturn = activeReturn; bindings = activeBindings; }
            }
            currentVoidReturn = Finish;
            try
            {
                var body = operation?.BlockBody ?? operation?.ExpressionBody;
                if (body is not null) ValidateBlockOperations(body);
                TypedValue Body(BindingEnvironment state) => body is null ? Finish(state) : Statements(body.Operations, 0, state, Finish);
                TypedValue Initializers(BindingEnvironment state) => RecordCopyShape(constructor) ? Body(state) : RunDataInitializers(constructor, origin, state, Body);
                TypedValue result;
                if (operation?.Initializer is IExpressionStatementOperation { Operation: IInvocationOperation initializer })
                {
                    if (SymbolEqualityComparer.Default.Equals(initializer.TargetMethod.ContainingType, constructor.ContainingType))
                    {
                        if (initializer.TargetMethod.IsImplicitlyDeclared) result = Initializers(environment);
                        else if (dataConstructors.TryGetValue(SymbolId(initializer.TargetMethod), out var target)) { calls[key].Add((SymbolId(target), initializer.Syntax.GetLocation())); result = BindConstructorArguments(initializer.Arguments.ToArray(), target, environment, state => RunDataConstructor(target, origin, state, Body)); }
                        else return Fail<TypedValue>("MCS3001", "Constructor chaining requires a selected supported constructor.", initializer.Syntax.GetLocation());
                    }
                    else if (initializer.TargetMethod.ContainingType.SpecialType == SpecialType.System_Object && initializer.Arguments.Length == 0) result = Initializers(environment);
                    else return Fail<TypedValue>("MCS3005", "Base construction requires the D10 inheritance mapping.", initializer.Syntax.GetLocation());
                }
                else result = InitializePrimaryCaptures(constructor, origin, environment, Initializers);
                if (body is not null) ValidateUnreachedArithmetic(body);
                return result;
            }
            finally { currentReceiver = previousReceiver; currentDataConstructionOrigin = previousOrigin; currentVoidReturn = previousReturn; currentMethod = previousMethod; bindings = previousBindings; }
        }
        private TypedValue BindConstructorArguments(IArgumentOperation[] arguments, IMethodSymbol constructor, BindingEnvironment environment, Func<BindingEnvironment, TypedValue> continuation)
        {
            var values = new Dictionary<int, TypedValue>();
            TypedValue Bind(int index, BindingEnvironment state)
            {
                if (index == constructor.Parameters.Length) return continuation(state);
                var parameter = constructor.Parameters[index]; var previous = bindings; bindings = state; TypedValue value;
                try { value = DataArgument(values[index], Location(parameter) ?? Microsoft.CodeAnalysis.Location.None); } finally { bindings = previous; }
                return BindValue(parameter, parameter.Type, value, state, next => Bind(index + 1, next), Location(parameter) ?? Microsoft.CodeAnalysis.Location.None);
            }
            TypedValue Evaluate(int index, BindingEnvironment state) => index == arguments.Length ? Bind(0, state) : Expression(arguments[index].Value, state, (value, next) => { values[arguments[index].Parameter!.Ordinal] = value; return Evaluate(index + 1, next); });
            return Evaluate(0, environment);
        }
        private TypedValue? ConstructUserData(IObjectCreationOperation creation, DataLayout data, BindingEnvironment environment, Func<TypedValue, BindingEnvironment, TypedValue> continuation)
        {
            if (creation.Constructor is null || !dataConstructors.TryGetValue(SymbolId(creation.Constructor), out var constructor)) return null;
            var location = creation.Syntax.GetLocation(); calls.TryAdd(currentMethod, []); calls[currentMethod].Add((SymbolId(constructor), location));
            return BindConstructorArguments(creation.Arguments.ToArray(), constructor, environment, state =>
            {
                var origin = new DataOrigin(++nextDataCell); state.Heap[origin.Cell] = NewDataCell(data, location);
                return RunDataConstructor(constructor, origin, state, next =>
                {
                    return RunDataObjectInitializer(creation.Initializer, origin, next, updated => { provedDataOperations.Add(creation); var value = ReadDataOrigin(origin, updated, location); if (data.Type.IsValueType) dataOrigins.Remove(value); return continuation(value, updated); });
                });
            });
        }
    }
}
