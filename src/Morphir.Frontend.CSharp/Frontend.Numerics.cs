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
        private sealed record IntegerDomain(int Bits, bool Signed)
        {
            public bool Wide => Bits == 0 || Bits > 32;
            public BigInteger Minimum => Signed && Bits > 0 ? -(BigInteger.One << (Bits - 1)) : BigInteger.Zero;
            public BigInteger Maximum => (BigInteger.One << (Signed ? Bits - 1 : Bits)) - 1;
        }
        private static IntegerDomain? Integral(ITypeSymbol? type)
        {
            if (type is null) return null;
            var primitive = type.SpecialType switch
            {
                SpecialType.System_Char => new IntegerDomain(16, false),
                SpecialType.System_Byte => new IntegerDomain(8, false),
                SpecialType.System_SByte => new IntegerDomain(8, true),
                SpecialType.System_Int16 => new IntegerDomain(16, true),
                SpecialType.System_UInt16 => new IntegerDomain(16, false),
                SpecialType.System_Int32 => new IntegerDomain(32, true),
                SpecialType.System_UInt32 => new IntegerDomain(32, false),
                SpecialType.System_Int64 or SpecialType.System_IntPtr => new IntegerDomain(64, true),
                SpecialType.System_UInt64 or SpecialType.System_UIntPtr => new IntegerDomain(64, false),
                _ => null
            };
            if (primitive is not null) return primitive;
            if (type.Locations.Any(l => l.IsInSource) || type.ContainingAssembly?.Identity.Version != new Version(10, 0, 0, 0)) return null;
            return type.ToDisplayString() switch
            {
                "System.Int128" when type.ContainingAssembly.Name == "System.Runtime" => new(128, true),
                "System.UInt128" when type.ContainingAssembly.Name == "System.Runtime" => new(128, false),
                "System.Numerics.BigInteger" when type.ContainingAssembly.Name == "System.Runtime.Numerics" => new(0, true),
                _ => null
            };
        }
        private static int? NumericSize(ITypeSymbol type) => type.SpecialType == SpecialType.System_Boolean ? 1 : Integral(type) is { Bits: > 0 } integer ? integer.Bits / 8 : Floating(type) is { } precision ? precision == 11 ? 2 : precision == 24 ? 4 : 8 : ComplexType(type) ? 16 : null;
        private static bool NumericProperty(IPropertyReferenceOperation property)
        {
            if (Integral(property.Property.ContainingType) is { } integer)
                return property.Instance is null ? property.Property.Name is "Zero" or "One" or "MinusOne" or "NegativeOne" or "Size" or "MinValue" or "MaxValue" or "AllBitsSet" : property.Property.Name is "Sign" or "IsZero" or "IsOne" or "IsEven";
            if (Floating(property.Property.ContainingType) is { } precision) return property.Instance is null && FloatingConstant(property.Property.Name, precision) is not null;
            return ComplexType(property.Property.ContainingType) && property.Instance is not null && property.Property.Name is "Real" or "Imaginary";
        }
        private static bool NumericConstruction(IObjectCreationOperation creation)
        {
            if (creation.Initializer is not null) return false;
            if (creation.Arguments.Length == 0) return NumericType(creation.Type);
            if (Integral(creation.Type) is { Bits: 0 }) return creation.Arguments.Length == 1 && (DecimalType(creation.Arguments[0].Value.Type) || Integral(creation.Arguments[0].Value.Type) is not null || Floating(creation.Arguments[0].Value.Type) is not null);
            if (Integral(creation.Type) is { Bits: 128 }) return creation.Arguments.Length == 2 && creation.Arguments.All(a => a.Value.Type?.SpecialType == SpecialType.System_UInt64);
            return ComplexType(creation.Type) && creation.Arguments.Length == 2 && creation.Arguments.All(a => a.Value.Type?.SpecialType == SpecialType.System_Double);
        }
        private IReadOnlyList<Entry<IrPath, AccessControlled<ModuleDefinition<Unit, IrType<Unit>>>>>? numericLibrary;
        private void RequireNumericLibrary(Location? location)
        {
            if (numericLibrary is not null) return;
            var generated = NumericLibrary.Modules(package);
            foreach (var entry in generated)
            {
                if (modules.ContainsKey(entry.Key.Key) || identities.Values.Any(id => id.Module.Key == entry.Key.Key)) Fail<object>("MCS1004", "A source owner collides with the generated numeric compatibility module.", location);
                modules[entry.Key.Key] = (entry.Key, entry.Value.Access, entry.Value.Value.Types.ToList(), entry.Value.Value.Values.Select(v => new Entry<Name, AccessControlled<Documented<ValueDefinition<Unit, IrType<Unit>>>>>(v.Key, new(Access.Private, v.Value.Value))).ToList());
            }
            numericLibrary = generated;
        }
        private IrType<Unit> IntegerType(IntegerDomain domain, Location? location)
        {
            if (!domain.Wide) return Types.Int;
            RequireNumericLibrary(location);
            return new ReferenceType<Unit>(default, new(package, IrPath.From("Compat.Integers"), Name.From("Integer")), []);
        }
        private TypedValue NumericCall(string name, Location? location, params TypedValue[] arguments) => CompatCall("Compat.Integers", name, location, arguments);
        private TypedValue FloatCall(string name, Location? location, params TypedValue[] arguments) => CompatCall("Compat.Floating", name, location, arguments);
        private TypedValue CompatCall(string moduleName, string name, Location? location, params TypedValue[] arguments)
        {
            RequireNumericLibrary(location);
            var module = numericLibrary!.Single(m => m.Key.Key == IrPath.From(moduleName).Key);
            var definition = module.Value.Value.Values.Single(v => v.Key.Key == Name.From(name).Key).Value.Value.Value;
            IrType<Unit> signature = definition.Output;
            foreach (var input in definition.Inputs.Reverse()) signature = Types.Function(input.Type, signature);
            TypedValue value = new ReferenceValue<Unit, IrType<Unit>>(signature, new(package, module.Key, Name.From(name)));
            foreach (var argument in arguments)
            {
                signature = ((FunctionType<Unit>)signature).Result;
                value = new ApplyValue<Unit, IrType<Unit>>(signature, value, argument);
            }
            return value;
        }
        private TypedValue IntegerLiteral(BigInteger value, IntegerDomain domain, Location? location)
        {
            if (!domain.Wide) return Lit(Types.Int, new IntLiteral((long)value));
            var digits = new List<TypedValue>(); var magnitude = BigInteger.Abs(value);
            while (magnitude != 0) { digits.Add(Lit(Types.Int, new IntLiteral((long)(magnitude & 65535)))); magnitude >>= 16; }
            return new RecordValue<Unit, IrType<Unit>>(IntegerType(domain, location),
                [new(Name.From("digits"), new ListValue<Unit, IrType<Unit>>(Types.List(Types.Int), digits)), new(Name.From("negative"), Lit(Types.Bool, new BoolLiteral(value.Sign < 0)))]);
        }
        private static BigInteger? IntegerConstant(object? value) => value switch
        {
            sbyte i => i,
            byte i => i,
            short i => i,
            ushort i => i,
            int i => i,
            uint i => i,
            long i => i,
            ulong i => i,
            char i => i,
            Int128 i => BigInteger.Parse(i.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            UInt128 i => BigInteger.Parse(i.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            BigInteger i => i,
            _ => null
        };
        private TypedValue AsInteger(TypedValue value, IntegerDomain domain, Location? location)
        {
            var range = IntegerRange(value, domain);
            var result = domain.Wide ? value : NumericCall("fromSmall", location, value);
            numericValueRanges[result] = range;
            return result;
        }
        private TypedValue FromInteger(TypedValue value, IntegerDomain domain, Location? location)
        {
            if (domain.Bits != 0) value = NumericCall("wrap", location, Lit(Types.Int, new IntLiteral(domain.Bits)), Lit(Types.Bool, new BoolLiteral(domain.Signed)), value);
            return domain.Wide ? value : NumericCall("toSmall", location, value);
        }
        private TypedValue? LowerInteger(IOperation operation)
        {
            var location = operation.Syntax.GetLocation();
            if (operation is ISizeOfOperation size && NumericSize(size.TypeOperand) is { } bytes) return Lit(Types.Int, new IntLiteral(bytes));
            if (operation is IObjectCreationOperation decimalCreation && Integral(decimalCreation.Type) is { Bits: 0 } && decimalCreation.Arguments.Length == 1 && DecimalType(decimalCreation.Arguments[0].Value.Type))
                return ConvertDecimalValue(Lower(decimalCreation.Arguments[0].Value), decimalCreation.Arguments[0].Value.Type!, decimalCreation.Type!, location);
            if (operation is IObjectCreationOperation wideCreation && Integral(wideCreation.Type) is { Bits: 128 } wideDomain && wideCreation.Arguments.Length == 2)
            {
                var upper = wideCreation.Arguments.Single(a => a.Parameter!.Ordinal == 0).Value; var lower = wideCreation.Arguments.Single(a => a.Parameter!.Ordinal == 1).Value;
                return FromInteger(NumericCall("add", location, NumericCall("shiftLeft", location, AsInteger(Lower(upper), Integral(upper.Type)!, location), Lit(Types.Int, new IntLiteral(64))), AsInteger(Lower(lower), Integral(lower.Type)!, location)), wideDomain, location);
            }
            if (operation is IObjectCreationOperation creation && Integral(creation.Type) is { Bits: 0 } && creation.Arguments.Length == 1 && Integral(creation.Arguments[0].Value.Type) is { } input)
                return AsInteger(Lower(creation.Arguments[0].Value), input, location);
            if (operation is IObjectCreationOperation { Arguments.Length: 0 } empty && Integral(empty.Type) is { } emptyDomain) return IntegerLiteral(BigInteger.Zero, emptyDomain, location);
            if (operation is IPropertyReferenceOperation property && Integral(property.Property.ContainingType) is { } owner)
            {
                BigInteger? known = property.Instance is null ? property.Property.Name switch { "Zero" => BigInteger.Zero, "Size" when property.Property.ContainingType.SpecialType is SpecialType.System_IntPtr or SpecialType.System_UIntPtr => new BigInteger(8), "One" => BigInteger.One, "MinusOne" or "NegativeOne" => -BigInteger.One, "MinValue" when owner.Bits > 0 => owner.Minimum, "MaxValue" when owner.Bits > 0 => owner.Maximum, "AllBitsSet" when owner.Bits > 0 => owner.Signed ? -BigInteger.One : owner.Maximum, _ => null } : null;
                if (known is { } special) return IntegerLiteral(special, Integral(property.Type)!, location);
                if (property.Instance is not null && property.Property.Name is "Sign" or "IsZero" or "IsOne" or "IsEven")
                {
                    var instance = AsInteger(Lower(property.Instance), owner, location);
                    if (property.Property.Name == "Sign") return NumericCall("compare", location, instance, IntegerLiteral(BigInteger.Zero, new(0, true), location));
                    if (property.Property.Name == "IsEven") instance = NumericCall("bitAnd", location, instance, IntegerLiteral(BigInteger.One, new(0, true), location));
                    var compared = NumericCall("compare", location, instance, IntegerLiteral(property.Property.Name == "IsOne" ? BigInteger.One : BigInteger.Zero, new(0, true), location));
                    return SdkBinary("equal", Types.Int, Types.Bool, compared, Lit(Types.Int, new IntLiteral(0)));
                }
            }
            if (operation is ILiteralOperation literal && Integral(literal.Type) is { } literalDomain && IntegerConstant(literal.ConstantValue.Value) is { } constant)
                return IntegerLiteral(constant, literalDomain, location);
            if (operation is IFieldReferenceOperation nativeZero && nativeZero.Instance is null && nativeZero.Field.Name == "Zero" && nativeZero.Type?.SpecialType is SpecialType.System_IntPtr or SpecialType.System_UIntPtr) return IntegerLiteral(BigInteger.Zero, Integral(nativeZero.Type)!, location);
            if (operation is IFieldReferenceOperation field && Integral(field.Type) is { } fieldDomain && field.Field.HasConstantValue && IntegerConstant(field.Field.ConstantValue) is { } fieldConstant)
                return IntegerLiteral(fieldConstant, fieldDomain, location);
            if (operation is IConversionOperation conversion && Integral(conversion.Type) is { } target && Integral(conversion.Operand.Type) is { } source)
            {
                if (conversion.IsChecked) return Fail<TypedValue>("MCS2004", "Checked semantic numeric conversions are excluded.", location);
                if (conversion.ConstantValue.HasValue && IntegerConstant(conversion.ConstantValue.Value) is { } converted) return IntegerLiteral(converted, target, location);
                return ConvertNumericValue(Lower(conversion.Operand), conversion.Operand.Type!, conversion.Type!, location);

            }
            if (operation is IUnaryOperation unary && Integral(unary.Type) is { } unaryDomain && Integral(unary.Operand.Type) is { } operandDomain && !unary.IsLifted)
            {
                if (unary.IsChecked) return Fail<TypedValue>("MCS2004", "Checked semantic numeric arithmetic is excluded.", location);
                if (unary.ConstantValue.HasValue && IntegerConstant(unary.ConstantValue.Value) is { } unaryConstant) return IntegerLiteral(unaryConstant, unaryDomain, location);
                if (unary.OperatorKind == UnaryOperatorKind.Plus) return Lower(unary.Operand);
                var name = unary.OperatorKind switch { UnaryOperatorKind.Minus => "negate", UnaryOperatorKind.BitwiseNegation => "complement", _ => null };
                if (name is null) return null;
                return FromInteger(NumericCall(name, location, AsInteger(Lower(unary.Operand), operandDomain, location)), unaryDomain, location);
            }
            if (operation is IBinaryOperation binary && !binary.IsLifted && Integral(binary.LeftOperand.Type) is { } leftDomain && Integral(binary.RightOperand.Type) is { } rightDomain)
            {
                bool comparison = binary.OperatorKind is BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals or BinaryOperatorKind.LessThan or BinaryOperatorKind.LessThanOrEqual or BinaryOperatorKind.GreaterThan or BinaryOperatorKind.GreaterThanOrEqual;
                if (binary.IsChecked && !comparison) return Fail<TypedValue>("MCS2004", "Checked semantic numeric arithmetic is excluded.", location);
                if (leftDomain is { Bits: 32, Signed: true } && rightDomain == leftDomain && (comparison || binary.OperatorKind == BinaryOperatorKind.Add && SafeAddition(binary))) return null; // retain prior exact mappings
                return IntegerBinary(binary, Lower(binary.LeftOperand), Lower(binary.RightOperand), leftDomain, rightDomain);
            }
            return null;
        }
        private TypedValue? IntegerBinary(IBinaryOperation binary, TypedValue left, TypedValue right, IntegerDomain leftDomain, IntegerDomain rightDomain)
        {
            var location = binary.Syntax.GetLocation();
            bool comparison = binary.OperatorKind is BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals or BinaryOperatorKind.LessThan or BinaryOperatorKind.LessThanOrEqual or BinaryOperatorKind.GreaterThan or BinaryOperatorKind.GreaterThanOrEqual;
            if (binary.IsChecked && !comparison) return Fail<TypedValue>("MCS2004", "Checked semantic arithmetic is excluded.", location);
            if (leftDomain is { Bits: 32, Signed: true } && rightDomain == leftDomain && binary.OperatorKind == BinaryOperatorKind.Add)
            {
                var x = KnownInteger(left); var y = KnownInteger(right);
                if (x == 0 || y == 0 || x is { } knownLeft && y is { } knownRight && knownLeft + knownRight >= int.MinValue && knownLeft + knownRight <= int.MaxValue) return SdkBinary("add", Types.Int, Types.Int, left, right);
            }
            if (comparison && !leftDomain.Wide && !rightDomain.Wide)
            {
                var name = binary.OperatorKind switch { BinaryOperatorKind.Equals => "equal", BinaryOperatorKind.NotEquals => "notEqual", BinaryOperatorKind.LessThan => "lessThan", BinaryOperatorKind.LessThanOrEqual => "lessThanOrEqual", BinaryOperatorKind.GreaterThan => "greaterThan", _ => "greaterThanOrEqual" };
                return SdkBinary(name, Types.Int, Types.Bool, left, right);
            }
            var a = AsInteger(left, leftDomain, location); var b = AsInteger(right, rightDomain, location);
            if (comparison)
            {
                var compared = NumericCall("compare", location, a, b);
                var name = binary.OperatorKind switch { BinaryOperatorKind.Equals => "equal", BinaryOperatorKind.NotEquals => "notEqual", BinaryOperatorKind.LessThan => "lessThan", BinaryOperatorKind.LessThanOrEqual => "lessThanOrEqual", BinaryOperatorKind.GreaterThan => "greaterThan", _ => "greaterThanOrEqual" };
                return SdkBinary(name, Types.Int, Types.Bool, compared, Lit(Types.Int, new IntLiteral(0)));
            }
            if (Integral(NullableElement(binary.Type) ?? binary.Type) is not { } resultDomain) return null;
            if (binary.OperatorKind is BinaryOperatorKind.Divide or BinaryOperatorKind.Remainder) ProveDivision(binary, leftDomain, left, right);
            string? helper = binary.OperatorKind switch
            {
                BinaryOperatorKind.Add => "add",
                BinaryOperatorKind.Subtract => "subtract",
                BinaryOperatorKind.Multiply => "multiply",
                BinaryOperatorKind.Divide => "divide",
                BinaryOperatorKind.Remainder => "remainder",
                BinaryOperatorKind.And => "bitAnd",
                BinaryOperatorKind.Or => "bitOr",
                BinaryOperatorKind.ExclusiveOr => "bitXor",
                BinaryOperatorKind.LeftShift => "shiftLeft",
                BinaryOperatorKind.RightShift => "shiftRight",
                BinaryOperatorKind.UnsignedRightShift => "shiftUnsigned",
                _ => null
            };
            if (helper is null) return null;
            TypedValue result;
            if (binary.OperatorKind is BinaryOperatorKind.LeftShift or BinaryOperatorKind.RightShift or BinaryOperatorKind.UnsignedRightShift)
            {
                var count = resultDomain.Bits == 0 ? right : SdkBinary("modBy", Types.Int, Types.Int, Lit(Types.Int, new IntLiteral(resultDomain.Bits)), right);
                result = binary.OperatorKind == BinaryOperatorKind.UnsignedRightShift ? resultDomain.Bits == 0 ? NumericCall("shiftBigUnsigned", location, a, count) : NumericCall(helper, location, Lit(Types.Int, new IntLiteral(resultDomain.Bits)), a, count) : NumericCall(helper, location, a, count);
            }
            else result = NumericCall(helper, location, a, b);
            if (binary.OperatorKind == BinaryOperatorKind.Add) provedArithmetic.Add(binary);
            return FromInteger(result, resultDomain, location);
        }
        private void ProveDivision(IBinaryOperation binary, IntegerDomain domain, TypedValue left, TypedValue right)
        {
            var divisor = IntegerRange(right, Integral(NullableElement(binary.RightOperand.Type) ?? binary.RightOperand.Type)!);
            var dividend = IntegerRange(left, domain);
            if (!divisor.Excludes(BigInteger.Zero)) Fail<object>("MCS2601", "Integer division/remainder requires a proved nonzero divisor.", binary.Syntax.GetLocation());
            if (domain.Bits != 0 && domain.Signed && !divisor.Excludes(-BigInteger.One) && !dividend.Excludes(domain.Minimum))
                Fail<object>("MCS2602", "Signed division/remainder requires proof excluding MinValue / -1.", binary.Syntax.GetLocation());
        }
    }
}
