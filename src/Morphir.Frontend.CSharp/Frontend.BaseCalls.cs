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
        private bool DirectBaseMember(IOperation operation) => InstanceInterfaceMember(operation) is { Instance.Syntax: BaseExpressionSyntax } member && member.Method.ContainingType.TypeKind == TypeKind.Class && ValueInterfaceType(member.Method.ContainingType);
        private bool DirectBaseCall(IInvocationOperation call) => DirectBaseMember(call);
        private void EmitBaseDependencies(IOperation root)
        {
            foreach (var operation in Operations(root).Where(DirectBaseMember)) EmitMethod(InstanceInterfaceMember(operation)!.Value.Method, true);
        }
        private TypedValue BaseReceiver(IMethodSymbol method, Location location)
        {
            const string suffix = "::default-body";
            var key = currentMethod.EndsWith(suffix, StringComparison.Ordinal) ? currentMethod[..^suffix.Length] : currentMethod;
            var callable = sourceMethods.GetValueOrDefault(key) ?? dataConstructors.GetValueOrDefault(key) ?? dataSetters.GetValueOrDefault(key);
            if (callable is null || currentReceiver is null) return Fail<TypedValue>("MCS3005", "The base-body receiver has no acquired source owner.", location);
            var source = (INamedTypeSymbol)ResolveRuntimeType(callable.ContainingType);
            var value = currentDataConstructionOrigin is { } origin ? DemandPresent(ReadDataOrigin(origin, bindings, location), location) : DemandPresent(DataArgument(currentReceiver, location), location);
            return ValueInterfaceType(source) ? MapInterfacePresent(source, method.ContainingType, value, location) : DemandPresent(InterfaceValue(method.ContainingType, source, value), location);
        }
        private TypedValue? LowerBaseCall(IOperation operation)
        {
            if (!DirectBaseMember(operation)) return null;
            var member = InstanceInterfaceMember(operation)!.Value;
            var target = member.Method; var location = operation.Syntax.GetLocation();
            if (operation is IInvocationOperation call) ValidateInvocation(call); else calls[currentMethod].Add((SymbolId(target), location));
            IrType<Unit> signature = InterfaceDefaultOutput(target, location);
            foreach (var parameter in target.Parameters.Reverse()) signature = Types.Function(Type(parameter.Type, location), signature);
            signature = Types.Function(ValueInterfaceIrType(target.ContainingType, true), signature);
            var witnesses = DefaultWitnessArguments(target, location); foreach (var witness in witnesses.Reverse()) signature = Types.Function(witness.Attributes, signature);
            TypedValue body = new ReferenceValue<Unit, IrType<Unit>>(signature, InterfaceDefaultIdentity(target));
            foreach (var witness in witnesses) body = new ApplyValue<Unit, IrType<Unit>>(((FunctionType<Unit>)body.Attributes).Result, body, witness);
            body = new ApplyValue<Unit, IrType<Unit>>(((FunctionType<Unit>)body.Attributes).Result, body, BaseReceiver(target, location));
            foreach (var argument in member.Arguments.OrderBy(argument => argument.Parameter!.Ordinal)) body = new ApplyValue<Unit, IrType<Unit>>(((FunctionType<Unit>)body.Attributes).Result, body, Adapt(DataArgument(Lower(argument.Value), location), Type(argument.Parameter!.Type, location)));
            return body;
        }
        private bool BaseReferenceCall(IOperation operation) => DirectBaseMember(operation) && DataReferenceResult(operation.Type!);
        private TypedValue? BaseReferenceExpression(IOperation operation, BindingEnvironment environment, Func<TypedValue, BindingEnvironment, TypedValue> continuation)
        {
            if (!BaseReferenceCall(operation)) return null;
            var member = InstanceInterfaceMember(operation)!.Value;
            var target = member.Method; var location = operation.Syntax.GetLocation();
            if (operation is IInvocationOperation call) ValidateInvocation(call); else calls[currentMethod].Add((SymbolId(target), location));
            var values = new Dictionary<int, TypedValue>();
            TypedValue Bind(int index, BindingEnvironment state)
            {
                var previous = bindings; bindings = state; TypedValue value;
                try { value = index == target.Parameters.Length ? BaseReceiver(target, location) : DataArgument(values[index], location); }
                finally { bindings = previous; }
                if (index == target.Parameters.Length) return RunDataCallable(target, value, state, continuation);
                var parameter = target.Parameters[index]; return BindValue(parameter.OriginalDefinition, parameter.Type, value, state, next => Bind(index + 1, next), location);
            }
            TypedValue Evaluate(int index, BindingEnvironment state) => index == member.Arguments.Length ? Bind(0, state) : Expression(member.Arguments[index].Value, state, (value, next) => { values[member.Arguments[index].Parameter!.Ordinal] = value; return Evaluate(index + 1, next); });
            return Evaluate(0, environment);
        }
    }
}
