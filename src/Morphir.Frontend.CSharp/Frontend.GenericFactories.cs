using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Morphir.IR.Classic;
using TypedValue = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        private sealed record FactoryRequirement(string Mode, ITypeSymbol Source, ITypeSymbol Target);
        private Dictionary<string, TypedValue> genericFactoryValues = [];
        private readonly Dictionary<string, FactoryRequirement[]> genericFactories = [];
        private readonly HashSet<string> findingGenericFactories = [];
        private readonly HashSet<IOperation> provedFactoryAdapters = [];
        private static FactoryRequirement? NumericFactoryRequirement(IMethodSymbol method)
        {
            if (NumericFactoryMethod(method)) return new(method.Name, DecimalIntegerMethod(method) || FloatingIntegerMethod(method) ? method.Parameters[0].Type : method.TypeArguments[0], method.ReturnType);
            // Only the pinned generic-math contracts are catalog mappings. A
            // similarly named user method remains an ordinary resolved call.
            if (method.IsStatic && method.Arity == 1 && method.Parameters.Length == 1 && method.Parameters[0].RefKind == RefKind.None && method.ContainingAssembly.Identity.Name == "System.Runtime" && method.ContainingAssembly.Identity.Version == new Version(10, 0, 0, 0) && method.ContainingNamespace.ToDisplayString() == "System.Numerics")
            {
                if (method.ContainingType.OriginalDefinition.MetadataName == "INumberBase`1" && method.Name is "CreateChecked" or "CreateSaturating" or "CreateTruncating") return new(method.Name, method.TypeArguments[0], method.ReturnType);
                if (method.ContainingType.OriginalDefinition.MetadataName == "IFloatingPoint`1" && method.Name is "ConvertToInteger" or "ConvertToIntegerNative") return new(method.Name, method.Parameters[0].Type, method.ReturnType);
            }
            return null;
        }
        private static bool FactoryVariables(FactoryRequirement requirement) => requirement.Source is ITypeParameterSymbol || requirement.Target is ITypeParameterSymbol;
        private static string FactoryKey(FactoryRequirement requirement) => requirement.Mode + "::" + GenericDispatchTypeKey(requirement.Source) + "::" + GenericDispatchTypeKey(requirement.Target);
        private FactoryRequirement SubstituteFactory(FactoryRequirement requirement, IReadOnlyDictionary<ITypeParameterSymbol, ITypeSymbol> substitutions) => requirement with { Source = SubstituteType(requirement.Source, substitutions), Target = SubstituteType(requirement.Target, substitutions) };
        private FactoryRequirement[] GenericFactories(IMethodSymbol method)
        {
            method = method.OriginalDefinition; var key = SymbolId(method);
            if (genericFactories.TryGetValue(key, out var found)) return found;
            if (!findingGenericFactories.Add(key)) return [];
            try
            {
                var required = new Dictionary<string, FactoryRequirement>();
                void Require(FactoryRequirement requirement) { if (FactoryVariables(requirement)) required[FactoryKey(requirement)] = requirement; }
                void RequireTarget(IMethodSymbol target) { foreach (var requirement in GenericFactories(target)) Require(SubstituteFactory(requirement, TypeSubstitutions(target))); }
                foreach (var target in InterfaceTargets(method).Concat(ConstructorTargets(method))) RequireTarget(target);
                var operations = GenericOperations(method);
                foreach (var operation in operations)
                {
                    if (GenericRequirementTarget(operation) is not { } target) continue;
                    if (NumericFactoryRequirement(target) is { } requirement) Require(requirement);
                    else if (SourceGenericCallable(target))
                    {
                        RequireTarget(target);
                        foreach (var created in GenericNews(target).Select(parameter => SubstituteType(parameter, TypeSubstitutions(target))))
                            if (ZeroDataConstructor(created) is { } constructor && dataConstructors.ContainsKey(SymbolId(constructor))) RequireTarget(constructor);
                    }
                }
                found = required.OrderBy(entry => entry.Key, StringComparer.Ordinal).Select(entry => entry.Value).ToArray(); genericFactories[key] = found; return found;
            }
            finally { findingGenericFactories.Remove(key); }
        }
        private Name FactoryWitnessName(IMethodSymbol method, FactoryRequirement requirement)
        {
            var index = Array.FindIndex(GenericFactories(method), item => FactoryKey(item) == FactoryKey(requirement));
            var stem = "numeric_" + requirement.Mode + "_" + index; var name = Name.From(stem); var suffix = 0;
            while (method.Parameters.Any(parameter => Name.From(parameter.Name).Key == name.Key) || ReceiverRequired(method) && ReceiverName(method).Key == name.Key) name = Name.From(stem + "_" + (++suffix));
            return name;
        }
        private IrType<Unit> FactoryWitnessType(FactoryRequirement requirement, Location? location) => Types.Function(Type(requirement.Source, location), Type(requirement.Target, location));
        private Input<Unit, IrType<Unit>>[] FactoryWitnessInputs(IMethodSymbol method) => GenericFactories(method).Select(requirement => { var type = FactoryWitnessType(requirement, Location(method)); return new Input<Unit, IrType<Unit>>(FactoryWitnessName(method, requirement), type, type); }).ToArray();
        private void PrepareFactoryWitnesses(IMethodSymbol method)
        {
            genericFactoryValues = [];
            foreach (var requirement in GenericFactories(method))
            {
                var name = FactoryWitnessName(method, requirement); bindingNames.Add(name.Key);
                genericFactoryValues[FactoryKey(requirement)] = new VariableValue<Unit, IrType<Unit>>(FactoryWitnessType(requirement, Location(method)), name);
            }
        }
        private TypedValue[] FactoryWitnessArguments(IMethodSymbol method, Location location) => GenericFactories(method).Select(requirement => FactoryWitness(SubstituteFactory(requirement, TypeSubstitutions(method)), location)).ToArray();
        private Dictionary<string, TypedValue> FactoryWitnessBindings(IMethodSymbol method, IReadOnlyList<TypedValue> arguments)
        {
            var result = new Dictionary<string, TypedValue>(genericFactoryValues);
            var offset = GenericDefaults(method).Length + GenericNews(method).Length + GenericAbsences(method).Length + GenericDispatches(method).Length;
            foreach (var pair in GenericFactories(method).Zip(arguments.Skip(offset))) result[FactoryKey(pair.First)] = pair.Second;
            return result;
        }
        private TypedValue FactoryWitness(FactoryRequirement requirement, Location location)
        {
            if (GenericArgument(requirement.Source, out var sourceArgument, out var sourceScope))
            {
                var target = ResolveGenericParameter(requirement.Target);
                return WithGenericScope(sourceScope, () => FactoryWitness(requirement with { Source = sourceArgument, Target = target }, location));
            }
            if (GenericArgument(requirement.Target, out var targetArgument, out var targetScope)) return WithGenericScope(targetScope, () => FactoryWitness(requirement with { Target = targetArgument }, location));
            if (FactoryVariables(requirement)) return genericFactoryValues.TryGetValue(FactoryKey(requirement), out var witness) ? witness : Fail<TypedValue>("MCS3106", "A generic numeric conversion requires its exact source/target/mode witness.", location);
            var name = Fresh("numericArgument"); var input = Type(requirement.Source, location); var variable = new VariableValue<Unit, IrType<Unit>>(input, name);
            var body = ConcreteFactoryValue(requirement, variable, location);
            return new LambdaValue<Unit, IrType<Unit>>(Types.Function(input, body.Attributes), new AsPattern<IrType<Unit>>(input, new WildcardPattern<IrType<Unit>>(input), name), body);
        }
        private TypedValue ConcreteFactoryValue(FactoryRequirement requirement, TypedValue value, Location location)
        {
            var integerApi = requirement.Mode is "ConvertToInteger" or "ConvertToIntegerNative";
            var owner = (integerApi ? requirement.Source : requirement.Target) as INamedTypeSymbol;
            var argument = integerApi ? requirement.Target : requirement.Source;
            var method = owner?.GetMembers(requirement.Mode).OfType<IMethodSymbol>().SingleOrDefault(candidate => candidate.IsStatic && candidate.Arity == 1 && candidate.Parameters.Length == 1 && NumericFactoryMethod(candidate));
            if (method is null) return Fail<TypedValue>("MCS3106", "The concrete numeric witness has no exact catalog conversion mapping.", location);
            return NumericFactoryValue(method.Construct(argument), value, location);
        }
    }
}
