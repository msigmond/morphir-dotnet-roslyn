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
        private readonly HashSet<string> customDataTypes = [];
        private readonly Dictionary<TypedValue, TypedValue> dataProjections = new(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<TypedValue, string> dataValueKeys = new(ReferenceEqualityComparer.Instance);
        private int nextDataVersion;
        private static readonly HashSet<string> referenceFreeSdkTypes = new[] { Types.Int, Types.Float, Types.Bool, Types.String, Types.Decimal }.OfType<ReferenceType<Unit>>().Select(type => FunctionOwner(type.Name)).ToHashSet();
        private bool ReferenceFreeDataType(IrType<Unit> type)
        {
            if (type is UnitType<Unit>) return true;
            if (type is TupleType<Unit> tuple) return tuple.Elements.All(ReferenceFreeDataType);
            if (type is RecordType<Unit> record) return record.Fields.All(field => ReferenceFreeDataType(field.Value));
            if (type is not ReferenceType<Unit> reference) return false;
            if (reference.Arguments.Count == 0 && referenceFreeSdkTypes.Contains(FunctionOwner(reference.Name))) return true;
            if (reference.Name.Package.Key == Types.Sdk.Key && (reference.Name.Module.Key == IrPath.From("Maybe").Key && reference.Name.Local.Key == Name.From("Maybe").Key || reference.Name.Module.Key == IrPath.From("List").Key && reference.Name.Local.Key == Name.From("List").Key)) return reference.Arguments.All(ReferenceFreeDataType);
            if (reference.Name.Package.Key == package.Key && (numericLibrary is not null && (reference.Name.Module.Key == IrPath.From("Compat.Integers").Key && reference.Name.Local.Key == Name.From("Integer").Key || reference.Name.Module.Key == IrPath.From("Compat.Floating").Key && reference.Name.Local.Key == Name.From("Complex").Key) || decimalLibrary is not null && reference.Name.Module.Key == IrPath.From("Compat.Decimal").Key && reference.Name.Local.Key == Name.From("Decimal").Key)) return true;
            if (DataLayoutForIr(type) is { } data) return !data.Type.IsReferenceType && ReferenceFreeDataType(AppliedDataPayloadType(data, type));
            var source = symbols.Keys.OfType<INamedTypeSymbol>().FirstOrDefault(symbol => FunctionOwner(Identity(symbol)) == FunctionOwner(reference.Name));
            if (source?.TypeKind == TypeKind.Enum) return true;
            if (source is null || !records.TryGetValue(SymbolId(source), out var fields)) return false;
            var previous = activeGenericTypes; activeGenericTypes = new(SymbolEqualityComparer.Default);
            try
            {
                var substitutions = SourceTypeParameters(source).Zip(reference.Arguments).ToDictionary(pair => TypeParameterName(pair.First).Key, pair => pair.Second);
                return fields.All(field => ReferenceFreeDataType(SubstituteIrTypes(Type(field.Type, Location(field)), substitutions)));
            }
            finally { activeGenericTypes = previous; }
        }
        private IEnumerable<INamedTypeSymbol> DataTypeChildren(ITypeSymbol type)
        {
            if (type is not INamedTypeSymbol named) yield break;
            if (ValueInterfaceType(named)) yield break;
            if (DataType(named)) { yield return named; yield break; }
            foreach (var argument in named.TypeArguments) foreach (var child in DataTypeChildren(argument)) yield return child;
        }
        private void RegisterDataHeaders(IEnumerable<INamedTypeSymbol> selected)
        {
            var types = selected.ToArray();
            foreach (var type in types.Where(type => !type.IsStatic && (!ValueInterfaceType(type) || ConcreteClassDomain(type)) && type.TypeKind is TypeKind.Class or TypeKind.Struct && !LegacyRecordShape(type))) dataTypes[SymbolId(type)] = new(type, []);
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
            foreach (var data in dataTypes.Values.Where(data => !ConcreteClassDomain(data.Type)))
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
        private RecordType<Unit> DataPayloadType(DataLayout data) => new(default, data.Slots.Select(slot => new Entry<Name, IrType<Unit>>(Name.From(slot.Name), slot.Representation ?? Type(slot.Type, Location(slot.Symbol)))).OrderBy(field => field.Key.Key, StringComparer.Ordinal).ToArray());
        private TypedValue PackData(DataLayout data, TypedValue payload, IrType<Unit>? resultType = null) => customDataTypes.Contains(SymbolId(data.Type)) ? new ApplyValue<Unit, IrType<Unit>>(resultType ?? LayoutIrType(data, true), new ConstructorValue<Unit, IrType<Unit>>(Types.Function(payload.Attributes, resultType ?? LayoutIrType(data, true)), Identity(data.Type)), payload) : payload;
        private TypedValue DataPayload(DataLayout data, TypedValue value)
        {
            if (!customDataTypes.Contains(SymbolId(data.Type))) return value;
            var record = AppliedDataPayloadType(data, value.Attributes); var name = Fresh("dataFields");
            return new MatchValue<Unit, IrType<Unit>>(record, value, [new(new ConstructorPattern<IrType<Unit>>(value.Attributes, Identity(data.Type), [new AsPattern<IrType<Unit>>(record, new WildcardPattern<IrType<Unit>>(record), name)]), new VariableValue<Unit, IrType<Unit>>(record, name))]);
        }
        private TypedValue UpdateData(DataLayout data, TypedValue current, string name, TypedValue value)
        {
            var payload = DataPayload(data, current); return PackData(data, new UpdateRecordValue<Unit, IrType<Unit>>(payload.Attributes, payload, [new(Name.From(name), value)]), current.Attributes);
        }
        private DataCell NewDataCell(DataLayout data, Location location)
        {
            var fields = data.Slots.ToDictionary(slot => Name.From(slot.Name).Key, slot => SlotDefault(data, slot, location));
            return new(data.Type, DataRecord(data, location), fields, ++nextDataVersion, Layout: data);
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
            return type is ReferenceType<Unit> reference ? dataTypes.Values.FirstOrDefault(data => !ValueInterfaceType(data.Type) && Identity(data.Type).Package.Key == reference.Name.Package.Key && Identity(data.Type).Module.Key == reference.Name.Module.Key && Identity(data.Type).Local.Key == reference.Name.Local.Key) : null;
        }
        private bool DataStateExpression(IOperation operation) => OwnedBoxConversion(operation) || BaseReferenceCall(operation) || StaticReferenceTarget(operation) is not null || InterfaceReferenceCall(operation) || operation is IWithOperation || operation is ICoalesceAssignmentOperation coalescing && (DataMemberOf(coalescing.Target) is not null || PropertyCoalescingTarget(coalescing.Target)) || operation is IBinaryOperation { OperatorKind: BinaryOperatorKind.ConditionalAnd or BinaryOperatorKind.ConditionalOr } boolean && (boolean.OperatorMethod is not null || PresenceProbe(boolean)) || DataCallTarget(operation) is not null || operation is IObjectCreationOperation creation && DataType(creation.Type) || operation is IConditionalOperation { Type: { } conditionalType } && DataReferenceResult(conditionalType) || operation.ChildOperations.Any(DataStateExpression);
        private TypedValue DataArgument(TypedValue value, Location location) => ReferencedDataCells(value).Any() ? MaterializeDataValue(value, bindings, location, []) : value;
        private TypedValue MaterializeDataValue(TypedValue value, BindingEnvironment environment, Location location, HashSet<int> visiting)
        {
            if (InterfaceGraphOf(value) is { } graph) return MaterializeInterfaceGraph(graph, environment, location, visiting);
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
            if (value is RecordValue<Unit, IrType<Unit>> record) return record with { Fields = record.Fields.Select(field => new Entry<Name, TypedValue>(field.Key, ReferencedDataCells(field.Value).Any() ? MaterializeDataValue(field.Value, environment, location, visiting) : field.Value)).ToArray() };
            if (value is UpdateRecordValue<Unit, IrType<Unit>> update) return update with { Subject = MaterializeDataValue(update.Subject, environment, location, visiting), Fields = update.Fields.Select(field => new Entry<Name, TypedValue>(field.Key, ReferencedDataCells(field.Value).Any() ? MaterializeDataValue(field.Value, environment, location, visiting) : field.Value)).ToArray() };
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
                foreach (var slot in CellLayout(cell).Slots)
                    if (cell.Fields.TryGetValue(Name.From(slot.Name).Key, out var value) && ReferencedDataCells(value).Any()) current = UpdateData(CellLayout(cell), current, slot.Name, MaterializeDataValue(value, environment, location, visiting));
            visiting.Remove(origin.Cell); var result = cell.Type.IsReferenceType ? Just(current) : current; if ((cell.Type.IsReferenceType || cell.Boxed) && !TupleType(cell.Type)) { dataOrigins[result] = origin; dataOrigins[current] = origin; }
            return result;
        }
        private IEnumerable<int> ReferencedDataCells(TypedValue value, HashSet<TypedValue>? seen = null)
        {
            if (DataOriginOf(value) is { Cell: > 0 } direct && bindings.Heap.TryGetValue(direct.Cell, out var owned) && (owned.Type.IsReferenceType || owned.Boxed)) { yield return direct.Cell; yield break; }
            // Scalar results do not retain the object arguments used to compute
            // them. Walking their expression trees invents aliases and cycles.
            if (ReferenceFreeDataType(value.Attributes)) yield break;
            seen ??= new(ReferenceEqualityComparer.Instance); if (!seen.Add(value)) yield break;
            if (InterfaceGraphOf(value) is { } graph) { foreach (var cell in ReferencedDataCells(graph.Payload, seen)) yield return cell; yield break; }
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
            var name = DataStorageName(CellLayout(cell!), member);
            var fields = new Dictionary<string, TypedValue>(cell!.Fields ?? new Dictionary<string, TypedValue>()) { [Name.From(name).Key] = value };
            environment.Heap[origin.Cell] = cell with { Current = UpdateData(CellLayout(cell), cell.Current, name, value), Fields = fields, Version = ++nextDataVersion };
            CheckDataCycle(origin, environment, location);
        }
        private void ReplaceDataValue(DataOrigin origin, TypedValue value, BindingEnvironment environment, Location location)
        {
            if (!environment.Heap.TryGetValue(origin.Cell, out var cell) || !cell.Mutable) Fail<object>("MCS3002", "Whole-value updates require an owned construction/receiver location.", location);
            if (TupleType(cell!.Type)) { environment.Heap[origin.Cell] = cell with { Current = value, Fields = null, Version = ++nextDataVersion }; CheckDataCycle(origin, environment, location); return; }
            var previous = bindings; bindings = environment; Dictionary<string, TypedValue> fields;
            try { fields = CellLayout(cell!).Slots.ToDictionary(slot => Name.From(slot.Name).Key, slot => KnownDataField(value, Name.From(slot.Name).Key) ?? (TypedValue)new FieldValue<Unit, IrType<Unit>>(slot.Representation ?? Type(slot.Type, location), DataPayload(CellLayout(cell), value), Name.From(slot.Name))); }
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
        private bool DataReferenceResult(ITypeSymbol type, bool unresolved = false)
        {
            if (GenericArgument(type, out var argument, out var scope)) return WithGenericScope(scope, () => DataReferenceResult(argument, unresolved));
            if (type is ITypeParameterSymbol parameter) return unresolved && !parameter.HasUnmanagedTypeConstraint;
            if (type is not INamedTypeSymbol named) return false;
            if (ValueInterfaceType(named)) return true;
            if (DataType(named))
            {
                if (named.IsReferenceType) return true;
                // Successful C# compilation proves finite value storage. A
                // repeated generic definition can still have smaller, distinct
                // constructed arguments, so it must not hide a reference leaf.
                return ResolvedDataLayout(named).Slots.Any(slot => DataReferenceResult(slot.Type, unresolved));
            }
            if (records.TryGetValue(SymbolId(named), out var fields)) return fields.Any(field => DataReferenceResult(SubstituteType(field.Type, TypeSubstitutions(named)), unresolved));
            return named.TypeArguments.Any(argument => DataReferenceResult(argument, unresolved));
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
