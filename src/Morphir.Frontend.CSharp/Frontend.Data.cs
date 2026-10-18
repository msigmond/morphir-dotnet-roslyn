using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Morphir.IR.Classic;
using TypedValue = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        private sealed record DataOrigin(int Cell);
        private sealed record DataCell(INamedTypeSymbol Type, TypedValue Current, IReadOnlyDictionary<string, TypedValue>? Fields = null, int Version = 0, bool Mutable = true, DataLayout? Layout = null, bool Boxed = false);
        private sealed class BindingEnvironment : Dictionary<ISymbol, Binding>
        {
            internal Dictionary<int, DataCell> Heap { get; } = [];
            internal BindingEnvironment(IEqualityComparer<ISymbol> comparer) : base(comparer) { }
            internal BindingEnvironment(BindingEnvironment source, IEqualityComparer<ISymbol> comparer) : base(source, comparer)
            { foreach (var cell in source.Heap) Heap[cell.Key] = cell.Value; }
        }
        private sealed record DataSlot(ISymbol Symbol, ITypeSymbol Type, IrType<Unit>? Representation = null, string? StorageName = null)
        {
            internal string Name => StorageName ?? Symbol.Name;
        }
        private sealed record DataMember(ISymbol Symbol, ITypeSymbol Type, IOperation Instance);
        private sealed record DataLayout(INamedTypeSymbol Type, DataSlot[] Slots, GenericScope? Context = null);
        private readonly Dictionary<string, DataLayout> dataTypes = [];
        private readonly Dictionary<TypedValue, DataOrigin> dataOrigins = new(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<string, DataOrigin> dataBindingOrigins = [];
        private readonly HashSet<IOperation> provedDataOperations = [];
        private int nextDataCell;
        private IrType<Unit> ReceiverIrType(IMethodSymbol method, Location? location) => ValueInterfaceType(method.ContainingType) ? ValueInterfaceIrType(method.ContainingType, true) : DataType(method.ContainingType) ? DataIrType(method.ContainingType, true) : Type(method.ContainingType, location);
        private bool DataType(ITypeSymbol? type) => type is INamedTypeSymbol { ContainingAssembly: not null } named && dataTypes.ContainsKey(SymbolId(named));
        private bool LegacyRecordShape(INamedTypeSymbol type) => type.IsRecord && type.IsReadOnly && type.TypeKind == TypeKind.Struct && type.DeclaringSyntaxReferences.Select(r => r.GetSyntax()).All(s => s is RecordDeclarationSyntax { ParameterList: not null, AttributeLists.Count: 0 } record && (record.BaseList is null || InterfaceOnlyBases(type)) && record.Members.All(m => m is MethodDeclarationSyntax or BaseTypeDeclarationSyntax));
        private void DiscoverData(INamedTypeSymbol type)
        {
            foreach (var reference in type.DeclaringSyntaxReferences)
            {
                var syntax = reference.GetSyntax();
                foreach (var node in syntax.DescendantNodesAndSelf())
                {
                    if (node.IsKind(SyntaxKind.CheckedExpression) || node.IsKind(SyntaxKind.CheckedStatement) || node is OperatorDeclarationSyntax op && !op.CheckedKeyword.IsKind(SyntaxKind.None) || node is ConversionOperatorDeclarationSyntax conversion && !conversion.CheckedKeyword.IsKind(SyntaxKind.None)) Error("MCS2001", "Explicit checked syntax is excluded.", node.GetLocation());
                    if (node is AwaitExpressionSyntax || node is MethodDeclarationSyntax method && method.Modifiers.Any(SyntaxKind.AsyncKeyword)) Error("MCS2002", "Async/await is excluded.", node.GetLocation());
                    if (node is PointerTypeSyntax or FunctionPointerTypeSyntax or StackAllocArrayCreationExpressionSyntax or ImplicitStackAllocArrayCreationExpressionSyntax || node is TypeDeclarationSyntax declaration && declaration.Modifiers.Any(SyntaxKind.RefKeyword)) Error("MCS2003", "Pointers, stack allocation and ref-like forms are excluded.", node.GetLocation());
                }
                if (syntax is not TypeDeclarationSyntax { AttributeLists.Count: 0 } dataDeclaration || dataDeclaration.BaseList is not null && !InterfaceOnlyBases(type) || type.IsRefLikeType) Fail<object>("MCS3001", "This data layout requires its generic/inheritance gate or explicit attribute/ref-like mapping.", syntax.GetLocation());
            }
            if (type.IsAbstract) Fail<object>("MCS3005", "Abstract data requires the D10 closed dispatch representation.", Location(type));
            var fields = type.GetMembers().OfType<IFieldSymbol>().Where(field => !field.IsStatic && (!field.IsImplicitlyDeclared || PrimaryCaptureField(field))).ToArray();
            var properties = type.GetMembers().OfType<IPropertySymbol>().ToArray();
            if (type.GetMembers().Any(member => member is IEventSymbol || member is IFieldSymbol { IsStatic: true } staticField && !dataConstants.ContainsKey(SymbolId(staticField)) || member is IMethodSymbol { IsImplicitlyDeclared: false, MethodKind: not (MethodKind.Ordinary or MethodKind.ExplicitInterfaceImplementation) } method && !(method.AssociatedSymbol is IPropertySymbol property && DataPropertyStorage(property) && !AccessorBody(method)) && !dataConstructors.ContainsKey(SymbolId(method)) && !dataGetters.ContainsKey(SymbolId(method)) && !dataSetters.ContainsKey(SymbolId(method)) && !dataOperators.ContainsKey(SymbolId(method))) || type.InstanceConstructors.Any(ctor => !ctor.IsImplicitlyDeclared && !dataConstructors.ContainsKey(SymbolId(ctor))) || fields.Any(field => !field.IsImplicitlyDeclared && field.GetAttributes().Length != 0) || properties.Any(property => !DataPropertyDeclaration(property)))
                Fail<object>("MCS3001", "Data requires mapped instance storage, compile-time constants and validated pure members; persistent/effectful storage is not inferred pure.", Location(type));
            var slots = InheritedSlots(type).Concat(fields.Select(field => new DataSlot(field, field.Type))).Concat(properties.Where(DataPropertyStorage).Select(property => new DataSlot(property, property.Type))).ToArray();
            if (slots.Any(slot => DataSlotHasArray(slot.Type))) Fail<object>("MCS2104", "Array data fields require the D11 collection gate.", Location(type));
            slots = QualifyInheritedSlots(slots);
            var collisions = slots.GroupBy(slot => Name.From(slot.Name).Key).Where(group => group.Count() > 1).ToArray();
            if (collisions.Length != 0) { foreach (var group in collisions) foreach (var slot in group) Error("MCS1004", "Data slot identities collide after normalization.", Location(slot.Symbol)); return; }
            dataTypes[SymbolId(type)] = new(type, slots);
        }
        private static bool AutoProperty(IPropertySymbol property) => !property.IsImplicitlyDeclared && property.DeclaringSyntaxReferences.Length != 0 && !property.IsStatic && !property.IsIndexer && property.RefKind == RefKind.None && property.GetAttributes().Length == 0 && property.DeclaringSyntaxReferences.All(reference => reference.GetSyntax() is PropertyDeclarationSyntax { AttributeLists.Count: 0, ExpressionBody: null, AccessorList: not null } syntax && syntax.AccessorList.Accessors.All(accessor => accessor.Body is null && accessor.ExpressionBody is null && accessor.AttributeLists.Count == 0));
        private DataMember? DataMemberOf(IOperation operation) => operation switch
        {
            IFieldReferenceOperation { Instance: not null } field when DataType(field.Field.ContainingType) || InheritedField(field.Field) => new(DataStorageSymbol(field.Field), field.Field.Type, field.Instance),
            IPropertyReferenceOperation { Instance: not null } property when (DataType(property.Property.ContainingType) || InheritedStorageMember(property.Property)) && DataPropertyStorage(property.Property) && (!(property.Property.GetMethod is { } getter && dataGetters.ContainsKey(SymbolId(getter))) || property.Parent is ISimpleAssignmentOperation assignment && ReferenceEquals(assignment.Target, property) && !AccessorBody(property.Property.SetMethod)) => new(property.Property, property.Property.Type, property.Instance!),
            _ => null
        };

        private IrType<Unit> DataIrType(INamedTypeSymbol type, bool present = false)
        {
            IrType<Unit> raw = GenericTypeReference(type);
            return type.IsReferenceType && !present ? Types.Maybe(raw) : raw;
        }
        private void EmitData(DataLayout data)
        {
            var type = data.Type; var id = Identity(type); EnsureModule(id.Module, type.ContainingType is null ? Access.Public : Visibility(type.ContainingType));
            var fields = data.Slots.Select(field => new Entry<Name, IrType<Unit>>(Name.From(field.Name), Type(field.Type, Location(field.Symbol)))).OrderBy(field => field.Key.Key, StringComparer.Ordinal).ToArray();
            modules[id.Module.Key].Types.Add(new(id.Local, new(Visibility(type), new(Documentation(type), customDataTypes.Contains(SymbolId(type)) ? (TypeDefinition<Unit>)new CustomDefinition<Unit>(SourceTypeParameters(type).Select(TypeParameterName).ToArray(), new(Access.Public, [new(id.Local, [new(Name.From("arg1"), new RecordType<Unit>(default, fields))])])) : new AliasDefinition<Unit>(SourceTypeParameters(type).Select(TypeParameterName).ToArray(), new RecordType<Unit>(default, fields))))));
            foreach (var getter in type.GetMembers().OfType<IPropertySymbol>().Select(property => property.GetMethod).Where(getter => getter is not null && dataGetters.ContainsKey(SymbolId(getter)))) Try(() => EmitMethod(getter!));
            foreach (var setter in type.GetMembers().OfType<IPropertySymbol>().Select(property => property.SetMethod).Where(setter => setter is not null && dataSetters.ContainsKey(SymbolId(setter)))) Try(() => EmitDataSetter(setter!));
            foreach (var constructor in type.InstanceConstructors.Where(constructor => dataConstructors.ContainsKey(SymbolId(constructor)))) Try(() => EmitDataConstructor(constructor));
            foreach (var method in type.GetMembers().OfType<IMethodSymbol>().Where(method => !method.IsImplicitlyDeclared && (SourceMethod(method) || dataOperators.ContainsKey(SymbolId(method))))) Try(() => EmitMethod(method));
            foreach (var local in sourceMethods.Values.Where(method => method.MethodKind == MethodKind.LocalFunction && SymbolEqualityComparer.Default.Equals(method.ContainingType, type))) Try(() => EmitMethod(local));
        }
        private DataOrigin? DataOriginOf(TypedValue value)
        {
            if (dataOrigins.TryGetValue(value, out var origin)) return origin;
            if (dataProjections.TryGetValue(value, out var projected)) return DataOriginOf(projected);
            if (tupleProjections.TryGetValue(value, out var tupleProjected)) return DataOriginOf(tupleProjected);
            if (value is VariableValue<Unit, IrType<Unit>> variable && dataBindingOrigins.TryGetValue(variable.Name.Key, out var bindingOrigin)) return bindingOrigin;
            if (value is ApplyValue<Unit, IrType<Unit>> { Function: ConstructorValue<Unit, IrType<Unit>> { Name.Local.Key: "just" }, Argument: var argument }) return DataOriginOf(argument);
            return null;
        }
        private TypedValue ReadDataOrigin(DataOrigin origin, BindingEnvironment environment, Location location)
        { var previous = bindings; bindings = environment; try { return MaterializeDataOrigin(origin, environment, location, []); } finally { bindings = previous; } }
        private void SnapshotDataBinding(DataOrigin? origin, Name name, IrType<Unit> type, BindingEnvironment environment)
        {
            if (origin is null || !environment.Heap.TryGetValue(origin.Cell, out var cell)) return;
            dataBindingOrigins[name.Key] = origin;
            var raw = MaybeElement(type) ?? type; var subject = new VariableValue<Unit, IrType<Unit>>(type, name);
            var extracted = Fresh("ownedData");
            TypedValue current = MaybeElement(type) is null ? subject : new MatchValue<Unit, IrType<Unit>>(raw, subject, [new(new ConstructorPattern<IrType<Unit>>(type, MaybeName("Just"), [new AsPattern<IrType<Unit>>(raw, new WildcardPattern<IrType<Unit>>(raw), extracted)]), new VariableValue<Unit, IrType<Unit>>(raw, extracted))]);
            environment.Heap[origin.Cell] = cell with { Current = current };
        }
        private bool DataTarget(IOperation operation) => DataMemberOf(operation) is not null || operation is IPropertyReferenceOperation property && DataSetter(property) || RecordUpdateTarget(operation) || MixedTupleTarget(operation) || operation is IInstanceReferenceOperation { Type.IsValueType: true } instance && DataType(instance.Type);
        private TypedValue DataRecord(DataLayout data, Location location) => PackData(data, new RecordValue<Unit, IrType<Unit>>(customDataTypes.Contains(SymbolId(data.Type)) ? DataPayloadType(data) : LayoutIrType(data, true), data.Slots.Select(field => new Entry<Name, TypedValue>(Name.From(field.Name), SlotDefault(data, field, location))).OrderBy(field => field.Key.Key, StringComparer.Ordinal).ToArray()));
        private TypedValue? DataExpression(IOperation operation, BindingEnvironment environment, Func<TypedValue, BindingEnvironment, TypedValue> continuation)
        {
            var location = operation.Syntax.GetLocation();
            if (InterfaceBoxExpression(operation, environment, continuation) is { } boxed) return boxed;
            if (PropertyCoalescingExpression(operation, environment, continuation) is { } propertyCoalescing) return propertyCoalescing;
            if (DataCoalescingAssignment(operation, environment, continuation) is { } coalescing) return coalescing;
            if (StaticReferenceExpression(operation, environment, continuation) is { } staticCall) return staticCall;
            if (BaseReferenceExpression(operation, environment, continuation) is { } baseCall) return baseCall;
            if (InterfaceCallExpression(operation, environment, continuation) is { } interfaceCall) return interfaceCall;
            if (GenericNewExpression(operation, environment, continuation) is { } genericNew) return genericNew;
            if (PrimaryCaptureExpression(operation, environment, continuation) is { } capturedAssignment) return capturedAssignment;
            if (operation is ISimpleAssignmentOperation { Target: IInstanceReferenceOperation { Type.IsValueType: true } self } whole && DataType(self.Type))
                return WithDataLocation(self, environment, (destination, state) => Expression(whole.Value, state, (value, next) => destination.Write(value, next, after => { provedDataOperations.Add(operation); return continuation(value, after); })));
            if (DataShortCircuitExpression(operation, environment, continuation) is { } shortCircuit) return shortCircuit;
            if (MixedTupleExpression(operation, environment, continuation) is { } mixedTuple) return mixedTuple;
            if (DataWithExpression(operation, environment, continuation) is { } recordUpdate) return recordUpdate;
            if (DataCallExpression(operation, environment, continuation) is { } dataCall) return dataCall;
            if (DataSetterExpression(operation, environment, continuation) is { } setterExpression) return setterExpression;
            if (operation is IObjectCreationOperation creation && creation.Type is INamedTypeSymbol owner && dataTypes.TryGetValue(SymbolId(owner), out var data))
            {
                data = ResolvedDataLayout(owner);
                if (creation.Constructor is { } copy && RecordCopyConstructor(copy) && creation.Arguments.Length == 1)
                    return Expression(creation.Arguments[0].Value, environment, (value, state) => CopyDataRecord(owner, value, state, location, creation.Initializer, (copied, next) => { provedDataOperations.Add(creation); return continuation(copied, next); }));
                if (ConstructUserData(creation, data, environment, continuation) is { } constructed) return constructed;
                if (creation.Arguments.Length != 0 || creation.Constructor?.IsImplicitlyDeclared != true) return Fail<TypedValue>("MCS3001", "Construction has no selected supported constructor or synthesized data-copy mapping.", location);
                var origin = new DataOrigin(++nextDataCell); environment.Heap[origin.Cell] = NewDataCell(data, location);
                return RunDataObjectInitializer(creation.Initializer, origin, environment, state => { provedDataOperations.Add(operation); return continuation(ConstructedClassValue(data, ReadDataOrigin(origin, state, location), location), state); });
            }
            if (operation is ISimpleAssignmentOperation update && DataMemberOf(update.Target) is { } fieldTarget)
            {
                var thisOrigin = fieldTarget.Instance is IInstanceReferenceOperation ? currentDataConstructionOrigin : null;
                if (fieldTarget.Symbol.ContainingType.IsValueType && thisOrigin is null) return AssignStructField(update.Target, update.Value, environment, (value, next) => { provedDataOperations.Add(operation); return continuation(value, next); }, location);
                return Expression(fieldTarget.Instance, environment, (receiver, state) => Expression(update.Value, state, (value, next) =>
                {
                    var origin = thisOrigin ?? ClassFieldOrigin(receiver); if (origin is null || !next.Heap.TryGetValue(origin.Cell, out var cell)) return Fail<TypedValue>("MCS3002", "Mutation requires a proved local owned object; input/external reachability is immutable.", location);
                    WriteDataCell(origin, fieldTarget, Adapt(value, Type(fieldTarget.Type, location)), next, location);
                    provedDataOperations.Add(operation); return continuation(value, next);
                }));
            }
            return null;
        }
        private bool DataSafetyOperation(IOperation operation) => OwnedBoxConversion(operation) || PrimaryCaptureTarget(operation) is not null || operation is IBinaryOperation equality && RecordEqualityBinary(equality) || operation is IInvocationOperation equalityCall && DataEqualityCall(equalityCall) || operation is IWithOperation || operation is IObjectCreationOperation creation && DataType(creation.Type) || operation is ICoalesceAssignmentOperation coalescing && (DataMemberOf(coalescing.Target) is not null || PropertyCoalescingTarget(coalescing.Target)) || operation is ISimpleAssignmentOperation assignment && DataTarget(assignment.Target);
        private bool DataOperand(IOperation operation) => DataType(operation.Type) || operation.Type is { } type && ValueInterfaceType(type) || operation is IConversionOperation { Conversion.IsUserDefined: false } conversion && DataOperand(conversion.Operand);
        private bool DataIdentityBinary(IBinaryOperation operation) => operation.OperatorKind is BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals && operation.OperatorMethod is null && (DataOperand(operation.LeftOperand) || DataOperand(operation.RightOperand)) && !PresenceBinary(operation);
        private TypedValue AssignStructField(IOperation target, IOperation value, BindingEnvironment environment, Func<TypedValue, BindingEnvironment, TypedValue> continuation, Location location)
        {
            return WithDataLocation(target, environment, (destination, state) => Expression(value, state, (replacement, next) => destination.Write(replacement, next, after => continuation(replacement, after))));
        }
        private TypedValue? LowerData(IOperation operation)
        {
            if (operation is IInstanceReferenceOperation { ReferenceKind: InstanceReferenceKind.ImplicitReceiver } && currentDataInitializerOrigin is { } initializerOrigin) return ReadDataOrigin(initializerOrigin, bindings, operation.Syntax.GetLocation());
            if (LowerDataConstant(operation) is { } constant) return constant;
            if (LowerDataEquality(operation) is { } equality) return equality;
            if (LowerDataOperator(operation) is { } userOperator) return userOperator;
            if (ReadPrimaryCapture(operation) is { } captured) return captured;
            if (DataSafetyOperation(operation)) return Expression(operation, bindings, (value, _) => value);
            if (operation is IPropertyReferenceOperation property && DataGetter(property)) return GetDataProperty(property);
            if (DataMemberOf(operation) is { } member)
            {
                if (InheritedStorageMember(member.Symbol)) return ReadInheritedMember(member, Lower(member.Instance), operation.Syntax.GetLocation());
                var location = operation.Syntax.GetLocation(); var receiver = Lower(member.Instance); if (DataOriginOf(receiver) is { } liveOrigin) receiver = ReadDataOrigin(liveOrigin, bindings, location); var subject = DemandPresent(receiver, location); var data = ResolvedDataLayout(member.Symbol.ContainingType);
                var name = DataStorageName(data, member.Symbol);
                var result = new FieldValue<Unit, IrType<Unit>>(Type(member.Type, location), DataPayload(data, subject), Name.From(name));
                var known = KnownDataField(receiver, Name.From(name).Key);
                if (known is not null)
                {
                    dataProjections[result] = known;
                    if (DataType(member.Type) && member.Type.IsReferenceType && DataOriginOf(known) is { } child) return ReadDataOrigin(child, bindings, location);
                }
                if (DataOriginOf(receiver) is { } owner && bindings.Heap.TryGetValue(owner.Cell, out var cell)) dataValueKeys[result] = "data:" + owner.Cell + ":" + cell.Version + "/" + Name.From(name).Key;
                else if (ValueKey(receiver) is { } receiverKey) dataValueKeys[result] = receiverKey + "/" + Name.From(name).Key;
                if (known is not null && Integral(NullableElement(member.Type) ?? member.Type) is { } integer && MaybeElement(known.Attributes) is null) { numericValueRanges[result] = IntegerRange(known, integer); if (ValueKey(result) is { } key) numericFacts.TryAdd(key, IntegerRange(known, integer)); }
                if (known is not null && DecimalType(member.Type)) decimalDomains[result] = DecimalRange(known);
                return result;
            }
            if (operation is IBinaryOperation binary && DataIdentityBinary(binary)) return Fail<TypedValue>("MCS3003", "Reference identity observations are excluded; source value equality requires its explicit D9 mapping.", operation.Syntax.GetLocation());
            return null;
        }
    }
}
