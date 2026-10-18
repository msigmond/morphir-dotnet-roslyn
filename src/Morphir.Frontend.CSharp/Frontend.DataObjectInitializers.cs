using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Morphir.IR.Classic;
using TypedValue = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        private DataOrigin? currentDataInitializerOrigin;
        private TypedValue RunDataObjectInitializer(IObjectOrCollectionInitializerOperation? initializer, DataOrigin origin, BindingEnvironment environment, Func<BindingEnvironment, TypedValue> continuation)
        {
            var previous = currentDataInitializerOrigin; currentDataInitializerOrigin = origin; var entries = initializer?.Initializers.ToArray() ?? [];
            TypedValue Finish(BindingEnvironment state)
            { currentDataInitializerOrigin = previous; try { return continuation(state); } finally { currentDataInitializerOrigin = origin; } }
            TypedValue Initialize(int index, BindingEnvironment state)
            {
                if (index == entries.Length) return Finish(state);
                if (entries[index] is ISimpleAssignmentOperation assignment && DataTarget(assignment.Target))
                    return Expression(assignment.Value, state, (value, next) => InitializeDataMember(assignment, origin, value, next, after => Initialize(index + 1, after)));
                if (entries[index] is IMemberInitializerOperation member)
                {
                    var location = member.Syntax.GetLocation();
                    if (TupleType(member.InitializedMember.Type) && member.InitializedMember.Type!.IsReferenceType)
                        return Expression(member.InitializedMember, state, (value, next) =>
                        {
                            var saved = bindings; bindings = next; TypedValue present; try { present = DemandPresent(value, location); } finally { bindings = saved; }
                            var tupleOrigin = new DataOrigin(++nextDataCell); next.Heap[tupleOrigin.Cell] = new((INamedTypeSymbol)member.InitializedMember.Type, present, null, ++nextDataVersion, false);
                            return RunDataObjectInitializer(member.Initializer, tupleOrigin, next, after => { provedDataOperations.Add(member); return Initialize(index + 1, after); });
                        });
                    if (member.InitializedMember.Type!.IsValueType)
                        return WithDataLocation(member.InitializedMember, state, (destination, next) =>
                        {
                            var nested = new DataOrigin(++nextDataCell); next.Heap[nested.Cell] = new((INamedTypeSymbol)member.InitializedMember.Type, destination.Read(next), null, ++nextDataVersion, Layout: DataType(member.InitializedMember.Type) ? ResolvedDataLayout((INamedTypeSymbol)member.InitializedMember.Type) : null);
                            return RunDataObjectInitializer(member.Initializer, nested, next, after => destination.Write(ReadDataOrigin(nested, after, location), after, final => { provedDataOperations.Add(member); return Initialize(index + 1, final); }));
                        });
                    return Expression(member.InitializedMember, state, (value, next) =>
                    {
                        var saved = bindings; bindings = next; try { DemandPresent(value, location); } finally { bindings = saved; }
                        if (DataOriginOf(value) is not { } nested || !next.Heap.TryGetValue(nested.Cell, out var cell) || !cell.Mutable) return Fail<TypedValue>("MCS3002", "Nested initialization retains input/external reachability; only owned members may change.", location);
                        return RunDataObjectInitializer(member.Initializer, nested, next, after => { provedDataOperations.Add(member); return Initialize(index + 1, after); });
                    });
                }
                return Fail<TypedValue>("MCS3001", "Object initialization requires a resolved data assignment/member initializer; collection additions retain D11.", entries[index].Syntax.GetLocation());
            }
            try { return Initialize(0, environment); } finally { currentDataInitializerOrigin = previous; }
        }
    }
}
