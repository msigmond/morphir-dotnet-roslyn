using System.Numerics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Morphir.IR.Classic;
using TypedValue = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        private static bool DecimalProperty(IPropertyReferenceOperation property) => DecimalType(property.Property.ContainingType) && property.Instance is not null && property.Property.Name == "Scale";
        private static bool DecimalConstruction(IObjectCreationOperation creation) => DecimalType(creation.Type) && creation.Initializer is null && (creation.Arguments.Length == 0 || creation.Arguments.Length == 1 && (Integral(creation.Arguments[0].Value.Type) is not null || Floating(creation.Arguments[0].Value.Type) is not null) || creation.Arguments.Length == 5 && creation.Constructor!.Parameters.Select(p => p.Type.SpecialType).SequenceEqual(new[] { SpecialType.System_Int32, SpecialType.System_Int32, SpecialType.System_Int32, SpecialType.System_Boolean, SpecialType.System_Byte }));
        private static bool DecimalMember(IInvocationOperation call) => DecimalMethod(call.TargetMethod);
        private static bool DecimalMethod(IMethodSymbol method)
        {
            if (!DecimalType(method.ContainingType) || method.Arity != 0 || method.Parameters.Any(p => p.RefKind != RefKind.None)) return false;
            if (!method.IsStatic) return method.Name is "CompareTo" or "Equals" && method.Parameters.Length == 1 && DecimalType(method.Parameters[0].Type);
            if (method.Name == "Round") return method.Parameters.Length is 1 or 2 && DecimalType(method.Parameters[0].Type) && (method.Parameters.Length == 1 || method.Parameters[1].Type.SpecialType == SpecialType.System_Int32);
            if (method.Parameters.Length == 1 && DecimalType(method.Parameters[0].Type)) return method.Name is "Abs" or "Negate" or "Ceiling" or "Floor" or "Truncate" or "Sign" or "IsNegative";
            return method.Parameters.Length == 2 && method.Parameters.All(p => DecimalType(p.Type)) && method.Name is "Add" or "Subtract" or "Multiply" or "Divide" or "Remainder" or "Compare" or "Equals" or "Max" or "Min";
        }
        private TypedValue ConvertDecimalValue(TypedValue value, ITypeSymbol source, ITypeSymbol target, Location location, bool isChecked = false)
        {
            if (isChecked) return Fail<TypedValue>("MCS2004", "Checked semantic conversion is excluded.", location);
            if (DecimalType(target))
            {
                if (DecimalType(source)) return value;
                if (Integral(source) is { } integer) return DecimalFromInteger(value, integer, location);
                return Fail<TypedValue>("MCS2704", "Binary-floating Decimal construction is excluded.", location);
            }
            if (Integral(target) is { } result)
            {
                var domain = DecimalRange(value); var min = domain.Minimum.Numerator / domain.Minimum.Denominator; var max = domain.Maximum.Numerator / domain.Maximum.Denominator;
                if (result.Bits != 0 && (min < result.Minimum || max > result.Maximum)) return Fail<TypedValue>("MCS2703", "Decimal integral conversion requires proved target bounds after truncation.", location);
                var converted = DecimalCall("toInteger", location, value); if (result.Bits != 0) converted = FromInteger(converted, result, location);
                numericValueRanges[converted] = new(min, max, new HashSet<BigInteger>()); return converted;
            }
            if (Floating(target) is not null || ComplexType(target))
            {
                var real = DecimalToDouble(value, location);
                if (ComplexType(target)) return ComplexValue(real, FloatLiteral(0, location), location);
                var precision = Floating(target)!.Value;
                // Half's Decimal conversion goes through Single, just like the pinned runtime.
                if (precision == 11) real = RoundFloat(real, 24, location);
                return RoundFloat(real, precision, location);
            }
            return Fail<TypedValue>("MCS2705", "Decimal conversion is not implemented in this active increment.", location);
        }
        private TypedValue DecimalToDouble(TypedValue value, Location location)
        {
            var coefficient = new FieldValue<Unit, IrType<Unit>>(IntegerType(new(0, true), location), value, Name.From("coefficient"));
            var low = NumericCall("wrap", location, Lit(Types.Int, new IntLiteral(64)), Lit(Types.Bool, new BoolLiteral(false)), coefficient);
            var high = NumericCall("shiftRight", location, coefficient, Lit(Types.Int, new IntLiteral(64)));
            TypedValue ToDouble(TypedValue integer) => NumericCall("toBinaryFloat", location, Lit(Types.Int, new IntLiteral(53)), integer);
            var whole = SdkBinary("add", Types.Float, Types.Float, ToDouble(low), SdkBinary("multiply", Types.Float, Types.Float, ToDouble(high), FloatLiteral(18446744073709551616d, location)));
            var scale = new FieldValue<Unit, IrType<Unit>>(Types.Int, value, Name.From("scale"));
            TypedValue divisor = FloatLiteral(double.Parse("1e28", System.Globalization.CultureInfo.InvariantCulture), location);
            for (var i = 27; i >= 0; i--) divisor = Choose(SdkBinary("equal", Types.Int, Types.Bool, scale, Lit(Types.Int, new IntLiteral(i))), FloatLiteral(double.Parse("1e" + i, System.Globalization.CultureInfo.InvariantCulture), location), divisor);
            var magnitude = SdkBinary("divide", Types.Float, Types.Float, whole, divisor);
            return Choose(new FieldValue<Unit, IrType<Unit>>(Types.Bool, value, Name.From("negative")), SdkUnary("negate", Types.Float, Types.Float, magnitude), magnitude);
        }
        private TypedValue DecimalUnary(UnaryOperatorKind kind, TypedValue value, Location location)
        {
            if (kind == UnaryOperatorKind.Plus) return value;
            if (kind != UnaryOperatorKind.Minus) return Fail<TypedValue>("MCS2705", "Unmapped Decimal unary operator.", location);
            var result = DecimalCall("negate", location, value); var range = DecimalRange(value);
            decimalDomains[result] = new(DecimalZero - range.Maximum, DecimalZero - range.Minimum, range.Nonzero); return result;
        }
        private TypedValue? NullableDecimalConversion(IConversionOperation conversion)
        {
            var source = NullableElement(conversion.Operand.Type) ?? conversion.Operand.Type;
            var target = NullableElement(conversion.Type) ?? conversion.Type;
            if (!(DecimalType(source) || DecimalType(target)) || NullableElement(conversion.Operand.Type) is null && NullableElement(conversion.Type) is null) return null;
            var location = conversion.Syntax.GetLocation();
            if (Null(conversion.Operand) && NullableElement(conversion.Type) is not null) return Nothing(Type(conversion.Type!, location));
            var value = Lower(conversion.Operand);
            TypedValue Convert(TypedValue present) => DecimalType(source) && DecimalType(target) ? present : ConvertDecimalValue(present, source!, target!, location, conversion.IsChecked);
            return NullableElement(conversion.Type) is not null ? MatchMaybe(value, () => Nothing(Type(conversion.Type!, location)), present => Just(Convert(present))) : Convert(DemandPresent(value, location));
        }
        private TypedValue? LowerDecimalMember(IOperation operation)
        {
            var location = operation.Syntax.GetLocation();
            if (operation is IPropertyReferenceOperation property && DecimalProperty(property)) return new FieldValue<Unit, IrType<Unit>>(Types.Int, Lower(property.Instance!), Name.From("scale"));
            if (operation is IObjectCreationOperation creation && DecimalConstruction(creation))
            {
                if (creation.Arguments.Length == 0) return DecimalConstant(0m, location);
                if (creation.Arguments.Length == 1) return ConvertDecimalValue(Lower(creation.Arguments[0].Value), creation.Arguments[0].Value.Type!, creation.Type!, location);
                var args = creation.Arguments.OrderBy(a => a.Parameter!.Ordinal).Select(a => a.Value).ToArray(); var scale = Lower(args[4]); var bounds = IntegerRange(scale, Integral(args[4].Type)!);
                if (bounds.Minimum is null || bounds.Maximum is null || bounds.Minimum < 0 || bounds.Maximum > 28) return Fail<TypedValue>("MCS2706", "Decimal construction requires a scale proved in 0..28.", location);
                TypedValue Word(int index) => NumericCall("wrap", location, Lit(Types.Int, new IntLiteral(32)), Lit(Types.Bool, new BoolLiteral(false)), AsInteger(Lower(args[index]), Integral(args[index].Type)!, location));
                var coefficient = NumericCall("add", location, Word(0), NumericCall("add", location, NumericCall("shiftLeft", location, Word(1), Lit(Types.Int, new IntLiteral(32))), NumericCall("shiftLeft", location, Word(2), Lit(Types.Int, new IntLiteral(64)))));
                return DecimalCall("make", location, DecimalEpsilon(location), Lower(args[3]), scale, coefficient);
            }
            if (operation is not IInvocationOperation call || !DecimalMember(call)) return null;
            var arguments = call.Arguments.OrderBy(a => a.Parameter!.Ordinal).Select(a => Lower(a.Value)).ToList(); if (call.Instance is not null) arguments.Insert(0, Lower(call.Instance));
            return DecimalMethodValue(call.TargetMethod, arguments, location);
        }
        private TypedValue DecimalMethodValue(IMethodSymbol method, List<TypedValue> arguments, Location location)
        {
            var a = arguments[0]; var name = method.Name;
            var binary = name switch { "Add" => BinaryOperatorKind.Add, "Subtract" => BinaryOperatorKind.Subtract, "Multiply" => BinaryOperatorKind.Multiply, "Divide" => BinaryOperatorKind.Divide, "Remainder" => BinaryOperatorKind.Remainder, _ => (BinaryOperatorKind?)null };
            if (binary is { } kind) return DecimalBinary(kind, a, arguments[1], location);
            if (name == "IsNegative") return new FieldValue<Unit, IrType<Unit>>(Types.Bool, a, Name.From("negative"));
            if (name is "Compare" or "CompareTo" or "Equals")
            {
                var compared = DecimalCall("compare", location, a, arguments[1]); return name == "Equals" ? SdkBinary("equal", Types.Int, Types.Bool, compared, Lit(Types.Int, new IntLiteral(0))) : compared;
            }
            if (name is "Max" or "Min") return Choose(SdkBinary(name == "Max" ? "greaterThanOrEqual" : "lessThan", Types.Int, Types.Bool, DecimalCall("compare", location, a, arguments[1]), Lit(Types.Int, new IntLiteral(0))), a, arguments[1]);
            if (name == "Sign") return DecimalCall("compare", location, a, DecimalConstant(0m, location));
            if (name is "Negate" or "Abs")
            {
                var negated = DecimalCall("negate", location, a); var domain = DecimalRange(a); decimalDomains[negated] = new(DecimalZero - domain.Maximum, DecimalZero - domain.Minimum, domain.Nonzero);
                return name == "Negate" ? negated : Choose(new FieldValue<Unit, IrType<Unit>>(Types.Bool, a, Name.From("negative")), negated, a);
            }
            var digits = arguments.Count == 2 ? arguments[1] : Lit(Types.Int, new IntLiteral(0));
            var range = IntegerRange(digits, new(32, true)); if (range.Minimum is null || range.Maximum is null || range.Minimum < 0 || range.Maximum > 28) return Fail<TypedValue>("MCS2706", "Decimal.Round digits require proof of 0..28.", location);
            var mode = name switch { "Truncate" => 2, "Floor" => 3, "Ceiling" => 4, _ => 0 };
            return DecimalCall("roundTo", location, DecimalEpsilon(location), digits, Lit(Types.Int, new IntLiteral(mode)), a);
        }
    }
}
