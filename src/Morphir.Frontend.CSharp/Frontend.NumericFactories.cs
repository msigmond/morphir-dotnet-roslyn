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
        private readonly HashSet<IOperation> provedNumericFactories = [];
        private static bool IntegerFactoryMethod(IMethodSymbol method) => method.IsStatic && method.Arity == 1 && method.Parameters.Length == 1 && method.Parameters[0].RefKind == RefKind.None && method.Name is "CreateChecked" or "CreateSaturating" or "CreateTruncating" && Integral(method.ContainingType) is not null && !method.ContainingType.Locations.Any(location => location.IsInSource) && method.ContainingAssembly.Identity.Version == new Version(10, 0, 0, 0) && SymbolEqualityComparer.Default.Equals(method.ReturnType, method.ContainingType);
        private static bool DecimalFactoryMethod(IMethodSymbol method) => DecimalType(method.ContainingType) && method.IsStatic && method.Arity == 1 && method.Parameters.Length == 1 && method.Parameters[0].RefKind == RefKind.None && method.Name is "CreateChecked" or "CreateSaturating" or "CreateTruncating" && DecimalType(method.ReturnType) && method.ContainingAssembly.Identity.Version == new Version(10, 0, 0, 0);
        private static bool DecimalIntegerMethod(IMethodSymbol method) => DecimalType(method.ContainingType) && method.IsStatic && method.Arity == 1 && method.Parameters.Length == 1 && method.Parameters[0].RefKind == RefKind.None && DecimalType(method.Parameters[0].Type) && method.Name is "ConvertToInteger" or "ConvertToIntegerNative" && SymbolEqualityComparer.Default.Equals(method.ReturnType, method.TypeArguments[0]) && method.ContainingAssembly.Identity.Version == new Version(10, 0, 0, 0);
        private static bool NumericFactoryMethod(IMethodSymbol method) => IntegerFactoryMethod(method) || DecimalFactoryMethod(method) || DecimalIntegerMethod(method) || FloatingFactoryMethod(method) || FloatingIntegerMethod(method);
        private TypedValue ClampInteger(TypedValue value, IntegerDomain target, Location location)
        {
            if (target.Bits == 0) return value;
            return ClampInteger(value, target.Minimum, target.Maximum, location);
        }
        private TypedValue ClampInteger(TypedValue value, BigInteger lower, BigInteger upper, Location location)
        {
            var minimum = IntegerLiteral(lower, new(0, true), location); var maximum = IntegerLiteral(upper, new(0, true), location);
            TypedValue Below(TypedValue bound) => SdkBinary("lessThan", Types.Int, Types.Bool, NumericCall("compare", location, value, bound), Lit(Types.Int, new IntLiteral(0)));
            return Choose(Below(minimum), minimum, Choose(SdkBinary("greaterThan", Types.Int, Types.Bool, NumericCall("compare", location, value, maximum), Lit(Types.Int, new IntLiteral(0))), maximum, value));
        }
        private static NumericRange ClampedRange(NumericRange source, BigInteger lower, BigInteger upper)
        {
            BigInteger Clamp(BigInteger value) => BigInteger.Min(upper, BigInteger.Max(lower, value));
            return new(source.Minimum is { } minimum ? Clamp(minimum) : lower, source.Maximum is { } maximum ? Clamp(maximum) : upper, new HashSet<BigInteger>());
        }
        private TypedValue? LowerNumericFactory(IOperation operation)
        {
            if (operation is not IInvocationOperation call || NumericFactoryRequirement(call.TargetMethod) is null) return null;
            var result = NumericFactoryValue(call.TargetMethod, Lower(call.Arguments[0].Value), operation.Syntax.GetLocation());
            provedNumericFactories.Add(operation); return result;
        }
        private TypedValue NumericFactoryValue(IMethodSymbol method, TypedValue value, Location location)
        {
            if (NumericFactoryRequirement(method) is { } requirement && FactoryVariables(requirement))
            {
                var resolved = requirement with { Source = ResolveGenericParameter(requirement.Source), Target = ResolveGenericParameter(requirement.Target) };
                return FactoryVariables(resolved) ? new ApplyValue<Unit, IrType<Unit>>(Type(requirement.Target, location), FactoryWitness(requirement, location), value) : ConcreteFactoryValue(resolved, value, location);
            }
            var sourceType = ResolveGenericParameter(DecimalIntegerMethod(method) || FloatingIntegerMethod(method) ? method.Parameters[0].Type : method.TypeArguments[0]); var targetType = ResolveGenericParameter(method.ReturnType);
            if (DecimalFactoryMethod(method)) return DecimalFactoryValue(method, value, sourceType, location);
            if (FloatingFactoryMethod(method)) return FloatingFactoryValue(method, value, sourceType, targetType, location);
            if (Integral(targetType) is not { } target) return Fail<TypedValue>("MCS3106", "The generic integer result requires its remaining concrete representation witness.", location);
            if (DecimalType(sourceType))
            {
                if (method.Name == "CreateChecked") return ConvertDecimalValue(value, sourceType, targetType, location);
                // Both truncating and saturating Decimal factories clamp the
                // truncated coefficient; neither wraps it modulo the width.
                var converted = FromInteger(ClampInteger(DecimalCall("toInteger", location, value), target, location), target, location);
                var domain = DecimalRange(value);
                var truncated = new NumericRange(domain.Minimum.Numerator / domain.Minimum.Denominator, domain.Maximum.Numerator / domain.Maximum.Denominator, new HashSet<BigInteger>());
                numericValueRanges[converted] = target.Bits == 0 ? truncated : ClampedRange(truncated, target.Minimum, target.Maximum);
                return converted;
            }
            if (ComplexType(sourceType))
            {
                if (method.Name == "CreateChecked" && !ProvedZeroFloat(Component(value, "imaginary"))) return Fail<TypedValue>("MCS2611", "Checked Complex conversion requires proved zero imaginary component.", location);
                return FloatingIntegerFactory(method, Component(value, "real"), target, location, true);
            }
            if (Floating(sourceType) is not null) return FloatingIntegerFactory(method, value, target, location);
            if (Integral(sourceType) is not { } source) return Fail<TypedValue>("MCS3106", "This generic numeric factory requires its remaining concrete numeric conversion or representation witness.", location);
            var range = IntegerRange(value, source); var mode = method.Name;
            if (mode == "CreateChecked" && target.Bits != 0 && (range.Minimum is null || range.Maximum is null || range.Minimum < target.Minimum || range.Maximum > target.Maximum)) return Fail<TypedValue>("MCS2610", "CreateChecked requires proof that every input fits the target numeric domain.", location);
            var integer = AsInteger(value, source, location);
            if (mode == "CreateSaturating") integer = ClampInteger(integer, target, location);
            var result = FromInteger(integer, target, location);
            numericValueRanges[result] = target.Bits == 0 || mode == "CreateChecked" || range.Minimum >= target.Minimum && range.Maximum <= target.Maximum ? range : mode == "CreateSaturating" ? ClampedRange(range, target.Minimum, target.Maximum) : new(target.Minimum, target.Maximum, new HashSet<BigInteger>());
            return result;
        }
        private TypedValue DecimalFactoryValue(IMethodSymbol method, TypedValue value, ITypeSymbol sourceType, Location location)
        {
            if (DecimalType(sourceType)) return value;
            if (Integral(sourceType) is not { } source) return Fail<TypedValue>("MCS3106", "The Decimal factory requires its remaining numeric input mapping or representation witness.", location);
            if (method.Name == "CreateChecked") return DecimalFromInteger(value, source, location);
            var limit = (BigInteger.One << 96) - 1; var clamped = ClampInteger(AsInteger(value, source, location), -limit, limit, location);
            numericValueRanges[clamped] = ClampedRange(IntegerRange(value, source), -limit, limit);
            return DecimalFromInteger(clamped, new(0, true), location);
        }
    }
}
