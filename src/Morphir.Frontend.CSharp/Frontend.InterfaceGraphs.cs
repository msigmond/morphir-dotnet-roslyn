using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Morphir.IR.Classic;
using TypedValue = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        private sealed record InterfaceGraph(INamedTypeSymbol Contract, INamedTypeSymbol Alternative, TypedValue Payload, bool Optional);
        private readonly Dictionary<TypedValue, InterfaceGraph> interfaceGraphs = new(ReferenceEqualityComparer.Instance);
        private TypedValue? KnownDataPresence(TypedValue value)
        {
            var type = MaybeElement(value.Attributes);
            if (type is not ReferenceType<Unit> reference || !(DataLayoutForIr(type)?.Type.IsReferenceType == true || valueInterfaces.Values.Any(contract => FunctionOwner(Identity(contract.Type)) == FunctionOwner(reference.Name)))) return null;
            var seen = new HashSet<TypedValue>(ReferenceEqualityComparer.Instance);
            TypedValue? Follow(TypedValue candidate)
            {
                if (!seen.Add(candidate)) return null;
                if (candidate is ConstructorValue<Unit, IrType<Unit>> { Name.Local.Key: "nothing" } || candidate is ApplyValue<Unit, IrType<Unit>> { Function: ConstructorValue<Unit, IrType<Unit>> { Name.Local.Key: "just" } }) return candidate;
                if (candidate is VariableValue<Unit, IrType<Unit>> variable && boundValues.TryGetValue(variable.Name.Key, out var bound)) return Follow(bound);
                return dataProjections.TryGetValue(candidate, out var projected) ? Follow(projected) : null;
            }
            return Follow(value);
        }
        private IrType<Unit> InterfacePayloadType(INamedTypeSymbol alternative) => ConcreteClassDomain(alternative) ? DataPayloadType(ResolvedDataLayout(alternative)) : alternative.IsReferenceType ? DataIrType(alternative, true) : Type(alternative, Location(alternative));
        private void RefineInterfacePresence(TypedValue value, TypedValue present)
        {
            if (InterfaceGraphOf(value) is { } graph) interfaceGraphs[present] = graph with { Optional = false };
        }
        private InterfaceGraph? InterfaceGraphOf(TypedValue value)
        {
            if (interfaceGraphs.TryGetValue(value, out var graph)) return graph;
            if (dataProjections.TryGetValue(value, out var projected)) return InterfaceGraphOf(projected);
            if (value is VariableValue<Unit, IrType<Unit>> variable && boundValues.TryGetValue(variable.Name.Key, out var bound)) return InterfaceGraphOf(bound);
            return null;
        }
        private TypedValue MaterializeInterfaceGraph(InterfaceGraph graph, BindingEnvironment environment, Location location, HashSet<int> visiting)
        {
            var payload = MaterializeDataValue(graph.Payload, environment, location, visiting);
            if (graph.Alternative.IsReferenceType && MaybeElement(payload.Attributes) is not null) payload = DemandPresent(payload, location);
            var result = InterfaceValue(graph.Contract, graph.Alternative, payload);
            return graph.Optional ? result : ((ApplyValue<Unit, IrType<Unit>>)result).Argument;
        }
        private TypedValue BoxInterfaceValue(INamedTypeSymbol contract, INamedTypeSymbol alternative, TypedValue value)
        {
            return MaybeElement(value.Attributes) is null ? InterfaceValue(contract, alternative, value) : MatchMaybe(value, () => Nothing(ValueInterfaceIrType(contract)), present => InterfaceValue(contract, alternative, present));
        }
        private TypedValue InterfaceView(ITypeSymbol type, IMethodSymbol contract, TypedValue value, Location location)
        {
            if (GenericArgument(type, out var actual, out var scope)) { var resolved = ResolveDispatchMethod(contract); return WithGenericScope(scope, () => InterfaceView(actual, resolved, value, location)); }
            if (type is ITypeParameterSymbol) return ApplyDispatchView(type, contract, value, location);
            if (type is INamedTypeSymbol named && ValueInterfaceType(named))
            {
                if (SameRuntimeType(named, contract.ContainingType)) return value;
                return MatchMaybe(value, () => Nothing(ValueInterfaceIrType(contract.ContainingType)), present => Just(MapInterfacePresent(named, contract.ContainingType, present, location)));
            }
            if (type is INamedTypeSymbol alternative && ValueAlternatives(contract.ContainingType).Any(candidate => SameRuntimeType(candidate, alternative))) return BoxInterfaceValue(contract.ContainingType, alternative, value);
            return Fail<TypedValue>("MCS3111", "No acquired runtime implementation matches this interface receiver.", location);
        }
        private (IMethodSymbol Method, IOperation Instance, IArgumentOperation[] Arguments)? InstanceInterfaceMember(IOperation operation) => operation switch
        {
            IInvocationOperation { Instance: not null } invocation => (invocation.TargetMethod, invocation.Instance, invocation.Arguments.ToArray()),
            IPropertyReferenceOperation { Instance: not null } property when DataGetter(property) => (property.Property.GetMethod!, property.Instance, property.Arguments.ToArray()),
            _ => null
        };
        private bool InterfaceReferenceCall(IOperation operation) => InstanceInterfaceMember(operation) is { } member && !member.Method.IsStatic && !DirectBaseMember(operation) && InterfaceDispatchMethod(member.Method) && DataReferenceResult(operation.Type!, currentDataConstructionOrigin is not null) && !(member.Instance.Type is ITypeParameterSymbol parameter && ResolveGenericParameter(parameter) is ITypeParameterSymbol);
        private TypedValue? InterfaceCallExpression(IOperation operation, BindingEnvironment environment, Func<TypedValue, BindingEnvironment, TypedValue> continuation)
        {
            if (!InterfaceReferenceCall(operation)) return null;
            var member = InstanceInterfaceMember(operation)!.Value; var method = member.Method; var location = operation.Syntax.GetLocation();
            if (operation is IInvocationOperation invocation) ValidateInvocation(invocation); else calls[currentMethod].Add((SymbolId(method), location));
            var arguments = new Dictionary<int, TypedValue>();
            return Expression(member.Instance, environment, (receiver, state) =>
            {
                TypedValue Evaluate(int index, BindingEnvironment next)
                {
                    if (index < member.Arguments.Length)
                    {
                        var argument = member.Arguments[index];
                        return Expression(argument.Value, next, (value, after) => { arguments[argument.Parameter!.Ordinal] = value; return Evaluate(index + 1, after); });
                    }
                    var previous = bindings; bindings = next;
                    try
                    {
                        var view = InterfaceView(member.Instance.Type!, method, receiver, location);
                        var present = DemandPresent(DataArgument(view, location), location);
                        return MatchInterface(present, method.ContainingType, (alternative, payload) =>
                        {
                            var target = ValueImplementation(alternative, method) ?? Fail<IMethodSymbol>("MCS3112", "No resolved interface target exists.", location);
                            TypedValue Bind(int parameterIndex, BindingEnvironment current)
                            {
                                if (parameterIndex == target.Parameters.Length)
                                {
                                    var targetReceiver = ValueInterfaceType(target.ContainingType) ? ((ApplyValue<Unit, IrType<Unit>>)InterfaceValue(target.ContainingType, alternative, payload)).Argument : payload;
                                    return RunDataCallable(target, targetReceiver, current, continuation);
                                }
                                var saved = bindings; bindings = current; TypedValue value;
                                try { value = DataArgument(arguments[parameterIndex], location); } finally { bindings = saved; }
                                var parameter = target.Parameters[parameterIndex];
                                return BindValue(parameter.OriginalDefinition, parameter.Type, value, current, after => Bind(parameterIndex + 1, after), location);
                            }
                            return Bind(0, Copy(next));
                        });
                    }
                    finally { bindings = previous; }
                }
                return Evaluate(0, state);
            });
        }
    }
}
