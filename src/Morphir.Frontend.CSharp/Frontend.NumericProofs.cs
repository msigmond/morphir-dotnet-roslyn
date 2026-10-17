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
        private sealed record NumericRange(BigInteger? Minimum, BigInteger? Maximum, IReadOnlySet<BigInteger> Excluded)
        {
            public bool Excludes(BigInteger value) => Minimum is { } min && value < min || Maximum is { } max && value > max || Excluded.Contains(value);
        }
        private Dictionary<string, NumericRange> numericFacts = [];
        private Dictionary<TypedValue, NumericRange> numericValueRanges = new(ReferenceEqualityComparer.Instance);
        private BigInteger? KnownInteger(TypedValue value)
        {
            if (numericValueRanges.TryGetValue(value, out var exact) && exact.Minimum is { } min && exact.Maximum == min) return min;
            if (value is LiteralValue<Unit, IrType<Unit>> { Literal: IntLiteral literal }) return literal.Value;
            if (value is VariableValue<Unit, IrType<Unit>> variable && boundValues.TryGetValue(variable.Name.Key, out var bound)) return KnownInteger(bound);
            if (value is RecordValue<Unit, IrType<Unit>> record && record.Fields.SingleOrDefault(f => f.Key.Key == "digits")?.Value is ListValue<Unit, IrType<Unit>> digits && record.Fields.SingleOrDefault(f => f.Key.Key == "negative")?.Value is LiteralValue<Unit, IrType<Unit>> { Literal: BoolLiteral negative })
            {
                BigInteger result = 0;
                foreach (var digit in digits.Elements.Reverse()) { if (KnownInteger(digit) is not { } n) return null; result = (result << 16) + n; }
                return negative.Value ? -result : result;
            }
            if (value is ApplyValue<Unit, IrType<Unit>>)
            {
                var arguments = new List<TypedValue>(); var root = value;
                while (root is ApplyValue<Unit, IrType<Unit>> apply) { arguments.Insert(0, apply.Argument); root = apply.Function; }
                if (root is ReferenceValue<Unit, IrType<Unit>> reference && reference.Name.Package.Key == package.Key && reference.Name.Module.Key == Morphir.IR.Classic.Path.From("Compat.Integers").Key)
                {
                    var name = reference.Name.Local.Key;
                    if (name is "from\0small" or "to\0small" && arguments.Count == 1) return KnownInteger(arguments[0]);
                    if (name == "wrap" && arguments.Count == 3 && KnownInteger(arguments[0]) is { } bits && arguments[1] is LiteralValue<Unit, IrType<Unit>> { Literal: BoolLiteral signed } && KnownInteger(arguments[2]) is { } number)
                    {
                        var modulus = BigInteger.One << (int)bits; var residue = (number % modulus + modulus) % modulus;
                        return signed.Value && residue >= (modulus >> 1) ? residue - modulus : residue;
                    }
                }
            }
            return null;
        }
        private NumericRange IntegerRange(TypedValue value, IntegerDomain domain)
        {
            if (KnownInteger(value) is { } constant) return new(constant, constant, new HashSet<BigInteger>());
            if (ValueKey(value) is { } key && numericFacts.TryGetValue(key, out var range)) return range;
            if (numericValueRanges.TryGetValue(value, out var knownRange)) return knownRange;
            if (value is VariableValue<Unit, IrType<Unit>> variable && boundValues.TryGetValue(variable.Name.Key, out var bound)) return IntegerRange(bound, domain);
            return new(domain.Bits == 0 ? null : domain.Minimum, domain.Bits == 0 ? null : domain.Maximum, new HashSet<BigInteger>());
        }
        private static bool NumericProbe(IOperation operation) => operation is IBinaryOperation binary && Integral(binary.LeftOperand.Type) is not null && binary.OperatorKind is BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals or BinaryOperatorKind.LessThan or BinaryOperatorKind.LessThanOrEqual or BinaryOperatorKind.GreaterThan or BinaryOperatorKind.GreaterThanOrEqual;
        private TypedValue? NumericDecision(IOperation condition, Dictionary<ISymbol, Binding> environment, Func<Dictionary<ISymbol, Binding>, TypedValue> yes, Func<Dictionary<ISymbol, Binding>, TypedValue> no)
        {
            if (condition is not IBinaryOperation binary || !NumericProbe(binary) || binary.IsLifted || HasAssignment(condition)) return null;
            return Expression(condition, environment, (test, state) =>
            {
                var left = In(state, binary.LeftOperand); var right = In(state, binary.RightOperand);
                TypedValue variable; IntegerDomain domain; BigInteger constant; var comparison = binary.OperatorKind;
                if (KnownInteger(right) is { } rc) { variable = left; domain = Integral(binary.LeftOperand.Type)!; constant = rc; }
                else if (KnownInteger(left) is { } lc)
                {
                    variable = right; domain = Integral(binary.RightOperand.Type)!; constant = lc;
                    comparison = comparison switch { BinaryOperatorKind.LessThan => BinaryOperatorKind.GreaterThan, BinaryOperatorKind.LessThanOrEqual => BinaryOperatorKind.GreaterThanOrEqual, BinaryOperatorKind.GreaterThan => BinaryOperatorKind.LessThan, BinaryOperatorKind.GreaterThanOrEqual => BinaryOperatorKind.LessThanOrEqual, _ => comparison };
                }
                else return Choose(test, yes(Copy(state)), no(Copy(state)));
                if (ValueKey(variable) is not { } key) return Choose(test, yes(Copy(state)), no(Copy(state)));
                var previous = numericFacts; var initial = IntegerRange(variable, domain);
                TypedValue Branch(bool truth, Func<Dictionary<ISymbol, Binding>, TypedValue> body)
                {
                    numericFacts = new(previous); var min = initial.Minimum; var max = initial.Maximum; var excluded = new HashSet<BigInteger>(initial.Excluded);
                    var op = truth ? comparison : comparison switch { BinaryOperatorKind.Equals => BinaryOperatorKind.NotEquals, BinaryOperatorKind.NotEquals => BinaryOperatorKind.Equals, BinaryOperatorKind.LessThan => BinaryOperatorKind.GreaterThanOrEqual, BinaryOperatorKind.LessThanOrEqual => BinaryOperatorKind.GreaterThan, BinaryOperatorKind.GreaterThan => BinaryOperatorKind.LessThanOrEqual, _ => BinaryOperatorKind.LessThan };
                    switch (op)
                    {
                        case BinaryOperatorKind.Equals: min = constant; max = constant; break;
                        case BinaryOperatorKind.NotEquals: excluded.Add(constant); break;
                        case BinaryOperatorKind.LessThan: max = max is null ? constant - 1 : BigInteger.Min(max.Value, constant - 1); break;
                        case BinaryOperatorKind.LessThanOrEqual: max = max is null ? constant : BigInteger.Min(max.Value, constant); break;
                        case BinaryOperatorKind.GreaterThan: min = min is null ? constant + 1 : BigInteger.Max(min.Value, constant + 1); break;
                        case BinaryOperatorKind.GreaterThanOrEqual: min = min is null ? constant : BigInteger.Max(min.Value, constant); break;
                    }
                    numericFacts[key] = new(min, max, excluded);
                    try { return body(Copy(state)); } finally { numericFacts = previous; }
                }
                return Choose(test, Branch(true, yes), Branch(false, no));
            });
        }
    }
}
