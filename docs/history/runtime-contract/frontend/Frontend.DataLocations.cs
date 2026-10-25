using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Morphir.IR.Classic;
using TypedValue = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        private sealed record DataLocation(Func<BindingEnvironment, TypedValue> Read, Func<TypedValue, BindingEnvironment, Func<BindingEnvironment, TypedValue>, TypedValue> Write);
        private TypedValue DataLocationMember(TypedValue subject, DataMember member, BindingEnvironment state, Location location)
        {
            var result = new FieldValue<Unit, IrType<Unit>>(Type(member.Type, location), DataPayload(dataTypes[SymbolId(member.Symbol.ContainingType)], subject), Name.From(member.Symbol.Name));
            var previous = bindings; bindings = state; try { if (KnownDataField(subject, Name.From(member.Symbol.Name).Key) is { } known) dataProjections[result] = known; } finally { bindings = previous; }
            if (DataOriginOf(subject) is { } origin && state.Heap.TryGetValue(origin.Cell, out var cell)) dataValueKeys[result] = "data:" + origin.Cell + ":" + cell.Version + "/" + Name.From(member.Symbol.Name).Key;
            else if (ValueKey(subject) is { } key) dataValueKeys[result] = key + "/" + Name.From(member.Symbol.Name).Key;
            return result;
        }
        private TypedValue WithDataLocation(IOperation target, BindingEnvironment environment, Func<DataLocation, BindingEnvironment, TypedValue> continuation)
        {
            var location = target.Syntax.GetLocation();
            if (target is ILocalReferenceOperation or IParameterReferenceOperation)
            {
                var symbol = target is ILocalReferenceOperation local ? (ISymbol)local.Local : ((IParameterReferenceOperation)target).Parameter;
                return continuation(new(state => { if (!state.TryGetValue(symbol, out var binding)) return Fail<TypedValue>("MCS2401", "Value location is not definitely assigned.", location); return new VariableValue<Unit, IrType<Unit>>(binding.Type, binding.Name); }, (value, state, next) => BindValue(symbol, target.Type!, value, state, next, location)), environment);
            }
            if (target is IInstanceReferenceOperation instance && (instance.ReferenceKind == InstanceReferenceKind.ImplicitReceiver ? currentDataInitializerOrigin : currentDataConstructionOrigin) is { } self)
                return continuation(new(state => DemandPresent(ReadDataOrigin(self, state, location), location), (value, state, next) => { ReplaceDataValue(self, value, state, location); return next(state); }), environment);
            if (DataMemberOf(target) is { } member)
            {
                if (member.Instance.Type!.IsValueType)
                    return WithDataLocation(member.Instance, environment, (parent, state) => continuation(new(next => DataLocationMember(parent.Read(next), member, next, location), (value, next, finish) => parent.Write(UpdateData(dataTypes[SymbolId(member.Symbol.ContainingType)], parent.Read(next), member.Symbol.Name, Adapt(value, Type(member.Type, location))), next, finish)), state));
                return Expression(member.Instance, environment, (receiver, state) =>
                {
                    var saved = bindings; bindings = state; try { DemandPresent(receiver, location); } finally { bindings = saved; }
                    var origin = DataOriginOf(receiver); if (origin is null || !state.Heap.TryGetValue(origin.Cell, out var cell) || !cell.Mutable) return Fail<TypedValue>("MCS3002", "Nested update requires owned receiver reachability; input/external data remains immutable.", location);
                    return continuation(new(next => DataLocationMember(DemandPresent(ReadDataOrigin(origin, next, location), location), member, next, location), (value, next, finish) => { WriteDataCell(origin, member, Adapt(value, Type(member.Type, location)), next, location); return finish(next); }), state);
                });
            }
            if (target is IFieldReferenceOperation { Instance: not null } tupleField && TupleType(tupleField.Field.ContainingType) && tupleField.Field.ContainingType.IsValueType && TupleIndex(tupleField.Field) is var index && index >= 0)
                return WithDataLocation(tupleField.Instance, environment, (parent, state) => continuation(new(next => TupleAt(parent.Read(next), index, location), (value, next, finish) =>
                {
                    var subject = parent.Read(next); var tuple = (TupleType<Unit>)subject.Attributes;
                    var changed = new TupleValue<Unit, IrType<Unit>>(tuple, tuple.Elements.Select((element, i) => i == index ? Adapt(value, element) : TupleAt(subject, i, location)).ToArray());
                    return parent.Write(changed, next, finish);
                }), state));
            return Fail<TypedValue>("MCS3002", "Update requires a resolved local, by-value parameter or owned member location.", location);
        }
        private bool MixedTupleTarget(IOperation operation) => operation is IFieldReferenceOperation { Instance: not null } field && TupleType(field.Field.ContainingType) && field.Field.ContainingType.IsValueType && !TupleLocalTarget(operation);
        private TypedValue? MixedTupleExpression(IOperation operation, BindingEnvironment environment, Func<TypedValue, BindingEnvironment, TypedValue> continuation)
        {
            if (operation is not ISimpleAssignmentOperation assignment || !MixedTupleTarget(assignment.Target)) return null;
            return WithDataLocation(assignment.Target, environment, (target, state) => Expression(assignment.Value, state, (value, next) => target.Write(value, next, after => { provedDataOperations.Add(operation); return continuation(value, after); })));
        }
    }
}
