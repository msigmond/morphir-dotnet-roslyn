using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Morphir.IR.Classic;
using TypedValue = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        private sealed record GenericDispatch(ITypeParameterSymbol Parameter, IMethodSymbol Method, bool View = false);
        private Dictionary<string, TypedValue> genericDispatchValues = [];
        private readonly Dictionary<string, GenericDispatch[]> genericDispatches = [];
        private readonly HashSet<string> findingGenericDispatches = [];
        private static string GenericParameterIdentity(ITypeParameterSymbol parameter) => SymbolId(parameter.ContainingSymbol) + "::parameter:" + parameter.TypeParameterKind + ":" + parameter.Ordinal;
        private static string GenericDispatchKey(GenericDispatch requirement) => GenericParameterIdentity(requirement.Parameter) + "::dispatch:" + SymbolId(requirement.Method) + GenericDispatchTypeKey(requirement.Method.ContainingType) + "<" + string.Join(";", requirement.Method.TypeArguments.Select(GenericDispatchTypeKey)) + ">" + (requirement.View ? "::view" : "");
        private GenericDispatch[] GenericDispatches(IMethodSymbol method)
        {
            method = method.OriginalDefinition; var key = SymbolId(method); if (genericDispatches.TryGetValue(key, out var found)) return found; if (!findingGenericDispatches.Add(key)) return [];
            try
            {
                var requirements = new Dictionary<string, GenericDispatch>();
                void Require(ITypeSymbol type, IMethodSymbol member, bool view) { if (type is ITypeParameterSymbol parameter) { var requirement = new GenericDispatch(parameter, member, view); requirements[GenericDispatchKey(requirement)] = requirement; } }
                void RequireCreation(ITypeSymbol type)
                {
                    if (ZeroDataConstructor(type) is { } constructor && dataConstructors.ContainsKey(SymbolId(constructor))) foreach (var required in GenericDispatches(constructor)) Require(SubstituteType(required.Parameter, TypeSubstitutions(constructor)), SubstituteDispatchMethod(required.Method, TypeSubstitutions(constructor)), required.View);
                }
                foreach (var target in InterfaceTargets(method).Concat(ConstructorTargets(method))) foreach (var required in GenericDispatches(target)) Require(SubstituteType(required.Parameter, TypeSubstitutions(target)), SubstituteDispatchMethod(required.Method, TypeSubstitutions(target)), required.View);
                var operations = GenericOperations(method);
                foreach (var operation in operations)
                {
                    if (operation is IInvocationOperation { Instance.Type: ITypeParameterSymbol receiver } invocation && ValueInterfaceType(invocation.TargetMethod.ContainingType)) Require(receiver, invocation.TargetMethod, !receiver.HasValueTypeConstraint);
                    if (operation is IInvocationOperation { ConstrainedToType: ITypeParameterSymbol staticReceiver, TargetMethod.IsStatic: true } staticCall && ValueInterfaceType(staticCall.TargetMethod.ContainingType)) Require(staticReceiver, staticCall.TargetMethod, false);
                    if (operation is IMethodReferenceOperation { ConstrainedToType: ITypeParameterSymbol staticReferenceReceiver, Method.IsStatic: true } staticReference && ValueInterfaceType(staticReference.Method.ContainingType)) Require(staticReferenceReceiver, staticReference.Method, false);
                    if (operation is IPropertyReferenceOperation { ConstrainedToType: ITypeParameterSymbol propertyReceiver, Property.GetMethod: { IsStatic: true } getter } property && ValueInterfaceType(property.Property.ContainingType)) Require(propertyReceiver, getter, false);
                    if (operation is IPropertyReferenceOperation { Instance.Type: ITypeParameterSymbol instanceReceiver, Property.GetMethod: { IsStatic: false } instanceGetter } && InterfaceDispatchMethod(instanceGetter)) Require(instanceReceiver, instanceGetter, !instanceReceiver.HasValueTypeConstraint);
                    if (GenericRequirementTarget(operation) is { } target && SourceGenericCallable(target))
                    {
                        foreach (var required in GenericDispatches(target)) Require(SubstituteType(required.Parameter, TypeSubstitutions(target)), SubstituteDispatchMethod(required.Method, TypeSubstitutions(target)), required.View);
                        foreach (var required in GenericNews(target)) RequireCreation(SubstituteType(required, TypeSubstitutions(target)));
                    }
                }
                found = requirements.Values.OrderBy(requirement => TypeParameterName(requirement.Parameter).Key, StringComparer.Ordinal).ThenBy(requirement => SymbolId(requirement.Method), StringComparer.Ordinal).ThenBy(requirement => requirement.View).ToArray(); genericDispatches[key] = found; return found;
            }
            finally { findingGenericDispatches.Remove(key); }
        }
        private Name DispatchWitnessName(IMethodSymbol method, GenericDispatch requirement)
        {
            string Stem(GenericDispatch item) => (item.View ? "view_" : "call_") + TypeParameterName(item.Parameter).Key + "_" + SourceOwnerName(item.Method.ContainingType) + "_" + Identity(item.Method).Local.Key;
            var stem = Stem(requirement); var siblings = GenericDispatches(method).Where(item => Stem(item) == stem).OrderBy(GenericDispatchKey, StringComparer.Ordinal).ToArray();
            if (siblings.Length > 1) stem += "_contract_" + Array.FindIndex(siblings, item => GenericDispatchKey(item) == GenericDispatchKey(requirement));
            var name = Name.From(stem); var index = 0;
            while (method.Parameters.Any(parameter => Name.From(parameter.Name).Key == name.Key) || ReceiverRequired(method) && ReceiverName(method).Key == name.Key) name = Name.From(stem + "_" + (++index)); return name;
        }
        private IrType<Unit> DispatchWitnessType(ITypeSymbol receiver, IMethodSymbol contract, Location? location, bool view = false)
        {
            if (contract.IsStatic) return StaticDispatchSignature(contract, location);
            if (view) return Types.Function(Type(receiver, location), ValueInterfaceIrType(contract.ContainingType));
            IrType<Unit> signature = Type(contract.ReturnType, location); foreach (var parameter in contract.Parameters.Reverse()) signature = Types.Function(Type(parameter.Type, location), signature); return Types.Function(Type(receiver, location), signature);
        }
        private Input<Unit, IrType<Unit>>[] DispatchWitnessInputs(IMethodSymbol method) => GenericDispatches(method).Select(requirement => { var type = DispatchWitnessType(requirement.Parameter, requirement.Method, Location(method), requirement.View); return new Input<Unit, IrType<Unit>>(DispatchWitnessName(method, requirement), type, type); }).ToArray();
        private void PrepareDispatchWitnesses(IMethodSymbol method)
        {
            genericDispatchValues = []; foreach (var requirement in GenericDispatches(method)) { var name = DispatchWitnessName(method, requirement); bindingNames.Add(name.Key); genericDispatchValues[GenericDispatchKey(requirement)] = new VariableValue<Unit, IrType<Unit>>(DispatchWitnessType(requirement.Parameter, requirement.Method, Location(method), requirement.View), name); }
        }
        private TypedValue[] DispatchWitnessArguments(IMethodSymbol method, Location location) => GenericDispatches(method).Select(requirement => DispatchWitness(SubstituteType(requirement.Parameter, TypeSubstitutions(method)), SubstituteDispatchMethod(requirement.Method, TypeSubstitutions(method)), location, requirement.View)).ToArray();
        private TypedValue DispatchWitness(ITypeSymbol receiver, IMethodSymbol contract, Location location, bool view = false)
        {
            if (GenericArgument(receiver, out var actual, out var scope)) { var resolved = ResolveDispatchMethod(contract); return WithGenericScope(scope, () => DispatchWitness(actual, resolved, location, view)); }
            if (receiver is ITypeParameterSymbol receiverParameter) return genericDispatchValues.TryGetValue(GenericDispatchKey(new(receiverParameter, contract, view)), out var witness) ? witness : Fail<TypedValue>("MCS3103", "Constrained dispatch requires its explicit receiver/member witness.", location);
            if (contract.IsStatic) return StaticDispatchWitness(receiver, contract, location);
            if (view) return ConcreteDispatchView(receiver, contract, location);
            if (receiver is not INamedTypeSymbol { IsValueType: true } concrete || !ValueInterfaceType(contract.ContainingType) || !ValueAlternatives(contract.ContainingType).Any(alternative => SameRuntimeType(alternative, concrete))) return Fail<TypedValue>("MCS3111", "The constrained target is outside the admitted closed implementation set.", location);
            var implementation = ValueImplementation(concrete, contract) ?? Fail<IMethodSymbol>("MCS3112", "No resolved constrained implementation exists.", location);
            var defaultBody = ValueInterfaceType(implementation.ContainingType); var output = defaultBody ? InterfaceDefaultOutput(implementation, location) : CallableOutput(implementation, location); IrType<Unit> signature = output; foreach (var parameter in implementation.Parameters.Reverse()) signature = Types.Function(Type(parameter.Type, location), signature); signature = Types.Function(defaultBody ? ValueInterfaceIrType(implementation.ContainingType, true) : Type(concrete, location), signature);
            var defaults = DefaultWitnessArguments(implementation, location); foreach (var argument in defaults.Reverse()) signature = Types.Function(argument.Attributes, signature);
            var arguments = new List<(Name Name, IrType<Unit> Type)> { (Fresh("dispatchReceiver"), Type(concrete, location)) }; arguments.AddRange(contract.Parameters.Select(parameter => (Fresh("dispatchArgument" + parameter.Ordinal), Type(parameter.Type, location))));
            TypedValue body = new ReferenceValue<Unit, IrType<Unit>>(signature, defaultBody ? InterfaceDefaultIdentity(implementation) : Identity(implementation)); foreach (var argument in defaults) body = new ApplyValue<Unit, IrType<Unit>>(((FunctionType<Unit>)body.Attributes).Result, body, argument); foreach (var argument in arguments)
            {
                TypedValue value = new VariableValue<Unit, IrType<Unit>>(argument.Type, argument.Name);
                if (defaultBody && argument.Name.Key == arguments[0].Name.Key) value = ((ApplyValue<Unit, IrType<Unit>>)InterfaceValue(implementation.ContainingType, concrete, value)).Argument;
                body = new ApplyValue<Unit, IrType<Unit>>(((FunctionType<Unit>)body.Attributes).Result, body, value);
            }
            body = Adapt(body, Type(contract.ReturnType, location));
            foreach (var argument in arguments.AsEnumerable().Reverse()) body = new LambdaValue<Unit, IrType<Unit>>(Types.Function(argument.Type, body.Attributes), new AsPattern<IrType<Unit>>(argument.Type, new WildcardPattern<IrType<Unit>>(argument.Type), argument.Name), body); return body;
        }
        private bool GenericDispatchCall(IInvocationOperation invocation) => (invocation.Instance?.Type is ITypeParameterSymbol || invocation.ConstrainedToType is ITypeParameterSymbol && invocation.TargetMethod.IsStatic) && ValueInterfaceType(invocation.TargetMethod.ContainingType);
        private TypedValue? LowerGenericDispatch(IOperation operation)
        {
            if (operation is IPropertyReferenceOperation property && GenericStaticProperty(property)) return StaticDispatchProperty(property);
            if (operation is not IInvocationOperation invocation || !GenericDispatchCall(invocation)) return null;
            if (invocation.TargetMethod.IsStatic) return StaticDispatchCall(invocation);
            var location = operation.Syntax.GetLocation(); ValidateInvocation(invocation); var receiverType = invocation.Instance!.Type!; var receiver = Lower(invocation.Instance); TypedValue function;
            if (receiverType is ITypeParameterSymbol { HasValueTypeConstraint: false })
            {
                receiver = DemandPresent(ApplyDispatchView(receiverType, invocation.TargetMethod, receiver, location), location);
                IrType<Unit> signature = Type(invocation.TargetMethod.ReturnType, location);
                foreach (var parameter in invocation.TargetMethod.Parameters.Reverse()) signature = Types.Function(Type(parameter.Type, location), signature);
                signature = Types.Function(receiver.Attributes, signature); var defaults = DefaultWitnessArguments(invocation.TargetMethod, location); foreach (var argument in defaults.Reverse()) signature = Types.Function(argument.Attributes, signature);
                function = new ReferenceValue<Unit, IrType<Unit>>(signature, Identity(invocation.TargetMethod)); foreach (var argument in defaults) function = new ApplyValue<Unit, IrType<Unit>>(((FunctionType<Unit>)function.Attributes).Result, function, argument);
            }
            else function = DispatchWitness(receiverType, invocation.TargetMethod, location);
            function = new ApplyValue<Unit, IrType<Unit>>(((FunctionType<Unit>)function.Attributes).Result, function, receiver);
            foreach (var argument in invocation.Arguments.OrderBy(argument => argument.Parameter!.Ordinal)) function = new ApplyValue<Unit, IrType<Unit>>(((FunctionType<Unit>)function.Attributes).Result, function, Adapt(DataArgument(Lower(argument.Value), location), Type(argument.Parameter!.Type, location))); return function;
        }
    }
}
