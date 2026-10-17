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
        private readonly record struct DecimalNumber(BigInteger Numerator, BigInteger Denominator) : IComparable<DecimalNumber>
        {
            public int CompareTo(DecimalNumber other) => (Numerator * other.Denominator).CompareTo(other.Numerator * Denominator);
            public static DecimalNumber From(decimal value)
            {
                var bits = decimal.GetBits(value); var coefficient = (BigInteger)(uint)bits[0] + ((BigInteger)(uint)bits[1] << 32) + ((BigInteger)(uint)bits[2] << 64);
                return new(bits[3] < 0 ? -coefficient : coefficient, BigInteger.Pow(10, (bits[3] >> 16) & 255));
            }
            public static DecimalNumber operator +(DecimalNumber a, DecimalNumber b) => new(a.Numerator * b.Denominator + b.Numerator * a.Denominator, a.Denominator * b.Denominator);
            public static DecimalNumber operator -(DecimalNumber a, DecimalNumber b) => new(a.Numerator * b.Denominator - b.Numerator * a.Denominator, a.Denominator * b.Denominator);
            public static DecimalNumber operator *(DecimalNumber a, DecimalNumber b) => new(a.Numerator * b.Numerator, a.Denominator * b.Denominator);
            public static DecimalNumber operator /(DecimalNumber a, DecimalNumber b) => b.Numerator.Sign < 0 ? new(-a.Numerator * b.Denominator, -a.Denominator * b.Numerator) : new(a.Numerator * b.Denominator, a.Denominator * b.Numerator);
            public DecimalNumber Abs() => new(BigInteger.Abs(Numerator), Denominator);
            public decimal Rounded()
            {
                for (var scale = 28; scale >= 0; scale--)
                {
                    var quotient = BigInteger.DivRem(BigInteger.Abs(Numerator) * BigInteger.Pow(10, scale), Denominator, out var rest);
                    if (rest * 2 > Denominator || rest * 2 == Denominator && !quotient.IsEven) quotient++;
                    if (quotient > (BigInteger.One << 96) - 1) continue;
                    return new(unchecked((int)(uint)(quotient & uint.MaxValue)), unchecked((int)(uint)((quotient >> 32) & uint.MaxValue)), unchecked((int)(uint)(quotient >> 64)), Numerator.Sign < 0, (byte)scale);
                }
                throw new InvalidOperationException("Decimal proof did not establish a representable endpoint");
            }
        }
        private sealed record DecimalDomain(DecimalNumber Minimum, DecimalNumber Maximum, bool Nonzero = false);
        private Dictionary<TypedValue, DecimalDomain> decimalDomains = new(ReferenceEqualityComparer.Instance);
        private Dictionary<TypedValue, decimal> decimalConstants = new(ReferenceEqualityComparer.Instance);
        private Dictionary<string, DecimalDomain> decimalFacts = [];
        private Dictionary<string, DecimalDomain> decimalMethodDomains = [];
        private static string DecimalOwnerKey(FQName name) => name.Module.Key + "\u0002" + name.Local.Key;
        private bool DecimalIrType(IrType<Unit> type) => type is ReferenceType<Unit> reference && reference.Name.Package.Key == package.Key && reference.Name.Module.Key == Morphir.IR.Classic.Path.From("Compat.Decimal").Key && reference.Name.Local.Key == "decimal";
        private static DecimalNumber DecimalZero => DecimalNumber.From(0m);
        private static DecimalNumber DecimalLimit => DecimalNumber.From(decimal.MaxValue);
        private static DecimalNumber MinDecimal(DecimalNumber a, DecimalNumber b) => a.CompareTo(b) <= 0 ? a : b;
        private static DecimalNumber MaxDecimal(DecimalNumber a, DecimalNumber b) => a.CompareTo(b) >= 0 ? a : b;
        private decimal? KnownDecimal(TypedValue value)
        {
            if (decimalConstants.TryGetValue(value, out var known)) return known;
            if (value is VariableValue<Unit, IrType<Unit>> variable && boundValues.TryGetValue(variable.Name.Key, out var bound)) return KnownDecimal(bound);
            return null;
        }
        private DecimalDomain DecimalRange(TypedValue value)
        {
            if (KnownDecimal(value) is { } known) { var endpoint = DecimalNumber.From(known); return new(endpoint, endpoint, known != 0m); }
            if (ValueKey(value) is { } key && decimalFacts.TryGetValue(key, out var fact)) return fact;
            if (decimalDomains.TryGetValue(value, out var range)) return range;
            if (value is VariableValue<Unit, IrType<Unit>> variable && boundValues.TryGetValue(variable.Name.Key, out var bound)) return DecimalRange(bound);
            if (value is LetValue<Unit, IrType<Unit>> let) return DecimalRange(let.Body);
            if (value is IfValue<Unit, IrType<Unit>> conditional) { var yes = DecimalRange(conditional.Then); var no = DecimalRange(conditional.Else); return new(MinDecimal(yes.Minimum, no.Minimum), MaxDecimal(yes.Maximum, no.Maximum), yes.Nonzero && no.Nonzero); }
            var root = value; while (root is ApplyValue<Unit, IrType<Unit>> apply) root = apply.Function;
            if (root is ReferenceValue<Unit, IrType<Unit>> reference && reference.Name.Package.Key == package.Key && decimalMethodDomains.TryGetValue(DecimalOwnerKey(reference.Name), out var summary)) return summary;
            return new(DecimalNumber.From(decimal.MinValue), DecimalLimit);
        }
        private DecimalDomain ProveDecimalArithmetic(BinaryOperatorKind operation, TypedValue left, TypedValue right, Location location)
        {
            var a = DecimalRange(left); var b = DecimalRange(right); DecimalNumber min, max;
            if (operation == BinaryOperatorKind.Add) { min = a.Minimum + b.Minimum; max = a.Maximum + b.Maximum; }
            else if (operation == BinaryOperatorKind.Subtract) { min = a.Minimum - b.Maximum; max = a.Maximum - b.Minimum; }
            else if (operation == BinaryOperatorKind.Multiply)
            {
                var products = new[] { a.Minimum * b.Minimum, a.Minimum * b.Maximum, a.Maximum * b.Minimum, a.Maximum * b.Maximum }; min = products.Min(); max = products.Max();
            }
            else
            {
                var excludesZero = b.Nonzero || b.Minimum.CompareTo(DecimalZero) > 0 || b.Maximum.CompareTo(DecimalZero) < 0;
                if (!excludesZero) return Fail<DecimalDomain>("MCS2701", "Decimal division/remainder requires a proved nonzero divisor.", location);
                if (operation == BinaryOperatorKind.Remainder) return new(MinDecimal(a.Minimum, DecimalZero), MaxDecimal(a.Maximum, DecimalZero)); // The magnitude cannot increase; zero remains possible.
                var epsilon = DecimalNumber.From(0.0000000000000000000000000001m);
                var least = b.Minimum.CompareTo(DecimalZero) > 0 ? b.Minimum : b.Maximum.CompareTo(DecimalZero) < 0 ? b.Maximum.Abs() : epsilon;
                var magnitude = MaxDecimal(a.Minimum.Abs(), a.Maximum.Abs()) / least;
                min = new(-magnitude.Numerator, magnitude.Denominator); max = magnitude;
            }
            if (min.CompareTo(DecimalNumber.From(decimal.MinValue)) < 0 || max.CompareTo(DecimalLimit) > 0) return Fail<DecimalDomain>("MCS2702", "Decimal arithmetic requires proved representable result bounds; nonzero alone does not establish division safety.", location);
            return new(DecimalNumber.From(min.Rounded()), DecimalNumber.From(max.Rounded()));
        }
        private static bool DecimalProbe(IOperation operation) => operation is IBinaryOperation binary && DecimalType(binary.LeftOperand.Type) && binary.OperatorKind is BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals or BinaryOperatorKind.LessThan or BinaryOperatorKind.LessThanOrEqual or BinaryOperatorKind.GreaterThan or BinaryOperatorKind.GreaterThanOrEqual;
        private TypedValue? DecimalDecision(IOperation condition, BindingEnvironment environment, Func<BindingEnvironment, TypedValue> yes, Func<BindingEnvironment, TypedValue> no)
        {
            if (condition is not IBinaryOperation binary || !DecimalProbe(binary) || binary.IsLifted || HasAssignment(condition)) return null;
            return Expression(condition, environment, (test, state) =>
            {
                var left = In(state, binary.LeftOperand); var right = In(state, binary.RightOperand); var kind = binary.OperatorKind; TypedValue variable; decimal constant;
                if (KnownDecimal(right) is { } rc) { variable = left; constant = rc; }
                else if (KnownDecimal(left) is { } lc) { variable = right; constant = lc; kind = kind switch { BinaryOperatorKind.LessThan => BinaryOperatorKind.GreaterThan, BinaryOperatorKind.LessThanOrEqual => BinaryOperatorKind.GreaterThanOrEqual, BinaryOperatorKind.GreaterThan => BinaryOperatorKind.LessThan, BinaryOperatorKind.GreaterThanOrEqual => BinaryOperatorKind.LessThanOrEqual, _ => kind }; }
                else return Choose(test, yes(Copy(state)), no(Copy(state)));
                if (ValueKey(variable) is not { } key) return Choose(test, yes(Copy(state)), no(Copy(state)));
                var initial = DecimalRange(variable); var endpoint = DecimalNumber.From(constant); var previous = decimalFacts;
                TypedValue Branch(bool truth, Func<BindingEnvironment, TypedValue> body)
                {
                    var op = truth ? kind : kind switch { BinaryOperatorKind.Equals => BinaryOperatorKind.NotEquals, BinaryOperatorKind.NotEquals => BinaryOperatorKind.Equals, BinaryOperatorKind.LessThan => BinaryOperatorKind.GreaterThanOrEqual, BinaryOperatorKind.LessThanOrEqual => BinaryOperatorKind.GreaterThan, BinaryOperatorKind.GreaterThan => BinaryOperatorKind.LessThanOrEqual, _ => BinaryOperatorKind.LessThan };
                    var min = initial.Minimum; var max = initial.Maximum; var nonzero = initial.Nonzero; var step = DecimalNumber.From(0.0000000000000000000000000001m);
                    switch (op) { case BinaryOperatorKind.Equals: min = endpoint; max = endpoint; nonzero = constant != 0m; break; case BinaryOperatorKind.NotEquals: nonzero |= constant == 0m; break; case BinaryOperatorKind.LessThan: max = MinDecimal(max, endpoint - step); break; case BinaryOperatorKind.LessThanOrEqual: max = MinDecimal(max, endpoint); break; case BinaryOperatorKind.GreaterThan: min = MaxDecimal(min, endpoint + step); break; case BinaryOperatorKind.GreaterThanOrEqual: min = MaxDecimal(min, endpoint); break; }
                    decimalFacts = new(previous) { [key] = new(min, max, nonzero) }; try { return body(Copy(state)); } finally { decimalFacts = previous; }
                }
                return Choose(test, Branch(true, yes), Branch(false, no));
            });
        }
    }
}
