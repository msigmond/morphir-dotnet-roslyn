using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Morphir.IR.Classic;
using TypedValue = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        private bool PropertyCoalescingTarget(IOperation target) => target is IPropertyReferenceOperation { Instance: not null, Property.GetMethod: not null, Property.SetMethod: not null } property && DataGetter(property) && (DataSetter(property) || DataPropertyStorage(property.Property));
        private TypedValue? PropertyCoalescingExpression(IOperation operation, BindingEnvironment environment, Func<TypedValue, BindingEnvironment, TypedValue> continuation)
        {
            if (operation is not ICoalesceAssignmentOperation { Target: IPropertyReferenceOperation property } assignment || !PropertyCoalescingTarget(property)) return null;
            var location = operation.Syntax.GetLocation();
            TypedValue Evaluate(TypedValue receiver, DataLocation? structLocation, BindingEnvironment state)
            {
                var indices = new TypedValue[property.Arguments.Length]; var captured = new Dictionary<IOperation, TypedValue> { [property.Instance!] = receiver };
                TypedValue Arguments(int index, BindingEnvironment next)
                {
                    if (index < property.Arguments.Length)
                    {
                        var argument = property.Arguments[index];
                        return Expression(argument.Value, next, (value, after) => { indices[argument.Parameter!.Ordinal] = value; captured[argument.Value] = value; return Arguments(index + 1, after); });
                    }
                    var previous = operandValues; operandValues = new(previous); foreach (var entry in captured) operandValues[entry.Key] = entry.Value;
                    try
                    {
                        return Expression(property, next, (value, after) =>
                        {
                            var active = operandValues; operandValues = previous;
                            try
                            {
                                TypedValue Present(TypedValue present) { provedDataOperations.Add(operation); return continuation(Adapt(present, Type(operation.Type!, location)), Copy(after)); }
                                TypedValue Missing() => Expression(assignment.Value, Copy(after), (replacement, updated) =>
                                {
                                    var origin = property.Instance is IInstanceReferenceOperation ? currentDataConstructionOrigin : ClassFieldOrigin(receiver);
                                    if (structLocation is not null)
                                    {
                                        origin = new DataOrigin(++nextDataCell);
                                        updated.Heap[origin.Cell] = new(property.Property.ContainingType, structLocation.Read(updated), null, ++nextDataVersion, Layout: DispatchStorageLayout(property.Property.ContainingType));
                                    }
                                    if (origin is null || !updated.Heap.TryGetValue(origin.Cell, out var cell) || !cell.Mutable) return Fail<TypedValue>("MCS3002", "Property coalescing requires owned receiver reachability.", location);
                                    TypedValue Finish(BindingEnvironment final)
                                    {
                                        provedDataOperations.Add(operation);
                                        return structLocation is null ? continuation(Adapt(replacement, Type(operation.Type!, location)), final) : structLocation.Write(ReadDataOrigin(origin, final, location), final, last => continuation(Adapt(replacement, Type(operation.Type!, location)), last));
                                    }
                                    if (DataSetter(property)) return ApplyDataSetter(property, origin, indices, Adapt(replacement, Type(property.Property.Type, location)), updated, Finish);
                                    WriteDataField(origin, property.Property, Adapt(replacement, Type(property.Property.Type, location)), updated, location); return Finish(updated);
                                });
                                return value.Attributes is VariableType<Unit> ? GenericCoalesceSubject(value, subject => Choose(GenericIsAbsent(property.Type!, subject, location), Missing(), Present(value))) : MatchMaybe(value, Missing, Present);
                            }
                            finally { operandValues = active; }
                        });
                    }
                    finally { operandValues = previous; }
                }
                return Arguments(0, state);
            }
            return property.Instance!.Type!.IsValueType ? WithDataLocation(property.Instance, environment, (destination, state) => Evaluate(destination.Read(state), destination, state)) : Expression(property.Instance, environment, (receiver, state) => Evaluate(receiver, null, state));
        }
    }
}
