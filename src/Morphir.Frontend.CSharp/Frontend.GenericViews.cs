using Microsoft.CodeAnalysis;
using Morphir.IR.Classic;
using TypedValue = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        // A view is total and preserves absence. A null guard matches this same
        // view, so dispatch extracts only a branch-proved present interface value.
        // The original argument remains t, including any actual Maybe nesting.
        private TypedValue ConcreteDispatchView(ITypeSymbol receiver, IMethodSymbol contract, Location location)
        {
            var input = Type(receiver, location); var name = Fresh("viewReceiver");
            var value = new VariableValue<Unit, IrType<Unit>>(input, name);
            var output = ValueInterfaceIrType(contract.ContainingType);
            TypedValue body;
            if (receiver is INamedTypeSymbol source && ValueInterfaceType(source)) body = SameRuntimeType(source, contract.ContainingType) ? value : MatchMaybe(value, () => Nothing(output), present => Just(MapInterfacePresent(source, contract.ContainingType, present, location)));
            else
            {
                var actual = NullableElement(receiver) ?? receiver;
                if (actual is not INamedTypeSymbol concrete || !ValueAlternatives(contract.ContainingType).Any(alternative => SameRuntimeType(alternative, concrete))) return Fail<TypedValue>("MCS3111", "The generic view target is outside the acquired implementation closure.", location);
                TypedValue Wrap(TypedValue present)
                {
                    return InterfaceValue(contract.ContainingType, concrete, present);
                }
                body = MaybeElement(input) is null ? Wrap(value) : MatchMaybe(value, () => Nothing(output), Wrap);
            }
            return new LambdaValue<Unit, IrType<Unit>>(Types.Function(input, output), new AsPattern<IrType<Unit>>(input, new WildcardPattern<IrType<Unit>>(input), name), body);
        }
        private TypedValue ApplyDispatchView(ITypeSymbol receiver, IMethodSymbol contract, TypedValue value, Location location)
        {
            if (GenericArgument(receiver, out var actual, out var scope)) { var resolved = ResolveDispatchMethod(contract); return WithGenericScope(scope, () => InterfaceView(actual, resolved, value, location)); }
            var witness = DispatchWitness(receiver, contract, location, true);
            var result = new ApplyValue<Unit, IrType<Unit>>(((FunctionType<Unit>)witness.Attributes).Result, witness, value);
            if (ValueKey(value) is { } key) dataValueKeys[result] = "generic-view:" + GenericDispatchTypeKey(contract.ContainingType) + ":" + key;
            return result;
        }
        private TypedValue[] GenericPresenceViews(ITypeSymbol type, TypedValue value, Location location)
        {
            if (GenericArgument(type, out var argument, out var scope)) return WithGenericScope(scope, () => GenericPresenceViews(argument, value, location));
            if (type is not ITypeParameterSymbol parameter) return [];
            return genericDispatches.Values.SelectMany(requirements => requirements).Where(requirement => requirement.View && SymbolEqualityComparer.Default.Equals(requirement.Parameter, parameter) && genericDispatchValues.ContainsKey(GenericDispatchKey(requirement))).DistinctBy(requirement => GenericDispatchTypeKey(requirement.Method.ContainingType)).OrderBy(requirement => GenericDispatchTypeKey(requirement.Method.ContainingType), StringComparer.Ordinal).Select(requirement => ApplyDispatchView(parameter, requirement.Method, value, location)).ToArray();
        }
        private TypedValue MatchGenericViews(IReadOnlyList<TypedValue> views, Func<TypedValue> absent, Func<TypedValue> present, int index = 0)
        {
            return index == views.Count ? present() : MatchMaybe(views[index], absent, _ => MatchGenericViews(views, absent, present, index + 1));
        }
    }
}
