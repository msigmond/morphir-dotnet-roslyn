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
        private static bool PositionalDataProperty(IPropertySymbol property) => property.ContainingType.IsRecord && property.DeclaringSyntaxReferences.Any(reference => reference.GetSyntax() is ParameterSyntax);
        private static bool RecordCopyShape(IMethodSymbol constructor) => constructor.ContainingType.IsRecord && constructor.ContainingType.IsReferenceType && constructor.Parameters.Length == 1 && SymbolEqualityComparer.Default.Equals(constructor.Parameters[0].Type, constructor.ContainingType);
        private static IMethodSymbol? ExplicitRecordCopy(INamedTypeSymbol type) => type.InstanceConstructors.SingleOrDefault(constructor => !constructor.IsImplicitlyDeclared && RecordCopyShape(constructor));
        private void EmitRecordCopyDependencies(IOperation root)
        {
            foreach (var update in Operations(root).OfType<IWithOperation>()) if (update.Type is INamedTypeSymbol owner && DataType(owner) && ExplicitRecordCopy(owner) is { } constructor) EmitDataConstructor(constructor);
        }
        private static bool RecordCopyConstructor(IMethodSymbol constructor) => constructor.ContainingType.IsRecord && constructor.Parameters.Length == 1 && SymbolEqualityComparer.Default.Equals(constructor.Parameters[0].Type, constructor.ContainingType) && constructor.IsImplicitlyDeclared;
        private TypedValue InitializePositionalRecord(IMethodSymbol constructor, DataOrigin origin, BindingEnvironment environment, Func<BindingEnvironment, TypedValue> continuation)
        {
            if (PrimaryDataConstructor(constructor) && constructor.ContainingType.IsRecord)
                foreach (var parameter in constructor.Parameters)
                    if (constructor.ContainingType.GetMembers(parameter.Name).OfType<IPropertySymbol>().FirstOrDefault(PositionalDataProperty) is { } property)
                    {
                        var previous = bindings; bindings = environment; TypedValue value;
                        try { value = ReadBinding(parameter, Location(parameter)!); } finally { bindings = previous; }
                        WriteDataField(origin, property, value, environment, Location(parameter)!);
                    }
            return continuation(environment);
        }
        private TypedValue CopyDataRecord(INamedTypeSymbol owner, TypedValue value, BindingEnvironment state, Location location, IObjectOrCollectionInitializerOperation? initializer, Func<TypedValue, BindingEnvironment, TypedValue> continuation)
        {
            var previous = bindings; bindings = state; TypedValue present; Dictionary<string, TypedValue> fields; var data = ResolvedDataLayout(owner);
            try
            {
                value = DataArgument(value, location); present = DemandPresent(value, location);
                fields = data.Slots.ToDictionary(slot => Name.From(slot.Name).Key, slot => KnownDataField(value, Name.From(slot.Name).Key) ?? (TypedValue)new FieldValue<Unit, IrType<Unit>>(Type(slot.Type, location), DataPayload(data, present), Name.From(slot.Name)));
            }
            finally { bindings = previous; }
            var origin = new DataOrigin(++nextDataCell); var payload = DataPayload(data, present); var copied = PackData(data, new UpdateRecordValue<Unit, IrType<Unit>>(payload.Attributes, payload, []), present.Attributes); state.Heap[origin.Cell] = new(owner, copied, fields, ++nextDataVersion, Layout: data);
            return RunDataObjectInitializer(initializer, origin, state, next => continuation(ReadDataOrigin(origin, next, location), next));
        }
        private bool RecordUpdateTarget(IOperation operation) => operation is IPropertyReferenceOperation property && records.ContainsKey(SymbolId(property.Property.ContainingType));
        private TypedValue? DataWithExpression(IOperation operation, BindingEnvironment environment, Func<TypedValue, BindingEnvironment, TypedValue> continuation)
        {
            if (operation is not IWithOperation update || update.Type is not INamedTypeSymbol owner || !(owner.IsRecord || owner.IsValueType && DataType(owner))) return null;
            var location = operation.Syntax.GetLocation();
            return Expression(update.Operand, environment, (value, state) =>
            {
                var previous = bindings; bindings = state; TypedValue present;
                try { present = DemandPresent(DataArgument(value, location), location); } finally { bindings = previous; }
                var entries = update.Initializer.Initializers.ToArray();
                if (!DataType(owner))
                {
                    TypedValue Initialize(int index, TypedValue current, BindingEnvironment next)
                    {
                        if (index == entries.Length) { provedDataOperations.Add(update); return continuation(current, next); }
                        if (entries[index] is not ISimpleAssignmentOperation { Target: IPropertyReferenceOperation property } assignment || !records.ContainsKey(SymbolId(property.Property.ContainingType))) return Fail<TypedValue>("MCS3001", "Record update requires resolved source slots.", location);
                        return Expression(assignment.Value, next, (replacement, after) => { provedDataOperations.Add(assignment); return Initialize(index + 1, new UpdateRecordValue<Unit, IrType<Unit>>(current.Attributes, current, [new(Name.From(property.Property.Name), Adapt(replacement, Type(property.Type!, location)))]), after); });
                    }
                    return Initialize(0, present, state);
                }
                if (ExplicitRecordCopy(owner) is { } constructor)
                {
                    var copyOrigin = new DataOrigin(++nextDataCell); state.Heap[copyOrigin.Cell] = NewDataCell(ResolvedDataLayout(owner), location); calls[currentMethod].Add((SymbolId(constructor), location));
                    return BindValue(constructor.Parameters[0], owner, Adapt(present, Type(owner, location)), state, next => RunDataConstructor(constructor, copyOrigin, next, after => RunDataObjectInitializer(update.Initializer, copyOrigin, after, final => { provedDataOperations.Add(update); return continuation(ReadDataOrigin(copyOrigin, final, location), final); })), location);
                }
                return CopyDataRecord(owner, value, state, location, update.Initializer, (copied, next) => { provedDataOperations.Add(update); return continuation(copied, next); });
            });
        }
    }
}
