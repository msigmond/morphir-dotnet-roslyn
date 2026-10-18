using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Morphir.IR.Classic;
using TypedValue = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        private readonly Dictionary<string, FQName> dataEqualityHelpers = [];
        private TypedValue RecursiveDataEquality(INamedTypeSymbol owner, TypedValue left, TypedValue right, Location location)
        {
            var id = SymbolId(owner) + "::" + SignatureType(owner); var raw = ReceiverIrType(owner.InstanceConstructors[0], location);
            if (!dataEqualityHelpers.TryGetValue(id, out var name))
            {
                var module = ModulePath(SourceOwnerName(owner)); var local = Name.From("finiteValueEquals"); var version = 0;
                while (identities.Values.Any(identity => identity.Module.Key == module.Key && identity.Local.Key == local.Key) || dataEqualityHelpers.Values.Any(identity => identity.Module.Key == module.Key && identity.Local.Key == local.Key)) local = Name.From("finiteValueEquals_" + (++version));
                name = new(package, module, local); dataEqualityHelpers[id] = name; var a = Fresh("equalLeft"); var b = Fresh("equalRight");
                var body = ClrDataEquality(new VariableValue<Unit, IrType<Unit>>(raw, a), new VariableValue<Unit, IrType<Unit>>(raw, b), owner, location, []);
                EnsureModule(module, Visibility(owner)); modules[module.Key].Values.Add(new(local, new(Access.Private, new("", new([new(a, raw, raw), new(b, raw, raw)], Types.Bool, body)))));
            }
            return new ApplyValue<Unit, IrType<Unit>>(Types.Bool, new ApplyValue<Unit, IrType<Unit>>(Types.Function(raw, Types.Bool), new ReferenceValue<Unit, IrType<Unit>>(Types.Function(raw, Types.Function(raw, Types.Bool)), name), left), right);
        }
        private void EmitDataEqualityDependencies(IOperation root)
        {
            var visited = new HashSet<string>();
            void Visit(ITypeSymbol type)
            {
                type = NullableElement(type) ?? type; if (type is not INamedTypeSymbol owner) return;
                if (TupleType(type)) { foreach (var element in TupleElements(type)) Visit(element); return; }
                if (!(DataType(type) || records.ContainsKey(SymbolId(type))) || !visited.Add(SymbolId(type))) return;
                foreach (var method in owner.GetMembers("Equals").OfType<IMethodSymbol>().Where(method => owner.IsRecord && !method.IsImplicitlyDeclared && method.Parameters.Length == 1 && SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, owner))) EmitMethod(method);
                foreach (var slot in DataType(type) ? ResolvedDataLayout(owner).Slots : records[SymbolId(type)].Select(parameter => new DataSlot(parameter, parameter.Type))) Visit(slot.Type);
            }
            foreach (var operation in Operations(root))
                if (operation is IBinaryOperation binary && RecordEqualityBinary(binary)) Visit(binary.OperatorMethod!.ContainingType);
                else if (operation is IInvocationOperation call && DataEqualityCall(call)) Visit(call.Instance!.Type!);
        }
        private bool RecordEqualityBinary(IBinaryOperation operation) => operation.OperatorMethod is { IsImplicitlyDeclared: true, ContainingType.IsRecord: true } method && (DataType(method.ContainingType) || records.ContainsKey(SymbolId(method.ContainingType))) && operation.OperatorKind is BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals;
        private bool DataEqualityCall(IInvocationOperation call) => call.Instance?.Type is INamedTypeSymbol owner && (DataType(owner) || records.ContainsKey(SymbolId(owner))) && call.TargetMethod.Name == "Equals" && call.Arguments.Length == 1 && (call.TargetMethod.IsImplicitlyDeclared && owner.IsRecord && SymbolEqualityComparer.Default.Equals(call.TargetMethod.Parameters[0].Type, owner) || owner.IsValueType && call.TargetMethod.ContainingType.SpecialType == SpecialType.System_ValueType);
        private bool DataEqualityBox(IOperation operation) => operation is IConversionOperation { Type.SpecialType: SpecialType.System_Object, Operand.Type.IsValueType: true, Conversion.IsUserDefined: false, Parent: IArgumentOperation { Parent: IInvocationOperation invocation } } && DataEqualityCall(invocation);
        private TypedValue DataFloatingEquals(TypedValue left, TypedValue right) => new IfValue<Unit, IrType<Unit>>(Types.Bool, SdkBinary("equal", Types.Float, Types.Bool, left, right), Lit(Types.Bool, new BoolLiteral(true)), new IfValue<Unit, IrType<Unit>>(Types.Bool, SdkUnary("isNaN", Types.Float, Types.Bool, left), SdkUnary("isNaN", Types.Float, Types.Bool, right), Lit(Types.Bool, new BoolLiteral(false))));
        private TypedValue ClrDataEquality(TypedValue left, TypedValue right, ITypeSymbol type, Location location, HashSet<string>? visiting = null)
        {
            if (MaybeElement(left.Attributes) is not null || MaybeElement(right.Attributes) is not null)
                return MatchMaybe(left, () => MatchMaybe(right, () => Lit(Types.Bool, new BoolLiteral(true)), _ => Lit(Types.Bool, new BoolLiteral(false))), a => MatchMaybe(right, () => Lit(Types.Bool, new BoolLiteral(false)), b => ClrDataEquality(a, b, NullableElement(type) ?? type, location, visiting)));
            if (Floating(type) is not null) return DataFloatingEquals(left, right);
            if (ComplexType(type)) return new IfValue<Unit, IrType<Unit>>(Types.Bool, DataFloatingEquals(Component(left, "real"), Component(right, "real")), DataFloatingEquals(Component(left, "imaginary"), Component(right, "imaginary")), Lit(Types.Bool, new BoolLiteral(false)));
            if (TupleType(type))
            {
                var elements = TupleElements(type); TypedValue result = Lit(Types.Bool, new BoolLiteral(true));
                for (var index = elements.Length - 1; index >= 0; index--) result = new IfValue<Unit, IrType<Unit>>(Types.Bool, ClrDataEquality(TupleAt(left, index, location), TupleAt(right, index, location), elements[index], location, visiting), result, Lit(Types.Bool, new BoolLiteral(false)));
                return result;
            }
            if (type is INamedTypeSymbol owner && (DataType(owner) || records.ContainsKey(SymbolId(owner))))
            {
                if (!owner.IsRecord && owner.GetMembers("Equals").OfType<IMethodSymbol>().Any(method => method.IsOverride)) return Fail<TypedValue>("MCS3001", "Object-based custom equality requires its resolved body mapping.", location);
                var custom = owner.GetMembers("Equals").OfType<IMethodSymbol>().FirstOrDefault(method => owner.IsRecord && !method.IsImplicitlyDeclared && !method.IsStatic && method.Parameters.Length == 1 && SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, owner));
                if (custom is not null)
                {
                    calls[currentMethod].Add((SymbolId(custom), location)); var output = CallableOutput(custom, location); var parameter = Type(custom.Parameters[0].Type, location); var receiver = ReceiverIrType(custom, location);
                    return new ApplyValue<Unit, IrType<Unit>>(output, new ApplyValue<Unit, IrType<Unit>>(Types.Function(parameter, output), new ReferenceValue<Unit, IrType<Unit>>(Types.Function(receiver, Types.Function(parameter, output)), Identity(custom)), left), Adapt(right, parameter));
                }
                if (owner.IsReferenceType && !owner.IsRecord) return Fail<TypedValue>("MCS3003", "Default reference-member equality observes identity; no structural replacement is permitted.", location);
                visiting ??= []; var id = SymbolId(owner); if (!visiting.Add(id)) return RecursiveDataEquality(owner, left, right, location);
                var fields = DataType(owner) ? ResolvedDataLayout(owner).Slots : records[id].Select(parameter => new DataSlot(parameter, parameter.Type)).ToArray();
                TypedValue result = Lit(Types.Bool, new BoolLiteral(true));
                var leftPayload = DataType(owner) ? DataPayload(ResolvedDataLayout(owner), left) : left; var rightPayload = DataType(owner) ? DataPayload(ResolvedDataLayout(owner), right) : right;
                foreach (var field in fields.Reverse())
                {
                    var fieldType = Type(field.Type, location); var name = Name.From(field.Name);
                    result = new IfValue<Unit, IrType<Unit>>(Types.Bool, ClrDataEquality(new FieldValue<Unit, IrType<Unit>>(fieldType, leftPayload, name), new FieldValue<Unit, IrType<Unit>>(fieldType, rightPayload, name), field.Type, location, new(visiting)), result, Lit(Types.Bool, new BoolLiteral(false)));
                }
                return result;
            }
            return TupleEqualElement(left, right, type, type, location);
        }
        private TypedValue? LowerDataEquality(IOperation operation)
        {
            var result = LowerDataEqualityCore(operation); if (result is not null) provedDataOperations.Add(operation); return result;
        }
        private TypedValue? LowerDataEqualityCore(IOperation operation)
        {
            if (DataEqualityBox(operation)) return Lower(((IConversionOperation)operation).Operand);
            if (operation is IBinaryOperation binary && RecordEqualityBinary(binary))
            {
                var owner = binary.OperatorMethod!.ContainingType; var location = operation.Syntax.GetLocation();
                if (owner.IsReferenceType && owner.GetMembers("Equals").OfType<IMethodSymbol>().Any(method => !method.IsImplicitlyDeclared && method.Parameters.Length == 1 && SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, owner))) return Fail<TypedValue>("MCS3003", "Synthesized reference-record operators with custom Equals need a proved identity-shortcut contract.", location);
                var left = DataArgument(Lower(binary.LeftOperand), location); var right = DataArgument(Lower(binary.RightOperand), location);
                var result = ClrDataEquality(left, right, owner, location);
                return binary.OperatorKind == BinaryOperatorKind.Equals ? result : SdkUnary("not", Types.Bool, Types.Bool, result);
            }
            if (operation is IInvocationOperation call && DataEqualityCall(call))
            {
                var location = operation.Syntax.GetLocation();
                var left = DemandPresent(DataArgument(Lower(call.Instance!), location), location); var right = DataArgument(Lower(call.Arguments[0].Value), location);
                if (call.TargetMethod.ContainingType.SpecialType == SpecialType.System_ValueType)
                {
                    var argument = call.Arguments[0].Value is IConversionOperation boxed && DataEqualityBox(boxed) ? boxed.Operand : call.Arguments[0].Value;
                    if (!SymbolEqualityComparer.Default.Equals(NullableElement(argument.Type) ?? argument.Type, call.Instance!.Type)) return Lit(Types.Bool, new BoolLiteral(false));
                }
                return ClrDataEquality(left, right, call.Instance!.Type!, location);
            }
            return null;
        }
    }
}
