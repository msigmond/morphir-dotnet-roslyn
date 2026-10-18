using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Morphir.IR.Classic;
using TypedValue = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed record InterfaceTest(IOperation Subject, INamedTypeSymbol Target, ISymbol? Binding, bool Negated);
    private sealed record InterfacePredicate(TypedValue Subject, InterfaceTest Test, Location Location);
    private sealed partial class Translation
    {
        private readonly Dictionary<string, TypedValue> interfaceCastProofs = [];
        private readonly Dictionary<TypedValue, InterfacePredicate> interfacePredicates = new(ReferenceEqualityComparer.Instance);
        private InterfaceTest? InterfaceTypeTest(IOperation operation)
        {
            IOperation subject; ITypeSymbol? target; ISymbol? binding = null; bool negated = false;
            if (operation is IIsTypeOperation test) { subject = test.ValueOperand; target = test.TypeOperand; negated = test.IsNegated; }
            else if (operation is IIsPatternOperation pattern)
            {
                subject = pattern.Value; var part = pattern.Pattern;
                if (part is INegatedPatternOperation not) { negated = true; part = not.Pattern; }
                if (part is IDeclarationPatternOperation { MatchesNull: false } declaration) { target = declaration.MatchedType; binding = declaration.DeclaredSymbol; }
                else if (part is ITypePatternOperation typePattern) target = typePattern.MatchedType;
                else return null;
            }
            else return null;
            return subject.Type is { } source && ValueInterfaceType(source) && target is INamedTypeSymbol named && (ValueInterfaceType(named) || ValueAlternatives((INamedTypeSymbol)source).Any(alternative => SameRuntimeType(alternative, named))) ? new(subject, named, binding, negated) : null;
        }
        private bool InterfacePatternPart(IOperation operation)
        {
            if (operation is not IPatternOperation) return false;
            while (operation.Parent is IPatternOperation parent) operation = parent;
            return operation.Parent is IIsPatternOperation test && InterfaceTypeTest(test) is not null;
        }
        private bool RuntimeTypesCouldCoincide(ITypeSymbol left, ITypeSymbol right)
        {
            left = ResolveRuntimeType(left); right = ResolveRuntimeType(right);
            if (SameRuntimeType(left, right)) return true;
            if (left is ITypeParameterSymbol || right is ITypeParameterSymbol) return true;
            if (left is IArrayTypeSymbol a && right is IArrayTypeSymbol b) return a.Rank == b.Rank && a.IsSZArray == b.IsSZArray && RuntimeTypesCouldCoincide(a.ElementType, b.ElementType);
            return left is INamedTypeSymbol x && right is INamedTypeSymbol y && SymbolId(x) == SymbolId(y) && SourceTypeArguments(x).Zip(SourceTypeArguments(y)).All(pair => RuntimeTypesCouldCoincide(pair.First, pair.Second));
        }
        private bool InterfaceAlternativeMatches(INamedTypeSymbol alternative, INamedTypeSymbol target)
        {
            if (SameRuntimeType(alternative, target)) return true;
            if (!ValueInterfaceType(target)) return false;
            var candidates = ValueAlternatives(target);
            if (candidates.Any(candidate => SameRuntimeType(candidate, alternative))) return true;
            if (candidates.Any(candidate => RuntimeTypesCouldCoincide(candidate, alternative))) return Fail<bool>("MCS3103", "Unresolved generic runtime type equality requires a resolved specialization or type-view witness.", Location(target));
            return false;
        }
        private string? InterfaceCastKey(TypedValue subject, ITypeSymbol target) => ValueKey(subject) is { } key ? key + "::cast:" + GenericDispatchTypeKey(target) : null;
        private TypedValue InterfaceValue(INamedTypeSymbol contract, INamedTypeSymbol alternative, TypedValue payload)
        {
            var raw = ValueInterfaceIrType(contract, true);
            var present = new ApplyValue<Unit, IrType<Unit>>(raw, new ConstructorValue<Unit, IrType<Unit>>(Types.Function(payload.Attributes, raw), DispatchConstructor(contract, alternative)), payload);
            var result = Just(present); interfaceGraphs[present] = new(contract, alternative, payload, false); interfaceGraphs[result] = new(contract, alternative, payload, true); return result;
        }
        private TypedValue MapInterfacePresent(INamedTypeSymbol source, INamedTypeSymbol target, TypedValue present, Location location)
        {
            if (SameRuntimeType(source, target)) return present;
            if (!ValueAlternatives((INamedTypeSymbol)source).All(alternative => InterfaceAlternativeMatches(alternative, target))) return Fail<TypedValue>("MCS3113", "An interface receiver conversion requires a complete compatible runtime set.", location);
            return MatchInterface(present, source, (alternative, payload) => DemandPresent(InterfaceValue(target, alternative, payload), location));
        }
        private TypedValue InterfaceReceiverValue(IOperation operation, INamedTypeSymbol target, Location location)
        {
            var present = DemandPresent(DataArgument(Lower(operation), location), location);
            if (operation.Type is not INamedTypeSymbol source) return present;
            if (ValueInterfaceType(source)) return MapInterfacePresent(source, target, present, location);
            return ValueAlternatives(target).Any(alternative => SameRuntimeType(alternative, source)) ? DemandPresent(InterfaceValue(target, source, present), location) : present;
        }
        private TypedValue MatchInterface(TypedValue subject, INamedTypeSymbol contract, Func<INamedTypeSymbol, TypedValue, TypedValue> branch)
        {
            if (KnownInterfacePayload(subject, contract) is { } known) return branch(known.Alternative, known.Payload);
            var cases = ValueAlternatives(contract).Select(alternative =>
            {
                var type = InterfacePayloadType(alternative); var name = Fresh("typeValue");
                return new MatchCase<Unit, IrType<Unit>>(new ConstructorPattern<IrType<Unit>>(subject.Attributes, DispatchConstructor(contract, alternative), [new AsPattern<IrType<Unit>>(type, new WildcardPattern<IrType<Unit>>(type), name)]), branch(alternative, new VariableValue<Unit, IrType<Unit>>(type, name)));
            }).ToArray();
            return new MatchValue<Unit, IrType<Unit>>(cases[0].Body.Attributes, subject, cases);
        }
        private TypedValue InterfaceDecisionValue(TypedValue subject, InterfaceTest test, BindingEnvironment environment, Func<BindingEnvironment, TypedValue> yes, Func<BindingEnvironment, TypedValue> no, Location location)
        {
            var match = test.Negated ? no : yes; var miss = test.Negated ? yes : no;
            TypedValue Cases(TypedValue present) => MatchInterface(present, (INamedTypeSymbol)test.Subject.Type!, (alternative, payload) => WithRuntimeAlternative(subject, alternative, payload, () =>
            {
                if (!InterfaceAlternativeMatches(alternative, test.Target)) return miss(Copy(environment));
                var value = ValueInterfaceType(test.Target) ? InterfaceValue(test.Target, alternative, payload) : payload;
                return test.Binding is { } binding ? BindValue(binding, test.Target, value, Copy(environment), match, location) : match(Copy(environment));
            }));
            return ValueKey(subject) is { } key && presentValues.TryGetValue(key, out var known) ? Cases(known) : MatchMaybe(subject, () => miss(Copy(environment)), Cases);
        }
        private TypedValue WithRuntimeAlternative(TypedValue subject, INamedTypeSymbol alternative, TypedValue payload, Func<TypedValue> action)
        {
            var proofs = new List<(string Key, bool Had, TypedValue? Previous)>();
            void Add(ITypeSymbol target, TypedValue value)
            {
                if (InterfaceCastKey(subject, target) is not { } key) return;
                proofs.Add((key, interfaceCastProofs.TryGetValue(key, out var previous), previous)); interfaceCastProofs[key] = value;
            }
            Add(alternative, ValueInterfaceType(alternative) ? InterfaceValue(alternative, alternative, payload) : payload);
            foreach (var contract in RuntimeContracts(alternative).Where(ValueInterfaceType)) Add(contract, InterfaceValue(contract, alternative, payload));
            try { return action(); }
            finally { foreach (var proof in proofs.AsEnumerable().Reverse()) { if (proof.Had) interfaceCastProofs[proof.Key] = proof.Previous!; else interfaceCastProofs.Remove(proof.Key); } }
        }
        private bool InterfaceConversion(IConversionOperation conversion) => !conversion.Conversion.IsIdentity && conversion.Operand.Type is INamedTypeSymbol source && ValueInterfaceType(source) && (NullableElement(conversion.Type) ?? conversion.Type) is INamedTypeSymbol target && (ValueInterfaceType(target) || ValueAlternatives((INamedTypeSymbol)source).Any(alternative => SameRuntimeType(alternative, target)));
        private (INamedTypeSymbol Alternative, TypedValue Payload)? KnownInterfacePayload(TypedValue value, INamedTypeSymbol contract)
        {
            if (InterfaceGraphOf(value) is { } graph && SameRuntimeType(graph.Contract, contract)) return (graph.Alternative, graph.Payload);
            if (value is VariableValue<Unit, IrType<Unit>> variable && boundValues.TryGetValue(variable.Name.Key, out var bound)) return KnownInterfacePayload(bound, contract);
            if (value is ApplyValue<Unit, IrType<Unit>> { Function: ConstructorValue<Unit, IrType<Unit>> { Name.Local.Key: "just" }, Argument: var present }) return KnownInterfacePayload(present, contract);
            if (value is ApplyValue<Unit, IrType<Unit>> { Function: ConstructorValue<Unit, IrType<Unit>> constructor, Argument: var payload } && ValueAlternatives(contract).FirstOrDefault(alternative => FunctionOwner(DispatchConstructor(contract, alternative)) == FunctionOwner(constructor.Name)) is { } alternative) return (alternative, payload);
            return null;
        }
        private TypedValue? LowerInterfaceCast(IOperation operation)
        {
            if (InterfaceTypeTest(operation) is { } test)
            {
                var value = Lower(test.Subject); var location = operation.Syntax.GetLocation();
                var result = InterfaceDecisionValue(value, test, bindings, _ => Lit(Types.Bool, new BoolLiteral(true)), _ => Lit(Types.Bool, new BoolLiteral(false)), location);
                interfacePredicates[result] = new(value, test, location); return result;
            }
            if (operation is not IConversionOperation conversion || !InterfaceConversion(conversion)) return null;
            var subject = Lower(conversion.Operand); var source = (INamedTypeSymbol)conversion.Operand.Type!; var target = (INamedTypeSymbol)(NullableElement(conversion.Type) ?? conversion.Type)!; var location2 = operation.Syntax.GetLocation();
            if (InterfaceCastKey(subject, target) is { } key && interfaceCastProofs.TryGetValue(key, out var proved)) return Adapt(proved, Type(conversion.Type!, location2));
            if (KnownInterfacePayload(subject, source) is { } known && InterfaceAlternativeMatches(known.Alternative, target)) return ValueInterfaceType(target) ? InterfaceValue(target, known.Alternative, known.Payload) : Adapt(known.Payload, Type(conversion.Type!, location2));
            var targetInterface = ValueInterfaceType(target); var total = targetInterface && ValueAlternatives((INamedTypeSymbol)source).All(alternative => InterfaceAlternativeMatches(alternative, target));
            if (!conversion.IsTryCast && !total) return Fail<TypedValue>("MCS3113", "Interface casts require a dominating runtime type proof for every possible alternative and null value.", location2);
            var output = Type(conversion.Type!, location2);
            return MatchMaybe(subject, () => Nothing(output), present => MatchInterface(present, source, (alternative, payload) => !InterfaceAlternativeMatches(alternative, target) ? Nothing(output) : targetInterface ? InterfaceValue(target, alternative, payload) : Just(payload)));
        }
    }
}
