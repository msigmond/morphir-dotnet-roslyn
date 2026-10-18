using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Morphir.IR.Classic;
using TypedValue = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        private Dictionary<ITypeParameterSymbol, TypedValue> genericNewValues = new(SymbolEqualityComparer.Default);
        private readonly Dictionary<string, ITypeParameterSymbol[]> genericNews = [];
        private readonly HashSet<string> findingGenericNews = [];
        private IMethodSymbol? ZeroDataConstructor(ITypeSymbol type) => type is INamedTypeSymbol named && DataType(named) ? named.InstanceConstructors.FirstOrDefault(constructor => constructor.Parameters.Length == 0) : null;
        private ITypeParameterSymbol[] GenericNews(IMethodSymbol method)
        {
            method = method.OriginalDefinition; var key = SymbolId(method); if (genericNews.TryGetValue(key, out var found)) return found; if (!findingGenericNews.Add(key)) return [];
            try
            {
                var parameters = new HashSet<ITypeParameterSymbol>(SymbolEqualityComparer.Default);
                void Require(ITypeSymbol type)
                {
                    if (type is ITypeParameterSymbol parameter) { parameters.Add(parameter); return; }
                    if (ZeroDataConstructor(type) is { } constructor && dataConstructors.ContainsKey(SymbolId(constructor))) foreach (var required in GenericNews(constructor)) Require(SubstituteType(required, TypeSubstitutions(constructor)));
                }
                foreach (var target in InterfaceTargets(method).Concat(ConstructorTargets(method))) foreach (var required in GenericNews(target)) Require(SubstituteType(required, TypeSubstitutions(target)));
                var operations = GenericOperations(method);
                foreach (var operation in operations)
                {
                    if (operation is ITypeParameterObjectCreationOperation && operation.Type is { } type) Require(type);
                    var target = GenericRequirementTarget(operation);
                    if (target is not null && SourceGenericCallable(target)) foreach (var required in GenericNews(target)) Require(SubstituteType(required, TypeSubstitutions(target)));
                }
                found = parameters.OrderBy(parameter => TypeParameterName(parameter).Key, StringComparer.Ordinal).ToArray(); genericNews[key] = found; return found;
            }
            finally { findingGenericNews.Remove(key); }
        }
        private IMethodSymbol? GenericRequirementTarget(IOperation operation) => operation switch
        {
            IInvocationOperation call => call.TargetMethod,
            IMethodReferenceOperation reference => reference.Method,
            IObjectCreationOperation creation => creation.Constructor,
            IPropertyReferenceOperation property when property.Parent is ISimpleAssignmentOperation assignment && ReferenceEquals(assignment.Target, property) && DataSetter(property) => property.Property.SetMethod,
            IPropertyReferenceOperation property when DataGetter(property) => property.Property.GetMethod,
            _ => null
        };
        private bool SourceGenericCallable(IMethodSymbol method) => sourceMethods.ContainsKey(SymbolId(method)) || dataConstructors.ContainsKey(SymbolId(method)) || dataSetters.ContainsKey(SymbolId(method));
        private Name NewWitnessName(IMethodSymbol method, ITypeParameterSymbol parameter)
        {
            var name = Name.From("new_" + TypeParameterName(parameter).Key); var index = 0;
            while (method.Parameters.Any(input => Name.From(input.Name).Key == name.Key) || ReceiverRequired(method) && ReceiverName(method).Key == name.Key) name = Name.From("new_" + TypeParameterName(parameter).Key + "_" + (++index)); return name;
        }
        private Input<Unit, IrType<Unit>>[] NewWitnessInputs(IMethodSymbol method) => GenericNews(method).Select(parameter => { var type = Types.Function(Types.Unit, Type(parameter, Location(method))); return new Input<Unit, IrType<Unit>>(NewWitnessName(method, parameter), type, type); }).ToArray();
        private void PrepareNewWitnesses(IMethodSymbol method)
        {
            genericNewValues = new(SymbolEqualityComparer.Default);
            foreach (var parameter in GenericNews(method)) { var name = NewWitnessName(method, parameter); bindingNames.Add(name.Key); genericNewValues[parameter] = new VariableValue<Unit, IrType<Unit>>(Types.Function(Types.Unit, Type(parameter, Location(method))), name); }
        }
        private TypedValue[] NewWitnessArguments(IMethodSymbol method, Location location) => GenericNews(method).Select(parameter => NewWitness(SubstituteType(parameter, TypeSubstitutions(method)), location)).ToArray();
        private TypedValue NewWitness(ITypeSymbol type, Location location)
        {
            if (GenericArgument(type, out var actual, out var scope)) return WithGenericScope(scope, () => NewWitness(actual, location));
            if (type is ITypeParameterSymbol parameter) return genericNewValues.TryGetValue(parameter, out var witness) ? witness : Fail<TypedValue>("MCS3105", "Generic construction requires its explicit creation witness.", location);
            TypedValue body;
            if (type is INamedTypeSymbol named && DataType(named))
            {
                var constructor = ZeroDataConstructor(named) ?? Fail<IMethodSymbol>("MCS3105", "No resolved parameterless data constructor exists.", location);
                if (dataConstructors.ContainsKey(SymbolId(constructor)))
                {
                    if (!emittedDataConstructors.Contains(SymbolId(constructor))) return Fail<TypedValue>("MCS3105", "Creation witness requires a validated constructor dependency.", location);
                    var arguments = DefaultWitnessArguments(constructor, location); IrType<Unit> signature = DataIrType(named, true); foreach (var argument in arguments.Reverse()) signature = Types.Function(argument.Attributes, signature);
                    body = new ReferenceValue<Unit, IrType<Unit>>(signature, Identity(constructor)); foreach (var argument in arguments) body = new ApplyValue<Unit, IrType<Unit>>(((FunctionType<Unit>)body.Attributes).Result, body, argument);
                }
                else body = DataRecord(ResolvedDataLayout(named), location);
                if (named.IsReferenceType) body = Just(body);
            }
            else body = Default(type, location);
            return new LambdaValue<Unit, IrType<Unit>>(Types.Function(Types.Unit, body.Attributes), new UnitPattern<IrType<Unit>>(Types.Unit), body);
        }
        private TypedValue? LowerGenericNew(IOperation operation)
        {
            if (operation is not ITypeParameterObjectCreationOperation || operation.Type is null) return null;
            var witness = NewWitness(operation.Type, operation.Syntax.GetLocation()); return new ApplyValue<Unit, IrType<Unit>>(((FunctionType<Unit>)witness.Attributes).Result, witness, new UnitValue<Unit, IrType<Unit>>(Types.Unit));
        }
        private void EmitNewWitnessDependencies(IOperation root)
        {
            void Require(ITypeSymbol type)
            {
                if (ZeroDataConstructor(type) is { } constructor && dataConstructors.TryGetValue(SymbolId(constructor), out var definition)) EmitDataConstructor(definition);
            }
            foreach (var operation in OwnOperations(root)) if (GenericRequirementTarget(operation) is { } target && SourceGenericCallable(target)) foreach (var required in GenericNews(target)) Require(SubstituteType(required, TypeSubstitutions(target)));
        }
        private TypedValue? GenericNewExpression(IOperation operation, BindingEnvironment environment, Func<TypedValue, BindingEnvironment, TypedValue> continuation)
        {
            if (operation is not ITypeParameterObjectCreationOperation || operation.Type is null) return null;
            return GenericNewFromType(operation.Type, operation.Syntax.GetLocation(), environment, continuation);
        }
        private TypedValue? GenericNewFromType(ITypeSymbol type, Location location, BindingEnvironment environment, Func<TypedValue, BindingEnvironment, TypedValue> continuation)
        {
            if (GenericArgument(type, out var actual, out var scope))
            {
                var caller = CaptureGenericScope();
                return WithGenericScope(scope, () => GenericNewFromType(actual, location, environment, (value, state) => WithGenericScope(caller, () => continuation(value, state))));
            }
            if (type is not INamedTypeSymbol named || !DataType(named)) return null;
            var data = ResolvedDataLayout(named); var origin = new DataOrigin(++nextDataCell); environment.Heap[origin.Cell] = NewDataCell(data, location);
            TypedValue Finish(BindingEnvironment state) { var value = ReadDataOrigin(origin, state, location); if (named.IsValueType) dataOrigins.Remove(value); return continuation(value, state); }
            // This is ownership analysis of a concrete creation witness. The
            // generic definition invokes its witness input, not its own source
            // constructor; finite nested type arguments must not add a false
            // source-recursion edge. Constructor dependencies validate bodies.
            if (ZeroDataConstructor(named) is { } constructor && dataConstructors.ContainsKey(SymbolId(constructor))) return RunDataConstructor(constructor, origin, environment, Finish);
            return Finish(environment);
        }
    }
}
