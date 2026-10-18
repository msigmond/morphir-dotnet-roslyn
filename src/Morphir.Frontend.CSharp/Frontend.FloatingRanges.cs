using System.Numerics;
using Microsoft.CodeAnalysis.Operations;
using Morphir.IR.Classic;
using TypedValue = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        private sealed record FloatingRange(double Minimum, double Maximum, bool MayNaN)
        {
            public bool Finite => !MayNaN && double.IsFinite(Minimum) && double.IsFinite(Maximum);
        }
        private Dictionary<string, FloatingRange> floatingFacts = [];
        private double? KnownFloat(TypedValue value)
        {
            if (dataProjections.TryGetValue(value, out var projected)) return KnownFloat(projected);
            if (value is LiteralValue<Unit, IrType<Unit>> { Literal: FloatLiteral literal }) return literal.Value;
            if (value is VariableValue<Unit, IrType<Unit>> variable && boundValues.TryGetValue(variable.Name.Key, out var bound)) return KnownFloat(bound);
            if (value is FieldValue<Unit, IrType<Unit>> field && field.Subject is RecordValue<Unit, IrType<Unit>> record) return KnownFloat(record.Fields.Single(item => item.Key.Key == field.Field.Key).Value);
            if (value is ApplyValue<Unit, IrType<Unit>> { Function: ReferenceValue<Unit, IrType<Unit>> negate, Argument: var operand } && negate.Name.Package.Key == Types.Sdk.Key && negate.Name.Module.Key == Morphir.IR.Classic.Path.From("Basics").Key && negate.Name.Local.Key == "negate" && KnownFloat(operand) is { } number) return -number;
            if (value is ReferenceValue<Unit, IrType<Unit>> reference && reference.Name.Package.Key == package.Key && reference.Name.Module.Key == Morphir.IR.Classic.Path.From("Compat.Floating").Key)
                return reference.Name.Local.Key switch { "nan" => double.NaN, "infinity" => double.PositiveInfinity, "negative\0infinity" => double.NegativeInfinity, "negative\0zero" => -0d, _ => null };
            return null;
        }
        private FloatingRange FloatRange(TypedValue value)
        {
            if (KnownFloat(value) is { } number) return double.IsNaN(number) ? new(double.NegativeInfinity, double.PositiveInfinity, true) : new(number, number, false);
            if (ValueKey(value) is { } key && floatingFacts.TryGetValue(key, out var range)) return range;
            if (dataProjections.TryGetValue(value, out var projected)) return FloatRange(projected);
            if (value is VariableValue<Unit, IrType<Unit>> variable && boundValues.TryGetValue(variable.Name.Key, out var bound)) return FloatRange(bound);
            if (value is FieldValue<Unit, IrType<Unit>> field)
            {
                var subject = field.Subject;
                while (subject is VariableValue<Unit, IrType<Unit>> owner && boundValues.TryGetValue(owner.Name.Key, out var initializer)) subject = initializer;
                if (subject is RecordValue<Unit, IrType<Unit>> record) return FloatRange(record.Fields.Single(item => item.Key.Key == field.Field.Key).Value);
            }
            var finite = ValueKey(value) is { } identity && finiteFacts.Contains(identity);
            return new(finite ? -double.MaxValue : double.NegativeInfinity, finite ? double.MaxValue : double.PositiveInfinity, !finite);
        }
        private bool ProvedFloatingInteger(TypedValue value, IntegerDomain target)
        {
            var range = FloatRange(value);
            return range.Finite && (target.Bits == 0 || new BigInteger(range.Minimum) >= target.Minimum && new BigInteger(range.Maximum) <= target.Maximum);
        }
        private static BinaryOperatorKind ReverseFloatingComparison(BinaryOperatorKind comparison) => comparison switch
        {
            BinaryOperatorKind.LessThan => BinaryOperatorKind.GreaterThan,
            BinaryOperatorKind.LessThanOrEqual => BinaryOperatorKind.GreaterThanOrEqual,
            BinaryOperatorKind.GreaterThan => BinaryOperatorKind.LessThan,
            BinaryOperatorKind.GreaterThanOrEqual => BinaryOperatorKind.LessThanOrEqual,
            _ => comparison
        };
        private static FloatingRange RefineFloatingComparison(FloatingRange initial, BinaryOperatorKind comparison, double constant, bool truth)
        {
            if (double.IsNaN(constant)) return initial;
            var positive = comparison != BinaryOperatorKind.NotEquals;
            var excludesNaN = truth == positive;
            var op = truth ? comparison : comparison switch
            {
                BinaryOperatorKind.Equals => BinaryOperatorKind.NotEquals,
                BinaryOperatorKind.NotEquals => BinaryOperatorKind.Equals,
                BinaryOperatorKind.LessThan => BinaryOperatorKind.GreaterThanOrEqual,
                BinaryOperatorKind.LessThanOrEqual => BinaryOperatorKind.GreaterThan,
                BinaryOperatorKind.GreaterThan => BinaryOperatorKind.LessThanOrEqual,
                _ => BinaryOperatorKind.LessThan
            };
            var min = initial.Minimum; var max = initial.Maximum;
            switch (op)
            {
                case BinaryOperatorKind.Equals: min = Math.Max(min, constant); max = Math.Min(max, constant); break;
                case BinaryOperatorKind.LessThan: max = Math.Min(max, Math.BitDecrement(constant)); break;
                case BinaryOperatorKind.LessThanOrEqual: max = Math.Min(max, constant); break;
                case BinaryOperatorKind.GreaterThan: min = Math.Max(min, Math.BitIncrement(constant)); break;
                case BinaryOperatorKind.GreaterThanOrEqual: min = Math.Max(min, constant); break;
            }
            return new(min, max, initial.MayNaN && !excludesNaN);
        }
    }
}
