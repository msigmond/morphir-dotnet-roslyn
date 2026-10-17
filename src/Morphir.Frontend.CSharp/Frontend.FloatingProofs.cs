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
        private static bool FloatProbe(IOperation operation) => operation is IInvocationOperation call && FloatingPredicateCall(call) && call.TargetMethod.Name == "IsFinite" || operation is IBinaryOperation { IsLifted: false, OperatorKind: BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals } binary && Floating(binary.LeftOperand.Type) is not null;
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
            if (value is LiteralValue<Unit, IrType<Unit>> { Literal: FloatLiteral f }) return double.IsFinite(f.Value);
            if (ValueKey(value) is { } key && finiteFacts.Contains(key)) return true;
            if (value is VariableValue<Unit, IrType<Unit>> variable && boundValues.TryGetValue(variable.Name.Key, out var bound)) return ProvedFinite(bound);
            if (value is FieldValue<Unit, IrType<Unit>> field && field.Subject is RecordValue<Unit, IrType<Unit>> record) return ProvedFinite(record.Fields.Single(f => f.Key.Key == field.Field.Key).Value);
            if (value is FieldValue<Unit, IrType<Unit>> projected && projected.Subject is VariableValue<Unit, IrType<Unit>> receiver && boundValues.TryGetValue(receiver.Name.Key, out var instance)) return ProvedFinite(new FieldValue<Unit, IrType<Unit>>(projected.Attributes, instance, projected.Field));
            return false;
        }
        private TypedValue? FloatingDecision(IOperation condition, Dictionary<ISymbol, Binding> environment, Func<Dictionary<ISymbol, Binding>, TypedValue> yes, Func<Dictionary<ISymbol, Binding>, TypedValue> no)
        {
            if (!FloatProbe(condition) || HasAssignment(condition)) return null;
            if (condition is IBinaryOperation comparison) return Expression(condition, environment, (test, state) =>
            {
                var left = In(state, comparison.LeftOperand); var right = In(state, comparison.RightOperand);
                var value = ProvedZeroFloat(right) ? left : ProvedZeroFloat(left) ? right : null;
                if (value is null || ValueKey(value) is not { } key) return Choose(test, yes(Copy(state)), no(Copy(state)));
                var equality = comparison.OperatorKind == BinaryOperatorKind.Equals;
                var otherwise = (equality ? no : yes)(Copy(state)); var previous = zeroFloatFacts; zeroFloatFacts = new(previous) { key }; TypedValue zero;
                try { zero = (equality ? yes : no)(Copy(state)); } finally { zeroFloatFacts = previous; }
                return equality ? Choose(test, zero, otherwise) : Choose(test, otherwise, zero);
            });
            var call = (IInvocationOperation)condition;
            return Expression(condition, environment, (test, state) =>
            {
                var argument = In(state, call.Arguments[0].Value);
                var otherwise = no(Copy(state));
                if (ValueKey(argument) is not { } key) return Choose(test, yes(Copy(state)), otherwise);
                var previous = finiteFacts; finiteFacts = new(previous) { key }; TypedValue present;
                try { present = yes(Copy(state)); } finally { finiteFacts = previous; }
                return Choose(test, present, otherwise);
            });
        }
    }
}
