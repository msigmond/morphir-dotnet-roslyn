using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Morphir.IR.Classic;
using TypedValue = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        private static bool BaseStorageField(ISymbol member) => member is IFieldSymbol { IsStatic: false } field && (PrimaryCaptureField(field) || !field.IsImplicitlyDeclared && field.GetAttributes().Length == 0 && field.DeclaringSyntaxReferences.All(reference => reference.GetSyntax() is VariableDeclaratorSyntax));
        private readonly Dictionary<string, string> qualifiedDataSlots = [];
        private DataSlot[] QualifyInheritedSlots(DataSlot[] slots)
        {
            var hidden = slots.GroupBy(slot => Name.From(slot.Name).Key).Where(group => group.Count() > 1 && group.Select(slot => SymbolId(slot.Symbol.ContainingType)).Distinct().Count() == group.Count()).SelectMany(group => group).Select(slot => SymbolId(slot.Symbol)).ToHashSet();
            return slots.Select(slot =>
            {
                if (!hidden.Contains(SymbolId(slot.Symbol))) return slot;
                var name = SourceOwnerName(slot.Symbol.ContainingType) + "." + slot.Symbol.Name;
                qualifiedDataSlots[SymbolId(slot.Symbol)] = name;
                return slot with { StorageName = name };
            }).ToArray();
        }
        private string DataStorageName(DataLayout data, ISymbol member) => data.Slots.FirstOrDefault(slot => SymbolId(slot.Symbol) == SymbolId(DataStorageSymbol(member)))?.Name ?? Fail<string>("MCS3001", "The resolved source member has no storage slot in this runtime layout.", Location(member));
        private string DataMemberKey(ISymbol member) => qualifiedDataSlots.GetValueOrDefault(SymbolId(member)) ?? member.Name;
        private bool InheritedField(ISymbol member) => member is IFieldSymbol { IsStatic: false } && member.ContainingType.TypeKind == TypeKind.Class && ValueInterfaceType(member.ContainingType);
        private IEnumerable<DataSlot> InheritedSlots(INamedTypeSymbol type) => RuntimeContracts(type).Where(parent => parent.TypeKind == TypeKind.Class && ValueInterfaceType(parent)).SelectMany(parent => parent.GetMembers().Where(BaseStorageField).OfType<IFieldSymbol>().Select(field => new DataSlot(field, field.Type)).Concat(parent.GetMembers().Where(BaseStorageProperty).OfType<IPropertySymbol>().Select(property => new DataSlot(property, property.Type))));
        private DataOrigin? ClassFieldOrigin(TypedValue value) => DataOriginOf(value) ?? (InterfaceGraphOf(value) is { } graph ? DataOriginOf(graph.Payload) : null);
        private TypedValue ReadInheritedMember(DataMember member, TypedValue receiver, Location location)
        {
            if (ClassFieldOrigin(receiver) is { } liveOrigin)
            {
                receiver = ReadDataOrigin(liveOrigin, bindings, location);
                return ReadInheritedStorage(member, receiver, CellLayout(bindings.Heap[liveOrigin.Cell]), location);
            }
            var subject = DemandPresent(receiver, location);
            if (DataLayoutForIr(subject.Attributes) is { } direct) return ReadInheritedStorage(member, subject, direct, location);
            var result = MatchInterface(subject, member.Symbol.ContainingType, (alternative, payload) => ReadInheritedStorage(member, payload, ResolvedDataLayout(alternative), location));
            if (ValueKey(receiver) is { } key) dataValueKeys[result] = key + "/" + Name.From(DataMemberKey(member.Symbol)).Key;
            return result;
        }
        private TypedValue ReadInheritedStorage(DataMember member, TypedValue receiver, DataLayout data, Location location) => ReadStoredMember(member.Symbol, member.Type, receiver, data, location);
        private TypedValue ReadStoredMember(ISymbol symbol, ITypeSymbol memberType, TypedValue receiver, DataLayout data, Location location)
        {
            var subject = DemandPresent(receiver, location);
            var name = DataStorageName(data, symbol);
            var result = new FieldValue<Unit, IrType<Unit>>(Type(memberType, location), DataPayload(data, subject), Name.From(name));
            var known = KnownDataField(receiver, Name.From(name).Key);
            if (known is not null)
            {
                dataProjections[result] = known;
                if (DataType(memberType) && memberType.IsReferenceType && DataOriginOf(known) is { } child) return ReadDataOrigin(child, bindings, location);
            }
            if (DataOriginOf(receiver) is { } owner && bindings.Heap.TryGetValue(owner.Cell, out var cell)) dataValueKeys[result] = "data:" + owner.Cell + ":" + cell.Version + "/" + Name.From(name).Key;
            else if (ValueKey(receiver) is { } key) dataValueKeys[result] = key + "/" + Name.From(name).Key;
            if (known is not null && Integral(NullableElement(memberType) ?? memberType) is { } integer && MaybeElement(known.Attributes) is null) { numericValueRanges[result] = IntegerRange(known, integer); if (ValueKey(result) is { } identity) numericFacts.TryAdd(identity, IntegerRange(known, integer)); }
            if (known is not null && DecimalType(memberType)) decimalDomains[result] = DecimalRange(known);
            return result;
        }
    }
}
