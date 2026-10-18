using System.Numerics;
using Microsoft.CodeAnalysis;
using Morphir.IR.Classic;
using TypedValue = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        private static bool FloatingFactoryMethod(IMethodSymbol method) => (Floating(method.ContainingType) is not null || ComplexType(method.ContainingType)) && method.IsStatic && method.Arity == 1 && method.Parameters.Length == 1 && method.Parameters[0].RefKind == RefKind.None && method.Name is "CreateChecked" or "CreateSaturating" or "CreateTruncating" && SymbolEqualityComparer.Default.Equals(method.ReturnType, method.ContainingType);
        private static bool FloatingIntegerMethod(IMethodSymbol method) => Floating(method.ContainingType) is not null && method.IsStatic && method.Arity == 1 && method.Parameters.Length == 1 && method.Parameters[0].RefKind == RefKind.None && SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, method.ContainingType) && method.Name is "ConvertToInteger" or "ConvertToIntegerNative" && SymbolEqualityComparer.Default.Equals(method.ReturnType, method.TypeArguments[0]);
        private bool KnownFactoryNaN(TypedValue value)
        {
            if (value is LiteralValue<Unit, IrType<Unit>> { Literal: FloatLiteral literal }) return double.IsNaN(literal.Value);
            if (value is ReferenceValue<Unit, IrType<Unit>> reference) return reference.Name.Package.Key == package.Key && reference.Name.Module.Key == Morphir.IR.Classic.Path.From("Compat.Floating").Key && reference.Name.Local.Key == "nan";
            if (value is VariableValue<Unit, IrType<Unit>> variable && boundValues.TryGetValue(variable.Name.Key, out var bound)) return KnownFactoryNaN(bound);
            if (value is FieldValue<Unit, IrType<Unit>> field && field.Subject is RecordValue<Unit, IrType<Unit>> record) return KnownFactoryNaN(record.Fields.Single(item => item.Key.Key == field.Field.Key).Value);
            return false;
        }
        private TypedValue FloatingFactoryValue(IMethodSymbol method, TypedValue value, ITypeSymbol source, ITypeSymbol target, Location location)
        {
            if (ComplexType(source) && ComplexType(target)) return value;
            if (ComplexType(source))
            {
                if (method.Name == "CreateChecked" && !ProvedZeroFloat(Component(value, "imaginary"))) return Fail<TypedValue>("MCS2611", "Checked conversion of Complex to a real domain requires proved zero imaginary component.", location);
                value = Component(value, "real"); source = compilation.GetSpecialType(SpecialType.System_Double);
            }
            if (DecimalType(source)) return ConvertDecimalValue(value, source, target, location);
            if (Integral(source) is not null || Floating(source) is not null) return ConvertNumericValue(value, source, target, location);
            return Fail<TypedValue>("MCS3106", "The numeric factory requires its remaining concrete input mapping or representation witness.", location);
        }
        private TypedValue FloatingIntegerFactory(IMethodSymbol method, TypedValue value, IntegerDomain target, Location location, bool complexSource = false)
        {
            if (ProvedZeroFloat(value)) return IntegerLiteral(BigInteger.Zero, target, location);
            if (method.Name == "ConvertToInteger" && Floating(method.ContainingType) is 24 or 53 && target.Bits is > 0 and <= 64)
                return ConvertNumericValue(value, method.Parameters[0].Type, method.TypeArguments[0], location);
            if (method.Name == "ConvertToIntegerNative" && Floating(method.ContainingType) is 24 or 53 && target.Bits is > 0 and <= 64)
            {
                if (!ProvedFloatingInteger(value, target)) return Fail<TypedValue>("MCS2612", "Native primitive conversions require proof excluding platform-dependent overflow and NaN behavior.", location);
                return FromInteger(NumericCall("fromFiniteFloat", location, value), target, location);
            }
            if (method.Name == "CreateChecked")
            {
                if (ProvedZeroFloat(value)) return IntegerLiteral(BigInteger.Zero, target, location);
                if (ProvedFloatingInteger(value, target))
                {
                    var checkedResult = FromInteger(NumericCall("fromFiniteFloat", location, value), target, location);
                    var range = FloatRange(value); numericValueRanges[checkedResult] = new(new BigInteger(range.Minimum), new BigInteger(range.Maximum), new HashSet<BigInteger>());
                    return checkedResult;
                }
                var unprovedRange = FloatRange(value);
                return Fail<TypedValue>("MCS2610", $"Checked floating-to-integer factories require finite and representable truncated input bounds; inferred [{unprovedRange.Minimum:R}, {unprovedRange.Maximum:R}], possible NaN={unprovedRange.MayNaN}.", location);
            }
            if (target.Bits == 0)
            {
                if (!complexSource && KnownFactoryNaN(value)) return IntegerLiteral(BigInteger.Zero, target, location);
                if (!ProvedFinite(value)) return Fail<TypedValue>("MCS2604", "Unbounded integer factories require proof excluding infinite floating inputs.", location);
                return NumericCall("fromFiniteFloat", location, value);
            }
            var boundary = Math.ScaleB(1d, target.Signed ? target.Bits - 1 : target.Bits);
            var minimum = IntegerLiteral(target.Minimum, new(0, true), location); var maximum = IntegerLiteral(target.Maximum, new(0, true), location);
            var integral = Choose(SdkUnary("isNaN", Types.Float, Types.Bool, value), IntegerLiteral(BigInteger.Zero, new(0, true), location),
                Choose(SdkBinary("lessThanOrEqual", Types.Float, Types.Bool, value, FloatLiteral(target.Signed ? -boundary : 0d, location)), minimum,
                    Choose(SdkBinary("greaterThanOrEqual", Types.Float, Types.Bool, value, FloatLiteral(boundary, location)), maximum, NumericCall("fromFiniteFloat", location, value))));
            var result = FromInteger(integral, target, location); numericValueRanges[result] = new(target.Minimum, target.Maximum, new HashSet<BigInteger>()); return result;
        }
    }
}
