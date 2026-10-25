using System.Globalization;
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
        private readonly HashSet<IOperation> provedDecimalOperations = [];
        private static bool DecimalType(ITypeSymbol? type) => type?.SpecialType == SpecialType.System_Decimal;
        private IReadOnlyList<Entry<IrPath, AccessControlled<ModuleDefinition<Unit, IrType<Unit>>>>>? decimalLibrary;
        private void RequireDecimalLibrary(Location? location)
        {
            if (decimalLibrary is not null) return;
            RequireNumericLibrary(location);
            decimalLibrary = DecimalLibrary.Modules(package);
            var module = decimalLibrary.Single(m => m.Key.Key == IrPath.From("Compat.Decimal").Key);
            if (modules.ContainsKey(module.Key.Key) || identities.Values.Any(id => id.Module.Key == module.Key.Key)) Fail<object>("MCS1004", "A source owner collides with the Decimal compatibility module.", location);
            modules[module.Key.Key] = (module.Key, module.Value.Access, module.Value.Value.Types.ToList(), module.Value.Value.Values.Select(v => new Entry<Name, AccessControlled<Documented<ValueDefinition<Unit, IrType<Unit>>>>>(v.Key, new(Access.Private, v.Value.Value))).ToList());
        }
        private IrType<Unit> DecimalIrType(Location? location)
        {
            RequireDecimalLibrary(location); return new ReferenceType<Unit>(default, new(package, IrPath.From("Compat.Decimal"), Name.From("Decimal")), []);
        }
        private TypedValue DecimalSdk(string name, IrType<Unit>[] input, IrType<Unit> output, params TypedValue[] arguments)
        {
            IrType<Unit> signature = output; foreach (var type in input.Reverse()) signature = Types.Function(type, signature);
            TypedValue value = new ReferenceValue<Unit, IrType<Unit>>(signature, new(Types.Sdk, IrPath.From("Decimal"), Name.From(name)));
            foreach (var argument in arguments) { signature = ((FunctionType<Unit>)signature).Result; value = new ApplyValue<Unit, IrType<Unit>>(signature, value, argument); }
            return value;
        }
        private TypedValue ParsedDecimalConstant(string text, Location location)
        {
            // CLR constant formatting is a proved SDK grammar subset: signed digits, <=28 fractional digits.
            var maybe = Types.Maybe(Types.Decimal); var name = Fresh("parsedDecimal");
            var parsed = DecimalSdk("fromString", [Types.String], maybe, Lit(Types.String, new StringLiteral(text)));
            var binder = new VariableValue<Unit, IrType<Unit>>(Types.Decimal, name);
            return new MatchValue<Unit, IrType<Unit>>(Types.Decimal, parsed, [new(new ConstructorPattern<IrType<Unit>>(maybe, MaybeName("Just"), [new AsPattern<IrType<Unit>>(Types.Decimal, new WildcardPattern<IrType<Unit>>(Types.Decimal), name)]), binder)]);
        }
        private TypedValue DecimalEpsilon(Location location) => ParsedDecimalConstant("0.0000000000000000000000000001", location);
        private TypedValue DecimalConstant(decimal value, Location location)
        {
            var bits = decimal.GetBits(value); var coefficient = (BigInteger)(uint)bits[0] + ((BigInteger)(uint)bits[1] << 32) + ((BigInteger)(uint)bits[2] << 64);
            var literal = new RecordValue<Unit, IrType<Unit>>(DecimalIrType(location), [
                new(Name.From("coefficient"),IntegerLiteral(coefficient,new(0,true),location)),
                new(Name.From("negative"),Lit(Types.Bool,new BoolLiteral(bits[3]<0))),
                new(Name.From("scale"),Lit(Types.Int,new IntLiteral((bits[3]>>16)&255))),
                new(Name.From("value"),ParsedDecimalConstant(value.ToString(CultureInfo.InvariantCulture),location))]);
            decimalConstants[literal] = value; return literal;
        }
        private TypedValue DecimalCall(string name, Location location, params TypedValue[] arguments)
        {
            RequireDecimalLibrary(location); var module = decimalLibrary!.Single(m => m.Key.Key == IrPath.From("Compat.Decimal").Key);
            var definition = module.Value.Value.Values.Single(v => v.Key.Key == Name.From(name).Key).Value.Value.Value;
            IrType<Unit> signature = definition.Output; foreach (var input in definition.Inputs.Reverse()) signature = Types.Function(input.Type, signature);
            TypedValue value = new ReferenceValue<Unit, IrType<Unit>>(signature, new(package, module.Key, Name.From(name)));
            foreach (var argument in arguments) { signature = ((FunctionType<Unit>)signature).Result; value = new ApplyValue<Unit, IrType<Unit>>(signature, value, argument); }
            return value;
        }
        private TypedValue DecimalFromInteger(TypedValue value, IntegerDomain source, Location location)
        {
            var range = IntegerRange(value, source);
            var limit = (BigInteger.One << 96) - 1;
            if (range.Minimum is null || range.Maximum is null || range.Minimum < -limit || range.Maximum > limit) return Fail<TypedValue>("MCS2703", "Decimal conversion requires proved 96-bit coefficient bounds.", location);
            var result = DecimalCall("fromInteger", location, DecimalEpsilon(location), AsInteger(value, source, location));
            decimalDomains[result] = new(new(range.Minimum.Value, BigInteger.One), new(range.Maximum.Value, BigInteger.One)); return result;
        }
        private TypedValue DecimalBinary(BinaryOperatorKind kind, TypedValue left, TypedValue right, Location location)
        {
            var comparison = kind switch { BinaryOperatorKind.Equals => "equal", BinaryOperatorKind.NotEquals => "notEqual", BinaryOperatorKind.LessThan => "lessThan", BinaryOperatorKind.LessThanOrEqual => "lessThanOrEqual", BinaryOperatorKind.GreaterThan => "greaterThan", BinaryOperatorKind.GreaterThanOrEqual => "greaterThanOrEqual", _ => null };
            if (comparison is not null) return SdkBinary(comparison, Types.Int, Types.Bool, DecimalCall("compare", location, left, right), Lit(Types.Int, new IntLiteral(0)));
            var helper = kind switch { BinaryOperatorKind.Add => "add", BinaryOperatorKind.Subtract => "subtract", BinaryOperatorKind.Multiply => "multiply", BinaryOperatorKind.Divide => "divide", BinaryOperatorKind.Remainder => "remainder", _ => null };
            if (helper is null) return Fail<TypedValue>("MCS2705", "Unmapped Decimal operator.", location);
            var domain = ProveDecimalArithmetic(kind, left, right, location);
            var result = DecimalCall(helper, location, DecimalEpsilon(location), left, right); decimalDomains[result] = domain; return result;
        }
        private TypedValue? LowerDecimal(IOperation operation)
        {
            var location = operation.Syntax.GetLocation();
            provedDecimalOperations.Add(operation);
            if (operation is IConversionOperation nullableConversion && NullableDecimalConversion(nullableConversion) is { } nullableResult) return nullableResult;
            if (LowerDecimalMember(operation) is { } member) return member;
            if (operation is IConversionOperation outgoing && DecimalType(outgoing.Operand.Type) && !DecimalType(outgoing.Type)) return ConvertDecimalValue(Lower(outgoing.Operand), outgoing.Operand.Type!, outgoing.Type!, location, outgoing.IsChecked);
            if (operation is IConversionOperation conversion && DecimalType(conversion.Type))
            {
                if (Floating(conversion.Operand.Type) is not null) return Fail<TypedValue>("MCS2704", "Binary-floating Decimal construction is excluded by the exact-construction policy.", location);
                if (conversion.IsChecked) return Fail<TypedValue>("MCS2004", "Checked semantic numeric conversion is excluded.", location);
                if (conversion.ConstantValue.HasValue && conversion.ConstantValue.Value is decimal constant) return DecimalConstant(constant, location);
                if (DecimalType(conversion.Operand.Type)) return Lower(conversion.Operand);
                if (Integral(conversion.Operand.Type) is { } integer) return DecimalFromInteger(Lower(conversion.Operand), integer, location);
            }
            if (operation is ILiteralOperation literal && DecimalType(literal.Type) && literal.ConstantValue.Value is decimal value) return DecimalConstant(value, location);
            if (operation is IFieldReferenceOperation field && DecimalType(field.Type) && field.Field.HasConstantValue && field.Field.ConstantValue is decimal constantField) return DecimalConstant(constantField, location);
            if (operation is IDefaultValueOperation defaultValue && DecimalType(defaultValue.Type)) return DecimalConstant(0m, location);
            if (operation is IBinaryOperation binary && !binary.IsLifted && DecimalType(binary.LeftOperand.Type) && DecimalType(binary.RightOperand.Type))
            {
                if (binary.ConstantValue.HasValue && binary.ConstantValue.Value is decimal constantBinary) return DecimalConstant(constantBinary, location);
                return DecimalBinary(binary.OperatorKind, Lower(binary.LeftOperand), Lower(binary.RightOperand), location);
            }
            if (operation is IUnaryOperation unary && !unary.IsLifted && DecimalType(unary.Type))
            {
                if (unary.ConstantValue.HasValue && unary.ConstantValue.Value is decimal constantUnary) return DecimalConstant(constantUnary, location);
                var operand = Lower(unary.Operand); if (unary.OperatorKind == UnaryOperatorKind.Plus) return operand;
                if (unary.OperatorKind == UnaryOperatorKind.Minus) { var result = DecimalCall("negate", location, operand); var range = DecimalRange(operand); decimalDomains[result] = new(DecimalZero - range.Maximum, DecimalZero - range.Minimum, range.Nonzero); return result; }
            }
            return null;
        }
    }
}
