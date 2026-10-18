using Microsoft.CodeAnalysis;
using Morphir.IR.Classic;
using IrPath = Morphir.IR.Classic.Path;

namespace Morphir.Frontend.CSharp;

/// <summary>An explicit reviewed semantic assertion, bound to an exact metadata method symbol.</summary>
public sealed record ExternalFunctionContract(IMethodSymbol Method, FQName Target, bool Pure, bool TotalOnFullInputDomain, string Semantics);
/// <summary>The complete unmodified dependency specification plus its explicitly mapped CLR functions.</summary>
public sealed record ExternalDependencyContract(IrPath Package, PackageSpecification<Unit> Specification, IReadOnlyList<ExternalFunctionContract> Functions);

public static partial class Frontend
{
    private sealed partial class Translation
    {
        private readonly Dictionary<string, ExternalFunctionContract> externalFunctions = [];
        private readonly List<Entry<IrPath, PackageSpecification<Unit>>> externalDependencies = [];
        private readonly Dictionary<string, ValueSpecification<Unit>> externalSpecifications = [];
        private void ConfigureExternalContracts()
        {
            foreach (var dependency in settings.Dependencies ?? [])
            {
                if (dependency.Package.Segments.Count == 0 || dependency.Package.Key == package.Key || dependency.Package.Key == Types.Sdk.Key || externalDependencies.Any(entry => entry.Key.Key == dependency.Package.Key))
                    Fail<object>("MCS2809", "External dependency packages must be nonempty, distinct and separate from the source/SDK package.", null);
                if (dependency.Specification.Modules.GroupBy(module => module.Key.Key).Any(group => group.Count() > 1)) Fail<object>("MCS2809", "Dependency module identities must be unique.", null);
                foreach (var module in dependency.Specification.Modules)
                {
                    if (module.Value.Values.GroupBy(value => value.Key.Key).Any(group => group.Count() > 1) || module.Value.Types.GroupBy(type => type.Key.Key).Any(group => group.Count() > 1)) Fail<object>("MCS2809", "Dependency declaration identities must be unique.", null);
                    foreach (var entry in module.Value.Types)
                    {
                        switch (entry.Value.Value)
                        {
                            case AliasSpecification<Unit> alias: TypeShape(alias.Body); break;
                            case CustomSpecification<Unit> custom: foreach (var constructor in custom.Constructors) foreach (var argument in constructor.Value) TypeShape(argument.Value); break;
                            case OpaqueSpecification<Unit>: break;
                            default: Fail<object>("MCS2809", "Unsupported dependency type specification.", null); break;
                        }
                    }
                    foreach (var value in module.Value.Values) { TypeShape(value.Value.Value.Output); foreach (var input in value.Value.Value.Inputs) TypeShape(input.Value); if (value.Value.Value.Inputs.GroupBy(input => input.Key.Key).Any(group => group.Count() > 1)) Fail<object>("MCS2809", "Dependency input names must be unique.", null); }
                }
                externalDependencies.Add(new(dependency.Package, dependency.Specification));
                foreach (var contract in dependency.Functions)
                {
                    var method = contract.Method; var location = Location(method);
                    if (!contract.Pure || !contract.TotalOnFullInputDomain || string.IsNullOrWhiteSpace(contract.Semantics) || !method.IsStatic || method.MethodKind != MethodKind.Ordinary || method.Arity != 0 || method.Parameters.Any(parameter => parameter.RefKind != RefKind.None) || compilations.Any(source => source.Assembly.Identity.Equals(method.ContainingAssembly.Identity)))
                        Fail<object>("MCS2809", "External contracts must explicitly assert pure, total full-domain behavior for exact non-generic static by-value metadata methods; source bodies cannot be overridden.", location);
                    var module = dependency.Specification.Modules.SingleOrDefault(entry => entry.Key.Key == contract.Target.Module.Key);
                    var specification = module?.Value.Values.SingleOrDefault(entry => entry.Key.Key == contract.Target.Local.Key)?.Value.Value;
                    if (contract.Target.Package.Key != dependency.Package.Key || specification is null || externalFunctions.ContainsKey(SymbolId(method)))
                        Fail<object>("MCS2809", "External targets must identify a declared dependency value with one exact CLR method mapping.", location);
                    externalFunctions[SymbolId(method)] = contract; externalSpecifications[SymbolId(method)] = specification!;
                    foreach (var parameter in method.Parameters.Where(parameter => parameter.IsParams && parameter.Type is IArrayTypeSymbol { Rank: 1, IsSZArray: true })) paramsArrayTypes.Add(parameter.Type);
                }
            }
        }
        private string TypeShape(IrType<Unit> type) => type switch
        {
            UnitType<Unit> => "unit",
            TupleType<Unit> tuple => "tuple(" + string.Join(";", tuple.Elements.Select(TypeShape)) + ")",
            VariableType<Unit> variable => "variable(" + variable.Name.Key + ")",
            ReferenceType<Unit> reference => "reference(" + reference.Name.Package.Key + ";" + reference.Name.Module.Key + ";" + reference.Name.Local.Key + ";" + string.Join(";", reference.Arguments.Select(TypeShape)) + ")",
            FunctionType<Unit> function => "function(" + TypeShape(function.Argument) + ";" + TypeShape(function.Result) + ")",
            RecordType<Unit> record => "record(" + string.Join(";", record.Fields.Select(field => field.Key.Key + ":" + TypeShape(field.Value))) + ")",
            _ => Fail<string>("MCS2809", "The dependency specification contains an unsupported type node.", null)
        };
        private FQName ExternalTarget(IMethodSymbol target, Location location)
        {
            var key = SymbolId(target); var contract = externalFunctions[key]; var specification = externalSpecifications[key];
            if (!SymbolEqualityComparer.Default.Equals(contract.Method, target) || specification.Inputs.Count != target.Parameters.Length || !specification.Inputs.Select(entry => TypeShape(entry.Value)).SequenceEqual(target.Parameters.Select(parameter => TypeShape(Type(parameter.Type, location)))) || TypeShape(specification.Output) != TypeShape(Type(target.ReturnType, location)))
                return Fail<FQName>("MCS2809", "External method symbol and complete input/output ABI must exactly match the reviewed dependency specification.", location);
            return contract.Target;
        }
        private IrType<Unit> CallableOutput(IMethodSymbol target, Location location) => methodOutputs.TryGetValue(SymbolId(target), out var output) ? InstantiateCallableOutput(target, output, location) : externalFunctions.ContainsKey(SymbolId(target)) ? externalSpecifications[SymbolId(target)].Output : Fail<IrType<Unit>>("MCS2202", "Callable has no validated body or external contract.", location);
    }
}
