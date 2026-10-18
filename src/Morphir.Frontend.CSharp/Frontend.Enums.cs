using System.Numerics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Morphir.IR.Classic;
using IrPath = Morphir.IR.Classic.Path;
using TypedValue = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        private static INamedTypeSymbol? EnumType(ITypeSymbol? type) => type is INamedTypeSymbol { TypeKind: TypeKind.Enum } e ? e : null;
        private static ITypeSymbol NumericUnderlying(ITypeSymbol type) => EnumType(type)?.EnumUnderlyingType ?? type;
        private bool SelectedEnum(ITypeSymbol? type) => EnumType(type) is { } e && identities.ContainsKey(SymbolId(e));
        private void DiscoverEnum(INamedTypeSymbol type)
        {
            foreach (var attribute in type.GetAttributes())
            {
                var owner = attribute.AttributeClass;
                if (owner?.ToDisplayString() != "System.FlagsAttribute" || owner.ContainingAssembly.Name != "System.Runtime" || owner.ContainingAssembly.Identity.Version != new Version(10, 0, 0, 0))
                    Fail<object>("MCS2901", "Only the exact baseline Flags attribute is mapped on enum declarations.", attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? Location(type));
            }
            foreach (var field in type.GetMembers().OfType<IFieldSymbol>().Where(f => !f.IsImplicitlyDeclared))
            {
                if (!field.HasConstantValue || field.GetAttributes().Length != 0) Fail<object>("MCS2901", "Enum members must be constants without unmapped attributes.", Location(field));
                AddSymbol(field, SourceOwnerName(type), field.Name);
            }
        }
        private IrType<Unit> EnumIrType(INamedTypeSymbol type, Location? location)
        {
            if (!SelectedEnum(type)) return Fail<IrType<Unit>>(type.Locations.Any(l => l.IsInSource) ? "MCS2201" : "MCS2902", "Enum data requires its explicitly selected source declaration; framework enum APIs remain separately cataloged.", location);
            return new ReferenceType<Unit>(default, Identity(type), []);
        }
        private void EmitEnum(INamedTypeSymbol type)
        {
            var id = Identity(type); var underlying = Type(type.EnumUnderlyingType!, Location(type)); var enumType = EnumIrType(type, Location(type));
            EnsureModule(id.Module, type.ContainingType is null ? Access.Public : Visibility(type.ContainingType));
            modules[id.Module.Key].Types.Add(new(id.Local, new(Visibility(type), new(Documentation(type), new CustomDefinition<Unit>([], new(Access.Public, [new(id.Local, [new(Name.From("arg1"), underlying)])]))))));
            EnsureModule(ModulePath(SourceOwnerName(type)), Visibility(type));
            foreach (var field in type.GetMembers().OfType<IFieldSymbol>().Where(f => !f.IsImplicitlyDeclared))
            {
                var member = Identity(field); var body = WrapEnum(type, IntegerLiteral(IntegerConstant(field.ConstantValue)!.Value, Integral(type.EnumUnderlyingType)!, Location(field)), Location(field));
                modules[member.Module.Key].Values.Add(new(member.Local, new(Visibility(field), new(Documentation(field), new([], enumType, body)))));
            }
        }
        private TypedValue WrapEnum(INamedTypeSymbol type, TypedValue number, Location? location)
        {
            var result = EnumIrType(type, location); return new ApplyValue<Unit, IrType<Unit>>(result, new ConstructorValue<Unit, IrType<Unit>>(Types.Function(number.Attributes, result), Identity(type)), number);
        }
        private TypedValue UnwrapEnum(INamedTypeSymbol type, TypedValue value, Location location)
        {
            var number = Type(type.EnumUnderlyingType!, location); var name = Fresh("enumValue");
            return new MatchValue<Unit, IrType<Unit>>(number, value, [new(new ConstructorPattern<IrType<Unit>>(value.Attributes, Identity(type), [new AsPattern<IrType<Unit>>(number, new WildcardPattern<IrType<Unit>>(number), name)]), new VariableValue<Unit, IrType<Unit>>(number, name))]);
        }
        private static bool EnumConversion(IConversionOperation conversion) => EnumType(NullableElement(conversion.Type) ?? conversion.Type) is not null || EnumType(NullableElement(conversion.Operand.Type) ?? conversion.Operand.Type) is not null;
        private TypedValue EnumConvert(TypedValue value, ITypeSymbol source, ITypeSymbol target, Location location)
        {
            if (SymbolEqualityComparer.Default.Equals(source, target)) return value;
            if (EnumType(source) is { } from) value = UnwrapEnum(from, value, location);
            var sourceNumber = NumericUnderlying(source); var targetNumber = NumericUnderlying(target);
            value = DecimalType(sourceNumber) || DecimalType(targetNumber) ? ConvertDecimalValue(value, sourceNumber, targetNumber, location) : ConvertNumericValue(value, sourceNumber, targetNumber, location);
            return EnumType(target) is { } to ? WrapEnum(to, value, location) : value;
        }
        private TypedValue? LowerEnum(IOperation operation)
        {
            var location = operation.Syntax.GetLocation();
            if (operation is IConversionOperation conversion && EnumConversion(conversion) && !Null(conversion.Operand))
            {
                if (conversion.IsChecked) return Fail<TypedValue>("MCS2004", "Checked enum conversions are excluded.", location);
                var source = NullableElement(conversion.Operand.Type) ?? conversion.Operand.Type!; var target = NullableElement(conversion.Type) ?? conversion.Type!;
                if (!SelectedEnum(source) && !SelectedEnum(target)) return null;
                if (conversion.ConstantValue.HasValue && EnumType(conversion.Type) is { } literalEnum && IntegerConstant(conversion.ConstantValue.Value) is { } constant) return WrapEnum(literalEnum, IntegerLiteral(constant, Integral(literalEnum.EnumUnderlyingType)!, location), location);
                var value = Lower(conversion.Operand);
                if (NullableElement(conversion.Type) is not null) return MatchMaybe(value, () => Nothing(Type(conversion.Type!, location)), present => Just(EnumConvert(present, source, target, location)));
                return EnumConvert(DemandPresent(value, location), source, target, location);
            }
            if (operation is IFieldReferenceOperation field && EnumType(field.Type) is { } owner && field.Field.HasConstantValue)
                return new ReferenceValue<Unit, IrType<Unit>>(EnumIrType(owner, location), Identity(field.Field));
            if (operation is IObjectCreationOperation { Arguments.Length: 0 } creation && EnumType(creation.Type) is { } empty) return Default(empty, location);
            if (operation is IBinaryOperation binary && !binary.IsLifted && (EnumType(binary.LeftOperand.Type) is not null || EnumType(binary.RightOperand.Type) is not null))
                return EnumBinary(binary, Lower(binary.LeftOperand), Lower(binary.RightOperand));
            if (operation is IUnaryOperation { IsLifted: false } unary && EnumType(unary.Operand.Type) is { } enumType)
                return EnumUnary(unary, Lower(unary.Operand), enumType);
            return null;
        }
        private TypedValue EnumBinary(IBinaryOperation binary, TypedValue left, TypedValue right)
        {
            var location = binary.Syntax.GetLocation(); var leftType = NullableElement(binary.LeftOperand.Type) ?? binary.LeftOperand.Type!; var rightType = NullableElement(binary.RightOperand.Type) ?? binary.RightOperand.Type!;
            if (binary.OperatorKind is BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals && EnumType(leftType) is not null && SymbolEqualityComparer.Default.Equals(leftType, rightType)) return SdkBinary(binary.OperatorKind == BinaryOperatorKind.Equals ? "equal" : "notEqual", left.Attributes, Types.Bool, left, right);
            if (EnumType(leftType) is { } a) left = UnwrapEnum(a, left, location);
            if (EnumType(rightType) is { } b) right = UnwrapEnum(b, right, location);
            var value = IntegerBinary(binary, left, right, Integral(NumericUnderlying(leftType))!, Integral(NumericUnderlying(rightType))!) ?? Fail<TypedValue>("MCS2903", "Unmapped enum operator.", location);
            return EnumType(NullableElement(binary.Type) ?? binary.Type) is { } result ? WrapEnum(result, value, location) : value;
        }
        private TypedValue EnumUnary(IUnaryOperation unary, TypedValue value, INamedTypeSymbol type)
        {
            var location = unary.Syntax.GetLocation(); if (unary.IsChecked) return Fail<TypedValue>("MCS2004", "Checked enum arithmetic is excluded.", location);
            if (unary.OperatorKind != UnaryOperatorKind.BitwiseNegation) return Fail<TypedValue>("MCS2903", "Unmapped enum unary operator.", location);
            var domain = Integral(type.EnumUnderlyingType)!; var number = UnwrapEnum(type, value, location);
            return WrapEnum(type, FromInteger(NumericCall("complement", location, AsInteger(number, domain, location)), domain, location), location);
        }
    }
}
