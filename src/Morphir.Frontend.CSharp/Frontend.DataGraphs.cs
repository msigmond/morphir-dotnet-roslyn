using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Morphir.IR.Classic;
using TypedValue = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        private readonly HashSet<string> customDataTypes = [];
        private readonly Dictionary<TypedValue, TypedValue> dataProjections = new(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<TypedValue, string> dataValueKeys = new(ReferenceEqualityComparer.Instance);
        private int nextDataVersion;
        private IEnumerable<INamedTypeSymbol> DataTypeChildren(ITypeSymbol type)
        {
            if (type is not INamedTypeSymbol named) yield break;
            if (DataType(named)) { yield return named; yield break; }
            foreach (var argument in named.TypeArguments) foreach (var child in DataTypeChildren(argument)) yield return child;
        }
        private void RegisterDataHeaders(IEnumerable<INamedTypeSymbol> selected)
        {
            var types = selected.ToArray();
            foreach (var type in types.Where(type => !type.IsStatic && type.TypeKind is TypeKind.Class or TypeKind.Struct && !LegacyRecordShape(type))) dataTypes[SymbolId(type)] = new(type, []);
            // Legacy scalar records keep their exact historical representation. A record
            // containing new data needs the general data layout and recursive-type checks.
            bool changed;
            do
            {
                changed = false;
                foreach (var type in types.Where(type => LegacyRecordShape(type) && !DataType(type)))
                    if (type.InstanceConstructors.Any(ctor => ctor.Parameters.Any(parameter => DataTypeChildren(parameter.Type).Any()))) { dataTypes[SymbolId(type)] = new(type, []); changed = true; }
            } while (changed);
        }
        private void FindRecursiveData()
        {
            foreach (var data in dataTypes.Values)
            {
                var start = SymbolId(data.Type); var visited = new HashSet<string>();
                bool Reach(string id)
                {
                    if (!visited.Add(id)) return false;
                    return dataTypes[id].Slots.SelectMany(slot => DataTypeChildren(slot.Type)).Select(SymbolId).Any(next => next == start || Reach(next));
                }
                if (Reach(start)) customDataTypes.Add(start);
            }
        }
        private RecordType<Unit> DataPayloadType(DataLayout data) => new(default, data.Slots.Select(slot => new Entry<Name, IrType<Unit>>(Name.From(slot.Name), Type(slot.Type, Location(slot.Symbol)))).OrderBy(field => field.Key.Key, StringComparer.Ordinal).ToArray());
        private TypedValue PackData(DataLayout data, TypedValue payload) => customDataTypes.Contains(SymbolId(data.Type)) ? new ApplyValue<Unit, IrType<Unit>>(DataIrType(data.Type, true), new ConstructorValue<Unit, IrType<Unit>>(Types.Function(payload.Attributes, DataIrType(data.Type, true)), Identity(data.Type)), payload) : payload;
        private TypedValue DataPayload(DataLayout data, TypedValue value)
        {
            if (!customDataTypes.Contains(SymbolId(data.Type))) return value;
            var record = DataPayloadType(data); var name = Fresh("dataFields");
            return new MatchValue<Unit, IrType<Unit>>(record, value, [new(new ConstructorPattern<IrType<Unit>>(value.Attributes, Identity(data.Type), [new AsPattern<IrType<Unit>>(record, new WildcardPattern<IrType<Unit>>(record), name)]), new VariableValue<Unit, IrType<Unit>>(record, name))]);
        }
        private TypedValue UpdateData(DataLayout data, TypedValue current, string name, TypedValue value)
        {
            var payload = DataPayload(data, current); return PackData(data, new UpdateRecordValue<Unit, IrType<Unit>>(payload.Attributes, payload, [new(Name.From(name), value)]));
        }
        private DataCell NewDataCell(DataLayout data, Location location)
        {
            var fields = data.Slots.ToDictionary(slot => Name.From(slot.Name).Key, slot => Default(slot.Type, location));
            return new(data.Type, DataRecord(data, location), fields, ++nextDataVersion);
        }
        private TypedValue? KnownDataField(TypedValue value, string name)
        {
            if (tupleProjections.TryGetValue(value, out var tupleProjection)) return KnownDataField(tupleProjection, name);
            if (dataProjections.TryGetValue(value, out var projection)) return KnownDataField(projection, name);
            if (DataOriginOf(value) is { Cell: > 0 } origin && bindings.Heap.TryGetValue(origin.Cell, out var cell) && cell.Fields is not null) return cell.Fields.GetValueOrDefault(name);
            return value switch
            {
                VariableValue<Unit, IrType<Unit>> variable when boundValues.TryGetValue(variable.Name.Key, out var bound) => KnownDataField(bound, name),
                RecordValue<Unit, IrType<Unit>> record => record.Fields.FirstOrDefault(field => field.Key.Key == name)?.Value,
                UpdateRecordValue<Unit, IrType<Unit>> update => update.Fields.FirstOrDefault(field => field.Key.Key == name)?.Value ?? KnownDataField(update.Subject, name),
                ApplyValue<Unit, IrType<Unit>> { Function: ConstructorValue<Unit, IrType<Unit>>, Argument: var argument } => KnownDataField(argument, name),
                _ => null
            };
        }
        private static bool DataSlotHasArray(ITypeSymbol type) => type is IArrayTypeSymbol || type is INamedTypeSymbol named && named.TypeArguments.Any(DataSlotHasArray);
        private DataLayout? DataLayoutForIr(IrType<Unit> type)
        {
            type = MaybeElement(type) ?? type;
            return type is ReferenceType<Unit> reference ? dataTypes.Values.FirstOrDefault(data => Identity(data.Type).Package.Key == reference.Name.Package.Key && Identity(data.Type).Module.Key == reference.Name.Module.Key && Identity(data.Type).Local.Key == reference.Name.Local.Key) : null;
        }
        private bool DataStateExpression(IOperation operation) => operation is IWithOperation || operation is IBinaryOperation { OperatorKind: BinaryOperatorKind.ConditionalAnd or BinaryOperatorKind.ConditionalOr } boolean && (boolean.OperatorMethod is not null || PresenceProbe(boolean)) || DataCallTarget(operation) is not null || operation is IObjectCreationOperation creation && DataType(creation.Type) || operation is IConditionalOperation { Type: { } conditionalType } && DataReferenceResult(conditionalType) || operation.ChildOperations.Any(DataStateExpression);
        private TypedValue DataArgument(TypedValue value, Location location) => ReferencedDataCells(value).Any() ? MaterializeDataValue(value, bindings, location, []) : value;
        private TypedValue MaterializeDataValue(TypedValue value, BindingEnvironment environment, Location location, HashSet<int> visiting)
        {
            if (DataOriginOf(value) is { Cell: > 0 } origin) return MaterializeDataOrigin(origin, environment, location, visiting);
            if (tupleProjections.TryGetValue(value, out var tupleProjected) && ReferencedDataCells(tupleProjected).Any()) return MaterializeDataValue(tupleProjected, environment, location, visiting);
            if (dataProjections.TryGetValue(value, out var projected) && ReferencedDataCells(projected).Any()) return MaterializeDataValue(projected, environment, location, visiting);
            if (value is ApplyValue<Unit, IrType<Unit>> { Function: ConstructorValue<Unit, IrType<Unit>> { Name.Local.Key: "just" }, Argument: var argument } && ReferencedDataCells(argument).Any()) return Just(MaterializeDataValue(argument, environment, location, visiting));
            if (DataLayoutForIr(value.Attributes) is { } data)
            {
                var current = value;
                foreach (var slot in data.Slots)
                    if (KnownDataField(value, Name.From(slot.Name).Key) is { } field && ReferencedDataCells(field).Any()) current = UpdateData(data, current, slot.Name, MaterializeDataValue(field, environment, location, visiting));
                return current;
            }
            if (value is TupleValue<Unit, IrType<Unit>> tuple) return new TupleValue<Unit, IrType<Unit>>(tuple.Attributes, tuple.Elements.Select(element => MaterializeDataValue(element, environment, location, visiting)).ToArray());
            if (value is VariableValue<Unit, IrType<Unit>> variable && boundValues.TryGetValue(variable.Name.Key, out var bound) && ReferencedDataCells(bound).Any()) return MaterializeDataValue(bound, environment, location, visiting);
            return value;
        }
        private TypedValue MaterializeDataOrigin(DataOrigin origin, BindingEnvironment environment, Location location, HashSet<int> visiting)
        {
            if (!environment.Heap.TryGetValue(origin.Cell, out var cell)) return Fail<TypedValue>("MCS3004", "The owned allocation is unavailable in this control-flow environment.", location);
            if (!visiting.Add(origin.Cell)) return Fail<TypedValue>("MCS3007", "Cyclic object graphs are excluded.", location);
            var current = cell.Current;
            if (TupleType(cell.Type)) current = MaterializeDataValue(current, environment, location, visiting);
            else if (cell.Fields is not null)
                foreach (var slot in dataTypes[SymbolId(cell.Type)].Slots)
                    if (cell.Fields.TryGetValue(Name.From(slot.Name).Key, out var value) && ReferencedDataCells(value).Any()) current = UpdateData(dataTypes[SymbolId(cell.Type)], current, slot.Name, MaterializeDataValue(value, environment, location, visiting));
            visiting.Remove(origin.Cell); var result = cell.Type.IsReferenceType ? Just(current) : current; if (cell.Type.IsReferenceType && !TupleType(cell.Type)) { dataOrigins[result] = origin; dataOrigins[current] = origin; }
            return result;
        }
        private IEnumerable<int> ReferencedDataCells(TypedValue value, HashSet<TypedValue>? seen = null)
        {
            seen ??= new(ReferenceEqualityComparer.Instance); if (!seen.Add(value)) yield break;
            if (DataOriginOf(value) is { Cell: > 0 } origin) { yield return origin.Cell; yield break; }
            if (tupleProjections.TryGetValue(value, out var tupleProjection)) { foreach (var cell in ReferencedDataCells(tupleProjection, seen)) yield return cell; yield break; }
            if (dataProjections.TryGetValue(value, out var projection)) { foreach (var cell in ReferencedDataCells(projection, seen)) yield return cell; yield break; }
            if (value is VariableValue<Unit, IrType<Unit>> variable && boundValues.TryGetValue(variable.Name.Key, out var bound)) { foreach (var cell in ReferencedDataCells(bound, seen)) yield return cell; yield break; }
            if (DataLayoutForIr(value.Attributes) is { } layout)
            {
                foreach (var slot in layout.Slots) if (KnownDataField(value, Name.From(slot.Name).Key) is { } field) foreach (var cell in ReferencedDataCells(field, seen)) yield return cell;
                yield break;
            }
            IEnumerable<TypedValue> children = value switch
            {
                RecordValue<Unit, IrType<Unit>> record => record.Fields.Select(field => field.Value),
                UpdateRecordValue<Unit, IrType<Unit>> update => update.Fields.Select(field => field.Value).Append(update.Subject),
                TupleValue<Unit, IrType<Unit>> tuple => tuple.Elements,
                ListValue<Unit, IrType<Unit>> list => list.Elements,
                ApplyValue<Unit, IrType<Unit>> apply => [apply.Function, apply.Argument],
                LetValue<Unit, IrType<Unit>> let => [let.Definition.Body, let.Body],
                IfValue<Unit, IrType<Unit>> conditional => [conditional.Then, conditional.Else],
                LambdaValue<Unit, IrType<Unit>> lambda => [lambda.Body],
                _ => []
            };
            foreach (var child in children) foreach (var cell in ReferencedDataCells(child, seen)) yield return cell;
        }
        private void WriteDataCell(DataOrigin origin, DataMember member, TypedValue value, BindingEnvironment environment, Location location) => WriteDataField(origin, member.Symbol, value, environment, location);
        private void WriteDataField(DataOrigin origin, ISymbol member, TypedValue value, BindingEnvironment environment, Location location)
        {
            if (!environment.Heap.TryGetValue(origin.Cell, out var cell) || !cell.Mutable) Fail<object>("MCS3002", "Mutation requires exclusive local ownership and complete reference reachability; borrowed/escaped data is immutable.", location);
            var fields = new Dictionary<string, TypedValue>(cell!.Fields ?? new Dictionary<string, TypedValue>()) { [Name.From(member.Name).Key] = value };
            environment.Heap[origin.Cell] = cell with { Current = UpdateData(dataTypes[SymbolId(cell.Type)], cell.Current, member.Name, value), Fields = fields, Version = ++nextDataVersion };
            CheckDataCycle(origin, environment, location);
        }
        private void ReplaceDataValue(DataOrigin origin, TypedValue value, BindingEnvironment environment, Location location)
        {
            if (!environment.Heap.TryGetValue(origin.Cell, out var cell) || !cell.Mutable) Fail<object>("MCS3002", "Whole-value updates require an owned construction/receiver location.", location);
            if (TupleType(cell!.Type)) { environment.Heap[origin.Cell] = cell with { Current = value, Fields = null, Version = ++nextDataVersion }; CheckDataCycle(origin, environment, location); return; }
            var previous = bindings; bindings = environment; Dictionary<string, TypedValue> fields;
            try { fields = dataTypes[SymbolId(cell!.Type)].Slots.ToDictionary(slot => Name.From(slot.Name).Key, slot => KnownDataField(value, Name.From(slot.Name).Key) ?? (TypedValue)new FieldValue<Unit, IrType<Unit>>(Type(slot.Type, location), DataPayload(dataTypes[SymbolId(cell.Type)], value), Name.From(slot.Name))); }
            finally { bindings = previous; }
            environment.Heap[origin.Cell] = cell! with { Current = value, Fields = fields, Version = ++nextDataVersion }; CheckDataCycle(origin, environment, location);
        }
        private void CheckDataCycle(DataOrigin origin, BindingEnvironment environment, Location location)
        {
            bool Cycle(int id, HashSet<int> visiting, HashSet<int> finished)
            {
                if (!visiting.Add(id)) return true; if (finished.Contains(id)) { visiting.Remove(id); return false; }
                if (environment.Heap.TryGetValue(id, out var node))
                    foreach (var child in (node.Fields?.Values ?? (TupleType(node.Type) ? new[] { node.Current } : Enumerable.Empty<TypedValue>())).SelectMany(field => ReferencedDataCells(field)).Distinct()) if (Cycle(child, visiting, finished)) return true;
                visiting.Remove(id); finished.Add(id); return false;
            }
            var previous = bindings; bindings = environment; try { if (Cycle(origin.Cell, [], [])) Fail<object>("MCS3007", "Cyclic object graphs are outside the finite acyclic data contract.", location); } finally { bindings = previous; }
        }
        private bool DataReferenceResult(ITypeSymbol type, HashSet<string>? seen = null)
        {
            if (type is not INamedTypeSymbol named) return false;
            if (DataType(named))
            {
                if (named.IsReferenceType) return true; seen ??= []; if (!seen.Add(SymbolId(named))) return false;
                return dataTypes[SymbolId(named)].Slots.Any(slot => DataReferenceResult(slot.Type, seen));
            }
            return named.TypeArguments.Any(argument => DataReferenceResult(argument, seen));
        }
        private void BorrowUnknownDataResult(ITypeSymbol result, IEnumerable<TypedValue> arguments)
        {
            if (!DataReferenceResult(result)) return;
            var visited = new HashSet<int>();
            void Borrow(int id)
            {
                if (!visited.Add(id) || !bindings.Heap.TryGetValue(id, out var cell)) return;
                bindings.Heap[id] = cell with { Mutable = false };
                if (cell.Fields is not null) foreach (var child in cell.Fields.Values.SelectMany(value => ReferencedDataCells(value))) Borrow(child);
            }
            foreach (var id in arguments.SelectMany(value => ReferencedDataCells(value))) Borrow(id);
        }
    }
}
