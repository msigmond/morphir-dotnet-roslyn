using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Morphir.IR.Classic;
using TypedValue = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        private Dictionary<IOperation, TypedValue> operandValues = [];
        private readonly Dictionary<string, object?> bindingConstants = new(StringComparer.Ordinal);
        private object? ValueConstant(TypedValue value) => value switch
        {
            LiteralValue<Unit, IrType<Unit>> { Literal: IntLiteral i } => i.Value is >= int.MinValue and <= int.MaxValue ? (object)(int)i.Value : new System.Numerics.BigInteger(i.Value),
            LiteralValue<Unit, IrType<Unit>> { Literal: BoolLiteral b } => b.Value,
            LiteralValue<Unit, IrType<Unit>> { Literal: StringLiteral s } => s.Value,
            VariableValue<Unit, IrType<Unit>> variable => bindingConstants.GetValueOrDefault(variable.Name.Key),
            ApplyValue<Unit, IrType<Unit>> { Function: ApplyValue<Unit, IrType<Unit>> { Function: ReferenceValue<Unit, IrType<Unit>> function, Argument: var left }, Argument: var right }
                when function.Name.Package.Key == Types.Sdk.Key && function.Name.Local.Key == "add" && ValueConstant(left) is int x && ValueConstant(right) is int y && (long)x + y is >= int.MinValue and <= int.MaxValue => x + y,
            _ => null
        };
        private static bool HasAssignment(IOperation operation) => operation is ISimpleAssignmentOperation or ICoalesceAssignmentOperation || operation.ChildOperations.Any(HasAssignment);
        private TypedValue Expression(IOperation operation, Dictionary<ISymbol, Binding> environment, Func<TypedValue, Dictionary<ISymbol, Binding>, TypedValue> continuation)
        {
            if (PresenceExpression(operation, environment, continuation) is { } presence) return presence;
            if (!HasAssignment(operation)) return continuation(In(environment, operation), environment);
            if (operation is ISimpleAssignmentOperation assignment)
            {
                var symbol = assignment.Target switch { ILocalReferenceOperation l => (ISymbol)l.Local, IParameterReferenceOperation p => p.Parameter, _ => null };
                if (assignment.IsRef || symbol is null) return Fail<TypedValue>("MCS2404", "Only local/by-value parameter rebinding is admitted.", operation.Syntax.GetLocation());
                return Expression(assignment.Value, environment, (value, updated) => BindValue(symbol, assignment.Target.Type!, value, updated,
                    next => continuation(new VariableValue<Unit, IrType<Unit>>(next[symbol].Type, next[symbol].Name), next), operation.Syntax.GetLocation()));
            }
            if (operation is IConditionalOperation { WhenFalse: not null, IsRef: false } conditional)
                return Decision(conditional.Condition, environment,
                    state => Expression(conditional.WhenTrue, state, continuation),
                    state => Expression(conditional.WhenFalse, state, continuation));
            if (operation is IBinaryOperation binary && binary.OperatorKind is BinaryOperatorKind.ConditionalAnd or BinaryOperatorKind.ConditionalOr)
                return Expression(binary.LeftOperand, environment, (condition, updated) =>
                {
                    var rhs = Expression(binary.RightOperand, Copy(updated), continuation);
                    bool isAnd = binary.OperatorKind == BinaryOperatorKind.ConditionalAnd;
                    var skipped = continuation(Lit(Types.Bool, new BoolLiteral(!isAnd)), Copy(updated));
                    return new IfValue<Unit, IrType<Unit>>(rhs.Attributes, condition, isAnd ? rhs : skipped, isAnd ? skipped : rhs);
                });
            IOperation[] operands = operation switch
            {
                IBinaryOperation b => [b.LeftOperand, b.RightOperand],
                IUnaryOperation u => [u.Operand],
                IConversionOperation c => [c.Operand],
                IInvocationOperation i => (i.Instance is null ? Enumerable.Empty<IOperation>() : new[] { i.Instance }).Concat(i.Arguments.Select(a => a.Value)).ToArray(),
                IObjectCreationOperation c => c.Arguments.Select(a => a.Value).ToArray(),
                IPropertyReferenceOperation { Instance: not null } p => [p.Instance],
                _ => []
            };
            var evaluated = new Dictionary<IOperation, TypedValue>();
            TypedValue Evaluate(int index, Dictionary<ISymbol, Binding> state)
            {
                if (index != operands.Length)
                    return Expression(operands[index], state, (value, updated) => { evaluated[operands[index]] = value; return Evaluate(index + 1, updated); });
                var previous = operandValues;
                operandValues = new(previous); foreach (var entry in evaluated) operandValues[entry.Key] = entry.Value;
                TypedValue result;
                try { result = In(state, operation); }
                finally { operandValues = previous; }
                return continuation(result, state);
            }
            return Evaluate(0, environment);
        }
        private TypedValue BindValue(ISymbol symbol, ITypeSymbol type, TypedValue value, Dictionary<ISymbol, Binding> environment,
            Func<Dictionary<ISymbol, Binding>, TypedValue> continuation, Location location)
        {
            var attr = Type(type, location, true);
            if (MaybeElement(attr) is not null) value = Adapt(value, attr);
            else attr = value.Attributes;
            var name = Fresh((symbol is IParameterSymbol ? "parameter_" : "local_") + symbol.Name);
            var constant = ValueConstant(value); bindingConstants[name.Key] = constant;
            bindingOrigins[name.Key] = ValueKey(value);
            boundValues[name.Key] = value;
            if (Integral(NullableElement(type) ?? type) is { } integerDomain)
            {
                var variable = new VariableValue<Unit, IrType<Unit>>(attr, name);
                if (ValueKey(variable) is { } key) numericFacts[key] = IntegerRange(value, integerDomain);
            }
            var next = Copy(environment); next[symbol] = new(name, attr, constant);
            var result = continuation(next);
            return new LetValue<Unit, IrType<Unit>>(result.Attributes, name, new([], attr, value), result);
        }
    }
}
