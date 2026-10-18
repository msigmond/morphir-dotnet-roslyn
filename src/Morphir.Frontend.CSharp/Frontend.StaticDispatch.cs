using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Morphir.IR.Classic;
using TypedValue = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        private IrType<Unit> StaticDispatchSignature(IMethodSymbol method, Location? location)
        {
            IrType<Unit> result = Type(method.ReturnType, location);
            if (method.Parameters.Length == 0) return Types.Function(Types.Unit, result);
            foreach (var parameter in method.Parameters.Reverse()) result = Types.Function(Type(parameter.Type, location), result);
            return result;
        }
        private void EmitStaticInterfaceContract(IMethodSymbol method)
        {
            ValidateOrdinaryGenerics(method.TypeParameters);
            var key = SymbolId(method); calls.TryAdd(key, []);
            foreach (var alternative in valueInterfaces[SymbolId(method.ContainingType)].Alternatives)
            {
                var target = ValueImplementation(alternative, method)!; var defaultBody = InterfaceDispatchMethod(target);
                EmitMethod(target, defaultBody); calls[key].Add((defaultBody ? InterfaceDefaultKey(target) : SymbolId(target), Location(method)!));
            }
            var name = Name.From("implementation"); var version = 0;
            while (method.Parameters.Any(parameter => Name.From(parameter.Name).Key == name.Key)) name = Name.From("implementation_" + (++version));
            var signature = StaticDispatchSignature(method, Location(method)); var output = Type(method.ReturnType, Location(method));
            var inputs = new[] { new Input<Unit, IrType<Unit>>(name, signature, signature) }.Concat(method.Parameters.Select(parameter => new Input<Unit, IrType<Unit>>(Name.From(parameter.Name), Type(parameter.Type, Location(parameter)), Type(parameter.Type, Location(parameter))))).ToArray();
            TypedValue body = new VariableValue<Unit, IrType<Unit>>(signature, name);
            if (method.Parameters.Length == 0) body = new ApplyValue<Unit, IrType<Unit>>(output, body, new UnitValue<Unit, IrType<Unit>>(Types.Unit));
            else foreach (var parameter in method.Parameters) body = new ApplyValue<Unit, IrType<Unit>>(((FunctionType<Unit>)body.Attributes).Result, body, new VariableValue<Unit, IrType<Unit>>(Type(parameter.Type, Location(parameter)), Name.From(parameter.Name)));
            var id = Identity(method); EnsureModule(id.Module, Visibility(method.ContainingType)); methodOutputs[key] = output;
            modules[id.Module.Key].Values.Add(new(id.Local, new(Visibility(method), new(Documentation(method.AssociatedSymbol ?? method), new(inputs, output, body)))));
        }
        private TypedValue StaticDispatchWitness(ITypeSymbol receiver, IMethodSymbol contract, Location location)
        {
            if (receiver is not INamedTypeSymbol concrete || !ValueAlternatives(contract.ContainingType).Any(alternative => SameRuntimeType(alternative, concrete))) return Fail<TypedValue>("MCS3111", "The static constrained implementation is outside the acquired source closure.", location);
            var target = ValueImplementation(concrete, contract) ?? Fail<IMethodSymbol>("MCS3112", "The static constrained member has no resolved source implementation.", location);
            var defaultBody = InterfaceDispatchMethod(target); var output = defaultBody ? InterfaceDefaultOutput(target, location) : CallableOutput(target, location);
            IrType<Unit> signature = output; foreach (var parameter in target.Parameters.Reverse()) signature = Types.Function(Type(parameter.Type, location), signature);
            var defaults = DefaultWitnessArguments(target, location); foreach (var witness in defaults.Reverse()) signature = Types.Function(witness.Attributes, signature);
            TypedValue body = new ReferenceValue<Unit, IrType<Unit>>(signature, defaultBody ? InterfaceDefaultIdentity(target) : Identity(target));
            foreach (var witness in defaults) body = new ApplyValue<Unit, IrType<Unit>>(((FunctionType<Unit>)body.Attributes).Result, body, witness);
            var parameters = target.Parameters.Select(parameter => (Name: Fresh("staticArgument" + parameter.Ordinal), Type: Type(parameter.Type, location))).ToArray();
            foreach (var parameter in parameters) body = new ApplyValue<Unit, IrType<Unit>>(((FunctionType<Unit>)body.Attributes).Result, body, new VariableValue<Unit, IrType<Unit>>(parameter.Type, parameter.Name));
            body = Adapt(body, Type(contract.ReturnType, location));
            if (parameters.Length == 0) return new LambdaValue<Unit, IrType<Unit>>(Types.Function(Types.Unit, body.Attributes), new UnitPattern<IrType<Unit>>(Types.Unit), body);
            foreach (var parameter in parameters.Reverse()) body = new LambdaValue<Unit, IrType<Unit>>(Types.Function(parameter.Type, body.Attributes), new AsPattern<IrType<Unit>>(parameter.Type, new WildcardPattern<IrType<Unit>>(parameter.Type), parameter.Name), body);
            return body;
        }
        private void ValidateStaticDispatchResult(ITypeSymbol type, Location location)
        {
            bool HasVariable(ITypeSymbol current) => current is ITypeParameterSymbol || current is INamedTypeSymbol named && SourceTypeArguments(named).Any(HasVariable);
            if (HasVariable(type)) Fail<object>("MCS3103", "Unresolved static dispatch results require their representation and source ownership continuation mapping.", location);
        }
        private TypedValue StaticDispatchCall(IInvocationOperation call)
        {
            var location = call.Syntax.GetLocation(); ValidateInvocation(call); ValidateStaticDispatchResult(call.TargetMethod.ReturnType, location);
            BorrowUnknownDataResult(call.TargetMethod.ReturnType, call.Arguments.Select(argument => Lower(argument.Value)));
            var function = DispatchWitness(call.ConstrainedToType!, call.TargetMethod, location);
            if (call.Arguments.Length == 0) return new ApplyValue<Unit, IrType<Unit>>(((FunctionType<Unit>)function.Attributes).Result, function, new UnitValue<Unit, IrType<Unit>>(Types.Unit));
            foreach (var argument in call.Arguments.OrderBy(argument => argument.Parameter!.Ordinal)) function = new ApplyValue<Unit, IrType<Unit>>(((FunctionType<Unit>)function.Attributes).Result, function, Adapt(DataArgument(Lower(argument.Value), location), Type(argument.Parameter!.Type, location)));
            return function;
        }
        private TypedValue StaticDispatchReference(ITypeSymbol receiver, IMethodSymbol method, Location location)
        {
            ValidateStaticDispatchResult(method.ReturnType, location); calls[currentMethod].Add((SymbolId(method), location));
            var function = DispatchWitness(receiver, method, location);
            if (method.Parameters.Length == 0) return new LambdaValue<Unit, IrType<Unit>>(function.Attributes, new UnitPattern<IrType<Unit>>(Types.Unit), new ApplyValue<Unit, IrType<Unit>>(((FunctionType<Unit>)function.Attributes).Result, function, new UnitValue<Unit, IrType<Unit>>(Types.Unit)));
            var parameters = method.Parameters.Select(parameter => (Name: Fresh("staticMethodArgument" + parameter.Ordinal), Type: Type(parameter.Type, location))).ToArray();
            TypedValue body = function; foreach (var parameter in parameters) body = new ApplyValue<Unit, IrType<Unit>>(((FunctionType<Unit>)body.Attributes).Result, body, new VariableValue<Unit, IrType<Unit>>(parameter.Type, parameter.Name));
            foreach (var parameter in parameters.Reverse()) body = new LambdaValue<Unit, IrType<Unit>>(Types.Function(parameter.Type, body.Attributes), new AsPattern<IrType<Unit>>(parameter.Type, new WildcardPattern<IrType<Unit>>(parameter.Type), parameter.Name), body);
            return body;
        }
    }
}
