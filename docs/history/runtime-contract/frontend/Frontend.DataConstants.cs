using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Morphir.IR.Classic;
using TypedValue = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        private readonly Dictionary<string, IFieldSymbol> dataConstants = [];
        private readonly Dictionary<string, TypedValue> dataConstantBodies = [];
        private bool SourceAssembly(IAssemblySymbol assembly) => compilations.Any(compilation => compilation.Assembly.Identity.Equals(assembly.Identity));
        private void RegisterDataConstants(IEnumerable<INamedTypeSymbol> selected)
        {
            foreach (var type in selected.Where(type => type.TypeKind is TypeKind.Class or TypeKind.Struct))
                foreach (var field in type.GetMembers().OfType<IFieldSymbol>().Where(field => !field.IsImplicitlyDeclared && field.IsConst))
                { AddSymbol(field, type.ToDisplayString(), field.Name); dataConstants[SymbolId(field)] = field; }
        }
        private void ValidateSourceDataMembers(IOperation root)
        {
            foreach (var member in Operations(root).Select(operation => operation switch { IFieldReferenceOperation field => (Symbol: (ISymbol)field.Field, Operation: operation), IPropertyReferenceOperation property => (Symbol: (ISymbol)property.Property, Operation: operation), _ => (Symbol: (ISymbol?)null, Operation: operation) }))
                if (member.Symbol is { } symbol && SourceAssembly(symbol.ContainingAssembly) && !identities.ContainsKey(SymbolId(symbol.ContainingType))) Fail<object>("MCS2201", $"Required source symbol {symbol} in namespace {symbol.ContainingNamespace} is not selected; add that namespace explicitly.", member.Operation.Syntax.GetLocation());
        }
        private void EmitDataConstant(IFieldSymbol field)
        {
            var key = SymbolId(field); if (dataConstantBodies.ContainsKey(key)) return;
            if (!emittingMethods.Add(key)) Fail<object>("MCS2203", "Cyclic constant dependencies are excluded.", Location(field));
            try
            {
                if (field.GetAttributes().Length != 0) Fail<object>("MCS3001", "Constant metadata attributes need an explicit mapping.", Location(field));
                var syntax = DataInitializerSyntax(field); var root = syntax is null ? null : Semantic(syntax.SyntaxTree).GetOperation(syntax);
                if (root is not null)
                {
                    ValidateSourceDataMembers(root);
                    foreach (var reference in Operations(root).OfType<IFieldReferenceOperation>()) if (dataConstants.TryGetValue(SymbolId(reference.Field), out var dependency)) EmitDataConstant(dependency);
                    foreach (var operation in Operations(root))
                        if (operation is not (IFieldInitializerOperation or ILiteralOperation or IFieldReferenceOperation or IConversionOperation or IBinaryOperation or IUnaryOperation or IDefaultValueOperation or ISizeOfOperation or IConditionalOperation)) Fail<object>("MCS2403", $"Constant initializer operation {operation.Kind} retains its language delivery gate.", operation.Syntax.GetLocation());
                }
                ResetValueState(); currentMethod = key; calls.TryAdd(key, []); var location = Location(field)!;
                // Type-check the declared domain even for a compiler-folded constant.
                Type(field.Type, location); TypedValue value;
                if (field.ConstantValue is null) value = Nothing(Type(field.Type, location));
                else if (EnumType(field.Type) is { } enumeration) value = WrapEnum(enumeration, IntegerLiteral(IntegerConstant(field.ConstantValue)!.Value, Integral(enumeration.EnumUnderlyingType)!, location), location);
                else if (Integral(field.Type) is { } integer) value = IntegerLiteral(IntegerConstant(field.ConstantValue)!.Value, integer, location);
                else if (DecimalType(field.Type)) value = DecimalConstant((decimal)field.ConstantValue, location);
                else if (Floating(field.Type) is not null) value = FloatLiteral(System.Convert.ToDouble(field.ConstantValue, System.Globalization.CultureInfo.InvariantCulture), location);
                else value = field.ConstantValue switch { bool boolean => Lit(Types.Bool, new BoolLiteral(boolean)), string text => Lit(Types.String, new StringLiteral(text)), _ => Fail<TypedValue>("MCS3001", "Constant value has no exact delivered representation.", location) };
                var id = Identity(field); EnsureModule(id.Module, Visibility(field.ContainingType)); modules[id.Module.Key].Values.Add(new(id.Local, new(Visibility(field), new(Documentation(field), new([], value.Attributes, value))))); dataConstantBodies[key] = value;
            }
            finally { emittingMethods.Remove(key); }
        }
        private TypedValue? LowerDataConstant(IOperation operation)
        {
            if (operation is not IFieldReferenceOperation field || !dataConstantBodies.TryGetValue(SymbolId(field.Field), out var body)) return null;
            var result = new ReferenceValue<Unit, IrType<Unit>>(body.Attributes, Identity(field.Field)); dataProjections[result] = body; dataValueKeys[result] = "constant:" + SymbolId(field.Field);
            if (Integral(field.Type) is { } integer && IntegerConstant(field.Field.ConstantValue) is { } value) numericValueRanges[result] = new(value, value, new HashSet<System.Numerics.BigInteger>());
            if (DecimalType(field.Type) && field.Field.ConstantValue is decimal exact) { decimalConstants[result] = exact; decimalDomains[result] = DecimalRange(result); }
            return result;
        }
    }
}
