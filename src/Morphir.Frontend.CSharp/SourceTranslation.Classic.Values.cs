using Microsoft.CodeAnalysis;
using Morphir.IR.Classic;
using V = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;
using IrPath = Morphir.IR.Classic.Path;

namespace Morphir.Frontend.CSharp;

internal sealed partial class SourceTranslation
{
    private static V Sdk(string module, string name, IrType<Unit> output, params V[] arguments)
    {
        IrType<Unit> signature = output;
        foreach (var argument in arguments.Reverse()) signature = Types.Function(argument.Attributes, signature);
        V value = new ReferenceValue<Unit, IrType<Unit>>(signature, new(Types.Sdk, IrPath.From(module), Name.From(name)));
        foreach (var argument in arguments)
        {
            signature = ((FunctionType<Unit>)signature).Result;
            value = new ApplyValue<Unit, IrType<Unit>>(signature, value, argument);
        }
        return value;
    }
    private V ClassicCast(CSharpOperation<Unit, IrType<Unit>> cast)
    {
        if(declared.GetValueOrDefault(Property(cast,"symbol")) is IMethodSymbol && identities.TryGetValue(Property(cast,"symbol"),out var operatorName))return ClassicFunction(operatorName,cast.Attributes,cast.Children.ToArray());
        if(MaybeElement(cast.Attributes) is not null)return ClassicExpected(cast.Children.Single(),cast.Attributes);
        var value = ClassicValue(cast.Children.Single());
        if(Shape(ScalarShape(value.Attributes))==Shape(ScalarShape(cast.Attributes)) && (EnumValueType(value.Attributes)||EnumValueType(cast.Attributes)))return ClassicScalarCast(value,cast.Attributes);
        if (Shape(value.Attributes) == Shape(cast.Attributes)) return value;
        var source = Shape(value.Attributes); var target = Shape(cast.Attributes);
        if (source == Shape(Types.Int) && target == Shape(Types.Float)) return Sdk("Basics", "toFloat", cast.Attributes, value);
        if (source == Shape(Types.Float) && target == Shape(Types.Int)) return Sdk("Basics", "truncate", cast.Attributes, value);
        if (source == Shape(Types.Int) && target == Shape(Types.Decimal)) return Sdk("Decimal", "fromInt", cast.Attributes, value);
        if (source == Shape(Types.Float) && target == Shape(Types.Decimal)) return Sdk("Decimal", "fromFloat", cast.Attributes, value);
        if (source == Shape(Types.Decimal) && target == Shape(Types.Float)) return Sdk("Decimal", "toFloat", cast.Attributes, value);
        if (source == Shape(Types.Reference("Char", "Char")) && target == Shape(Types.Int)) return Sdk("Char", "toCode", cast.Attributes, value);
        if (source == Shape(Types.Int) && target == Shape(Types.Reference("Char", "Char"))) return Sdk("Char", "fromCode", cast.Attributes, value);
        return ClassicFailure("This cast needs an approved standard Elm conversion.");
    }
    private bool RecordValueType(IrType<Unit> type) => type is ReferenceType<Unit> r &&
        declared.Values.OfType<INamedTypeSymbol>().Any(t => t.IsRecord && identities[Id(t)] is { } name &&
            name.Package.Key == r.Name.Package.Key && name.Module.Key == r.Name.Module.Key && name.Local.Key == r.Name.Local.Key);
    private RecordType<Unit>? RecordShape(IrType<Unit> type)
    {
        if (type is RecordType<Unit> record) return record;
        if (type is not ReferenceType<Unit> reference || reference.Name.Package.Key != package.Key) return null;
        var alias = classicInput.Definition.Modules.FirstOrDefault(m => m.Key.Key == reference.Name.Module.Key)?.Value.Value.Types
            .FirstOrDefault(t => t.Key.Key == reference.Name.Local.Key)?.Value.Value.Value as AliasDefinition<Unit>;
        if (alias is null || alias.Parameters.Count != reference.Arguments.Count) return null;
        var arguments = alias.Parameters.Select((name, i) => (name.Key, Type: reference.Arguments[i])).ToDictionary(p => p.Key, p => p.Type);
        return Substitute(alias.Body, arguments) as RecordType<Unit>;
    }
    private V ClassicUpdate(CSharpOperation<Unit, IrType<Unit>> update)
    {
        var subject = ClassicValue(update.Children[0]); var shape = RecordShape(subject.Attributes);
        if (!RecordValueType(subject.Attributes) || shape is null || update.Children[1] is not CSharpOperation<Unit, IrType<Unit>> { Kind: "Initializer" } initializer)
            return ClassicFailure("This update has no record value mapping.");
        var fields = new List<Entry<Name, V>>();
        foreach (var child in initializer.Children)
        {
            if (child is not CSharpOperation<Unit, IrType<Unit>> { Kind: "Assignment", Children.Count: 2 } assignment || assignment.Children[0] is not FieldValue<Unit, IrType<Unit>> field)
                return ClassicFailure("Only written record-field updates have this mapping.");
            var slot = shape.Fields.SingleOrDefault(f => f.Key.Key == field.Field.Key);
            var value = ClassicValue(assignment.Children[1]);
            if (slot is null || Shape(slot.Value) != Shape(value.Attributes)) return ClassicFailure("The updated field requires another conversion mapping.");
            fields.Add(new(field.Field, value));
        }
        return new UpdateRecordValue<Unit, IrType<Unit>>(update.Attributes, subject, fields.OrderBy(f => f.Key.Key, StringComparer.Ordinal).ToArray());
    }
    private V ClassicStatementValue(V statement) => statement is CSharpOperation<Unit, IrType<Unit>> { Kind: "Block" } block
        ? ClassicValue(block) : ClassicStatements([statement], 0);
    private static bool Terminates(V value) => value is CSharpOperation<Unit, IrType<Unit>> source && source.Kind switch
    {
        "Return" => true,
        "Block" or "checked" or "unchecked" => source.Children.LastOrDefault(c=>c is not CSharpOperation<Unit,IrType<Unit>> {Kind:"LocalFunction"}) is { } last && Terminates(last),
        "If" => source.Children.Count == 3 && Terminates(source.Children[1]) && Terminates(source.Children[2]),
        _ => false
    };
}
