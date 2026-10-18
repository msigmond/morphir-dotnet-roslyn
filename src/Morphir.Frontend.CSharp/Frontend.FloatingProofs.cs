using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Morphir.IR.Classic;
using TypedValue = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;
namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        private HashSet<string> finiteFacts = [];
        private HashSet<string> zeroFloatFacts = [];
        private static bool FloatingPredicateCall(IInvocationOperation call) => FloatingPredicateMethod(call.TargetMethod);
        private static bool FloatingPredicateMethod(IMethodSymbol method) => method.IsStatic && Floating(method.ContainingType) is not null && method.Name is "IsFinite" or "IsNaN" or "IsInfinity" && method.Parameters.Length == 1;
        private static bool FloatProbe(IOperation operation) => operation is IInvocationOperation call && FloatingPredicateCall(call) || operation is IBinaryOperation { IsLifted: false, OperatorKind: BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals or BinaryOperatorKind.LessThan or BinaryOperatorKind.LessThanOrEqual or BinaryOperatorKind.GreaterThan or BinaryOperatorKind.GreaterThanOrEqual } binary && Floating(binary.LeftOperand.Type) is not null;
        private bool ProvedZeroFloat(TypedValue value)
        {
            if (value is LiteralValue<Unit, IrType<Unit>> { Literal: FloatLiteral f }) return f.Value == 0;
            if (ValueKey(value) is { } key && zeroFloatFacts.Contains(key)) return true;
            if (value is VariableValue<Unit, IrType<Unit>> variable && boundValues.TryGetValue(variable.Name.Key, out var bound)) return ProvedZeroFloat(bound);
            if (value is FieldValue<Unit, IrType<Unit>> field && field.Subject is RecordValue<Unit, IrType<Unit>> record) return ProvedZeroFloat(record.Fields.Single(f => f.Key.Key == field.Field.Key).Value);
            if (value is FieldValue<Unit, IrType<Unit>> projected && projected.Subject is VariableValue<Unit, IrType<Unit>> receiver && boundValues.TryGetValue(receiver.Name.Key, out var instance)) return ProvedZeroFloat(new FieldValue<Unit, IrType<Unit>>(projected.Attributes, instance, projected.Field));
            return false;
        }
        private bool ProvedFinite(TypedValue value)
        {
            if (ProvedZeroFloat(value) || FloatRange(value).Finite) return true;
            if (value is LiteralValue<Unit, IrType<Unit>> { Literal: FloatLiteral f }) return double.IsFinite(f.Value);
            if (ValueKey(value) is { } key && finiteFacts.Contains(key)) return true;
            if (value is VariableValue<Unit, IrType<Unit>> variable && boundValues.TryGetValue(variable.Name.Key, out var bound)) return ProvedFinite(bound);
            if (value is FieldValue<Unit, IrType<Unit>> field && field.Subject is RecordValue<Unit, IrType<Unit>> record) return ProvedFinite(record.Fields.Single(f => f.Key.Key == field.Field.Key).Value);
            if (value is FieldValue<Unit, IrType<Unit>> projected && projected.Subject is VariableValue<Unit, IrType<Unit>> receiver && boundValues.TryGetValue(receiver.Name.Key, out var instance)) return ProvedFinite(new FieldValue<Unit, IrType<Unit>>(projected.Attributes, instance, projected.Field));
            return false;
        }
        private TypedValue? FloatingDecision(IOperation condition, BindingEnvironment environment, Func<BindingEnvironment, TypedValue> yes, Func<BindingEnvironment, TypedValue> no)
        {
            if (!FloatProbe(condition) || HasAssignment(condition)) return null;
            return Expression(condition, environment, (test, state) => FloatingDecisionValue(test, state, yes, no) ?? Choose(test, yes(Copy(state)), no(Copy(state))));
        }
        private sealed record FloatingPredicate(TypedValue Left, TypedValue? Right, BinaryOperatorKind Comparison, string? Classification);
        private readonly Dictionary<TypedValue, FloatingPredicate> floatingPredicates = new(ReferenceEqualityComparer.Instance);
        private TypedValue RememberFloatingPredicate(TypedValue test, TypedValue left, TypedValue? right = null, BinaryOperatorKind comparison = BinaryOperatorKind.None, string? classification = null)
        {
            floatingPredicates[test] = new(left, right, comparison, classification); return test;
        }
        private TypedValue? FloatingDecisionValue(TypedValue test, BindingEnvironment state, Func<BindingEnvironment, TypedValue> yes, Func<BindingEnvironment, TypedValue> no)
        {
            if (!floatingPredicates.TryGetValue(test, out var predicate)) return null;
            if (predicate.Right is { } right)
            {
                var left = predicate.Left;
                TypedValue value; double constant; var op = predicate.Comparison;
                if ((KnownFloat(right) ?? (ProvedZeroFloat(right) ? 0d : null)) is { } rc) { value = left; constant = rc; }
                else if ((KnownFloat(left) ?? (ProvedZeroFloat(left) ? 0d : null)) is { } lc) { value = right; constant = lc; op = ReverseFloatingComparison(op); }
                else return Choose(test, yes(Copy(state)), no(Copy(state)));
                if (ValueKey(value) is not { } key) return Choose(test, yes(Copy(state)), no(Copy(state)));
                var initial = FloatRange(value); var previousRanges = floatingFacts; var previousZero = zeroFloatFacts;
                TypedValue Branch(bool truth, Func<BindingEnvironment, TypedValue> body)
                {
                    floatingFacts = new(previousRanges) { [key] = RefineFloatingComparison(initial, op, constant, truth) };
                    zeroFloatFacts = new(previousZero);
                    if (constant == 0 && (op == BinaryOperatorKind.Equals && truth || op == BinaryOperatorKind.NotEquals && !truth)) zeroFloatFacts.Add(key);
                    try { return body(Copy(state)); } finally { floatingFacts = previousRanges; zeroFloatFacts = previousZero; }
                }
                // Preserve the established equality branch emission order.
                if (op == BinaryOperatorKind.Equals) { var otherwise = Branch(false, no); return Choose(test, Branch(true, yes), otherwise); }
                return Choose(test, Branch(true, yes), Branch(false, no));

            }
            {
                var argument = predicate.Left; var classification = predicate.Classification;
                if (ValueKey(argument) is not { } key) return Choose(test, yes(Copy(state)), no(Copy(state)));
                var initial = FloatRange(argument); var previous = finiteFacts; var previousRanges = floatingFacts;
                TypedValue Branch(bool truth, Func<BindingEnvironment, TypedValue> body)
                {
                    finiteFacts = new(previous); floatingFacts = new(previousRanges);
                    if (classification == "IsFinite" && truth) { finiteFacts.Add(key); floatingFacts[key] = new(Math.Max(initial.Minimum, -double.MaxValue), Math.Min(initial.Maximum, double.MaxValue), false); }
                    if (classification == "IsNaN" && !truth) floatingFacts[key] = initial with { MayNaN = false };
                    if (classification == "IsInfinity" && !truth) floatingFacts[key] = new(Math.Max(initial.Minimum, -double.MaxValue), Math.Min(initial.Maximum, double.MaxValue), initial.MayNaN);
                    try { return body(Copy(state)); } finally { finiteFacts = previous; floatingFacts = previousRanges; }
                }
                var otherwise = Branch(false, no);
                return Choose(test, Branch(true, yes), otherwise);
            }
        }
    }
}
