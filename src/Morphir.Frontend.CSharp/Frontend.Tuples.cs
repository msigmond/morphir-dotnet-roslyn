using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Morphir.IR.Classic;
using TypedValue = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        private readonly HashSet<IOperation> provedTupleOperations = [];
        private readonly Dictionary<TypedValue, TypedValue> tupleProjections = new(ReferenceEqualityComparer.Instance);
        private static bool TupleType(ITypeSymbol? type) => type is INamedTypeSymbol named && named.ContainingAssembly.Name == "System.Runtime" && named.ContainingAssembly.Identity.Version == new Version(10, 0, 0, 0) && (named.IsTupleType || named.ContainingNamespace.ToDisplayString() == "System" && named.Name is "Tuple" or "ValueTuple");
        private static ITypeSymbol[] TupleElements(ITypeSymbol type)
        {
            var named = (INamedTypeSymbol)type;
            return named.IsTupleType ? named.TupleElements.Select(f => f.Type).ToArray() : named.TypeArguments.ToArray();
        }
        private IrType<Unit> TupleIrType(ITypeSymbol type, Location? location)
        {
            var tuple = new TupleType<Unit>(default, TupleElements(type).Select((element, index) =>
            {
                var slot = Type(element, location);
                // The baseline Tuple<T1,...,T7,TRest> constructor enforces an actual
                // nonnull tuple rest. This is a framework data invariant, unlike ItemN.
                return type is INamedTypeSymbol { IsReferenceType: true, Arity: 8 } && index == 7 && TupleType(element) && element.IsReferenceType ? MaybeElement(slot) ?? slot : slot;
            }).ToArray());
            return type.IsReferenceType ? Types.Maybe(tuple) : tuple;
        }
        private bool TupleConstruction(IObjectCreationOperation operation) => TupleType(operation.Type) && operation.Initializer is null && (operation.Arguments.Length == 0 && operation.Type!.IsValueType || operation.Arguments.Length == TupleElements(operation.Type!).Length);
        private static bool TupleFactory(IInvocationOperation call) => call.TargetMethod.Name == "Create" && call.Instance is null && TupleType(call.Type) && call.TargetMethod.ContainingType is { Name: "Tuple" or "ValueTuple", Arity: 0 } owner && TupleType(owner) && call.Arguments.Length > 0;
        private int TupleIndex(ISymbol member)
        {
            var owner = member.ContainingType;
            if (owner.IsTupleType && member is IFieldSymbol field)
            {
                for (int i = 0; i < owner.TupleElements.Length; i++) if (SymbolEqualityComparer.Default.Equals(owner.TupleElements[i], field) || SymbolEqualityComparer.Default.Equals(owner.TupleElements[i].CorrespondingTupleField, field.CorrespondingTupleField ?? field)) return i;
            }
            return member.Name.StartsWith("Item", StringComparison.Ordinal) && int.TryParse(member.Name.AsSpan(4), out var index) ? index - 1 : -1;
        }
        private TypedValue TupleAt(TypedValue value, int index, Location location)
        {
            value = DemandPresent(value, location);
            if (value.Attributes is not TupleType<Unit> tuple || index < 0 || index >= tuple.Elements.Count) return Fail<TypedValue>("MCS2904", "Tuple member has no mapped element.", location);
            var name = Fresh("tupleElement"); var element = tuple.Elements[index];
            var result = new MatchValue<Unit, IrType<Unit>>(element, value, [new(new TuplePattern<IrType<Unit>>(tuple, tuple.Elements.Select((t, i) => (Pattern<IrType<Unit>>)(i == index ? new AsPattern<IrType<Unit>>(t, new WildcardPattern<IrType<Unit>>(t), name) : new WildcardPattern<IrType<Unit>>(t))).ToArray()), new VariableValue<Unit, IrType<Unit>>(element, name))]);
            var origin = value; while (origin is VariableValue<Unit, IrType<Unit>> variable && boundValues.TryGetValue(variable.Name.Key, out var bound)) origin = bound;
            if (origin is TupleValue<Unit, IrType<Unit>> known) tupleProjections[result] = known.Elements[index];
            return result;
        }
        private TypedValue MakeTuple(ITypeSymbol type, IReadOnlyList<TypedValue> values, Location location)
        {
            var elements = TupleElements(type);
            if (type is INamedTypeSymbol { Arity: 8, IsTupleType: false })
            {
                if (!TupleType(elements[7]) || elements[7].IsReferenceType != type.IsReferenceType) return Fail<TypedValue>("MCS2906", "Tuple rest construction requires a tuple of the same family.", location);
                if (type.IsReferenceType) DemandPresent(values[7], location);
            }
            var tuple = (TupleType<Unit>)(MaybeElement(Type(type, location)) ?? Type(type, location));
            if (values.Count != elements.Length) return Fail<TypedValue>("MCS2904", "Tuple arity does not match its source type.", location);
            TypedValue value = new TupleValue<Unit, IrType<Unit>>(tuple, values.Select((v, i) => Adapt(type.IsReferenceType && elements.Length == 8 && i == 7 ? DemandPresent(v, location) : v, tuple.Elements[i])).ToArray());
            return type.IsReferenceType ? Just(value) : value;
        }
        private static bool SameTupleElementType(ITypeSymbol source, ITypeSymbol target) => SymbolEqualityComparer.Default.Equals(source, target) || TupleType(source) && TupleType(target) && source.IsReferenceType == target.IsReferenceType && TupleElements(source).Length == TupleElements(target).Length && TupleElements(source).Zip(TupleElements(target)).All(pair => SameTupleElementType(pair.First, pair.Second));
        private TypedValue TupleConvert(TypedValue value, ITypeSymbol source, ITypeSymbol target, Location location)
        {
            // Element labels affect source lookup only; they do not reconstruct runtime data.
            if (SameTupleElementType(source, target)) return value;
            var from = TupleElements(source); var to = TupleElements(target);
            if (from.Length != to.Length) return Fail<TypedValue>("MCS2904", "Tuple conversion must retain arity.", location);
            return MakeTuple(target, from.Select((t, i) => ConvertTupleElement(TupleAt(value, i, location), t, to[i], location)).ToArray(), location);
        }
        private TypedValue ConvertTupleElement(TypedValue value, ITypeSymbol source, ITypeSymbol target, Location location)
        {
            if (SymbolEqualityComparer.Default.Equals(source, target)) return Adapt(value, Type(target, location));
            if (NullableElement(target) is { } targetElement) return MatchMaybe(value, () => Nothing(Type(target, location)), present => Just(ConvertTupleElement(present, NullableElement(source) ?? source, targetElement, location)));
            if (TupleType(source) && TupleType(target)) return TupleConvert(value, source, target, location);
            if (EnumType(source) is not null || EnumType(target) is not null) return EnumConvert(value, source, target, location);
            if (DecimalType(source) || DecimalType(target)) return ConvertDecimalValue(value, source, target, location);
            return ConvertNumericValue(value, source, target, location);
        }
        private TypedValue TupleEqualElement(TypedValue left, TypedValue right, ITypeSymbol leftType, ITypeSymbol rightType, Location location)
        {
            if (NullableElement(leftType) is not null || NullableElement(rightType) is not null)
                return MatchMaybe(left, () => MatchMaybe(right, () => Lit(Types.Bool, new BoolLiteral(true)), _ => Lit(Types.Bool, new BoolLiteral(false))), a => MatchMaybe(right, () => Lit(Types.Bool, new BoolLiteral(false)), b => TupleEqualElement(a, b, NullableElement(leftType) ?? leftType, NullableElement(rightType) ?? rightType, location)));
            if (TupleType(leftType) && TupleType(rightType) && leftType.IsValueType && rightType.IsValueType) return TupleEqual(left, right, leftType, rightType, location);
            if (EnumType(leftType) is { } e) left = UnwrapEnum(e, left, location);
            if (EnumType(rightType) is { } f) right = UnwrapEnum(f, right, location);
            leftType = NumericUnderlying(leftType); rightType = NumericUnderlying(rightType);
            if (Integral(leftType) is { } aDomain && Integral(rightType) is { } bDomain)
            {
                if (!aDomain.Wide && !bDomain.Wide) return SdkBinary("equal", Types.Int, Types.Bool, left, right);
                return SdkBinary("equal", Types.Int, Types.Bool, NumericCall("compare", location, AsInteger(left, aDomain, location), AsInteger(right, bDomain, location)), Lit(Types.Int, new IntLiteral(0)));
            }
            if (DecimalType(leftType) || DecimalType(rightType))
            {
                if (!DecimalType(leftType)) left = ConvertDecimalValue(left, leftType, rightType, location);
                if (!DecimalType(rightType)) right = ConvertDecimalValue(right, rightType, leftType, location);
                return DecimalBinary(BinaryOperatorKind.Equals, left, right, location);
            }
            if (ComplexType(leftType) || ComplexType(rightType))
            {
                if (!ComplexType(leftType)) left = ConvertNumericValue(left, leftType, rightType, location);
                if (!ComplexType(rightType)) right = ConvertNumericValue(right, rightType, leftType, location);
                return new IfValue<Unit, IrType<Unit>>(Types.Bool, SdkBinary("equal", Types.Float, Types.Bool, Component(left, "real"), Component(right, "real")), SdkBinary("equal", Types.Float, Types.Bool, Component(left, "imaginary"), Component(right, "imaginary")), Lit(Types.Bool, new BoolLiteral(false)));
            }
            if (Floating(leftType) is { } || Floating(rightType) is { })
            {
                var target = (Floating(leftType) ?? 0) >= (Floating(rightType) ?? 0) ? leftType : rightType;
                left = ConvertNumericValue(left, leftType, target, location); right = ConvertNumericValue(right, rightType, target, location);
                return SdkBinary("equal", Types.Float, Types.Bool, left, right);
            }
            if (leftType.SpecialType == SpecialType.System_Boolean && rightType.SpecialType == SpecialType.System_Boolean) return SdkBinary("equal", Types.Bool, Types.Bool, left, right);
            if (leftType.SpecialType == SpecialType.System_String && rightType.SpecialType == SpecialType.System_String) return SdkBinary("equal", left.Attributes, Types.Bool, left, right);
            return Fail<TypedValue>("MCS2907", "Tuple equality requires exact delivered element operators; reference identity and unknown user operators are not structural equality.", location);
        }
        private TypedValue TupleEqual(TypedValue left, TypedValue right, ITypeSymbol leftType, ITypeSymbol rightType, Location location)
        {
            var a = TupleElements(leftType); var b = TupleElements(rightType); if (a.Length != b.Length) return Fail<TypedValue>("MCS2907", "Tuple equality arities differ.", location);
            TypedValue result = Lit(Types.Bool, new BoolLiteral(true));
            for (int i = a.Length - 1; i >= 0; i--) result = new IfValue<Unit, IrType<Unit>>(Types.Bool, TupleEqualElement(TupleAt(left, i, location), TupleAt(right, i, location), a[i], b[i], location), result, Lit(Types.Bool, new BoolLiteral(false)));
            return result;
        }
        private TypedValue TupleBinary(ITupleBinaryOperation binary)
        {
            var location = binary.Syntax.GetLocation(); var left = Lower(binary.LeftOperand); var right = Lower(binary.RightOperand);
            var leftType = NullableElement(binary.LeftOperand.Type) ?? binary.LeftOperand.Type!; var rightType = NullableElement(binary.RightOperand.Type) ?? binary.RightOperand.Type!;
            var result = MatchMaybe(left, () => MatchMaybe(right, () => Lit(Types.Bool, new BoolLiteral(true)), _ => Lit(Types.Bool, new BoolLiteral(false))), a => MatchMaybe(right, () => Lit(Types.Bool, new BoolLiteral(false)), b => TupleEqual(a, b, leftType, rightType, location)));
            return binary.OperatorKind == BinaryOperatorKind.Equals ? result : SdkUnary("not", Types.Bool, Types.Bool, result);
        }
        private static bool TupleSafetyOperation(IOperation operation) => operation is ITupleBinaryOperation || operation is IObjectCreationOperation creation && TupleType(creation.Type) || operation is IInvocationOperation call && TupleFactory(call) || operation is IPropertyReferenceOperation property && TupleType(property.Property.ContainingType) || operation is IConversionOperation conversion && TupleType(NullableElement(conversion.Type) ?? conversion.Type) && TupleType(NullableElement(conversion.Operand.Type) ?? conversion.Operand.Type);
        private TypedValue? LowerTuple(IOperation operation)
        {
            var value = LowerTupleCore(operation); if (value is not null) provedTupleOperations.Add(operation); return value;
        }
        private TypedValue? LowerTupleCore(IOperation operation)
        {
            var location = operation.Syntax.GetLocation();
            if (operation is ITupleBinaryOperation binary) return TupleBinary(binary);
            if (operation is ITupleOperation tuple) return MakeTuple(tuple.Type!, tuple.Elements.Select(Lower).ToArray(), location);
            if (operation is IConversionOperation conversion && TupleType(NullableElement(conversion.Type) ?? conversion.Type) && TupleType(NullableElement(conversion.Operand.Type) ?? conversion.Operand.Type))
            {
                if (conversion.IsChecked) return Fail<TypedValue>("MCS2004", "Checked tuple element conversions are excluded.", location);
                var source = NullableElement(conversion.Operand.Type) ?? conversion.Operand.Type!; var target = NullableElement(conversion.Type) ?? conversion.Type!; var value = Lower(conversion.Operand);
                if (NullableElement(conversion.Type) is not null) return MatchMaybe(value, () => Nothing(Type(conversion.Type!, location)), present => Just(TupleConvert(present, source, target, location)));
                return TupleConvert(NullableElement(conversion.Operand.Type) is not null ? DemandPresent(value, location) : value, source, target, location);
            }
            if (operation is IObjectCreationOperation creation && TupleConstruction(creation)) return creation.Arguments.Length == 0 ? Default(creation.Type!, location) : MakeTuple(creation.Type!, creation.Arguments.OrderBy(a => a.Parameter!.Ordinal).Select(a => Lower(a.Value)).ToArray(), location);
            if (operation is IInvocationOperation factory && TupleFactory(factory))
            {
                // Tuple.Create with eight arguments constructs a nested Tuple<T8> rest.
                var values = factory.Arguments.OrderBy(a => a.Parameter!.Ordinal).Select(a => Lower(a.Value)).ToArray();
                var elements = TupleElements(factory.Type!);
                if (factory.TargetMethod.ContainingType.Name == "Tuple" && values.Length == 8) values[7] = MakeTuple(elements[7], [values[7]], location);
                return MakeTuple(factory.Type!, values, location);
            }
            if (operation is IFieldReferenceOperation { Instance: not null } field && TupleType(field.Field.ContainingType))
            {
                if (field.Field.Name == "Rest" && field.Field.ContainingType.IsTupleType)
                {
                    var value = Lower(field.Instance); var tail = ((TupleType<Unit>)value.Attributes).Elements.Skip(7).ToArray();
                    return new TupleValue<Unit, IrType<Unit>>(new TupleType<Unit>(default, tail), tail.Select((_, i) => TupleAt(value, i + 7, location)).ToArray());
                }
                return TupleAt(Lower(field.Instance), TupleIndex(field.Field), location);
            }
            if (operation is IPropertyReferenceOperation { Instance: not null } property && TupleType(property.Property.ContainingType)) return TupleAt(Lower(property.Instance), property.Property.Name == "Rest" ? 7 : TupleIndex(property.Property), location);
            return null;
        }
        private TypedValue Deconstruct(IOperation target, TypedValue value, ITypeSymbol sourceType, Dictionary<ISymbol, Binding> environment, Func<Dictionary<ISymbol, Binding>, TypedValue> continuation, Location location)
        {
            if (target is IDeclarationExpressionOperation declaration) return Deconstruct(declaration.Expression, value, sourceType, environment, continuation, location);
            if (target is IDiscardOperation) return continuation(environment);
            if (target is ITupleOperation tuple)
            {
                TypedValue Element(int index, Dictionary<ISymbol, Binding> state) => index == tuple.Elements.Length ? continuation(state) : Deconstruct(tuple.Elements[index], TupleAt(value, index, location), TupleElements(sourceType)[index], state, next => Element(index + 1, next), location);
                return Element(0, environment);
            }
            if (target is IFieldReferenceOperation field && TupleLocalTarget(field)) return AssignTupleField(field, ConvertTupleElement(value, sourceType, target.Type!, location), environment, (_, next) => continuation(next), location);
            var symbol = target switch { ILocalReferenceOperation local => (ISymbol)local.Local, IParameterReferenceOperation parameter => parameter.Parameter, _ => null };
            if (symbol is null) return Fail<TypedValue>("MCS2905", "Deconstruction targets must be local/by-value bindings or discards.", location);
            return BindValue(symbol, target.Type!, ConvertTupleElement(value, sourceType, target.Type!, location), environment, continuation, location);
        }
        private static bool TupleLocalTarget(IOperation target) => target is IFieldReferenceOperation { Instance: not null } field && TupleType(field.Field.ContainingType) && field.Field.ContainingType.IsValueType && (field.Instance is ILocalReferenceOperation or IParameterReferenceOperation || TupleLocalTarget(field.Instance));
        private TypedValue AssignTupleField(IFieldReferenceOperation target, TypedValue value, Dictionary<ISymbol, Binding> environment, Func<TypedValue, Dictionary<ISymbol, Binding>, TypedValue> continuation, Location location)
        {
            var path = new List<int>(); IOperation root = target;
            while (root is IFieldReferenceOperation { Instance: not null } field) { var index = TupleIndex(field.Field); if (index < 0) return Fail<TypedValue>("MCS2905", "Tuple location requires a mapped element.", location); path.Insert(0, index); root = field.Instance; }
            var symbol = root switch { ILocalReferenceOperation local => (ISymbol)local.Local, IParameterReferenceOperation parameter => parameter.Parameter, _ => null };
            if (symbol is null || !environment.TryGetValue(symbol, out var binding)) return Fail<TypedValue>("MCS2905", "Tuple writes require a definitely assigned local/by-value root.", location);
            TypedValue Replace(TypedValue subject, int depth)
            {
                var tuple = (TupleType<Unit>)subject.Attributes;
                return new TupleValue<Unit, IrType<Unit>>(tuple, tuple.Elements.Select((_, i) => i == path[depth] ? (depth == path.Count - 1 ? Adapt(value, tuple.Elements[i]) : Replace(TupleAt(subject, i, location), depth + 1)) : TupleAt(subject, i, location)).ToArray());
            }
            var changed = Replace(new VariableValue<Unit, IrType<Unit>>(binding.Type, binding.Name), 0);
            return BindValue(symbol, root.Type!, changed, environment, next => continuation(value, next), location);
        }
        private TypedValue? TupleExpression(IOperation operation, Dictionary<ISymbol, Binding> environment, Func<TypedValue, Dictionary<ISymbol, Binding>, TypedValue> continuation)
        {
            if (operation is ISimpleAssignmentOperation { Target: IFieldReferenceOperation target } update && TupleLocalTarget(target)) return Expression(update.Value, environment, (value, state) => AssignTupleField(target, value, state, continuation, operation.Syntax.GetLocation()));
            if (operation is not IDeconstructionAssignmentOperation assignment) return null;
            var location = operation.Syntax.GetLocation();
            if (!TupleType(assignment.Value.Type)) return Fail<TypedValue>("MCS2905", "D8 deconstruction requires tuple data; user deconstructors need their later data/call gates.", location);
            return Expression(assignment.Value, environment, (value, state) =>
            {
                var name = Fresh("deconstructionValue"); var variable = new VariableValue<Unit, IrType<Unit>>(value.Attributes, name); boundValues[name.Key] = value;
                var body = Deconstruct(assignment.Target, variable, assignment.Value.Type!, state, next => continuation(variable, next), location);
                return new LetValue<Unit, IrType<Unit>>(body.Attributes, name, new([], value.Attributes, value), body);
            });
        }
    }
}
