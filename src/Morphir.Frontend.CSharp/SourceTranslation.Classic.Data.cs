using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Morphir.IR.Classic;
using V = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

internal sealed partial class SourceTranslation
{
    private V ClassicFunction(FQName name, params V[] arguments)
    {
        var definition=Lookup.Value(classicInput,name);
        if(definition is null)return ClassicFailure("The source function definition is missing.");
        IrType<Unit> signature=definition.Output;
        foreach(var input in definition.Inputs.Reverse())signature=Types.Function(input.Type,signature);
        V result=new ReferenceValue<Unit,IrType<Unit>>(signature,name);
        if(arguments.Length!=definition.Inputs.Count)return ClassicFailure("Source member call arity differs from its definition.");
        foreach(var argument in arguments)
        {
            var function=(FunctionType<Unit>)signature;
            var value=ClassicExpected(argument,function.Argument);
            signature=function.Result;result=new ApplyValue<Unit,IrType<Unit>>(signature,result,value);
        }
        return result;
    }
    private V ClassicDataConstructor(IMethodSymbol constructor, ValueDefinition<Unit,IrType<Unit>> definition)
    {
        var record=RecordShape(definition.Output);
        if(record is null)return ClassicFailure("The constructor has no record alias.");
        var fields=new List<Entry<Name,V>>();
        if(definition.Body is CSharpOperation<Unit,IrType<Unit>> {Kind:"PrimaryConstructor"})
        {
            foreach(var input in definition.Inputs)fields.Add(new(input.Name,new VariableValue<Unit,IrType<Unit>>(input.Type,input.Name)));
        }
        else if(definition.Body is CSharpOperation<Unit,IrType<Unit>> {Kind:"Block"} block)
        {
            foreach(var statement in block.Children)
            {
                if(statement is CSharpOperation<Unit,IrType<Unit>> {Kind:"EmptyStatement"})continue;
                if(statement is not CSharpOperation<Unit,IrType<Unit>> {Kind:"ExpressionStatement",Children.Count:1} expression || expression.Children[0] is not CSharpOperation<Unit,IrType<Unit>> {Kind:"Assignment",Children.Count:2} assignment || Property(assignment,"operator")!="=" || assignment.Children[0] is not FieldValue<Unit,IrType<Unit>> {Subject:VariableValue<Unit,IrType<Unit>> receiver} field || receiver.Name.Key!=Receiver(constructor).Key)
                    return ClassicFailure("A data constructor must explicitly assign each field once and have no other statements.");
                var slot=record.Fields.SingleOrDefault(f=>f.Key.Key==field.Field.Key);
                if(slot is null || fields.Any(f=>f.Key.Key==field.Field.Key))return ClassicFailure("The constructor must assign each stored field once.");
                fields.Add(new(field.Field,ClassicExpected(assignment.Children[1],slot.Value)));
            }
        }
        else return ClassicFailure("Constructor chaining or this constructor body has no approved record-building mapping.");
        if(fields.Count!=record.Fields.Count || record.Fields.Any(f=>fields.All(v=>v.Key.Key!=f.Key.Key)))return ClassicFailure("Implicit field initialization has no approved mapping.");
        return new RecordValue<Unit,IrType<Unit>>(definition.Output,fields.OrderBy(f=>f.Key.Key,StringComparer.Ordinal).ToArray());
    }
    private bool EnumValueType(IrType<Unit> type)=>type is ReferenceType<Unit> r && declared.Values.OfType<INamedTypeSymbol>().Any(t=>t.TypeKind==TypeKind.Enum && identities[Id(t)] is {} id && id.Package.Key==r.Name.Package.Key && id.Module.Key==r.Name.Module.Key && id.Local.Key==r.Name.Local.Key);
    private IrType<Unit> ScalarShape(IrType<Unit> type)=>EnumValueType(type)?Types.Int:type;
    private readonly Dictionary<string,IrType<Unit>> classicBindingTypes=[];
    private V ClassicScalarCast(V value,IrType<Unit> target)
    {
        if(value is VariableValue<Unit,IrType<Unit>> variable && classicBindingTypes.ContainsKey(variable.Name.Key))classicBindingTypes[variable.Name.Key]=target;
        return value switch
        {
            VariableValue<Unit,IrType<Unit>> v=>v with{Attributes=target},
            LiteralValue<Unit,IrType<Unit>> v=>v with{Attributes=target},
            ReferenceValue<Unit,IrType<Unit>> v=>v with{Attributes=target},
            FieldValue<Unit,IrType<Unit>> v=>v with{Attributes=target},
            ApplyValue<Unit,IrType<Unit>> v=>v with{Attributes=target},
            _=>ClassicFailure("This scalar alias cast needs another typed expression mapping.")
        };
    }
}
