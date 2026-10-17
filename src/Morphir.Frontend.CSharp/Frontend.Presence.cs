using System.Numerics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Morphir.IR.Classic;
using IrPath = Morphir.IR.Classic.Path;
using TypedValue = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        private readonly Dictionary<string, IMethodSymbol> sourceMethods = [];
        private readonly Dictionary<string, IrType<Unit>> methodOutputs = [];
        private readonly HashSet<string> emittingMethods = [];
        private readonly Dictionary<string, TypedValue> presentValues = [];
        private readonly Dictionary<string, string?> bindingOrigins = [];
        private readonly Dictionary<string, TypedValue> boundValues = [];
        private readonly Stack<TypedValue> conditionalInstances = [];
        private IrType<Unit> currentReturnSlot = Types.Bool;
        private static IEnumerable<IOperation> Operations(IOperation root) => root.ChildOperations.SelectMany(Operations).Prepend(root);
        private static ITypeSymbol? NullableElement(ITypeSymbol? type) => type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable ? nullable.TypeArguments[0] : null;
        private static bool AdmittedDataSlot(ITypeSymbol type) => DecimalType(type) || Integral(type) is not null || Floating(type) is not null || ComplexType(type) || type.SpecialType is SpecialType.System_Boolean or SpecialType.System_String || NullableElement(type) is { } element && (DecimalType(element) || Integral(element) is not null || Floating(element) is not null || ComplexType(element) || element.SpecialType == SpecialType.System_Boolean);
        private static IrType<Unit>? MaybeElement(IrType<Unit> type) => type is ReferenceType<Unit> reference && reference.Name.Package.Key == Types.Sdk.Key && reference.Name.Module.Key == IrPath.From("Maybe").Key && reference.Name.Local.Key == Name.From("Maybe").Key && reference.Arguments.Count == 1 ? reference.Arguments[0] : null;
        private static FQName MaybeName(string name) => new(Types.Sdk, IrPath.From("Maybe"), Name.From(name));
        private TypedValue Nothing(IrType<Unit> type) => new ConstructorValue<Unit, IrType<Unit>>(type, MaybeName("Nothing"));
        private TypedValue Just(TypedValue value) => new ApplyValue<Unit, IrType<Unit>>(Types.Maybe(value.Attributes),
            new ConstructorValue<Unit, IrType<Unit>>(Types.Function(value.Attributes, Types.Maybe(value.Attributes)), MaybeName("Just")), value);
        private TypedValue Adapt(TypedValue value, IrType<Unit> target)
        {
            if (MaybeElement(target) is { } element && MaybeElement(value.Attributes) is null) return Just(Adapt(value, element));
            return value;
        }
        private TypedValue ReturnValue(TypedValue value)
        {
            var result = Adapt(value, currentReturnSlot);
            if (DecimalIrType(value.Attributes)) decimalDomains[result] = DecimalRange(value);
            return result;
        }
        private string? ValueKey(TypedValue value) => value switch
        {
            VariableValue<Unit, IrType<Unit>> variable => bindingOrigins.GetValueOrDefault(variable.Name.Key) ?? "var:" + variable.Name.Key,
            FieldValue<Unit, IrType<Unit>> field when ValueKey(field.Subject) is { } owner => owner + "/" + field.Field.Key,
            _ => null
        };
        private TypedValue DemandPresent(TypedValue value, Location location)
        {
            if (MaybeElement(value.Attributes) is null) return value;
            if (ValueKey(value) is { } key && presentValues.TryGetValue(key, out var present)) return present;
            if (value is VariableValue<Unit, IrType<Unit>> variable && boundValues.TryGetValue(variable.Name.Key, out var bound)) return DemandPresent(bound, location);
            if (value is FieldValue<Unit, IrType<Unit>> field)
            {
                var subject = field.Subject;
                while (subject is VariableValue<Unit, IrType<Unit>> owner && boundValues.TryGetValue(owner.Name.Key, out var initializer)) subject = initializer;
                if (subject is RecordValue<Unit, IrType<Unit>> record) return DemandPresent(record.Fields.Single(f => f.Key.Key == field.Field.Key).Value, location);
            }
            if (value is ApplyValue<Unit, IrType<Unit>> { Function: ConstructorValue<Unit, IrType<Unit>> constructor, Argument: var argument } && constructor.Name.Local.Key == "just") return argument;
            return Fail<TypedValue>("MCS2501", "A present-value proof is required; annotation and null-forgiving syntax supply no proof.", location);
        }
        private (TypedValue Then, TypedValue Else) Join(TypedValue then, TypedValue otherwise)
        {
            if (MaybeElement(then.Attributes) is not null) otherwise = Adapt(otherwise, then.Attributes);
            else if (MaybeElement(otherwise.Attributes) is not null) then = Adapt(then, otherwise.Attributes);
            return (then, otherwise);
        }
        private TypedValue Choose(TypedValue condition, TypedValue then, TypedValue otherwise)
        {
            (then, otherwise) = Join(then, otherwise);
            return new IfValue<Unit, IrType<Unit>>(then.Attributes, condition, then, otherwise);
        }
        private TypedValue MatchMaybe(TypedValue value, Func<TypedValue> absent, Func<TypedValue, TypedValue> present)
        {
            if (MaybeElement(value.Attributes) is not { } element) return present(value);
            if (value is ApplyValue<Unit, IrType<Unit>> { Function: ConstructorValue<Unit, IrType<Unit>> constructor, Argument: var argument } && constructor.Name.Local.Key == "just") return present(argument);
            if (value is ConstructorValue<Unit, IrType<Unit>> nothingValue && nothingValue.Name.Local.Key == "nothing") return absent();
            var nothing = absent(); var name = Fresh("presentValue"); var variable = new VariableValue<Unit, IrType<Unit>>(element, name);
            bindingOrigins[name.Key] = ValueKey(value);
            var key = ValueKey(value); bool had = key is not null && presentValues.TryGetValue(key, out _); var previous = key is null ? null : presentValues.GetValueOrDefault(key);
            TypedValue just;
            if (key is not null) presentValues[key] = variable;
            try { just = present(variable); }
            finally { if (key is not null) { if (had) presentValues[key] = previous!; else presentValues.Remove(key); } }
            (just, nothing) = Join(just, nothing);
            return new MatchValue<Unit, IrType<Unit>>(just.Attributes, value,
                [new(new ConstructorPattern<IrType<Unit>>(value.Attributes, MaybeName("Nothing"), []), nothing),
                 new(new ConstructorPattern<IrType<Unit>>(value.Attributes, MaybeName("Just"), [new AsPattern<IrType<Unit>>(element, new WildcardPattern<IrType<Unit>>(element), name)]), just)]);
        }
        private static bool Null(IOperation value) => value.ConstantValue is { HasValue: true, Value: null } || value is IConversionOperation conversion && Null(conversion.Operand);
        private static bool HasValue(IPropertyReferenceOperation property) => NullableElement(property.Property.ContainingType) is not null && property.Property.Name == "HasValue" && property.Instance is not null;
        private static bool NullPattern(IPatternOperation pattern, out bool negated)
        {
            negated = false;
            if (pattern is INegatedPatternOperation not) { negated = true; pattern = not.Pattern; }
            return pattern is IConstantPatternOperation constant && Null(constant.Value);
        }
        private static bool PresenceProbe(IOperation operation) => Operations(operation).Any(op => DecimalProbe(op) || FloatProbe(op) || NumericProbe(op) || op is IPropertyReferenceOperation p && HasValue(p) || op is IIsPatternOperation p2 && NullPattern(p2.Pattern, out _) || op is IBinaryOperation b && b.OperatorKind is BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals && (Null(b.LeftOperand) || Null(b.RightOperand)));
        private TypedValue Decision(IOperation condition, Dictionary<ISymbol, Binding> environment, Func<Dictionary<ISymbol, Binding>, TypedValue> yes, Func<Dictionary<ISymbol, Binding>, TypedValue> no)
        {
            if (DecimalDecision(condition, environment, yes, no) is { } decimalDecision) return decimalDecision;
            if (FloatingDecision(condition, environment, yes, no) is { } floatingDecision) return floatingDecision;
            if (NumericDecision(condition, environment, yes, no) is { } numericDecision) return numericDecision;
            if (condition.ConstantValue is { HasValue: true, Value: bool known }) return known ? yes(environment) : no(environment);
            if (condition is IUnaryOperation { OperatorKind: UnaryOperatorKind.Not, IsLifted: false } not && PresenceProbe(not.Operand)) return Decision(not.Operand, environment, no, yes);
            if (condition is IBinaryOperation { OperatorKind: BinaryOperatorKind.ConditionalAnd } and && PresenceProbe(and)) return Decision(and.LeftOperand, environment, state => Decision(and.RightOperand, state, yes, no), no);
            if (condition is IBinaryOperation { OperatorKind: BinaryOperatorKind.ConditionalOr } or && PresenceProbe(or)) return Decision(or.LeftOperand, environment, yes, state => Decision(or.RightOperand, state, yes, no));
            IOperation? tested = null; bool positive = true;
            if (condition is IPropertyReferenceOperation property && HasValue(property)) tested = property.Instance;
            if (condition is IBinaryOperation binary && binary.OperatorKind is BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals && (Null(binary.LeftOperand) || Null(binary.RightOperand)))
            { tested = Null(binary.LeftOperand) ? binary.RightOperand : binary.LeftOperand; positive = binary.OperatorKind == BinaryOperatorKind.NotEquals; }
            if (condition is IIsPatternOperation pattern && NullPattern(pattern.Pattern, out var negated)) { tested = pattern.Value; positive = negated; }
            if (tested is not null)
                return Expression(tested, environment, (value, state) => MatchMaybe(value, () => positive ? no(Copy(state)) : yes(Copy(state)), _ => positive ? yes(Copy(state)) : no(Copy(state))));
            return Expression(condition, environment, (value, state) => DecisionValue(value, state, yes, no));
        }
        private bool PresencePredicate(TypedValue value) => value switch
        {
            MatchValue<Unit, IrType<Unit>> match => match.Cases.Count == 2 && match.Cases.All(c => c.Body is LiteralValue<Unit, IrType<Unit>> { Literal: BoolLiteral }),
            VariableValue<Unit, IrType<Unit>> variable when boundValues.TryGetValue(variable.Name.Key, out var initializer) => PresencePredicate(initializer),
            ApplyValue<Unit, IrType<Unit>> { Function: ReferenceValue<Unit, IrType<Unit>> function, Argument: var argument } => function.Name.Package.Key == Types.Sdk.Key && function.Name.Module.Key == IrPath.From("Basics").Key && function.Name.Local.Key == "not" && PresencePredicate(argument),
            _ => false
        };
        private TypedValue DecisionValue(TypedValue value, Dictionary<ISymbol, Binding> state, Func<Dictionary<ISymbol, Binding>, TypedValue> yes, Func<Dictionary<ISymbol, Binding>, TypedValue> no)
        {
            if (value is VariableValue<Unit, IrType<Unit>> variable && boundValues.TryGetValue(variable.Name.Key, out var initializer) && PresencePredicate(initializer)) return DecisionValue(initializer, state, yes, no);
            if (value is MatchValue<Unit, IrType<Unit>> match && match.Cases.Count == 2 && match.Cases[0].Body is LiteralValue<Unit, IrType<Unit>> { Literal: BoolLiteral missing } && match.Cases[1].Body is LiteralValue<Unit, IrType<Unit>> { Literal: BoolLiteral present })
                return MatchMaybe(match.Subject, () => missing.Value ? yes(Copy(state)) : no(Copy(state)), _ => present.Value ? yes(Copy(state)) : no(Copy(state)));
            if (value is ApplyValue<Unit, IrType<Unit>> { Function: ReferenceValue<Unit, IrType<Unit>> function, Argument: var argument } && function.Name.Package.Key == Types.Sdk.Key && function.Name.Module.Key == IrPath.From("Basics").Key && function.Name.Local.Key == "not" && PresencePredicate(argument))
                return DecisionValue(argument, state, no, yes);
            return Choose(value, yes(Copy(state)), no(Copy(state)));
        }
        private static bool PresenceOperation(IOperation operation) => operation is ICoalesceAssignmentOperation or ICoalesceOperation or IConditionalAccessOperation or IConditionalAccessInstanceOperation or IDefaultValueOperation || operation is IIsPatternOperation pattern && NullPattern(pattern.Pattern, out _) || operation is IConstantPatternOperation constant && Null(constant.Value) || operation is INegatedPatternOperation not && NullPattern(not, out _);
        private static bool PresenceProperty(IPropertyReferenceOperation property) => NullableElement(property.Property.ContainingType) is not null && property.Property.Name is "HasValue" or "Value" || property.Property.ContainingType.SpecialType == SpecialType.System_String && property.Property.Name == "Length";
        private static bool NullableDefaultCall(IInvocationOperation call) => NullableElement(call.TargetMethod.ContainingType) is not null && call.TargetMethod.Name == "GetValueOrDefault" && call.Arguments.Length <= 1 && call.TargetMethod.Parameters.All(p => p.RefKind == RefKind.None) && call.Instance is not null;
        private bool DefaultRecordCreation(IObjectCreationOperation creation) => creation.Type is { IsValueType: true } && records.ContainsKey(SymbolId(creation.Type)) && creation.Arguments.Length == 0 && creation.Initializer is null && creation.Constructor?.Parameters.Length == 0;
        private static bool NullableCreation(IObjectCreationOperation creation) => NullableElement(creation.Type) is not null && creation.Initializer is null && creation.Arguments.Length <= 1;
        private static bool PresenceConversion(IConversionOperation conversion) => Null(conversion.Operand) || conversion.Operand is IDefaultValueOperation || NullableElement(conversion.Type) is not null || NullableElement(conversion.Operand.Type) is not null;
        private static bool PresenceBinary(IBinaryOperation binary) => binary.IsLifted || binary.OperatorKind is BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals && (Null(binary.LeftOperand) || Null(binary.RightOperand));
        private TypedValue Default(ITypeSymbol type, Location location)
        {
            if (NullableElement(type) is not null || type.IsReferenceType) return Nothing(Type(type, location));
            if (DecimalType(type)) return DecimalConstant(0m, location);
            if (Floating(type) is not null) return FloatLiteral(0, location);
            if (ComplexType(type)) return ComplexValue(FloatLiteral(0, location), FloatLiteral(0, location), location);
            if (Integral(type) is { } integer) return IntegerLiteral(BigInteger.Zero, integer, location);
            if (type.SpecialType == SpecialType.System_Boolean) return Lit(Types.Bool, new BoolLiteral(false));
            if (records.TryGetValue(SymbolId(type), out var fields)) return new RecordValue<Unit, IrType<Unit>>(Type(type, location), fields.Select(f => new Entry<Name, TypedValue>(Name.From(f.Name), Default(f.Type, location))).OrderBy(e => e.Key.Key, StringComparer.Ordinal).ToArray());
            return Fail<TypedValue>("MCS2502", "Default construction has no delivered representation.", location);
        }
        private TypedValue? PresenceExpression(IOperation operation, Dictionary<ISymbol, Binding> environment, Func<TypedValue, Dictionary<ISymbol, Binding>, TypedValue> continuation)
        {
            if (operation is ICoalesceAssignmentOperation assignment)
            {
                var symbol = assignment.Target switch { ILocalReferenceOperation local => (ISymbol)local.Local, IParameterReferenceOperation parameter => parameter.Parameter, _ => null };
                if (symbol is null) return Fail<TypedValue>("MCS2404", "Coalescing assignment must rebind a local/by-value parameter.", operation.Syntax.GetLocation());
                return Expression(assignment.Target, environment, (value, state) => MatchMaybe(value,
                    () => Expression(assignment.Value, Copy(state), (replacement, updated) => BindValue(symbol, assignment.Target.Type!, replacement, updated,
                        next => continuation(new VariableValue<Unit, IrType<Unit>>(next[symbol].Type, next[symbol].Name), next), operation.Syntax.GetLocation())),
                    present => continuation(Adapt(present, value.Attributes), Copy(state))));
            }
            if (operation is ICoalesceOperation coalesce)
                return Expression(coalesce.Value, environment, (value, state) => MatchMaybe(value,
                    () => Expression(coalesce.WhenNull, Copy(state), continuation), present => continuation(present, Copy(state))));
            if (operation is IConditionalAccessOperation access)
                return Expression(access.Operation, environment, (value, state) => MatchMaybe(value,
                    () => continuation(Nothing(Type(access.Type!, access.Syntax.GetLocation())), Copy(state)), present =>
                    {
                        conditionalInstances.Push(present);
                        try { return Expression(access.WhenNotNull, Copy(state), (result, updated) => continuation(MaybeElement(result.Attributes) is null ? Just(result) : result, updated)); }
                        finally { conditionalInstances.Pop(); }
                    }));
            return null;
        }
        private TypedValue Lifted(IBinaryOperation operation)
        {
            var location = operation.Syntax.GetLocation();
            bool allowed = operation.OperatorKind is BinaryOperatorKind.Add or BinaryOperatorKind.Subtract or BinaryOperatorKind.Multiply or BinaryOperatorKind.Divide or BinaryOperatorKind.Remainder or BinaryOperatorKind.LeftShift or BinaryOperatorKind.RightShift or BinaryOperatorKind.UnsignedRightShift or BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals or BinaryOperatorKind.GreaterThan or BinaryOperatorKind.GreaterThanOrEqual or BinaryOperatorKind.LessThan or BinaryOperatorKind.LessThanOrEqual or BinaryOperatorKind.And or BinaryOperatorKind.Or or BinaryOperatorKind.ExclusiveOr;
            if (!allowed) return Fail<TypedValue>("MCS2106", "Unmapped lifted operator.", location);
            if (operation.OperatorMethod is not null && !(NumericType(NullableElement(operation.LeftOperand.Type) ?? operation.LeftOperand.Type) || DecimalType(NullableElement(operation.LeftOperand.Type) ?? operation.LeftOperand.Type))) return Fail<TypedValue>("MCS2106", "Unmapped lifted user operator.", location);
            var left = Lower(operation.LeftOperand); var right = Lower(operation.RightOperand);
            if (operation.OperatorKind == BinaryOperatorKind.Add && (left is ConstructorValue<Unit, IrType<Unit>> { Name.Local.Key: "nothing" } || right is ConstructorValue<Unit, IrType<Unit>> { Name.Local.Key: "nothing" })) provedArithmetic.Add(operation);
            bool equality = operation.OperatorKind is BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals;
            bool boolean = (NullableElement(operation.LeftOperand.Type) ?? operation.LeftOperand.Type)?.SpecialType == SpecialType.System_Boolean;
            bool nullableResult = NullableElement(operation.Type) is not null;
            var resultType = Type(operation.Type!, location, true);
            TypedValue Missing() => nullableResult ? Nothing(resultType) : Lit(Types.Bool, new BoolLiteral(false));
            TypedValue Compare(TypedValue a, TypedValue b)
            {
                var leftType = NullableElement(operation.LeftOperand.Type) ?? operation.LeftOperand.Type;
                var rightType = NullableElement(operation.RightOperand.Type) ?? operation.RightOperand.Type;
                if (DecimalType(leftType) && DecimalType(rightType))
                {
                    var result = DecimalBinary(operation.OperatorKind, a, b, location);
                    return nullableResult ? Just(result) : result;
                }
                if (Integral(leftType) is { } leftDomain && Integral(rightType) is { } rightDomain)
                {
                    var result = IntegerBinary(operation, a, b, leftDomain, rightDomain) ?? Fail<TypedValue>("MCS2106", "Unmapped lifted integer operation.", location);
                    return nullableResult ? Just(result) : result;
                }
                if (Floating(leftType) is not null && Floating(rightType) is not null)
                {
                    var result = FloatingBinary(operation, a, b) ?? Fail<TypedValue>("MCS2106", "Unmapped lifted floating operation.", location);
                    return nullableResult ? Just(result) : result;
                }
                if (ComplexType(leftType) || ComplexType(rightType))
                {
                    var result = ComplexBinary(operation, a, b) ?? Fail<TypedValue>("MCS2106", "Unmapped lifted complex operation.", location);
                    return nullableResult ? Just(result) : result;
                }
                if (operation.IsChecked && operation.OperatorKind == BinaryOperatorKind.Add) return Fail<TypedValue>("MCS2004", "Checked semantic arithmetic is excluded.", location);
                string? name = operation.OperatorKind switch
                {
                    BinaryOperatorKind.Add => "add",
                    BinaryOperatorKind.Equals => "equal",
                    BinaryOperatorKind.NotEquals => "notEqual",
                    BinaryOperatorKind.GreaterThan => "greaterThan",
                    BinaryOperatorKind.GreaterThanOrEqual => "greaterThanOrEqual",
                    BinaryOperatorKind.LessThan => "lessThan",
                    BinaryOperatorKind.LessThanOrEqual => "lessThanOrEqual",
                    BinaryOperatorKind.And when boolean => "and",
                    BinaryOperatorKind.Or when boolean => "or",
                    BinaryOperatorKind.ExclusiveOr when boolean => "xor",
                    _ => null
                };
                if (name is null) return Fail<TypedValue>("MCS2106", "Unmapped lifted operator.", location);
                if (operation.OperatorKind == BinaryOperatorKind.Add)
                {
                    provedArithmetic.Add(operation);
                    var x = ValueConstant(a); var y = ValueConstant(b);
                    if (!(x is int i && i == 0 || y is int j && j == 0 || x is int k && y is int l && (long)k + l is >= int.MinValue and <= int.MaxValue)) return Fail<TypedValue>("MCS2302", "Lifted Int32 addition requires the delivered safety proof.", location);
                }
                var output = nullableResult ? MaybeElement(resultType)! : resultType;
                var value = SdkBinary(name, a.Attributes, output, a, b);
                return nullableResult ? Just(value) : value;
            }
            if (boolean && operation.OperatorKind is BinaryOperatorKind.And or BinaryOperatorKind.Or)
            {
                bool and = operation.OperatorKind == BinaryOperatorKind.And;
                return MatchMaybe(left, () => MatchMaybe(right, () => Nothing(resultType), b => Choose(b, and ? Nothing(resultType) : Just(b), and ? Just(b) : Nothing(resultType))),
                    a => MatchMaybe(right, () => Choose(a, and ? Nothing(resultType) : Just(a), and ? Just(a) : Nothing(resultType)), b => Compare(a, b)));
            }
            return MatchMaybe(left,
                () => equality ? MatchMaybe(right, () => Lit(Types.Bool, new BoolLiteral(operation.OperatorKind == BinaryOperatorKind.Equals)), _ => Lit(Types.Bool, new BoolLiteral(operation.OperatorKind == BinaryOperatorKind.NotEquals))) : Missing(),
                a => MatchMaybe(right, () => equality ? Lit(Types.Bool, new BoolLiteral(operation.OperatorKind == BinaryOperatorKind.NotEquals)) : Missing(), b => Compare(a, b)));
        }
        private TypedValue? LowerPresence(IOperation operation)
        {
            var location = operation.Syntax.GetLocation();
            if (operation is ILiteralOperation { ConstantValue: { HasValue: true, Value: null } })
            {
                var nullableType = operation.Type is null ? currentReturnSlot : Type(operation.Type, location);
                return Nothing(MaybeElement(nullableType) is null ? Types.Maybe(nullableType) : nullableType);
            }
            if (operation is IInvocationOperation call && NullableDefaultCall(call)) return MatchMaybe(Lower(call.Instance!),
                () => call.Arguments.Length == 0 ? Default(NullableElement(call.TargetMethod.ContainingType)!, location) : Lower(call.Arguments[0].Value), value => value);
            if (operation is IObjectCreationOperation recordCreation && DefaultRecordCreation(recordCreation)) return Default(recordCreation.Type!, location);
            if (operation is IObjectCreationOperation creation && NullableCreation(creation))
                return creation.Arguments.Length == 0 ? Nothing(Type(creation.Type!, location)) : Just(Lower(creation.Arguments[0].Value));
            if (operation is IDefaultValueOperation && operation.Type is { } defaultType) return Default(defaultType, location);
            if (operation is IConditionalAccessInstanceOperation) return conditionalInstances.Peek();
            if (operation is ICoalesceAssignmentOperation or ICoalesceOperation or IConditionalAccessOperation) return Expression(operation, Copy(bindings), (value, _) => value);
            if (operation is IConversionOperation conversion && PresenceConversion(conversion))
            {
                var target = Type(conversion.Type!, location, true);
                if (Null(conversion.Operand)) return Nothing(MaybeElement(target) is null ? Types.Maybe(target) : target);
                var value = Lower(conversion.Operand);
                if (MaybeElement(target) is not null) return Adapt(value, target);
                return DemandPresent(value, location);
            }
            if (operation is IPropertyReferenceOperation property && property.Instance is not null)
            {
                if (HasValue(property)) return MatchMaybe(Lower(property.Instance), () => Lit(Types.Bool, new BoolLiteral(false)), _ => Lit(Types.Bool, new BoolLiteral(true)));
                if (NullableElement(property.Property.ContainingType) is not null && property.Property.Name == "Value") return DemandPresent(Lower(property.Instance), location);
                if (property.Property.ContainingType.SpecialType == SpecialType.System_String && property.Property.Name == "Length")
                    return new ApplyValue<Unit, IrType<Unit>>(Types.Int, new ReferenceValue<Unit, IrType<Unit>>(Types.Function(Types.String, Types.Int), new(Types.Sdk, IrPath.From("String"), Name.From("length"))), DemandPresent(Lower(property.Instance), location));
            }
            if (operation is IBinaryOperation { IsLifted: true } lifted && !(lifted.OperatorKind is BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals && (Null(lifted.LeftOperand) || Null(lifted.RightOperand)))) return Lifted(lifted);
            if (operation is IUnaryOperation { IsLifted: true } numericUnary && (NumericType(NullableElement(numericUnary.Operand.Type) ?? numericUnary.Operand.Type) || DecimalType(NullableElement(numericUnary.Operand.Type) ?? numericUnary.Operand.Type)))
                return MatchMaybe(Lower(numericUnary.Operand), () => Nothing(Type(numericUnary.Type!, location)), present => Just(NumericUnary(numericUnary, present)));
            if (operation is IUnaryOperation { IsLifted: true, OperatorKind: UnaryOperatorKind.Not } unary)
                return MatchMaybe(Lower(unary.Operand), () => Nothing(Types.Maybe(Types.Bool)), value => Just(new ApplyValue<Unit, IrType<Unit>>(Types.Bool,
                    new ReferenceValue<Unit, IrType<Unit>>(Types.Function(Types.Bool, Types.Bool), new(Types.Sdk, IrPath.From("Basics"), Name.From("not"))), value)));
            if (operation is IIsPatternOperation pattern && NullPattern(pattern.Pattern, out _) || operation is IBinaryOperation binary && binary.OperatorKind is BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals && (Null(binary.LeftOperand) || Null(binary.RightOperand)))
                return Decision(operation, Copy(bindings), _ => Lit(Types.Bool, new BoolLiteral(true)), _ => Lit(Types.Bool, new BoolLiteral(false)));
            return null;
        }
    }
}
