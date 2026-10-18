using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Morphir.IR.Classic;
using TypedValue = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        private Func<TypedValue, BindingEnvironment, TypedValue>? currentDataReturn;
        private readonly HashSet<string> activeDataCalls = [];
        private IMethodSymbol? DataCallTarget(IOperation operation) => operation switch
        {
            IInvocationOperation invocation when sourceMethods.ContainsKey(SymbolId(invocation.TargetMethod)) && !InterfaceDispatchMethod(invocation.TargetMethod) && DataReferenceResult(invocation.TargetMethod.ReturnType) => invocation.TargetMethod,
            IPropertyReferenceOperation property when DataGetter(property) && !InterfaceDispatchMethod(property.Property.GetMethod!) && DataReferenceResult(property.Property.Type) => property.Property.GetMethod,
            _ => DataOperator(operation) is { } method && DataReferenceResult(method.ReturnType) ? method : null
        };
        private TypedValue? DataCallExpression(IOperation operation, BindingEnvironment environment, Func<TypedValue, BindingEnvironment, TypedValue> continuation)
        {
            if (DataCallTarget(operation) is not { } method) return null;
            var location = operation.Syntax.GetLocation();
            IOperation? instance = operation switch { IInvocationOperation invocation => invocation.Instance, IPropertyReferenceOperation property => property.Instance, _ => null };
            (IOperation Value, int Ordinal)[] arguments = operation switch { IInvocationOperation invocation => invocation.Arguments.Select(argument => (argument.Value, argument.Parameter!.Ordinal)).ToArray(), IPropertyReferenceOperation property => property.Arguments.Select(argument => (argument.Value, argument.Parameter!.Ordinal)).ToArray(), IBinaryOperation binary => [(binary.LeftOperand, 0), (binary.RightOperand, 1)], IUnaryOperation unary => [(unary.Operand, 0)], IConversionOperation conversion => [(conversion.Operand, 0)], _ => [] };
            if (operation is IInvocationOperation invocationOperation) ValidateInvocation(invocationOperation);
            else { calls.TryAdd(currentMethod, []); calls[currentMethod].Add((SymbolId(method), location)); }
            TypedValue? receiver = null; var values = new Dictionary<int, TypedValue>();
            TypedValue Bind(int index, BindingEnvironment state)
            {
                if (index == method.Parameters.Length)
                {
                    var previous = bindings; bindings = state; TypedValue? presentReceiver;
                    try { presentReceiver = ReceiverRequired(method) ? DemandPresent(DataArgument(receiver!, location), location) : null; }
                    finally { bindings = previous; }
                    return RunDataCallable(method, presentReceiver, state, continuation);
                }
                var parameter = method.Parameters[index]; var saved = bindings; bindings = state; TypedValue value;
                try { value = DataArgument(values[index], location); } finally { bindings = saved; }
                return BindValue(parameter.OriginalDefinition, parameter.Type, value, state, next => Bind(index + 1, next), location);
            }
            TypedValue Evaluate(int index, BindingEnvironment state) => index == arguments.Length ? Bind(0, state) : Expression(arguments[index].Value, state, (value, next) => { values[arguments[index].Ordinal] = value; return Evaluate(index + 1, next); });
            return instance is null ? Evaluate(0, environment) : Expression(instance, environment, (value, state) => { receiver = value; return Evaluate(0, state); });
        }
        private TypedValue RunDataCallable(IMethodSymbol method, TypedValue? receiver, BindingEnvironment environment, Func<TypedValue, BindingEnvironment, TypedValue> continuation)
        {
            if (AutomaticGetter(method))
            {
                var saved = bindings; bindings = environment;
                try { return continuation(ReadAutomaticGetter(method, receiver!, Location(method)!), environment); } finally { bindings = saved; }
            }
            var key = InterfaceDispatchMethod(method) ? InterfaceDefaultKey(method) : SymbolId(method); if (!activeDataCalls.Add(key)) return Fail<TypedValue>("MCS2203", "Recursive data calls require D14.", Location(method));
            var syntax = method.DeclaringSyntaxReferences.Single().GetSyntax(); var root = CallableOperation(Semantic(syntax.SyntaxTree), syntax);
            var body = root switch { IMethodBodyOperation callable => callable.BlockBody ?? callable.ExpressionBody!, IBlockOperation block => block, ILocalFunctionOperation local => local.Body!, _ => Fail<IBlockOperation>("MCS3001", "Source data call requires a validated body.", syntax.GetLocation()) };
            var callerScope = CaptureGenericScope(); var callOutput = InterfaceDispatchMethod(method) ? InterfaceDefaultOutput(method, syntax.GetLocation()) : CallableOutput(method, syntax.GetLocation());
            var previousDispatches = genericDispatchValues; var previousAbsences = genericAbsenceValues; var previousNews = genericNewValues; var previousDefaults = genericDefaultValues; var requiredDefaults = GenericDefaults(method); var defaultArguments = DefaultWitnessArguments(method, syntax.GetLocation());
            genericDefaultValues = new(previousDefaults, SymbolEqualityComparer.Default); foreach (var pair in requiredDefaults.Zip(defaultArguments)) genericDefaultValues[pair.First] = pair.Second;
            genericNewValues = new(previousNews, SymbolEqualityComparer.Default); foreach (var pair in GenericNews(method).Zip(defaultArguments.Skip(requiredDefaults.Length))) genericNewValues[pair.First] = pair.Second;
            genericAbsenceValues = new(previousAbsences, SymbolEqualityComparer.Default); foreach (var pair in GenericAbsences(method).Zip(defaultArguments.Skip(requiredDefaults.Length + GenericNews(method).Length))) genericAbsenceValues[pair.First] = pair.Second;
            genericDispatchValues = new(previousDispatches); foreach (var pair in GenericDispatches(method).Zip(defaultArguments.Skip(requiredDefaults.Length + GenericNews(method).Length + GenericAbsences(method).Length))) genericDispatchValues[GenericDispatchKey(pair.First)] = pair.Second;
            genericFactoryValues = FactoryWitnessBindings(method, defaultArguments);
            var previousScopes = activeGenericScopes; var previousGenerics = activeGenericTypes; activeGenericTypes = GenericContext(method, callerScope);
            var previousReceiver = currentReceiver; var previousOrigin = currentDataConstructionOrigin; var previousVoidReturn = currentVoidReturn; var previousReturn = currentDataReturn; var previousMethod = currentMethod; var previousBindings = bindings; var previousSlot = currentReturnSlot; var previousOperands = operandValues;
            currentReceiver = receiver; currentDataConstructionOrigin = null; currentVoidReturn = null; currentMethod = key; bindings = environment; currentReturnSlot = callOutput; operandValues = [];
            TypedValue Finish(TypedValue value, BindingEnvironment state)
            {
                var activeFactories = genericFactoryValues; var activeDispatches = genericDispatchValues; var activeAbsences = genericAbsenceValues; var activeNews = genericNewValues; var activeDefaults = genericDefaultValues; var activeScopes = activeGenericScopes; var activeGenerics = activeGenericTypes;
                var activeReceiver = currentReceiver; var activeOrigin = currentDataConstructionOrigin; var activeVoidReturn = currentVoidReturn; var activeReturn = currentDataReturn; var activeMethod = currentMethod; var activeBindings = bindings; var activeSlot = currentReturnSlot; var activeOperands = operandValues;
                genericFactoryValues = callerScope.Factories; genericDispatchValues = previousDispatches; genericAbsenceValues = previousAbsences; genericNewValues = previousNews; genericDefaultValues = previousDefaults; activeGenericScopes = previousScopes; activeGenericTypes = previousGenerics; currentReceiver = previousReceiver; currentDataConstructionOrigin = previousOrigin; currentVoidReturn = previousVoidReturn; currentDataReturn = previousReturn; currentMethod = previousMethod; bindings = previousBindings; currentReturnSlot = previousSlot; operandValues = previousOperands; activeDataCalls.Remove(key);
                try { return continuation(value, state); }
                finally { activeDataCalls.Add(key); genericFactoryValues = activeFactories; genericDispatchValues = activeDispatches; genericAbsenceValues = activeAbsences; genericNewValues = activeNews; genericDefaultValues = activeDefaults; activeGenericScopes = activeScopes; activeGenericTypes = activeGenerics; currentReceiver = activeReceiver; currentDataConstructionOrigin = activeOrigin; currentVoidReturn = activeVoidReturn; currentDataReturn = activeReturn; currentMethod = activeMethod; bindings = activeBindings; currentReturnSlot = activeSlot; operandValues = activeOperands; }
            }
            currentDataReturn = Finish;
            try { return Statements(body.Operations, 0, environment, _ => Fail<TypedValue>("MCS2402", "Source data call has an unproved return path.", syntax.GetLocation())); }
            finally { activeDataCalls.Remove(key); genericFactoryValues = callerScope.Factories; genericDispatchValues = previousDispatches; genericAbsenceValues = previousAbsences; genericNewValues = previousNews; genericDefaultValues = previousDefaults; activeGenericScopes = previousScopes; activeGenericTypes = previousGenerics; currentReceiver = previousReceiver; currentDataConstructionOrigin = previousOrigin; currentVoidReturn = previousVoidReturn; currentDataReturn = previousReturn; currentMethod = previousMethod; bindings = previousBindings; currentReturnSlot = previousSlot; operandValues = previousOperands; }
        }
    }
}
