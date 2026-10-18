using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Morphir.IR.Classic;
using TypedValue = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        private readonly Dictionary<string, IMethodSymbol> dataOperators = [];
        private void RegisterDataOperators(IEnumerable<INamedTypeSymbol> selected)
        {
            foreach (var type in selected.Where(DataType)) foreach (var method in type.GetMembers().OfType<IMethodSymbol>().Where(method => !method.IsImplicitlyDeclared && method.MethodKind is MethodKind.UserDefinedOperator or MethodKind.Conversion))
                { AddSymbol(method, SourceOwnerName(type), method.MetadataName + SignatureSuffix(method)); dataOperators[SymbolId(method)] = method; RegisterLocalMethods(method); }
        }
        private IMethodSymbol? DataOperator(IOperation operation)
        {
            var method = operation switch { IBinaryOperation binary => binary.OperatorMethod, IUnaryOperation unary => unary.OperatorMethod, IConversionOperation conversion => conversion.OperatorMethod, _ => null };
            return method is not null && dataOperators.ContainsKey(SymbolId(method)) ? method : null;
        }
        private void EmitDataOperatorDependencies(IOperation root)
        {
            ValidateSourceDataMembers(root);
            EmitDataEqualityDependencies(root);
            EmitRecordCopyDependencies(root);
            foreach (var binary in Operations(root).OfType<IBinaryOperation>().Where(binary => DataOperator(binary) is not null && binary.OperatorKind is BinaryOperatorKind.ConditionalAnd or BinaryOperatorKind.ConditionalOr)) if (DataTruthOperator(binary) is { } truth) EmitMethod(truth);
            foreach (var method in Operations(root).Select(DataOperator).Where(method => method is not null)) EmitMethod(method!);
        }
        private TypedValue ApplyDataOperator(IMethodSymbol method, IReadOnlyList<TypedValue> arguments, Location location)
        {
            calls.TryAdd(currentMethod, []); calls[currentMethod].Add((SymbolId(method), location));
            IrType<Unit> signature = CallableOutput(method, location); foreach (var parameter in method.Parameters.Reverse()) signature = Types.Function(Type(parameter.Type, location), signature);
            TypedValue result = new ReferenceValue<Unit, IrType<Unit>>(signature, Identity(method));
            for (var index = 0; index < arguments.Count; index++) { signature = ((FunctionType<Unit>)signature).Result; result = new ApplyValue<Unit, IrType<Unit>>(signature, result, Adapt(DataArgument(arguments[index], location), Type(method.Parameters[index].Type, location))); }
            return result;
        }
        private IMethodSymbol? DataTruthOperator(IBinaryOperation operation) => operation.OperatorMethod?.ContainingType.GetMembers(operation.OperatorKind == BinaryOperatorKind.ConditionalAnd ? "op_False" : "op_True").OfType<IMethodSymbol>().SingleOrDefault(method => dataOperators.ContainsKey(SymbolId(method)));
        private TypedValue? DataShortCircuitExpression(IOperation operation, BindingEnvironment environment, Func<TypedValue, BindingEnvironment, TypedValue> continuation)
        {
            if (operation is not IBinaryOperation binary || binary.OperatorKind is not (BinaryOperatorKind.ConditionalAnd or BinaryOperatorKind.ConditionalOr) || DataOperator(binary) is not { } method) return null;
            var location = operation.Syntax.GetLocation(); var truth = DataTruthOperator(binary) ?? Fail<IMethodSymbol>("MCS3001", "Conditional operator requires the selected true/false body.", location);
            return Expression(binary.LeftOperand, environment, (left, state) =>
            {
                var saved = bindings; bindings = state; TypedValue condition; try { condition = ApplyDataOperator(truth, [left], location); } finally { bindings = saved; }
                var skipped = continuation(left, Copy(state));
                var evaluated = Expression(binary.RightOperand, Copy(state), (right, next) =>
                {
                    var previous = bindings; bindings = next;
                    try
                    {
                        if (!DataReferenceResult(method.ReturnType)) return continuation(ApplyDataOperator(method, [left, right], location), next);
                        TypedValue Bind(int index, BindingEnvironment updated) => index == method.Parameters.Length ? RunDataCallable(method, null, updated, continuation) : BindValue(method.Parameters[index], method.Parameters[index].Type, index == 0 ? left : right, updated, after => Bind(index + 1, after), location);
                        return Bind(0, next);
                    }
                    finally { bindings = previous; }
                });
                return new IfValue<Unit, IrType<Unit>>(skipped.Attributes, condition, skipped, evaluated);
            });
        }
        private TypedValue? LowerDataOperator(IOperation operation)
        {
            if (DataOperator(operation) is not { } method) return null;
            var location = operation.Syntax.GetLocation();
            if (operation is IBinaryOperation binary)
            {
                if (binary.OperatorKind is BinaryOperatorKind.ConditionalAnd or BinaryOperatorKind.ConditionalOr) return Fail<TypedValue>("MCS3001", "Conditional user operators require their true/false short-circuit mapping.", location);
                var left = Lower(binary.LeftOperand); var right = Lower(binary.RightOperand);
                if (!binary.IsLifted) return ApplyDataOperator(method, [left, right], location);
                var equality = binary.OperatorKind is BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals;
                TypedValue Missing(bool both) => equality ? Lit(Types.Bool, new BoolLiteral(binary.OperatorKind == BinaryOperatorKind.Equals ? both : !both)) : MaybeElement(Type(binary.Type!, location)) is not null ? Nothing(Type(binary.Type!, location)) : Lit(Types.Bool, new BoolLiteral(false));
                return MatchMaybe(left, () => MatchMaybe(right, () => Missing(true), _ => Missing(false)), a => MatchMaybe(right, () => Missing(false), b => Adapt(ApplyDataOperator(method, [a, b], location), Type(binary.Type!, location))));
            }
            if (operation is IUnaryOperation unary)
                return unary.IsLifted ? MatchMaybe(Lower(unary.Operand), () => Nothing(Type(unary.Type!, location)), value => Adapt(ApplyDataOperator(method, [value], location), Type(unary.Type!, location))) : ApplyDataOperator(method, [Lower(unary.Operand)], location);
            if (operation is IConversionOperation conversion)
                return NullableElement(conversion.Operand.Type) is not null && NullableElement(method.Parameters[0].Type) is null ? MatchMaybe(Lower(conversion.Operand), () => Nothing(Type(conversion.Type!, location)), value => Adapt(ApplyDataOperator(method, [value], location), Type(conversion.Type!, location))) : ApplyDataOperator(method, [Lower(conversion.Operand)], location);
            return null;
        }
    }
}
