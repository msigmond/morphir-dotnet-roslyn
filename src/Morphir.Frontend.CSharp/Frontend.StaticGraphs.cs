using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Morphir.IR.Classic;
using TypedValue = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        private IMethodSymbol? StaticReferenceTarget(IOperation operation)
        {
            var pair = operation switch
            {
                IInvocationOperation call when call.TargetMethod.IsStatic => (Method: call.TargetMethod, Type: call.ConstrainedToType),
                IPropertyReferenceOperation { Property.GetMethod: { IsStatic: true } getter } property => (Method: getter, Type: property.ConstrainedToType),
                _ => (Method: (IMethodSymbol?)null, Type: (ITypeSymbol?)null)
            };
            if (pair.Method is null || pair.Type is null || !InterfaceDispatchMethod(pair.Method) || !DataReferenceResult(pair.Method.ReturnType) || ResolveGenericParameter(pair.Type) is not INamedTypeSymbol concrete) return null;
            return ValueImplementation(concrete, pair.Method) ?? Fail<IMethodSymbol>("MCS3112", "The static reference result has no acquired resolved target.", operation.Syntax.GetLocation());
        }
        private TypedValue? StaticReferenceExpression(IOperation operation, BindingEnvironment environment, Func<TypedValue, BindingEnvironment, TypedValue> continuation)
        {
            if (StaticReferenceTarget(operation) is not { } target) return null;
            var location = operation.Syntax.GetLocation();
            if (operation is IInvocationOperation call) ValidateInvocation(call);
            else calls[currentMethod].Add((SymbolId(((IPropertyReferenceOperation)operation).Property.GetMethod!), location));
            var arguments = operation is IInvocationOperation invocation ? invocation.Arguments.ToArray() : [];
            var values = new Dictionary<int, TypedValue>();
            TypedValue Bind(int index, BindingEnvironment state)
            {
                if (index == target.Parameters.Length) return RunDataCallable(target, null, state, continuation);
                var previous = bindings; bindings = state; TypedValue value;
                try { value = DataArgument(values[index], location); } finally { bindings = previous; }
                var parameter = target.Parameters[index];
                return BindValue(parameter.OriginalDefinition, parameter.Type, value, state, next => Bind(index + 1, next), location);
            }
            TypedValue Evaluate(int index, BindingEnvironment state) => index == arguments.Length ? Bind(0, state) : Expression(arguments[index].Value, state, (value, next) => { values[arguments[index].Parameter!.Ordinal] = value; return Evaluate(index + 1, next); });
            return Evaluate(0, environment);
        }
    }
}
