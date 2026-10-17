using System.Numerics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Morphir.IR.Classic;
using IrPath = Morphir.IR.Classic.Path;
using TypedValue = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        private readonly HashSet<ITypeSymbol> paramsArrayTypes = new(SymbolEqualityComparer.Default);
        private Dictionary<string, NumericRange> paramsLengths = [];
        private readonly HashSet<IOperation> provedParamsReads = [];
        private bool ParamsArray(ITypeSymbol? type) => type is IArrayTypeSymbol { Rank: 1, IsSZArray: true } && paramsArrayTypes.Contains(type);
        private bool ParamsLength(IPropertyReferenceOperation property) => property.Property.Name == "Length" && property.Instance is not null && ParamsArray(property.Instance.Type);
        private bool ParamsPack(IArrayCreationOperation array) => ParamsArray(array.Type) && array.Initializer is not null && array.Parent is IArgumentOperation { Parameter.IsParams: true };
        private TypedValue ListSdk(string name, IrType<Unit>[] inputs, IrType<Unit> output, params TypedValue[] arguments)
        {
            IrType<Unit> signature = output; foreach (var type in inputs.Reverse()) signature = Types.Function(type, signature);
            TypedValue result = new ReferenceValue<Unit, IrType<Unit>>(signature, new(Types.Sdk, IrPath.From("List"), Name.From(name)));
            foreach (var argument in arguments) { signature = ((FunctionType<Unit>)signature).Result; result = new ApplyValue<Unit, IrType<Unit>>(signature, result, argument); }
            return result;
        }
        private NumericRange ParamsLengthRange(TypedValue value)
        {
            if (ValueKey(value) is { } key && paramsLengths.TryGetValue(key, out var range)) return range;
            if (value is VariableValue<Unit, IrType<Unit>> variable && boundValues.TryGetValue(variable.Name.Key, out var bound)) return ParamsLengthRange(bound);
            if (value is ApplyValue<Unit, IrType<Unit>> { Function: ConstructorValue<Unit, IrType<Unit>> { Name.Local.Key: "just" }, Argument: var argument }) return ParamsLengthRange(argument);
            if (value is ListValue<Unit, IrType<Unit>> list) return new(list.Elements.Count, list.Elements.Count, new HashSet<BigInteger>());
            return new(0, int.MaxValue, new HashSet<BigInteger>());
        }
        private TypedValue? LowerParams(IOperation operation)
        {
            var location = operation.Syntax.GetLocation();
            if (operation is IArrayCreationOperation creation)
            {
                if (!ParamsPack(creation)) return Fail<TypedValue>("MCS2807", "D7 array construction is limited to explicit/expanded params argument initializers; general arrays require D11.", location);
                var element = Type(((IArrayTypeSymbol)creation.Type!).ElementType, location);
                return Just(new ListValue<Unit, IrType<Unit>>(Types.List(element), creation.Initializer!.ElementValues.Select(value => Adapt(Lower(value), element)).ToArray()));
            }
            if (operation is IPropertyReferenceOperation property && ParamsLength(property))
            {
                var list = DemandPresent(Lower(property.Instance!), location); var result = ListSdk("length", [list.Attributes], Types.Int, list); numericValueRanges[result] = ParamsLengthRange(list); return result;
            }
            if (operation is IArrayElementReferenceOperation access && ParamsArray(access.ArrayReference.Type))
            {
                if (access.Indices.Length != 1) return Fail<TypedValue>("MCS2807", "Params vectors require one index.", location);
                var list = DemandPresent(Lower(access.ArrayReference), location); var index = Lower(access.Indices[0]); var indexDomain = Integral(access.Indices[0].Type) ?? Fail<IntegerDomain>("MCS2807", "Params indices require a delivered integral type.", location); var bounds = IntegerRange(index, indexDomain); var length = ParamsLengthRange(list);
                if (bounds.Minimum is null || bounds.Maximum is null || bounds.Minimum < 0 || length.Minimum is null || bounds.Maximum >= length.Minimum) return Fail<TypedValue>("MCS2808", "Params indexing requires a proved nonnegative index below the minimum length.", location);
                provedParamsReads.Add(access); if (indexDomain.Wide) index = NumericCall("toSmall", location, index);
                var element = Type(((IArrayTypeSymbol)access.ArrayReference.Type!).ElementType, location); var tail = ListSdk("drop", [Types.Int, list.Attributes], list.Attributes, index, list);
                var head = ListSdk("head", [list.Attributes], Types.Maybe(element), tail); var name = Fresh("paramsElement");
                return new MatchValue<Unit, IrType<Unit>>(element, head, [new(new ConstructorPattern<IrType<Unit>>(Types.Maybe(element), MaybeName("Just"), [new AsPattern<IrType<Unit>>(element, new WildcardPattern<IrType<Unit>>(element), name)]), new VariableValue<Unit, IrType<Unit>>(element, name))]);
            }
            return null;
        }
        private TypedValue? ParamsDecision(IOperation condition, Dictionary<ISymbol, Binding> environment, Func<Dictionary<ISymbol, Binding>, TypedValue> yes, Func<Dictionary<ISymbol, Binding>, TypedValue> no)
        {
            if (condition is not IBinaryOperation binary || HasAssignment(condition)) return null;
            IPropertyReferenceOperation? length = null; int bound; var kind = binary.OperatorKind;
            if (binary.LeftOperand is IPropertyReferenceOperation left && ParamsLength(left) && binary.RightOperand.ConstantValue is { HasValue: true, Value: int right }) { length = left; bound = right; }
            else if (binary.RightOperand is IPropertyReferenceOperation rightLength && ParamsLength(rightLength) && binary.LeftOperand.ConstantValue is { HasValue: true, Value: int leftBound })
            { length = rightLength; bound = leftBound; kind = kind switch { BinaryOperatorKind.LessThan => BinaryOperatorKind.GreaterThan, BinaryOperatorKind.LessThanOrEqual => BinaryOperatorKind.GreaterThanOrEqual, BinaryOperatorKind.GreaterThan => BinaryOperatorKind.LessThan, BinaryOperatorKind.GreaterThanOrEqual => BinaryOperatorKind.LessThanOrEqual, _ => kind }; }
            else return null;
            if (kind is not (BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals or BinaryOperatorKind.LessThan or BinaryOperatorKind.LessThanOrEqual or BinaryOperatorKind.GreaterThan or BinaryOperatorKind.GreaterThanOrEqual)) return null;
            return Expression(condition, environment, (test, state) =>
            {
                var array = DemandPresent(In(state, length.Instance!), length.Syntax.GetLocation()); if (ValueKey(array) is not { } key) return Choose(test, yes(Copy(state)), no(Copy(state)));
                var initial = ParamsLengthRange(array); var previous = paramsLengths;
                TypedValue Branch(bool truth, Func<Dictionary<ISymbol, Binding>, TypedValue> body)
                {
                    var operation = truth ? kind : kind switch { BinaryOperatorKind.Equals => BinaryOperatorKind.NotEquals, BinaryOperatorKind.NotEquals => BinaryOperatorKind.Equals, BinaryOperatorKind.LessThan => BinaryOperatorKind.GreaterThanOrEqual, BinaryOperatorKind.LessThanOrEqual => BinaryOperatorKind.GreaterThan, BinaryOperatorKind.GreaterThan => BinaryOperatorKind.LessThanOrEqual, _ => BinaryOperatorKind.LessThan };
                    var min = initial.Minimum!.Value; var max = initial.Maximum!.Value;
                    switch (operation) { case BinaryOperatorKind.Equals: min = BigInteger.Max(min, bound); max = BigInteger.Min(max, bound); break; case BinaryOperatorKind.NotEquals: if (bound == 0) min = BigInteger.Max(min, 1); break; case BinaryOperatorKind.LessThan: max = BigInteger.Min(max, (BigInteger)bound - 1); break; case BinaryOperatorKind.LessThanOrEqual: max = BigInteger.Min(max, bound); break; case BinaryOperatorKind.GreaterThan: min = BigInteger.Max(min, (BigInteger)bound + 1); break; case BinaryOperatorKind.GreaterThanOrEqual: min = BigInteger.Max(min, bound); break; }
                    paramsLengths = new(previous) { [key] = new(min, max, new HashSet<BigInteger>()) }; try { return body(Copy(state)); } finally { paramsLengths = previous; }
                }
                return Choose(test, Branch(true, yes), Branch(false, no));
            });
        }
    }
}
