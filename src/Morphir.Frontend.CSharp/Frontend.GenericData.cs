using Microsoft.CodeAnalysis;
using Morphir.IR.Classic;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        private DataLayout ResolvedDataLayout(INamedTypeSymbol type)
        {
            var layout = dataTypes[SymbolId(type)]; var substitutions = TypeSubstitutions(type);
            return new(type, layout.Slots.Select(slot => { var actual = SubstituteType(slot.Type, substitutions); return new DataSlot(slot.Symbol, actual, Type(actual, Location(slot.Symbol)), slot.StorageName); }).ToArray(), CaptureGenericScope());
        }
        private static IrType<Unit> SubstituteIrTypes(IrType<Unit> type, IReadOnlyDictionary<string, IrType<Unit>> substitutions) => type switch
        {
            VariableType<Unit> variable when substitutions.TryGetValue(variable.Name.Key, out var replacement) => replacement,
            ReferenceType<Unit> reference => reference with { Arguments = reference.Arguments.Select(argument => SubstituteIrTypes(argument, substitutions)).ToArray() },
            FunctionType<Unit> function => function with { Argument = SubstituteIrTypes(function.Argument, substitutions), Result = SubstituteIrTypes(function.Result, substitutions) },
            TupleType<Unit> tuple => tuple with { Elements = tuple.Elements.Select(element => SubstituteIrTypes(element, substitutions)).ToArray() },
            RecordType<Unit> record => record with { Fields = record.Fields.Select(field => new Entry<Name, IrType<Unit>>(field.Key, SubstituteIrTypes(field.Value, substitutions))).ToArray() },
            _ => type
        };
        private RecordType<Unit> AppliedDataPayloadType(DataLayout data, IrType<Unit> reference)
        {
            if (data.Context is not null) return DataPayloadType(data);
            reference = MaybeElement(reference) ?? reference;
            if (reference is not ReferenceType<Unit> applied || applied.Arguments.Count == 0) return DataPayloadType(data);
            var previous = activeGenericTypes; activeGenericTypes = new(SymbolEqualityComparer.Default);
            try
            {
                var substitutions = SourceTypeParameters(data.Type).Zip(applied.Arguments).ToDictionary(pair => TypeParameterName(pair.First).Key, pair => pair.Second);
                return (RecordType<Unit>)SubstituteIrTypes(DataPayloadType(data), substitutions);
            }
            finally { activeGenericTypes = previous; }
        }
    }
}
