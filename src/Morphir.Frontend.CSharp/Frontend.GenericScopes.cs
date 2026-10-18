using Microsoft.CodeAnalysis;
using Morphir.IR.Classic;
using TypedValue = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        // Type arguments are expressions in the caller's scope, even when their
        // symbols have the same original definition as a callee parameter.
        private sealed record GenericScope(
            Dictionary<ITypeParameterSymbol, ITypeSymbol> Types,
            Dictionary<ITypeParameterSymbol, GenericScope> Arguments,
            Dictionary<ITypeParameterSymbol, TypedValue> Defaults,
            Dictionary<ITypeParameterSymbol, TypedValue> News,
            Dictionary<ITypeParameterSymbol, TypedValue> Absences,
            Dictionary<string, TypedValue> Dispatches,
            Dictionary<string, TypedValue> Factories);
        private Dictionary<ITypeParameterSymbol, GenericScope> activeGenericScopes = new(SymbolEqualityComparer.Default);
        private GenericScope CaptureGenericScope() => new(activeGenericTypes, activeGenericScopes, genericDefaultValues, genericNewValues, genericAbsenceValues, genericDispatchValues, genericFactoryValues);
        private void RestoreGenericScope(GenericScope scope)
        {
            activeGenericTypes = scope.Types; activeGenericScopes = scope.Arguments; genericDefaultValues = scope.Defaults; genericNewValues = scope.News; genericAbsenceValues = scope.Absences; genericDispatchValues = scope.Dispatches; genericFactoryValues = scope.Factories;
        }
        private TResult WithGenericScope<TResult>(GenericScope scope, Func<TResult> action)
        {
            var previous = CaptureGenericScope(); RestoreGenericScope(scope); try { return action(); } finally { RestoreGenericScope(previous); }
        }
        private bool GenericArgument(ITypeSymbol type, out ITypeSymbol argument, out GenericScope scope)
        {
            if (type is ITypeParameterSymbol parameter && activeGenericTypes.TryGetValue(parameter, out var actual) && activeGenericScopes.TryGetValue(parameter, out var captured)) { argument = actual; scope = captured; return true; }
            argument = type; scope = null!; return false;
        }
        private ITypeSymbol ResolveRuntimeType(ITypeSymbol type)
        {
            if (GenericArgument(type, out var argument, out var scope)) return WithGenericScope(scope, () => ResolveRuntimeType(argument));
            if (type is IArrayTypeSymbol array) return compilation.CreateArrayTypeSymbol(ResolveRuntimeType(array.ElementType), array.Rank).WithNullableAnnotation(array.NullableAnnotation);
            if (type is not INamedTypeSymbol named || named.TypeArguments.Length == 0 && named.ContainingType is null) return type;
            var owner = named.ContainingType is { } containing ? (INamedTypeSymbol)ResolveRuntimeType(containing) : null;
            var definition = owner is null ? named.ConstructedFrom : owner.GetTypeMembers(named.Name, named.Arity).Single();
            return (named.Arity == 0 ? definition : definition.Construct(named.TypeArguments.Select(ResolveRuntimeType).ToArray())).WithNullableAnnotation(named.NullableAnnotation);
        }
        private IrType<Unit> LayoutIrType(DataLayout data, bool present) => data.Context is { } scope ? WithGenericScope(scope, () => ConcreteClassDomain(data.Type) ? DataPayloadType(data) : DataIrType(data.Type, present)) : ConcreteClassDomain(data.Type) ? DataPayloadType(data) : DataIrType(data.Type, present);
        private TypedValue SlotDefault(DataLayout data, DataSlot slot, Location location) => data.Context is { } scope ? WithGenericScope(scope, () => Default(slot.Type, location)) : Default(slot.Type, location);
        private DataLayout CellLayout(DataCell cell) => cell.Layout ?? ResolvedDataLayout(cell.Type);
    }
}
