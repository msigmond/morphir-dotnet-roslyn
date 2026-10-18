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
        private sealed record ValueInterface(INamedTypeSymbol Type, INamedTypeSymbol[] Alternatives);
        private readonly Dictionary<string, ValueInterface> valueInterfaces = [];
        private bool ValueInterfaceType(ITypeSymbol type) => type is INamedTypeSymbol named && valueInterfaces.ContainsKey(SymbolId(named));
        private bool InterfaceOnlyBases(INamedTypeSymbol type) => type.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax()).OfType<TypeDeclarationSyntax>().Where(declaration => declaration.BaseList is not null).SelectMany(declaration => declaration.BaseList!.Types).All(item => Semantic(item.SyntaxTree).GetTypeInfo(item.Type).Type is INamedTypeSymbol contract && ValueInterfaceType(contract));
        private void RegisterValueInterfaces(INamedTypeSymbol[] selected)
        {
            if (!selected.Any(DispatchContractType)) return;
            var external = compilations.SelectMany(source => source.References.Select(reference => source.GetAssemblyOrModuleSymbol(reference))).OfType<IAssemblySymbol>().Where(assembly => !compilations.Any(source => source.Assembly.Identity.Equals(assembly.Identity))).GroupBy(assembly => assembly.Identity.ToString()).Select(group => group.First()).ToArray();
            var all = compilations.SelectMany(source => AllTypes(source.Assembly.GlobalNamespace)).ToArray();
            foreach (var type in selected.Where(DispatchContractType)) Try(() =>
            {
                if (type.GetAttributes().Length != 0 || type.GetMembers().Where(member => !ImplicitBaseConstructor(member)).Any(member => !(type.TypeKind == TypeKind.Class && (BaseStorageField(member) || SourceBaseConstructor(member) || BasePropertyMember(member))) && !AdmittedInterfaceMember(member)))
                    Fail<object>("MCS3110", "This interface requires the remaining generic/inherited/default/static dispatch gate.", Location(type));
                foreach (var parent in RuntimeContracts(type).Where(parent => parent.SpecialType != SpecialType.System_Object)) if (!Selected(parent) || !identities.ContainsKey(SymbolId(parent))) Fail<object>("MCS2201", $"Inherited contract {parent} is outside the selected source set; select its namespace explicitly.", Location(type));
                foreach (var assembly in external.Where(assembly => assembly.Modules.Any(module => module.ReferencedAssemblySymbols.Any(reference => reference.Identity.Equals(type.ContainingAssembly.Identity)))))
                    if (AllTypes(assembly.GlobalNamespace).Any(candidate => RuntimeContracts(candidate).Any(contract => SymbolId(contract) == SymbolId(type)))) Fail<object>("MCS3111", $"Binary assembly {assembly.Identity.Name} contains implementations outside the acquired source closure.", Location(type));
                var alternatives = all.Where(candidate => !candidate.IsAbstract && ImplementedRuntimeContracts(candidate, type).Any()).Select(candidate => UniformInterfaceAlternative(candidate, type)).ToArray();
                if (alternatives.Length == 0) Fail<object>("MCS3111", "The runtime implementation set is unknown or empty.", Location(type));
                foreach (var candidate in alternatives)
                {
                    if (!Selected(candidate)) Fail<object>("MCS3111", $"Implementation {candidate} is outside the selected source closure; select its namespace explicitly.", Location(candidate));
                    foreach (var method in InterfaceMethods(type).Concat(type.GetMembers().OfType<IPropertySymbol>().Select(property => property.SetMethod).OfType<IMethodSymbol>()).Where(InterfaceRequiresImplementation))
                        if (ValueImplementation(candidate, method) is not IMethodSymbol implementation || !identities.ContainsKey(SymbolId(implementation))) Fail<object>("MCS3112", "The interface implementation set is incomplete or has no selected resolved target.", Location(candidate));
                }
                if (alternatives.GroupBy(alternative => DispatchConstructor(type, alternative).Local.Key).Any(group => group.Count() > 1)) Fail<object>("MCS3112", "Runtime alternatives have ambiguous normalized constructor identities.", Location(type));
                valueInterfaces[SymbolId(type)] = new(type, alternatives);
            });
        }
        private IMethodSymbol? ValueImplementation(INamedTypeSymbol candidate, IMethodSymbol method)
        {
            if (method.IsAbstract && method.ExplicitInterfaceImplementations is [var declaration])
                return ValueImplementation(candidate, declaration.Arity == 0 ? declaration : declaration.Construct(method.TypeArguments.ToArray()));
            if (method.ContainingType.TypeKind == TypeKind.Class) return ClassImplementation(candidate, method);
            var contract = candidate.AllInterfaces.FirstOrDefault(type => SameRuntimeType(type, method.ContainingType));
            if (method.AssociatedSymbol is IPropertySymbol property)
            {
                var memberProperty = contract?.GetMembers(property.Name).OfType<IPropertySymbol>().FirstOrDefault(member => SymbolId(member) == SymbolId(property));
                var implementationProperty = memberProperty is null ? null : candidate.FindImplementationForInterfaceMember(memberProperty) as IPropertySymbol;
                var getter = method.MethodKind == MethodKind.PropertySet ? implementationProperty?.SetMethod : implementationProperty?.GetMethod;
                return getter is { ContainingType.TypeKind: TypeKind.Class } inheritedGetter && !SameRuntimeType(inheritedGetter.ContainingType, candidate) && (inheritedGetter.IsVirtual || inheritedGetter.IsAbstract || inheritedGetter.IsOverride) ? ClassImplementation(candidate, inheritedGetter) : getter;
            }
            var member = contract?.GetMembers(method.Name).OfType<IMethodSymbol>().FirstOrDefault(member => SymbolId(member) == SymbolId(method));
            var implementation = member is null ? null : candidate.FindImplementationForInterfaceMember(member) as IMethodSymbol;
            if (implementation is { ContainingType.TypeKind: TypeKind.Class } inherited && !SameRuntimeType(inherited.ContainingType, candidate) && (inherited.IsVirtual || inherited.IsAbstract || inherited.IsOverride)) implementation = ClassImplementation(candidate, inherited)?.ConstructedFrom;
            return implementation is null || implementation.Arity == 0 ? implementation : implementation.Construct(method.TypeArguments.ToArray());
        }
        private IrType<Unit> ValueInterfaceIrType(INamedTypeSymbol type, bool present = false)
        {
            IrType<Unit> raw = GenericTypeReference(type); return present ? raw : Types.Maybe(raw);
        }
        private FQName DispatchConstructor(INamedTypeSymbol contract, INamedTypeSymbol implementation)
        {
            string Stem(INamedTypeSymbol type) => SourceTypeName(implementation) + (type.TypeKind == TypeKind.Class ? SourceTypeName(type) : type.Name.Length > 1 && type.Name[0] == 'I' && char.IsUpper(type.Name[1]) ? type.Name[1..] : type.Name);
            var stem = Stem(contract);
            if (contract.TypeKind == TypeKind.Class)
            {
                var reserved = RuntimeContracts(implementation).Where(type => SymbolId(type) != SymbolId(contract) && identities.TryGetValue(SymbolId(type), out var other) && other.Module.Key == Identity(contract).Module.Key).Select(type => Name.From(Stem(type)).Key).ToHashSet();
                while (reserved.Contains(Name.From(stem).Key)) stem += "_Class";
            }
            return Identity(contract) with { Local = Name.From(stem) };
        }
        private void EmitValueInterface(ValueInterface contract)
        {
            var id = Identity(contract.Type); EnsureModule(id.Module, contract.Type.ContainingType is null ? Access.Public : Visibility(contract.Type.ContainingType));
            var constructors = contract.Alternatives.Select(alternative => new Entry<Name, IReadOnlyList<Entry<Name, IrType<Unit>>>>(DispatchConstructor(contract.Type, alternative).Local, [new(Name.From("arg1"), InterfacePayloadType(alternative))])).OrderBy(entry => entry.Key.Key, StringComparer.Ordinal).ToArray();
            modules[id.Module.Key].Types.Add(new(id.Local, new(Visibility(contract.Type), new(Documentation(contract.Type), new CustomDefinition<Unit>(SourceTypeParameters(contract.Type).Select(TypeParameterName).ToArray(), new(Access.Public, constructors))))));
            foreach (var method in InterfaceMethods(contract.Type))
            {
                Try(() => EmitMethod(method));
                // A selected default body remains part of the source model even
                // when every currently acquired alternative overrides it.
                if (InterfaceDispatchMethod(method) && !method.IsAbstract) Try(() => EmitMethod(method, true));
            }
            foreach (var constructor in contract.Type.InstanceConstructors.Where(constructor => dataConstructors.ContainsKey(SymbolId(constructor)))) Try(() => EmitDataConstructor(constructor));
            foreach (var setter in contract.Type.GetMembers().OfType<IPropertySymbol>().Select(property => property.SetMethod).Where(setter => setter is not null && dataSetters.ContainsKey(SymbolId(setter))))
            {
                Try(() => EmitDataSetter(setter!));
                if (InterfaceDispatchMethod(setter!) && !setter!.IsAbstract) Try(() => EmitDataSetter(setter, true));
            }
        }
        private void EmitValueDispatch(IMethodSymbol method)
        {
            ValidateOrdinaryGenerics(method.TypeParameters);
            var contract = valueInterfaces[SymbolId(method.ContainingType)]; var key = SymbolId(method); calls.TryAdd(key, []);
            foreach (var alternative in contract.Alternatives) { var target = ValueImplementation(alternative, method)!; EmitMethod(target, ValueInterfaceType(target.ContainingType)); }
            var receiver = ValueInterfaceIrType(contract.Type, true); var receiverName = ReceiverName(method); var output = Type(method.ReturnType, Location(method));
            var inputs = new[] { new Input<Unit, IrType<Unit>>(receiverName, receiver, receiver) }.Concat(method.Parameters.Select(parameter => new Input<Unit, IrType<Unit>>(Name.From(parameter.Name), Type(parameter.Type, Location(parameter)), Type(parameter.Type, Location(parameter))))).ToArray();
            currentMethod = key; PrepareBindings(method); PrepareDefaultWitnesses(method); inputs = DefaultWitnessInputs(method).Concat(inputs).ToArray();
            var branches = new List<MatchCase<Unit, IrType<Unit>>>();
            foreach (var alternative in contract.Alternatives)
            {
                var target = (IMethodSymbol)ValueImplementation(alternative, method)!; var concrete = InterfacePayloadType(alternative); var name = Name.From("rule"); var version = 0; while (inputs.Any(input => input.Name.Key == name.Key)) name = Name.From("rule_" + (++version)); var defaultBody = ValueInterfaceType(target.ContainingType); var signature = defaultBody ? InterfaceDefaultOutput(target, Location(method)!) : CallableOutput(target, Location(method)!);
                foreach (var parameter in target.Parameters.Reverse()) signature = Types.Function(Type(parameter.Type, Location(parameter)), signature);
                var targetReceiver = defaultBody ? ValueInterfaceIrType(target.ContainingType, true) : concrete; signature = Types.Function(targetReceiver, signature);
                var witnesses = DefaultWitnessArguments(target, Location(method)!); foreach (var witness in witnesses.Reverse()) signature = Types.Function(witness.Attributes, signature);
                TypedValue body = new ReferenceValue<Unit, IrType<Unit>>(signature, defaultBody ? InterfaceDefaultIdentity(target) : Identity(target));
                foreach (var witness in witnesses) body = new ApplyValue<Unit, IrType<Unit>>(((FunctionType<Unit>)body.Attributes).Result, body, witness);
                TypedValue payload = new VariableValue<Unit, IrType<Unit>>(concrete, name);
                if (defaultBody) payload = ((ApplyValue<Unit, IrType<Unit>>)InterfaceValue(target.ContainingType, alternative, payload)).Argument;
                body = new ApplyValue<Unit, IrType<Unit>>(((FunctionType<Unit>)body.Attributes).Result, body, payload);
                foreach (var parameter in method.Parameters) body = new ApplyValue<Unit, IrType<Unit>>(((FunctionType<Unit>)body.Attributes).Result, body, new VariableValue<Unit, IrType<Unit>>(Type(parameter.Type, Location(parameter)), Name.From(parameter.Name)));
                body = Adapt(body, output);
                branches.Add(new(new ConstructorPattern<IrType<Unit>>(receiver, DispatchConstructor(contract.Type, alternative), [new AsPattern<IrType<Unit>>(concrete, new WildcardPattern<IrType<Unit>>(concrete), name)]), body));
                calls[key].Add((defaultBody ? InterfaceDefaultKey(target) : SymbolId(target), Location(method)!));
            }
            var result = new MatchValue<Unit, IrType<Unit>>(output, new VariableValue<Unit, IrType<Unit>>(receiver, receiverName), branches);
            var id = Identity(method); EnsureModule(id.Module, Visibility(method.ContainingType)); methodOutputs[key] = output;
            modules[id.Module.Key].Values.Add(new(id.Local, new(Visibility(method), new(Documentation(method), new(inputs, output, result)))));
        }
        private bool ValueInterfaceConversion(IConversionOperation conversion) => !conversion.Conversion.IsIdentity && !Null(conversion.Operand) && conversion.Type is INamedTypeSymbol target && ValueInterfaceType(target) && (NullableElement(conversion.Operand.Type) ?? conversion.Operand.Type) is INamedTypeSymbol source && !ValueInterfaceType(source) && ValueAlternatives(target).Any(alternative => SameRuntimeType(alternative, source));
        private TypedValue? LowerValueDispatch(IOperation operation)
        {
            if (operation is not IConversionOperation conversion || !ValueInterfaceConversion(conversion)) return null;
            var contract = (INamedTypeSymbol)conversion.Type!; var alternative = (INamedTypeSymbol)(NullableElement(conversion.Operand.Type) ?? conversion.Operand.Type)!; var raw = ValueInterfaceIrType(contract, true); var value = Lower(conversion.Operand);
            TypedValue Wrap(TypedValue present) => InterfaceValue(contract, alternative, present);
            return MaybeElement(value.Attributes) is null ? Wrap(value) : MatchMaybe(value, () => Nothing(Types.Maybe(raw)), Wrap);
        }
    }
}
