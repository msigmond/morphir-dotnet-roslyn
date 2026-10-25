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
            IInvocationOperation invocation when sourceMethods.ContainsKey(SymbolId(invocation.TargetMethod)) && DataReferenceResult(invocation.TargetMethod.ReturnType) => invocation.TargetMethod,
            IPropertyReferenceOperation property when DataGetter(property) && DataReferenceResult(property.Property.Type) => property.Property.GetMethod,
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
                return BindValue(parameter, parameter.Type, value, state, next => Bind(index + 1, next), location);
            }
            TypedValue Evaluate(int index, BindingEnvironment state) => index == arguments.Length ? Bind(0, state) : Expression(arguments[index].Value, state, (value, next) => { values[arguments[index].Ordinal] = value; return Evaluate(index + 1, next); });
            return instance is null ? Evaluate(0, environment) : Expression(instance, environment, (value, state) => { receiver = value; return Evaluate(0, state); });
        }
        private TypedValue RunDataCallable(IMethodSymbol method, TypedValue? receiver, BindingEnvironment environment, Func<TypedValue, BindingEnvironment, TypedValue> continuation)
        {
            var key = SymbolId(method); if (!activeDataCalls.Add(key)) return Fail<TypedValue>("MCS2203", "Recursive data calls require D14.", Location(method));
            var syntax = method.DeclaringSyntaxReferences.Single().GetSyntax(); var root = CallableOperation(Semantic(syntax.SyntaxTree), syntax);
            var body = root switch { IMethodBodyOperation callable => callable.BlockBody ?? callable.ExpressionBody!, IBlockOperation block => block, ILocalFunctionOperation local => local.Body!, _ => Fail<IBlockOperation>("MCS3001", "Source data call requires a validated body.", syntax.GetLocation()) };
            var previousReceiver = currentReceiver; var previousOrigin = currentDataConstructionOrigin; var previousVoidReturn = currentVoidReturn; var previousReturn = currentDataReturn; var previousMethod = currentMethod; var previousBindings = bindings; var previousSlot = currentReturnSlot; var previousOperands = operandValues;
            currentReceiver = receiver; currentDataConstructionOrigin = null; currentVoidReturn = null; currentMethod = key; bindings = environment; currentReturnSlot = CallableOutput(method, syntax.GetLocation()); operandValues = [];
            TypedValue Finish(TypedValue value, BindingEnvironment state)
            {
                var activeReceiver = currentReceiver; var activeOrigin = currentDataConstructionOrigin; var activeVoidReturn = currentVoidReturn; var activeReturn = currentDataReturn; var activeMethod = currentMethod; var activeBindings = bindings; var activeSlot = currentReturnSlot; var activeOperands = operandValues;
                currentReceiver = previousReceiver; currentDataConstructionOrigin = previousOrigin; currentVoidReturn = previousVoidReturn; currentDataReturn = previousReturn; currentMethod = previousMethod; bindings = previousBindings; currentReturnSlot = previousSlot; operandValues = previousOperands; activeDataCalls.Remove(key);
                try { return continuation(value, state); }
                finally { activeDataCalls.Add(key); currentReceiver = activeReceiver; currentDataConstructionOrigin = activeOrigin; currentVoidReturn = activeVoidReturn; currentDataReturn = activeReturn; currentMethod = activeMethod; bindings = activeBindings; currentReturnSlot = activeSlot; operandValues = activeOperands; }
            }
            currentDataReturn = Finish;
            try { return Statements(body.Operations, 0, environment, _ => Fail<TypedValue>("MCS2402", "Source data call has an unproved return path.", syntax.GetLocation())); }
            finally { activeDataCalls.Remove(key); currentReceiver = previousReceiver; currentDataConstructionOrigin = previousOrigin; currentVoidReturn = previousVoidReturn; currentDataReturn = previousReturn; currentMethod = previousMethod; bindings = previousBindings; currentReturnSlot = previousSlot; operandValues = previousOperands; }
        }
    }
}
