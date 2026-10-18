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
        private static int? Floating(ITypeSymbol? type) => type?.SpecialType switch
        {
            SpecialType.System_Single => 24,
            SpecialType.System_Double => 53,
            _ => TrustedNumeric(type, "System.Half", "System.Runtime") ? 11 : null
        };
        private static bool TrustedNumeric(ITypeSymbol? type, string fullName, string assembly) => type is not null && !type.Locations.Any(l => l.IsInSource) && type.ContainingAssembly?.Identity.Name == assembly && type.ContainingAssembly.Identity.Version == new Version(10, 0, 0, 0) && type.ToDisplayString() == fullName;
        private static bool ComplexType(ITypeSymbol? type) => TrustedNumeric(type, "System.Numerics.Complex", "System.Runtime.Numerics");
        private static bool NumericType(ITypeSymbol? type) => Integral(type) is not null || Floating(type) is not null || ComplexType(type);
        private static bool NumericConversion(IConversionOperation conversion) => NumericType(conversion.Type) && NumericType(conversion.Operand.Type);
        private IrType<Unit> ComplexIrType(Location? location)
        {
            RequireNumericLibrary(location);
            return new ReferenceType<Unit>(default, new(package, IrPath.From("Compat.Floating"), Name.From("Complex")), []);
        }
        private TypedValue ComplexValue(TypedValue real, TypedValue imaginary, Location? location) => new RecordValue<Unit, IrType<Unit>>(ComplexIrType(location), [new(Name.From("imaginary"), imaginary), new(Name.From("real"), real)]);
        private static double? FloatingConstant(string name, int precision) => name switch
        {
            "NaN" => double.NaN,
            "PositiveInfinity" => double.PositiveInfinity,
            "NegativeInfinity" => double.NegativeInfinity,
            "Zero" => 0.0,
            "One" or "MultiplicativeIdentity" => 1.0,
            "NegativeOne" => -1.0,
            "E" => precision == 11 ? (double)Half.E : precision == 24 ? float.E : double.E,
            "Pi" => precision == 11 ? (double)Half.Pi : precision == 24 ? float.Pi : double.Pi,
            "Tau" => precision == 11 ? (double)Half.Tau : precision == 24 ? float.Tau : double.Tau,
            "NegativeZero" => -0.0,
            "Epsilon" => precision == 24 ? float.Epsilon : precision == 11 ? (double)Half.Epsilon : double.Epsilon,
            "MinValue" => precision == 24 ? float.MinValue : precision == 11 ? (double)Half.MinValue : double.MinValue,
            "MaxValue" => precision == 24 ? float.MaxValue : precision == 11 ? (double)Half.MaxValue : double.MaxValue,
            _ => null
        };
        private TypedValue FloatLiteral(double value, Location? location)
        {
            if (double.IsNaN(value)) return FloatCall("nan", location);
            if (double.IsPositiveInfinity(value)) return FloatCall("infinity", location);
            if (double.IsNegativeInfinity(value)) return FloatCall("negativeInfinity", location);
            if (BitConverter.DoubleToInt64Bits(value) == long.MinValue) return FloatCall("negativeZero", location);
            return Lit(Types.Float, new Morphir.IR.Classic.FloatLiteral(value));
        }
        private TypedValue RoundFloat(TypedValue value, int precision, Location? location) => precision == 53 ? value : FloatCall("roundBinary", location, Lit(Types.Int, new IntLiteral(precision)), Lit(Types.Int, new IntLiteral(precision == 24 ? -126 : -14)), Lit(Types.Int, new IntLiteral(precision == 24 ? 127 : 15)), value);
        private TypedValue SdkUnary(string name, IrType<Unit> input, IrType<Unit> output, TypedValue value) => new ApplyValue<Unit, IrType<Unit>>(output, new ReferenceValue<Unit, IrType<Unit>>(Types.Function(input, output), new(Types.Sdk, IrPath.From("Basics"), Name.From(name))), value);
        private static TypedValue Component(TypedValue value, string name) => new FieldValue<Unit, IrType<Unit>>(Types.Float, value, Name.From(name));
        private TypedValue? ComplexBinary(IBinaryOperation complexBinary, TypedValue a, TypedValue b)
        {
            var location = complexBinary.Syntax.GetLocation();
            bool leftComplex = ComplexType(NullableElement(complexBinary.LeftOperand.Type) ?? complexBinary.LeftOperand.Type), rightComplex = ComplexType(NullableElement(complexBinary.RightOperand.Type) ?? complexBinary.RightOperand.Type);
            if (leftComplex && rightComplex)
            {
                var helper = complexBinary.OperatorKind switch { BinaryOperatorKind.Add => "addComplex", BinaryOperatorKind.Subtract => "subtractComplex", BinaryOperatorKind.Multiply => "multiplyComplex", BinaryOperatorKind.Divide => "divideComplex", _ => null };
                if (helper is not null) return FloatCall(helper, location, a, b);
                if (complexBinary.OperatorKind is BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals)
                {
                    var real = SdkBinary("equal", Types.Float, Types.Bool, Component(a, "real"), Component(b, "real")); var imaginary = SdkBinary("equal", Types.Float, Types.Bool, Component(a, "imaginary"), Component(b, "imaginary"));
                    var equal = SdkBinary("and", Types.Bool, Types.Bool, real, imaginary);
                    return complexBinary.OperatorKind == BinaryOperatorKind.Equals ? equal : SdkUnary("not", Types.Bool, Types.Bool, equal);
                }
            }
            else
            {
                var complex = leftComplex ? a : b; var scalar = leftComplex ? b : a;
                if (complexBinary.OperatorKind == BinaryOperatorKind.Multiply) return FloatCall("multiplyScalar", location, complex, scalar);
                if (complexBinary.OperatorKind == BinaryOperatorKind.Divide) return leftComplex ? FloatCall("divideScalar", location, complex, scalar) : FloatCall("scalarDivide", location, scalar, complex);
                if (complexBinary.OperatorKind == BinaryOperatorKind.Add) return ComplexValue(SdkBinary("add", Types.Float, Types.Float, leftComplex ? Component(a, "real") : a, rightComplex ? Component(b, "real") : b), Component(complex, "imaginary"), location);
                if (complexBinary.OperatorKind == BinaryOperatorKind.Subtract) return ComplexValue(SdkBinary("subtract", Types.Float, Types.Float, leftComplex ? Component(a, "real") : a, rightComplex ? Component(b, "real") : b), leftComplex ? Component(complex, "imaginary") : SdkUnary("negate", Types.Float, Types.Float, Component(complex, "imaginary")), location);
            }
            return null;
        }
        private TypedValue? FloatingBinary(IBinaryOperation binary, TypedValue a, TypedValue b)
        {
            var location = binary.Syntax.GetLocation();
            string? helper = binary.OperatorKind switch { BinaryOperatorKind.Add => "add", BinaryOperatorKind.Subtract => "subtract", BinaryOperatorKind.Multiply => "multiply", BinaryOperatorKind.Divide => "divide", BinaryOperatorKind.Remainder => "remainder", BinaryOperatorKind.Equals => "equal", BinaryOperatorKind.NotEquals => "notEqual", BinaryOperatorKind.LessThan => "lessThan", BinaryOperatorKind.LessThanOrEqual => "lessThanOrEqual", BinaryOperatorKind.GreaterThan => "greaterThan", BinaryOperatorKind.GreaterThanOrEqual => "greaterThanOrEqual", _ => null };
            if (helper is null) return null;
            if (Floating(NullableElement(binary.Type) ?? binary.Type) is not { } resultPrecision)
            {
                var test = SdkBinary(helper, Types.Float, Types.Bool, a, b);
                return binary.IsLifted ? test : RememberFloatingPredicate(test, a, b, binary.OperatorKind);
            }
            var result = helper == "remainder" ? FloatCall(helper, location, a, b) : SdkBinary(helper, Types.Float, Types.Float, a, b);
            if (resultPrecision == 11) result = RoundFloat(result, 24, location);
            return RoundFloat(result, resultPrecision, location);
        }
        private TypedValue? LowerFloating(IOperation operation)
        {
            var location = operation.Syntax.GetLocation();
            if (operation is IConversionOperation nullableConversion && NullableNumericConversion(nullableConversion) is { } nullableResult) return nullableResult;
            if (operation is IInvocationOperation numericCall && FloatingPredicateCall(numericCall))
            {
                var argument = Lower(numericCall.Arguments[0].Value);
                var test = numericCall.TargetMethod.Name == "IsNaN" ? SdkUnary("isNaN", Types.Float, Types.Bool, argument) : numericCall.TargetMethod.Name == "IsInfinity" ? SdkUnary("isInfinite", Types.Float, Types.Bool, argument) : SdkUnary("not", Types.Bool, Types.Bool, SdkBinary("or", Types.Bool, Types.Bool, SdkUnary("isNaN", Types.Float, Types.Bool, argument), SdkUnary("isInfinite", Types.Float, Types.Bool, argument)));
                return RememberFloatingPredicate(test, argument, classification: numericCall.TargetMethod.Name);
            }
            if (operation is IObjectCreationOperation bigIntegerCreation && Integral(bigIntegerCreation.Type) is { Bits: 0 } && bigIntegerCreation.Arguments.Length == 1 && Floating(bigIntegerCreation.Arguments[0].Value.Type) is not null)
                return ConvertNumericValue(Lower(bigIntegerCreation.Arguments[0].Value), bigIntegerCreation.Arguments[0].Value.Type!, bigIntegerCreation.Type!, location);
            if (operation is IObjectCreationOperation { Arguments.Length: 0 } emptyFloat && Floating(emptyFloat.Type) is not null) return FloatLiteral(0, location);
            if (operation is IObjectCreationOperation creation && ComplexType(creation.Type))
            {
                if (creation.Arguments.Length == 0) return ComplexValue(FloatLiteral(0, location), FloatLiteral(0, location), location);
                if (creation.Arguments.Length == 2 && creation.Initializer is null) return ComplexValue(Lower(creation.Arguments[0].Value), Lower(creation.Arguments[1].Value), location);
            }
            if (operation is IFieldReferenceOperation complexField && ComplexType(complexField.Type) && complexField.Instance is null)
            {
                return complexField.Field.Name switch
                {
                    "Zero" => ComplexValue(FloatLiteral(0, location), FloatLiteral(0, location), location),
                    "One" => ComplexValue(FloatLiteral(1, location), FloatLiteral(0, location), location),
                    "ImaginaryOne" => ComplexValue(FloatLiteral(0, location), FloatLiteral(1, location), location),
                    "NaN" => ComplexValue(FloatLiteral(double.NaN, location), FloatLiteral(double.NaN, location), location),
                    "Infinity" => ComplexValue(FloatLiteral(double.PositiveInfinity, location), FloatLiteral(double.PositiveInfinity, location), location),
                    _ => null
                };
            }
            if (operation is IPropertyReferenceOperation complexProperty && complexProperty.Instance is not null && ComplexType(complexProperty.Property.ContainingType) && complexProperty.Property.Name is "Real" or "Imaginary")
                return new FieldValue<Unit, IrType<Unit>>(Types.Float, Lower(complexProperty.Instance), Name.From(complexProperty.Property.Name));
            if (operation is IUnaryOperation complexUnary && !complexUnary.IsLifted && ComplexType(complexUnary.Type))
            {
                var operand = Lower(complexUnary.Operand);
                if (complexUnary.OperatorKind == UnaryOperatorKind.Plus) return operand;
                if (complexUnary.OperatorKind == UnaryOperatorKind.Minus) return ComplexValue(SdkUnary("negate", Types.Float, Types.Float, Component(operand, "real")), SdkUnary("negate", Types.Float, Types.Float, Component(operand, "imaginary")), location);
            }
            if (operation is IBinaryOperation complexBinary && !complexBinary.IsLifted && (ComplexType(complexBinary.LeftOperand.Type) || ComplexType(complexBinary.RightOperand.Type)))
                return ComplexBinary(complexBinary, Lower(complexBinary.LeftOperand), Lower(complexBinary.RightOperand));
            if (operation is ILiteralOperation literal && Floating(literal.Type) is not null && literal.ConstantValue.HasValue) return FloatLiteral(System.Convert.ToDouble(literal.ConstantValue.Value, System.Globalization.CultureInfo.InvariantCulture), location);
            if (operation is IPropertyReferenceOperation floatingProperty && floatingProperty.Instance is null && Floating(floatingProperty.Property.ContainingType) is { } propertyPrecision && FloatingConstant(floatingProperty.Property.Name, propertyPrecision) is { } propertyConstant)
                return FloatLiteral(propertyConstant, location);
            if (operation is IFieldReferenceOperation field && Floating(field.Type) is not null)
            {
                if (field.Field.HasConstantValue) return FloatLiteral(System.Convert.ToDouble(field.Field.ConstantValue, System.Globalization.CultureInfo.InvariantCulture), location);
                if (Floating(field.Field.ContainingType) is { } precision && field.Instance is null)
                {
                    double? constant = field.Field.Name switch { "NaN" => double.NaN, "PositiveInfinity" => double.PositiveInfinity, "NegativeInfinity" => double.NegativeInfinity, "Epsilon" => precision == 24 ? float.Epsilon : precision == 11 ? (double)Half.Epsilon : double.Epsilon, "MinValue" => precision == 24 ? float.MinValue : precision == 11 ? (double)Half.MinValue : double.MinValue, "MaxValue" => precision == 24 ? float.MaxValue : precision == 11 ? (double)Half.MaxValue : double.MaxValue, _ => null };
                    if (constant is { } value) return FloatLiteral(value, location);
                }
            }
            if (operation is IConversionOperation conversion && NumericConversion(conversion) && (Floating(conversion.Type) is not null || Floating(conversion.Operand.Type) is not null || ComplexType(conversion.Type) || ComplexType(conversion.Operand.Type)))
            {
                if (conversion.IsChecked) return Fail<TypedValue>("MCS2004", "Checked numeric conversions are excluded.", location);
                if (conversion.ConstantValue.HasValue)
                {
                    if (Integral(conversion.Type) is { } constantDomain && IntegerConstant(conversion.ConstantValue.Value) is { } integerConstant) return IntegerLiteral(integerConstant, constantDomain, location);
                    if (Floating(conversion.Type) is not null && conversion.ConstantValue.Value is float or double) return FloatLiteral(System.Convert.ToDouble(conversion.ConstantValue.Value, System.Globalization.CultureInfo.InvariantCulture), location);
                }
                return ConvertNumericValue(Lower(conversion.Operand), conversion.Operand.Type!, conversion.Type!, location);

            }
            if (operation is IUnaryOperation unary && !unary.IsLifted && Floating(unary.Type) is { } unaryPrecision)
            {
                if (unary.OperatorKind == UnaryOperatorKind.Plus) return Lower(unary.Operand);
                if (unary.OperatorKind == UnaryOperatorKind.Minus) return SdkUnary("negate", Types.Float, Types.Float, Lower(unary.Operand));
            }
            if (operation is IBinaryOperation binary && !binary.IsLifted && Floating(binary.LeftOperand.Type) is not null && Floating(binary.RightOperand.Type) is not null)
                return FloatingBinary(binary, Lower(binary.LeftOperand), Lower(binary.RightOperand));
            return null;
        }
    }
}
