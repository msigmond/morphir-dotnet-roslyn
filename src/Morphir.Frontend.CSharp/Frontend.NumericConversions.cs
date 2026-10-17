using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Morphir.IR.Classic;
using TypedValue = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;
namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        private TypedValue ConvertNumericValue(TypedValue value, ITypeSymbol sourceType, ITypeSymbol targetType, Location location, bool isChecked = false)
        {
            if (isChecked) return Fail<TypedValue>("MCS2004", "Checked semantic numeric conversion is excluded.", location);
            if (ComplexType(targetType))
            {
                if (ComplexType(sourceType)) return value;
                if (Integral(sourceType) is { } integer) value = integer.Bits == 0 ? NumericCall("toTruncatedFloat", location, value) : NumericCall("toBinaryFloat", location, Lit(Types.Int, new IntLiteral(53)), AsInteger(value, integer, location));
                return ComplexValue(value, FloatLiteral(0, location), location);
            }
            if (Floating(targetType) is { } precision)
            {
                if (Integral(sourceType) is { } integer)
                {
                    var intermediate = integer.Bits is 0 or 128 ? 53 : precision == 11 ? 24 : precision;
                    value = integer.Bits == 0 ? NumericCall("toTruncatedFloat", location, value) : NumericCall("toBinaryFloat", location, Lit(Types.Int, new IntLiteral(intermediate)), AsInteger(value, integer, location));
                    if (precision == 11 && integer.Bits == 128) value = RoundFloat(value, 24, location);
                }
                return RoundFloat(value, precision, location);
            }
            if (Integral(targetType) is { } target)
            {
                if (ComplexType(sourceType) && target.Bits == 0)
                {
                    if (!ProvedZeroFloat(Component(value, "imaginary"))) return Fail<TypedValue>("MCS2606", "Complex-to-BigInteger conversion requires proved zero imaginary component.", location);
                    var real = Component(value, "real");
                    if (!ProvedFinite(real)) return Fail<TypedValue>("MCS2604", "Complex-to-BigInteger conversion requires a proved finite real component.", location);
                    return NumericCall("fromFiniteFloat", location, real);
                }
                if (Integral(sourceType) is { } source)
                {
                    if (source == target || !source.Wide && !target.Wide && target.Minimum <= source.Minimum && target.Maximum >= source.Maximum) return value;
                    if (source.Bits == 0 && target.Bits != 0)
                    {
                        var range = IntegerRange(value, source);
                        if (range.Minimum is null || range.Maximum is null || range.Minimum < target.Minimum || range.Maximum > target.Maximum) return Fail<TypedValue>("MCS2603", "BigInteger narrowing requires a proved target range.", location);
                    }
                    return FromInteger(AsInteger(value, source, location), target, location);
                }
                if (Floating(sourceType) is not null)
                {
                    if (target.Bits == 0)
                    {
                        if (!ProvedFinite(value)) return Fail<TypedValue>("MCS2604", "Floating BigInteger construction requires a proved finite input.", location);
                        return NumericCall("fromFiniteFloat", location, value);
                    }
                    var converted = NumericCall("floatCast", location, Lit(Types.Int, new IntLiteral(target.Bits)), Lit(Types.Bool, new BoolLiteral(target.Signed)), value);
                    return target.Wide ? converted : NumericCall("toSmall", location, converted);
                }
            }
            return Fail<TypedValue>("MCS2605", "Numeric conversion has no delivered representation.", location);
        }
        private TypedValue NumericUnary(IUnaryOperation unary, TypedValue value)
        {
            var location = unary.Syntax.GetLocation(); var source = NullableElement(unary.Operand.Type) ?? unary.Operand.Type; var target = NullableElement(unary.Type) ?? unary.Type;
            if (unary.IsChecked) return Fail<TypedValue>("MCS2004", "Checked semantic arithmetic is excluded.", location);
            if (Integral(source) is { } integer && Integral(target) is { } result)
            {
                if (unary.OperatorKind == UnaryOperatorKind.Plus) return ConvertNumericValue(value, source!, target!, location);
                var helper = unary.OperatorKind switch { UnaryOperatorKind.Minus => "negate", UnaryOperatorKind.BitwiseNegation => "complement", _ => null };
                if (helper is not null) return FromInteger(NumericCall(helper, location, AsInteger(value, integer, location)), result, location);
            }
            if (Floating(source) is not null)
            {
                if (unary.OperatorKind == UnaryOperatorKind.Plus) return value;
                if (unary.OperatorKind == UnaryOperatorKind.Minus) return SdkUnary("negate", Types.Float, Types.Float, value);
            }
            if (ComplexType(source))
            {
                if (unary.OperatorKind == UnaryOperatorKind.Plus) return value;
                if (unary.OperatorKind == UnaryOperatorKind.Minus) return ComplexValue(SdkUnary("negate", Types.Float, Types.Float, Component(value, "real")), SdkUnary("negate", Types.Float, Types.Float, Component(value, "imaginary")), location);
            }
            return Fail<TypedValue>("MCS2107", "Unmapped lifted numeric unary operation.", location);
        }
        private TypedValue? NullableNumericConversion(IConversionOperation conversion)
        {
            var source = NullableElement(conversion.Operand.Type) ?? conversion.Operand.Type;
            var target = NullableElement(conversion.Type) ?? conversion.Type;
            if (!NumericType(source) || !NumericType(target) || NullableElement(conversion.Operand.Type) is null && NullableElement(conversion.Type) is null) return null;
            var location = conversion.Syntax.GetLocation();
            if (conversion.IsChecked) return Fail<TypedValue>("MCS2004", "Checked numeric conversions are excluded.", location);
            if (Null(conversion.Operand) && NullableElement(conversion.Type) is not null) return Nothing(Type(conversion.Type!, location));
            var value = Lower(conversion.Operand);
            if (NullableElement(conversion.Type) is not null) return MatchMaybe(value, () => Nothing(Type(conversion.Type!, location)), present => Just(ConvertNumericValue(present, source!, target!, location)));
            return ConvertNumericValue(DemandPresent(value, location), source!, target!, location);
        }
    }
}
