using Microsoft.CodeAnalysis;
using Morphir.IR.Classic;
using V = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;
using IrPath = Morphir.IR.Classic.Path;

namespace Morphir.Frontend.CSharp;

internal sealed partial class SourceTranslation
{
    private static IrType<Unit>? MaybeElement(IrType<Unit> type) => type is ReferenceType<Unit> r && r.Name.Package.Key == Types.Sdk.Key && r.Name.Module.Key == IrPath.From("Maybe").Key && r.Name.Local.Key == Name.From("Maybe").Key && r.Arguments.Count == 1 ? r.Arguments[0] : null;
    private static V Nothing(IrType<Unit> type) => new ConstructorValue<Unit, IrType<Unit>>(type, new(Types.Sdk, IrPath.From("Maybe"), Name.From("Nothing")));
    private V ClassicExpected(V source, IrType<Unit> expected)
    {
        if(source is CSharpOperation<Unit,IrType<Unit>> {Kind:"Parenthesized" or "checked" or "unchecked" or "NullForgiving",Children.Count:1} context)return ClassicExpected(context.Children[0],expected);
        if (source is CSharpOperation<Unit, IrType<Unit>> { Kind: "Null" or "Default" } empty)
            return empty.Kind == "Null" || MaybeElement(expected) is not null ? MaybeElement(expected) is not null ? Nothing(expected) : ClassicFailure("Null requires an explicit nullable type.") : ClassicDefault(expected);
        if(source is TupleValue<Unit,IrType<Unit>> tuple && expected is TupleType<Unit> target && target.Elements.Count==tuple.Elements.Count)
            return new TupleValue<Unit,IrType<Unit>>(expected,tuple.Elements.Select((e,i)=>ClassicExpected(e,target.Elements[i])).ToArray());
        if(source is ListValue<Unit,IrType<Unit>> list && expected is ReferenceType<Unit> {Arguments.Count:1} listType && listType.Name.Module.Key==IrPath.From("List").Key && listType.Name.Package.Key==Types.Sdk.Key)
            return new ListValue<Unit,IrType<Unit>>(expected,list.Elements.Select(e=>ClassicExpected(e,listType.Arguments[0])).ToArray());
        if (source is IfValue<Unit, IrType<Unit>> branch)
            return new IfValue<Unit, IrType<Unit>>(expected, ClassicValue(branch.Condition), ClassicExpected(branch.Then, expected), ClassicExpected(branch.Else, expected));
        if(MaybeElement(expected) is {} decimalResult && Shape(decimalResult)==Shape(Types.Decimal) && source is CSharpOperation<Unit,IrType<Unit>> {Kind:"Invocation",Children.Count:3} call && Property(call,"decimalDivision")=="true")
            return Sdk("Decimal","div",expected,call.Children.Skip(1).Select(a=>ClassicExpected(((CSharpOperation<Unit,IrType<Unit>>)a).Children.Single(),Types.Decimal)).ToArray());
        if (MaybeElement(expected) is { } element && source is CSharpOperation<Unit, IrType<Unit>> { Kind: "Binary", Children.Count: 2 } division && Property(division, "operator") == "/" && Shape(element) == Shape(Types.Decimal) && division.Children.All(c => Shape(c.Attributes) == Shape(Types.Decimal)))
            return Sdk("Decimal", "div", expected, ClassicValue(division.Children[0]), ClassicValue(division.Children[1]));
        var value = ClassicValue(source);
        if (Shape(value.Attributes) == Shape(expected)) return value;
        if (MaybeElement(expected) is { } item && Shape(value.Attributes) == Shape(item))
            return new ApplyValue<Unit, IrType<Unit>>(expected, new ConstructorValue<Unit, IrType<Unit>>(Types.Function(item, expected), new(Types.Sdk, IrPath.From("Maybe"), Name.From("Just"))), value);
        return ClassicFailure("The written value has no approved conversion to its declared result type.");
    }
    private V ClassicDefault(IrType<Unit> type)
    {
        if (MaybeElement(type) is not null) return Nothing(type);
        if (Shape(type) == Shape(Types.Int) || EnumValueType(type)) return new LiteralValue<Unit, IrType<Unit>>(type, new IntLiteral(0));
        if (Shape(type) == Shape(Types.Float)) return new LiteralValue<Unit, IrType<Unit>>(type, new FloatLiteral(0));
        if (Shape(type) == Shape(Types.Bool)) return new LiteralValue<Unit, IrType<Unit>>(type, new BoolLiteral(false));
        if (Shape(type) == Shape(Types.Reference("Char", "Char"))) return new LiteralValue<Unit, IrType<Unit>>(type, new CharLiteral("\0"));
        if (Shape(type) == Shape(Types.Decimal)) return ClassicValue(new LiteralValue<Unit, IrType<Unit>>(type, new DecimalLiteral("0.0")));
        return ClassicFailure("This default requires an approved Elm value mapping.");
    }
    private V ClassicMember(CSharpOperation<Unit, IrType<Unit>> member) => ClassicBoundMember(ClassicValue(member.Children.Single()),Property(member,"member"),member.Attributes,Property(member,"symbol"));
    private V ClassicBoundMember(V subject,string name,IrType<Unit> result,string symbol)
    {
        if (MaybeElement(subject.Attributes) is not null && name == "HasValue") return Sdk("Basics","notEqual",Types.Bool,subject,Nothing(subject.Attributes));
        if (Shape(subject.Attributes)==Shape(Types.String) && name=="Length")return Sdk("String","length",Types.Int,subject);
        if(declared.Values.OfType<IMethodSymbol>().FirstOrDefault(m=>m.MethodKind==MethodKind.PropertyGet && m.AssociatedSymbol is {} property && Id(property)==symbol) is {} getter)
            return ClassicFunction(identities[Id(getter)],subject);
        if(RecordShape(subject.Attributes) is not null)return ClassicField(new(result,subject,Name.From(name)));
        return ClassicFailure("This member needs an approved Elm function mapping.");
    }
    private V ClassicNullPattern(CSharpOperation<Unit, IrType<Unit>> pattern)
    {
        var subject = ClassicValue(pattern.Children[0]);
        if(MaybeElement(subject.Attributes) is null)subject=ClassicExpected(subject,Types.Maybe(subject.Attributes));
        var test = pattern.Children[1]; var negate = false;
        if (test is CSharpOperation<Unit, IrType<Unit>> { Kind:"NotPattern", Children.Count:1 } not) { negate=true;test=not.Children[0]; }
        if (MaybeElement(subject.Attributes) is not null && test is CSharpOperation<Unit, IrType<Unit>> {Kind:"ConstantPattern",Children.Count:1} constant && constant.Children[0] is CSharpOperation<Unit, IrType<Unit>> {Kind:"Null"})
            return Sdk("Basics",negate?"notEqual":"equal",Types.Bool,subject,Nothing(subject.Attributes));
        return ClassicFailure("This pattern retains its later delivery gate.");
    }
    private V? classicConditionalSubject;
    private V ClassicConditionalMember(CSharpOperation<Unit,IrType<Unit>> binding) => classicConditionalSubject is {} subject
        ? ClassicBoundMember(subject,Property(binding,"member"),binding.Attributes,Property(binding,"symbol")) : ClassicFailure("A conditional member has no receiver.");
    private V ClassicConditionalAccess(CSharpOperation<Unit, IrType<Unit>> access)
    {
        var subject = ClassicValue(access.Children[0]);
        if(MaybeElement(subject.Attributes) is null)subject=ClassicExpected(subject,Types.Maybe(subject.Attributes));
        var element = MaybeElement(subject.Attributes);
        if (element is null || MaybeElement(access.Attributes) is not { } result)
            return ClassicFailure("This conditional access needs another approved mapping.");
        var name = Name.From("item");var index=0;
        while(classicBindings.Contains(name.Key))name=Name.From("item"+(++index).ToString(System.Globalization.CultureInfo.InvariantCulture));
        var previous=classicConditionalSubject;classicConditionalSubject=new VariableValue<Unit,IrType<Unit>>(element,name);classicBindings.Add(name.Key);
        V body;
        try { body=ClassicValue(access.Children[1]); }
        finally {classicBindings.Remove(name.Key);classicConditionalSubject=previous;}
        if(Shape(body.Attributes)!=Shape(result))return ClassicFailure("A nullable member result needs an approved flattening mapping.");
        var lambda = new LambdaValue<Unit, IrType<Unit>>(Types.Function(element,result),new AsPattern<IrType<Unit>>(element,new WildcardPattern<IrType<Unit>>(element),name),body);
        return Sdk("Maybe","map",access.Attributes,lambda,subject);
    }
    private V ClassicNullableBinary(CSharpOperation<Unit, IrType<Unit>> binary)
    {
        var op = Property(binary,"operator");
        if (op=="??")
        {
            var left=binary.Children[0] is CSharpOperation<Unit,IrType<Unit>> {Kind:"Null"}?Nothing(Types.Maybe(binary.Attributes)):ClassicValue(binary.Children[0]);
            if(MaybeElement(left.Attributes) is null)left=ClassicExpected(left,Types.Maybe(left.Attributes));
            var element=MaybeElement(left.Attributes)!;
            return Sdk("Maybe","withDefault",element,ClassicExpected(binary.Children[1],element),left);
        }
        if(op is "==" or "!=" && binary.Children.Any(c=>c is CSharpOperation<Unit, IrType<Unit>> {Kind:"Null"}))
        {
            var typed=binary.Children.First(c=>c is not CSharpOperation<Unit, IrType<Unit>> {Kind:"Null"});var value=ClassicValue(typed);
            if(MaybeElement(value.Attributes) is null)value=ClassicExpected(value,Types.Maybe(value.Attributes));
            return Sdk("Basics",op=="=="?"equal":"notEqual",Types.Bool,binary.Children[0]==typed?value:Nothing(value.Attributes),binary.Children[1]==typed?value:Nothing(value.Attributes));
        }
        var a=ClassicValue(binary.Children[0]);var b=ClassicValue(binary.Children[1]);var inner=MaybeElement(a.Attributes);var other=MaybeElement(b.Attributes);
        if(inner is not null&&other is null&&Shape(inner)==Shape(b.Attributes)){b=ClassicExpected(b,Types.Maybe(inner));other=inner;}
        if(other is not null&&inner is null&&Shape(other)==Shape(a.Attributes)){a=ClassicExpected(a,Types.Maybe(other));inner=other;}
        if(inner is null||other is null||Shape(inner)!=Shape(other)||MaybeElement(binary.Attributes) is not {} output)return ClassicFailure("This lifted operator needs another approved Maybe mapping.");
        var module=Shape(inner)==Shape(Types.Decimal)?"Decimal":"Basics";
        var name=op switch {"+"=>"add","-"=>module=="Decimal"?"sub":"subtract","*"=>module=="Decimal"?"mul":"multiply","/" when module=="Basics"=>Shape(inner)==Shape(Types.Int)?"integerDivide":"divide",_=>null};
        if(name is null)return ClassicFailure("This lifted operator has no approved SDK signature.");
        V function=new ReferenceValue<Unit, IrType<Unit>>(Types.Function(inner,Types.Function(other,output)),new(Types.Sdk,IrPath.From(module),Name.From(name)));
        return Sdk("Maybe","map2",binary.Attributes,function,a,b);
    }
}
