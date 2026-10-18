using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Morphir.IR.Classic;
using IrPath = Morphir.IR.Classic.Path;
using TypedValue = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        private sealed record Binding(Name Name, IrType<Unit> Type, object? Constant, DataOrigin? Data = null);
        private BindingEnvironment bindings = new(SymbolEqualityComparer.Default);
        private readonly HashSet<string> bindingNames = new(StringComparer.Ordinal);
        private readonly HashSet<IOperation> provedArithmetic = [];
        private TypedValue? currentReceiver;
        private static Name ReceiverName(IMethodSymbol method)
        {
            var name = Name.From("receiver"); var used = method.Parameters.Select(p => Name.From(p.Name).Key).ToHashSet(); var version = 0;
            while (used.Contains(name.Key)) name = Name.From("receiver_" + (++version));
            return name;
        }
        private void ResetValueState()
        {
            bindings = new(SymbolEqualityComparer.Default); dataOrigins.Clear(); dataBindingOrigins.Clear(); provedDataOperations.Clear(); dataProjections.Clear(); dataValueKeys.Clear();
            bindingNames.Clear();
            bindingConstants.Clear();
            presentValues.Clear();
            interfaceCastProofs.Clear(); interfacePredicates.Clear();
            interfaceGraphs.Clear();
            provedNumericFactories.Clear();
            provedFactoryAdapters.Clear();
            bindingOrigins.Clear();
            boundValues.Clear();
            operandValues.Clear(); tupleProjections.Clear(); provedTupleOperations.Clear();
            provedArithmetic.Clear();
            paramsLengths.Clear(); provedParamsReads.Clear();
            pureFunctionOrigins.Clear(); provedDelegateCalls.Clear();
            provedDecimalOperations.Clear();
            numericFacts.Clear();
            decimalFacts.Clear(); decimalDomains.Clear(); decimalConstants.Clear();
            numericValueRanges.Clear();
            finiteFacts.Clear();
            zeroFloatFacts.Clear();
            floatingFacts.Clear();
            floatingPredicates.Clear();
            foreach (var id in identities.Values) bindingNames.Add(id.Local.Key);
            currentReceiver = null;
        }
        private void PrepareBindings(IMethodSymbol method)
        {
            ResetValueState();
            currentReceiver = !ReceiverRequired(method) ? null : new VariableValue<Unit, IrType<Unit>>(ReceiverIrType(method, Location(method)), ReceiverName(method));
            if (currentReceiver is not null) bindingNames.Add(ReceiverName(method).Key);
            foreach (var parameter in method.Parameters)
            {
                var name = Name.From(parameter.Name); bindingNames.Add(name.Key);
                bindingConstants[name.Key] = null;
                bindings[parameter] = new(name, Type(parameter.Type, Location(parameter)), null);
                if (Integral(NullableElement(parameter.Type) ?? parameter.Type) is { } integerDomain)
                    numericFacts["var:" + name.Key] = new(integerDomain.Bits == 0 ? null : integerDomain.Minimum, integerDomain.Bits == 0 ? null : integerDomain.Maximum, new HashSet<System.Numerics.BigInteger>());
            }
        }
        private static BindingEnvironment Copy(BindingEnvironment source) => new(source, SymbolEqualityComparer.Default);
        private Name Fresh(string source)
        {
            var name = Name.From(source); int version = 0;
            while (!bindingNames.Add(name.Key)) name = Name.From(source + "_" + ++version);
            return name;
        }
        private TypedValue ReadBinding(ISymbol symbol, Location location)
        {
            if (!bindings.TryGetValue(symbol.OriginalDefinition, out var binding)) return Fail<TypedValue>("MCS2401", $"Local {symbol.Name} is not definitely assigned in the current source scope.", location);
            return binding.Data is { } origin ? ReadDataOrigin(origin, bindings, location) : new VariableValue<Unit, IrType<Unit>>(binding.Type, binding.Name);
        }
        private object? Constant(IOperation operation)
        {
            if (operandValues.TryGetValue(operation, out var evaluated)) return ValueConstant(evaluated);
            if (operation.ConstantValue.HasValue) return operation.ConstantValue.Value;
            var symbol = operation switch { ILocalReferenceOperation l => (ISymbol)l.Local, IParameterReferenceOperation p => p.Parameter, _ => null };
            if (symbol is not null && bindings.TryGetValue(symbol, out var value)) return value.Constant;
            if (operation is IConversionOperation c && c.Conversion.IsIdentity) return Constant(c.Operand);
            if (operation is IBinaryOperation { OperatorKind: BinaryOperatorKind.Add } b && Constant(b.LeftOperand) is int x && Constant(b.RightOperand) is int y && (long)x + y is >= int.MinValue and <= int.MaxValue) return x + y;
            return null;
        }
        private TypedValue In(BindingEnvironment environment, IOperation expression)
        {
            var previous = bindings; bindings = environment;
            try { return Lower(expression); }
            finally { bindings = previous; }
        }
        private TypedValue SdkBinary(string name, IrType<Unit> argument, IrType<Unit> result, TypedValue left, TypedValue right)
        {
            var partial = Types.Function(argument, result);
            return new ApplyValue<Unit, IrType<Unit>>(result, new ApplyValue<Unit, IrType<Unit>>(partial,
                new ReferenceValue<Unit, IrType<Unit>>(Types.Function(argument, partial), new(Types.Sdk, IrPath.From("Basics"), Name.From(name))), left), right);
        }
        private TypedValue LowerMethodBlock(IMethodBodyOperation methodBody, IBlockOperation body, MethodDeclarationSyntax syntax)
            => LowerCallableBlock(ControlFlowGraph.Create(methodBody), body, syntax, syntax.Body!);
        private TypedValue LowerCallableBlock(ControlFlowGraph graph, IBlockOperation body, SyntaxNode syntax, SyntaxNode flowSyntax)
        {
            foreach (var predecessor in graph.Blocks[^1].Predecessors)
            {
                if (!predecessor.Source.IsReachable) continue;
                if (predecessor.Source.BranchValue?.ConstantValue is { HasValue: true, Value: bool value } && predecessor.Source.ConditionKind != ControlFlowConditionKind.None)
                {
                    bool conditional = predecessor.Source.ConditionKind == ControlFlowConditionKind.WhenTrue ? value : !value;
                    if (predecessor.IsConditionalSuccessor != conditional) continue;
                }
                if (predecessor.Semantics != ControlFlowBranchSemantics.Return)
                    Fail<object>("MCS2402", "The control-flow graph has an unproved value-return path.", syntax.GetLocation());
            }
            var dataFlow = Semantic(syntax.SyntaxTree).AnalyzeDataFlow(flowSyntax);
            if (!dataFlow.Succeeded) Fail<object>("MCS2401", "Definite-assignment/scope analysis failed.", syntax.GetLocation());
            ValidateBlockOperations(body);
            var result = Statements(body.Operations, 0, Copy(bindings), _ => Fail<TypedValue>("MCS2402", "A value-return path falls through.", syntax.GetLocation()));
            ValidateUnreachedArithmetic(body);
            return result;
        }
        private void ValidateUnreachedArithmetic(IOperation operation)
        {
            if (operation is ILocalFunctionOperation) return;
            if (operation is IBinaryOperation { OperatorKind: BinaryOperatorKind.Add } addition && !SelectedEnum(NullableElement(addition.Type) ?? addition.Type) && !NumericType(NullableElement(addition.Type) ?? addition.Type) && !DecimalType(NullableElement(addition.Type) ?? addition.Type) && DataOperator(operation) is null && !provedArithmetic.Contains(operation) && !SafeAddition(addition))
                Fail<object>("MCS2302", "An unreachable addition still requires the delivered numeric proof contract.", operation.Syntax.GetLocation());
            bool decimalFailure = operation is IBinaryOperation decimalBinary && DecimalType(NullableElement(decimalBinary.LeftOperand.Type) ?? decimalBinary.LeftOperand.Type)
                || operation is IConversionOperation decimalConversion && (DecimalType(NullableElement(decimalConversion.Type) ?? decimalConversion.Type) || DecimalType(NullableElement(decimalConversion.Operand.Type) ?? decimalConversion.Operand.Type))
                || operation is IInvocationOperation decimalCall && DecimalMember(decimalCall)
                || operation is IObjectCreationOperation decimalCreation && DecimalConstruction(decimalCreation);
            if (decimalFailure && !provedDecimalOperations.Contains(operation)) In(bindings, operation);
            if ((operation is IInvocationOperation factory && NumericFactoryRequirement(factory.TargetMethod) is not null || operation is IMethodReferenceOperation factoryReference && NumericFactoryRequirement(factoryReference.Method) is not null) && !provedNumericFactories.Contains(operation)) In(bindings, operation);
            if (operation is IInvocationOperation genericCall && SourceGenericCallable(genericCall.TargetMethod) && GenericFactories(genericCall.TargetMethod).Length != 0 && !provedFactoryAdapters.Contains(operation)) In(bindings, operation);
            if (operation is IDelegateCreationOperation { Target: IMethodReferenceOperation genericReference } && SourceGenericCallable(genericReference.Method) && GenericFactories(genericReference.Method).Length != 0 && !provedFactoryAdapters.Contains(genericReference)) In(bindings, operation);
            if (operation is IArrayElementReferenceOperation arrayRead && ParamsArray(arrayRead.ArrayReference.Type) && !provedParamsReads.Contains(operation)) In(bindings, operation);
            if (operation is IInvocationOperation delegateCall && delegateCall.TargetMethod.MethodKind == MethodKind.DelegateInvoke && !provedDelegateCalls.Contains(operation)) In(bindings, operation);
            if (operation is IBinaryOperation identity && DataIdentityBinary(identity)) Fail<object>("MCS3003", "Reference identity observations are excluded, including unreachable source.", operation.Syntax.GetLocation());
            if (DataSafetyOperation(operation) && !provedDataOperations.Contains(operation)) In(bindings, operation);
            if (TupleSafetyOperation(operation) && !provedTupleOperations.Contains(operation)) In(bindings, operation);
            foreach (var child in operation.ChildOperations) ValidateUnreachedArithmetic(child);
        }
        private void ValidateBlockOperations(IOperation operation)
        {
            if (operation is ILocalFunctionOperation) return;
            if (operation is IBinaryOperation identity && DataIdentityBinary(identity)) Fail<object>("MCS3003", "Reference identity observations require rejection; value equality needs its explicit D9 mapping.", operation.Syntax.GetLocation());
            // Validate selected source even where a constant branch/return makes it unreachable.
            bool supported = PresenceOperation(operation) || InterfaceTypeTest(operation) is not null || InterfacePatternPart(operation) || operation is IBlockOperation or IVariableDeclarationGroupOperation or IVariableDeclarationOperation
                or IVariableDeclaratorOperation or IVariableInitializerOperation or IExpressionStatementOperation
                or ISimpleAssignmentOperation or IDeconstructionAssignmentOperation or IDeclarationExpressionOperation or ITupleOperation or ITupleBinaryOperation or IDiscardOperation or IReturnOperation or IConditionalOperation or IEmptyOperation
                or IMemberInitializerOperation or IWithOperation or IObjectOrCollectionInitializerOperation or IArrayCreationOperation or IArrayInitializerOperation or IArrayElementReferenceOperation or IDelegateCreationOperation or IMethodReferenceOperation or IInstanceReferenceOperation or ISizeOfOperation or IFieldReferenceOperation or ILiteralOperation or IParameterReferenceOperation or ILocalReferenceOperation or IPropertyReferenceOperation
                or ITypeParameterObjectCreationOperation or IObjectCreationOperation or IArgumentOperation or IConversionOperation or IBinaryOperation or IUnaryOperation or IInvocationOperation;
            if (!supported) Fail<object>("MCS2403", $"Operation {operation.Kind} is outside the D3 structured block contract.", operation.Syntax.GetLocation());
            if (operation is IBinaryOperation { IsChecked: true } checkedBinary && (NumericType(NullableElement(checkedBinary.LeftOperand.Type) ?? checkedBinary.LeftOperand.Type) || EnumType(NullableElement(checkedBinary.LeftOperand.Type) ?? checkedBinary.LeftOperand.Type) is not null))
                Fail<object>("MCS2004", "Checked semantic numeric arithmetic is excluded, including unreachable source.", operation.Syntax.GetLocation());
            if (operation is IUnaryOperation { IsChecked: true } checkedUnary && (NumericType(NullableElement(checkedUnary.Operand.Type) ?? checkedUnary.Operand.Type) || EnumType(NullableElement(checkedUnary.Operand.Type) ?? checkedUnary.Operand.Type) is not null))
                Fail<object>("MCS2004", "Checked semantic numeric arithmetic is excluded, including unreachable source.", operation.Syntax.GetLocation());
            if (operation is IConversionOperation { IsChecked: true } checkedConversion && (NumericType(NullableElement(checkedConversion.Type) ?? checkedConversion.Type) || EnumConversion(checkedConversion)))
                Fail<object>("MCS2004", "Checked semantic numeric conversion is excluded, including unreachable source.", operation.Syntax.GetLocation());
            if (operation.Type is { SpecialType: not SpecialType.System_Void } type && !(operation is IConversionOperation nullComparison && NullTestConversion(nullComparison)) && !DataEqualityBox(operation) && !NullTestConstant(operation)) Type(type, operation.Syntax.GetLocation(), true);
            if (operation is ISimpleAssignmentOperation assignment && (assignment.IsRef || assignment.Target is not (ILocalReferenceOperation or IParameterReferenceOperation) && !TupleLocalTarget(assignment.Target) && !DataTarget(assignment.Target)))
                Fail<object>("MCS2404", "D3 assignment must rebind a local/by-value parameter; input/external member updates are not permitted.", operation.Syntax.GetLocation());
            if (operation is IArrayCreationOperation packed && !ParamsPack(packed)) Fail<object>("MCS2807", "General array construction requires D11; D7 packs only initialized params arguments.", operation.Syntax.GetLocation());
            if (operation is IPropertyReferenceOperation property && !PresenceProperty(property) && !ParamsLength(property) && !NumericProperty(property) && !DecimalProperty(property) && !DataTarget(property) && !DataGetter(property) && !TupleType(property.Property.ContainingType) && !records.ContainsKey(SymbolId(property.Property.ContainingType)))
                Fail<object>("MCS2107", "Unknown/effectful property read is outside the admitted record mapping.", operation.Syntax.GetLocation());
            if (operation is IBinaryOperation binary && DataOperator(operation) is null && !RecordEqualityBinary(binary) && !PresenceBinary(binary) && !DecimalType(binary.LeftOperand.Type) && Integral(binary.LeftOperand.Type) is null && Floating(binary.LeftOperand.Type) is null && !ComplexType(binary.LeftOperand.Type) && EnumType(binary.LeftOperand.Type) is null && EnumType(binary.RightOperand.Type) is null)
            {
                bool boolean = binary.LeftOperand.Type?.SpecialType == SpecialType.System_Boolean && binary.RightOperand.Type?.SpecialType == SpecialType.System_Boolean;
                bool admitted = boolean
                    ? binary.OperatorKind is BinaryOperatorKind.ConditionalAnd or BinaryOperatorKind.ConditionalOr or BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals or BinaryOperatorKind.And or BinaryOperatorKind.Or or BinaryOperatorKind.ExclusiveOr
                    : binary.LeftOperand.Type?.SpecialType == SpecialType.System_Int32 && binary.RightOperand.Type?.SpecialType == SpecialType.System_Int32 && binary.OperatorKind is BinaryOperatorKind.Add or BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals or BinaryOperatorKind.GreaterThan or BinaryOperatorKind.GreaterThanOrEqual or BinaryOperatorKind.LessThan or BinaryOperatorKind.LessThanOrEqual;
                if (!admitted || binary.OperatorMethod is not null || binary.IsLifted) Fail<object>("MCS2106", "Unmapped binary operator in selected source.", operation.Syntax.GetLocation());
                if (binary.IsChecked && binary.OperatorKind == BinaryOperatorKind.Add) Fail<object>("MCS2004", "Checked semantic arithmetic is excluded.", operation.Syntax.GetLocation());
            }
            if (operation is IUnaryOperation unary && DataOperator(operation) is null && !unary.IsLifted && !DecimalType(unary.Operand.Type) && Integral(unary.Operand.Type) is null && Floating(unary.Operand.Type) is null && !ComplexType(unary.Operand.Type) && EnumType(unary.Operand.Type) is null)
            {
                if (unary.IsChecked) Fail<object>("MCS2004", "Checked semantic arithmetic is excluded.", operation.Syntax.GetLocation());
                if (!(unary.OperatorMethod is null && (unary.OperatorKind == UnaryOperatorKind.Not && unary.Type?.SpecialType == SpecialType.System_Boolean || unary.OperatorKind == UnaryOperatorKind.Minus && unary.Type?.SpecialType == SpecialType.System_Int32 && unary.ConstantValue is { HasValue: true, Value: int })))
                    Fail<object>("MCS2107", "Unmapped unary operator in selected source.", operation.Syntax.GetLocation());
            }
            if (operation is IConversionOperation conversion && DataOperator(operation) is null && !DataEqualityBox(operation) && !ValueInterfaceConversion(conversion) && !InterfaceConversion(conversion) && !PresenceConversion(conversion) && !NullTestConversion(conversion) && !NumericConversion(conversion) && !EnumConversion(conversion) && !(TupleType(NullableElement(conversion.Type) ?? conversion.Type) && TupleType(NullableElement(conversion.Operand.Type) ?? conversion.Operand.Type)) && !((DecimalType(conversion.Type) && (DecimalType(conversion.Operand.Type) || Integral(conversion.Operand.Type) is not null || Floating(conversion.Operand.Type) is not null)) || DecimalType(conversion.Operand.Type) && (Integral(conversion.Type) is not null || Floating(conversion.Type) is not null || ComplexType(conversion.Type))))
            {
                if (conversion.IsChecked) Fail<object>("MCS2004", "Checked semantic conversions are excluded.", operation.Syntax.GetLocation());
                if (!(conversion.Conversion.IsIdentity || conversion.Operand is IObjectCreationOperation creation && !conversion.Conversion.IsUserDefined && SymbolEqualityComparer.Default.Equals(conversion.Type, creation.Type)))
                    Fail<object>("MCS2107", "Unmapped conversion in selected source.", operation.Syntax.GetLocation());
            }
            if (operation is IConditionalOperation { IsRef: true }) Fail<object>("MCS2404", "Ref conditionals require later ownership analysis.", operation.Syntax.GetLocation());
            if (operation is IObjectCreationOperation creationOperation && !NullableCreation(creationOperation) && !DefaultRecordCreation(creationOperation) && !NumericConstruction(creationOperation) && !DecimalConstruction(creationOperation) && !DataType(creationOperation.Type) && !TupleConstruction(creationOperation) && !(EnumType(creationOperation.Type) is not null && creationOperation.Arguments.Length == 0) && (creationOperation.Type is not INamedTypeSymbol named || !records.TryGetValue(SymbolId(named), out var fields) || creationOperation.Initializer is not null || creationOperation.Constructor?.Parameters.Length != fields.Length || creationOperation.Arguments.Length != fields.Length || creationOperation.Arguments.Any(a => a.ArgumentKind != ArgumentKind.Explicit)))
                Fail<object>("MCS2107", "Unmapped construction in selected source.", operation.Syntax.GetLocation());
            if (operation is IFieldReferenceOperation field && !DataType(field.Field.ContainingType) && !InheritedField(field.Field) && !TupleType(field.Field.ContainingType) && !field.Field.HasConstantValue && !(field.Instance is null && field.Field.Name == "Zero" && field.Type?.SpecialType is SpecialType.System_IntPtr or SpecialType.System_UIntPtr) && !(field.Instance is null && ComplexType(field.Type) && field.Field.Name is "Zero" or "One" or "ImaginaryOne" or "NaN" or "Infinity") && !(field.Instance is null && Floating(field.Type) is { } precision && FloatingConstant(field.Field.Name, precision) is not null))
                Fail<object>("MCS2107", "Unmapped field read in selected source.", field.Syntax.GetLocation());
            if (operation is ISizeOfOperation size && NumericSize(size.TypeOperand) is null) Fail<object>("MCS2107", "sizeof requires a delivered fixed unmanaged numeric domain.", size.Syntax.GetLocation());
            if (operation is IInvocationOperation invocation) ValidateInvocation(invocation);
            foreach (var child in operation.ChildOperations) ValidateBlockOperations(child);
        }
        private TypedValue Statements(IReadOnlyList<IOperation> statements, int index, BindingEnvironment environment, Func<BindingEnvironment, TypedValue> continuation)
        {
            if (index == statements.Count) return continuation(environment);
            TypedValue Next(BindingEnvironment state) => Statements(statements, index + 1, state, continuation);
            var statement = statements[index];
            switch (statement)
            {
                case IBlockOperation block:
                    return Statements(block.Operations, 0, Copy(environment), state =>
                    {
                        var outer = Copy(state); foreach (var local in block.Locals) outer.Remove(local);
                        return Next(outer);
                    });
                case IVariableDeclarationGroupOperation group:
                    var declarations = group.Declarations.SelectMany(d => d.Declarators).ToArray();
                    TypedValue Declare(int i, BindingEnvironment state)
                    {
                        if (i == declarations.Length) return Next(state);
                        var declarator = declarations[i];
                        return declarator.Initializer is null ? Declare(i + 1, state)
                            : Bind(declarator.Symbol, declarator.Symbol.Type, declarator.Initializer.Value, state, next => Declare(i + 1, next));
                    }
                    return Declare(0, environment);
                case IExpressionStatementOperation expression:
                    return Expression(expression.Operation, environment, (value, state) =>
                    {
                        if (expression.Operation is ISimpleAssignmentOperation or ICoalesceAssignmentOperation or IDeconstructionAssignmentOperation) return Next(state);
                        var name = Fresh("discardedValue"); var next = Next(state);
                        return new LetValue<Unit, IrType<Unit>>(next.Attributes, name, new([], value.Attributes, value), next);
                    });
                case IReturnOperation { ReturnedValue: null } when currentVoidReturn is not null:
                    return currentVoidReturn(environment);
                case IReturnOperation { ReturnedValue: not null } result:
                    return Expression(result.ReturnedValue, environment, (value, state) => currentDataReturn is { } dataReturn ? dataReturn(ReturnValue(value, result.Syntax.GetLocation(), state), state) : ReturnValue(value, result.Syntax.GetLocation(), state));
                case IConditionalOperation conditional:
                    return Decision(conditional.Condition, environment,
                        state => Statements([conditional.WhenTrue], 0, Copy(state), Next),
                        state => conditional.WhenFalse is null ? Next(Copy(state)) : Statements([conditional.WhenFalse], 0, Copy(state), Next));
                case ILocalFunctionOperation:
                    return Next(environment);
                case IEmptyOperation:
                    return Next(environment);
                default:
                    return Fail<TypedValue>("MCS2403", $"Statement {statement.Kind} is outside the D3 block contract.", statement.Syntax.GetLocation());
            }
        }
        private TypedValue Bind(ISymbol symbol, ITypeSymbol type, IOperation initializer, BindingEnvironment environment, Func<BindingEnvironment, TypedValue> continuation)
        {
            return Expression(initializer, environment, (value, state) => BindValue(symbol, type, value, state, continuation, initializer.Syntax.GetLocation()));
        }
    }
}
