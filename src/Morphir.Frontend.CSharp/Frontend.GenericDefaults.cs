using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Morphir.IR.Classic;
using TypedValue = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        private Dictionary<ITypeParameterSymbol, TypedValue> genericDefaultValues = new(SymbolEqualityComparer.Default);
        private readonly Dictionary<string, ITypeParameterSymbol[]> genericDefaults = [];
        private readonly HashSet<string> findingGenericDefaults = [];
        private static IEnumerable<IOperation> OwnOperations(IOperation root)
        {
            yield return root;
            foreach (var child in root.ChildOperations.Where(child => child is not ILocalFunctionOperation)) foreach (var operation in OwnOperations(child)) yield return operation;
        }
        private IEnumerable<IOperation> GenericOperations(IMethodSymbol method)
        {
            var syntax = method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(); var root = syntax is null ? null : Semantic(syntax.SyntaxTree).GetOperation(syntax);
            while (root?.Parent is { } parent) root = parent;
            var operations = root is null ? Enumerable.Empty<IOperation>() : OwnOperations(root);
            if (PrimaryDataConstructor(method) && ConstructorInitializer(method) is { } primary) operations = operations.Concat(OwnOperations(primary));
            if (method.MethodKind == MethodKind.Constructor && (DataType(method.ContainingType) || BaseConstructor(method))) operations = operations.Concat(ConstructorInitializers(method).SelectMany(initializer => OwnOperations(initializer.Value)));
            return operations;
        }
        private IEnumerable<ITypeParameterSymbol> DefaultParameters(ITypeSymbol type, HashSet<ITypeSymbol>? visiting = null)
        {
            if (type is ITypeParameterSymbol parameter) { yield return parameter; yield break; }
            if (type.IsReferenceType || NullableElement(type) is not null) yield break;
            if (TupleType(type)) { foreach (var element in TupleElements(type)) foreach (var child in DefaultParameters(element, visiting)) yield return child; yield break; }
            if (type is not INamedTypeSymbol named) yield break;
            visiting ??= new(SymbolEqualityComparer.Default); if (!visiting.Add(named)) { Fail<object>("MCS3102", "A CLR value default requires a finite constructed storage layout.", Location(named)); yield break; }
            var slots = dataTypes.TryGetValue(SymbolId(named), out var layout) ? layout.Slots.Select(slot => slot.Type) : records.TryGetValue(SymbolId(named), out var fields) ? fields.Select(field => field.Type) : [];
            var substitutions = TypeSubstitutions(named);
            foreach (var slot in slots) foreach (var child in DefaultParameters(SubstituteType(slot, substitutions), visiting)) yield return child;
            visiting.Remove(named);
        }
        private ITypeParameterSymbol[] GenericDefaults(IMethodSymbol method)
        {
            method = method.OriginalDefinition; var key = SymbolId(method);
            if (genericDefaults.TryGetValue(key, out var found)) return found;
            if (!findingGenericDefaults.Add(key)) return []; // Ordinary recursion is rejected by the call graph gate.
            try
            {
                var parameters = new HashSet<ITypeParameterSymbol>(SymbolEqualityComparer.Default);
                if (method.MethodKind == MethodKind.Constructor && dataTypes.TryGetValue(SymbolId(method.ContainingType), out var layout))
                    foreach (var slot in layout.Slots) parameters.UnionWith(DefaultParameters(slot.Type));
                if (ImplicitBaseTarget(method) is { } implicitBase) foreach (var parameter in implicitBase.Parameters.Where(parameter => parameter.HasExplicitDefaultValue && parameter.ExplicitDefaultValue is null)) parameters.UnionWith(DefaultParameters(parameter.Type));
                foreach (var target in InterfaceTargets(method).Concat(ConstructorTargets(method))) foreach (var required in GenericDefaults(target)) parameters.UnionWith(DefaultParameters(SubstituteType(required, TypeSubstitutions(target))));
                var operations = GenericOperations(method);
                foreach (var operation in operations)
                {
                    if (operation is IObjectCreationOperation { Type: INamedTypeSymbol created } && DataType(created)) foreach (var slot in dataTypes[SymbolId(created)].Slots) parameters.UnionWith(DefaultParameters(SubstituteType(slot.Type, TypeSubstitutions(created))));
                    if (operation is IDefaultValueOperation && operation.Type is { } type) parameters.UnionWith(DefaultParameters(type));
                    if (operation.Type is ITypeParameterSymbol parameter && Null(operation)) parameters.Add(parameter);
                    var target = GenericRequirementTarget(operation);
                    if (target is not null && SourceGenericCallable(target))
                    {
                        foreach (var required in GenericDefaults(target)) parameters.UnionWith(DefaultParameters(SubstituteType(required, TypeSubstitutions(target))));
                        foreach (var required in GenericNews(target))
                        {
                            var witnessCreated = SubstituteType(required, TypeSubstitutions(target));
                            if (witnessCreated is INamedTypeSymbol owner && DataType(owner))
                            {
                                foreach (var slot in dataTypes[SymbolId(owner)].Slots) parameters.UnionWith(DefaultParameters(SubstituteType(slot.Type, TypeSubstitutions(owner))));
                                if (ZeroDataConstructor(owner) is { } constructor && dataConstructors.ContainsKey(SymbolId(constructor))) foreach (var requiredDefault in GenericDefaults(constructor)) parameters.UnionWith(DefaultParameters(SubstituteType(requiredDefault, TypeSubstitutions(constructor))));
                            }
                        }
                    }
                }
                found = parameters.OrderBy(parameter => TypeParameterName(parameter).Key, StringComparer.Ordinal).ToArray(); genericDefaults[key] = found; return found;
            }
            finally { findingGenericDefaults.Remove(key); }
        }
        private Name DefaultWitnessName(IMethodSymbol method, ITypeParameterSymbol parameter)
        {
            var name = Name.From("default_" + TypeParameterName(parameter).Key); var index = 0;
            while (method.Parameters.Any(input => Name.From(input.Name).Key == name.Key) || ReceiverRequired(method) && ReceiverName(method).Key == name.Key) name = Name.From("default_" + TypeParameterName(parameter).Key + "_" + (++index));
            return name;
        }
        private Input<Unit, IrType<Unit>>[] DefaultWitnessInputs(IMethodSymbol method) => GenericDefaults(method).Select(parameter => new Input<Unit, IrType<Unit>>(DefaultWitnessName(method, parameter), Type(parameter, Location(method)), Type(parameter, Location(method)))).Concat(NewWitnessInputs(method)).Concat(AbsenceWitnessInputs(method)).Concat(DispatchWitnessInputs(method)).Concat(FactoryWitnessInputs(method)).ToArray();
        private void PrepareDefaultWitnesses(IMethodSymbol method)
        {
            genericDefaultValues = new(SymbolEqualityComparer.Default);
            foreach (var parameter in GenericDefaults(method))
            {
                var name = DefaultWitnessName(method, parameter); bindingNames.Add(name.Key);
                genericDefaultValues[parameter] = new VariableValue<Unit, IrType<Unit>>(Type(parameter, Location(method)), name);
            }
            PrepareNewWitnesses(method); PrepareAbsenceWitnesses(method); PrepareDispatchWitnesses(method); PrepareFactoryWitnesses(method);
        }
        private TypedValue[] DefaultWitnessArguments(IMethodSymbol method, Location location) => GenericDefaults(method).Select(parameter => Default(SubstituteType(parameter, TypeSubstitutions(method)), location)).Concat(NewWitnessArguments(method, location)).Concat(AbsenceWitnessArguments(method, location)).Concat(DispatchWitnessArguments(method, location)).Concat(FactoryWitnessArguments(method, location)).ToArray();
    }
}
