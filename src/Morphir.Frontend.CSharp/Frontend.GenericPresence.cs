using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Morphir.IR.Classic;
using TypedValue = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        private Dictionary<ITypeParameterSymbol, TypedValue> genericAbsenceValues = new(SymbolEqualityComparer.Default);
        private readonly Dictionary<string, ITypeParameterSymbol[]> genericAbsences = [];
        private readonly HashSet<string> findingGenericAbsences = [];
        private static IOperation? AbsenceSubject(IOperation operation) => operation switch
        {
            IIsPatternOperation pattern when NullPattern(pattern.Pattern, out _) => pattern.Value,
            IBinaryOperation binary when binary.OperatorMethod is null && binary.OperatorKind is BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals && (Null(binary.LeftOperand) || Null(binary.RightOperand)) => Null(binary.LeftOperand) ? binary.RightOperand : binary.LeftOperand,
            ICoalesceOperation coalesce => coalesce.Value,
            ICoalesceAssignmentOperation assignment => assignment.Target,
            _ => null
        };
        private static ITypeSymbol? AbsenceSubjectType(IOperation operation)
        {
            var subject = AbsenceSubject(operation);
            while (subject is IConversionOperation conversion && NullTestConversion(conversion)) subject = conversion.Operand;
            return subject?.Type;
        }
        private ITypeParameterSymbol[] GenericAbsences(IMethodSymbol method)
        {
            method = method.OriginalDefinition; var key = SymbolId(method); if (genericAbsences.TryGetValue(key, out var found)) return found; if (!findingGenericAbsences.Add(key)) return [];
            try
            {
                var parameters = new HashSet<ITypeParameterSymbol>(SymbolEqualityComparer.Default);
                void Require(ITypeSymbol? type) { if (type is ITypeParameterSymbol { HasValueTypeConstraint: false } parameter) parameters.Add(parameter); }
                void RequireCreation(ITypeSymbol type)
                {
                    if (ZeroDataConstructor(type) is { } constructor && dataConstructors.ContainsKey(SymbolId(constructor))) foreach (var required in GenericAbsences(constructor)) Require(SubstituteType(required, TypeSubstitutions(constructor)));
                }
                foreach (var target in InterfaceTargets(method).Concat(ConstructorTargets(method))) foreach (var required in GenericAbsences(target)) Require(SubstituteType(required, TypeSubstitutions(target)));
                var operations = GenericOperations(method);
                foreach (var operation in operations)
                {
                    Require(AbsenceSubjectType(operation));
                    if (GenericRequirementTarget(operation) is { } target && SourceGenericCallable(target))
                    {
                        foreach (var required in GenericAbsences(target)) Require(SubstituteType(required, TypeSubstitutions(target)));
                        foreach (var required in GenericNews(target)) RequireCreation(SubstituteType(required, TypeSubstitutions(target)));
                    }
                }
                found = parameters.Where(parameter => !GenericDispatches(method).Any(requirement => requirement.View && SymbolEqualityComparer.Default.Equals(requirement.Parameter, parameter))).OrderBy(parameter => TypeParameterName(parameter).Key, StringComparer.Ordinal).ToArray(); genericAbsences[key] = found; return found;
            }
            finally { findingGenericAbsences.Remove(key); }
        }
        private Name AbsenceWitnessName(IMethodSymbol method, ITypeParameterSymbol parameter)
        {
            var name = Name.From("isNull_" + TypeParameterName(parameter).Key); var index = 0;
            while (method.Parameters.Any(input => Name.From(input.Name).Key == name.Key) || ReceiverRequired(method) && ReceiverName(method).Key == name.Key) name = Name.From("isNull_" + TypeParameterName(parameter).Key + "_" + (++index)); return name;
        }
        private Input<Unit, IrType<Unit>>[] AbsenceWitnessInputs(IMethodSymbol method) => GenericAbsences(method).Select(parameter => { var type = Types.Function(Type(parameter, Location(method)), Types.Bool); return new Input<Unit, IrType<Unit>>(AbsenceWitnessName(method, parameter), type, type); }).ToArray();
        private void PrepareAbsenceWitnesses(IMethodSymbol method)
        {
            genericAbsenceValues = new(SymbolEqualityComparer.Default);
            foreach (var parameter in GenericAbsences(method)) { var name = AbsenceWitnessName(method, parameter); bindingNames.Add(name.Key); genericAbsenceValues[parameter] = new VariableValue<Unit, IrType<Unit>>(Types.Function(Type(parameter, Location(method)), Types.Bool), name); }
        }
        private TypedValue[] AbsenceWitnessArguments(IMethodSymbol method, Location location) => GenericAbsences(method).Select(parameter => AbsenceWitness(SubstituteType(parameter, TypeSubstitutions(method)), location)).ToArray();
        private TypedValue AbsenceWitness(ITypeSymbol type, Location location)
        {
            if (GenericArgument(type, out var actual, out var scope)) return WithGenericScope(scope, () => AbsenceWitness(actual, location));
            if (type is ITypeParameterSymbol { HasValueTypeConstraint: false } parameter) return genericAbsenceValues.TryGetValue(parameter, out var witness) ? witness : Fail<TypedValue>("MCS3102", "Generic absence requires its explicit representation witness.", location);
            var argument = Type(type, location); var name = Fresh("absenceArgument"); var value = new VariableValue<Unit, IrType<Unit>>(argument, name);
            var body = type is ITypeParameterSymbol { HasValueTypeConstraint: true } ? Lit(Types.Bool, new BoolLiteral(false)) : MatchMaybe(value, () => Lit(Types.Bool, new BoolLiteral(true)), _ => Lit(Types.Bool, new BoolLiteral(false)));
            return new LambdaValue<Unit, IrType<Unit>>(Types.Function(argument, Types.Bool), new AsPattern<IrType<Unit>>(argument, new WildcardPattern<IrType<Unit>>(argument), name), body);
        }
        private TypedValue GenericIsAbsent(ITypeSymbol type, TypedValue value, Location location)
        {
            if (GenericArgument(type, out var actual, out var scope)) return WithGenericScope(scope, () => GenericIsAbsent(actual, value, location));
            if (GenericPresenceViews(type, value, location) is { Length: > 0 } views) return MatchGenericViews(views, () => Lit(Types.Bool, new BoolLiteral(true)), () => Lit(Types.Bool, new BoolLiteral(false)));
            if (type is ITypeParameterSymbol { HasValueTypeConstraint: true }) return Lit(Types.Bool, new BoolLiteral(false));
            var witness = AbsenceWitness(type, location); return new ApplyValue<Unit, IrType<Unit>>(Types.Bool, witness, value);
        }
        private TypedValue? LowerGenericAbsence(IOperation operation)
        {
            if (operation is not (IIsPatternOperation or IBinaryOperation) || AbsenceSubjectType(operation) is not ITypeParameterSymbol parameter || Type(parameter, operation.Syntax.GetLocation()) is not VariableType<Unit> || AbsenceSubject(operation) is not { } subject) return null;
            var absent = GenericIsAbsent(parameter, Lower(subject), operation.Syntax.GetLocation());
            var positive = operation is IIsPatternOperation pattern ? NullPattern(pattern.Pattern, out var negated) && !negated : ((IBinaryOperation)operation).OperatorKind == BinaryOperatorKind.Equals;
            return positive ? absent : SdkUnary("not", Types.Bool, Types.Bool, absent);
        }
        private TypedValue GenericCoalesceSubject(TypedValue value, Func<TypedValue, TypedValue> continuation)
        {
            if (value is VariableValue<Unit, IrType<Unit>>) return continuation(value);
            var name = Fresh("genericSubject"); bindingOrigins[name.Key] = ValueKey(value); boundValues[name.Key] = value;
            var body = continuation(new VariableValue<Unit, IrType<Unit>>(value.Attributes, name)); return new LetValue<Unit, IrType<Unit>>(body.Attributes, name, new([], value.Attributes, value), body);
        }
    }
}
