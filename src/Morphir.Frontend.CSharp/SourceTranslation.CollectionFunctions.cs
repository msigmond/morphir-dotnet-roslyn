using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Morphir.IR.Classic;
using V = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

internal sealed partial class SourceTranslation
{
    private V? CollectionCombinator(InvocationExpressionSyntax syntax, IMethodSymbol method, IrType<Unit> output)
    {
        if (SourceSymbol(method) || external.ContainsKey(Id(method))) return null;
        var original = method.ReducedFrom ?? method;
        var enumerable = original.ContainingType.ToDisplayString() == "System.Linq.Enumerable";
        var list = original.ContainingType.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.List<T>";
        if (!enumerable && !(list && method.Name is "FindAll" or "ConvertAll")) return null;
        var parameters = original.Parameters;
        var name = (enumerable, method.Name, parameters.Length) switch
        {
            (true, "Where", 2) or (false, "FindAll", 1) => "filter",
            (true, "Select", 2) or (false, "ConvertAll", 1) => "map",
            (true, "Aggregate", 3) => "foldl",
            (true, "ToList" or "ToArray", 1) => "copy",
            _ => null
        };
        if (name is null || name is "filter" or "map" && parameters[^1].Type is INamedTypeSymbol callback && callback.DelegateInvokeMethod?.Parameters.Length != 1)
            return Fail<V>("MCS3101", "This collection overload has no approved callback mapping.", syntax.GetLocation());
        var operation = (IInvocationOperation)Semantic(syntax).GetOperation(syntax)!;
        V Argument(string parameter)
        {
            var bound = operation.Arguments.Single(a => a.Parameter!.Name == parameter);
            return Expression(bound.Syntax is ArgumentSyntax argument ? argument.Expression : (ExpressionSyntax)bound.Value.Syntax);
        }
        var subject = enumerable ? Argument(parameters[0].Name) : syntax.Expression is MemberAccessExpressionSyntax member
            ? Expression(member.Expression) : Source(Type(method.ContainingType), "ConditionalReceiver", []);
        if (CollectionModule(subject.Attributes) != "List")
            return Fail<V>("MCS3101", "These collection callbacks require an approved List carrier.", syntax.GetLocation());
        var children = new List<V> { subject };
        if (name == "foldl") children.Add(Argument(parameters[1].Name));
        if (name != "copy") children.Add(Argument(parameters[^1].Name));
        return Source(output, "CollectionCombinator", children, ("member", name));
    }

    private V ClassicCollectionCombinator(CSharpOperation<Unit, IrType<Unit>> source)
    {
        var subject = ClassicValue(source.Children[0]);
        if (CollectionModule(subject.Attributes) != "List") return ClassicFailure("Collection callbacks require a List receiver.");
        var member = Property(source, "member");
        if (member == "copy") return Shape(subject.Attributes) == Shape(source.Attributes) ? subject : ClassicFailure("Collection conversion needs the declared element type.");
        var element = ((ReferenceType<Unit>)subject.Attributes).Arguments[0];
        if (member != "foldl")
        {
            var result = member == "filter" ? Types.Bool : ((ReferenceType<Unit>)source.Attributes).Arguments[0];
            return Sdk("List", member, source.Attributes, ClassicExpected(source.Children[1], Types.Function(element, result)), subject);
        }
        var seed = ClassicExpected(source.Children[1], source.Attributes);
        var callback = ClassicExpected(source.Children[2], Types.Function(source.Attributes, Types.Function(element, source.Attributes)));
        Name Fresh(string basis)
        {
            var text = basis; var index = 0;
            while (classicBindings.Contains(Name.From(text).Key) || ContainsValue(source.Children[2], value => value is VariableValue<Unit, IrType<Unit>> variable && variable.Name.Key == Name.From(text).Key
                || value is ReferenceValue<Unit, IrType<Unit>> reference && reference.Name.Local.Key == Name.From(text).Key
                || value is CSharpOperation<Unit, IrType<Unit>> named && Property(named, "name") is { Length: > 0 } name && Name.From(name).Key == Name.From(text).Key)) text = basis + ++index;
            return Name.From(text);
        }
        var itemName = Fresh("item"); var stateName = Fresh("state");
        var item = new VariableValue<Unit, IrType<Unit>>(element, itemName);
        var state = new VariableValue<Unit, IrType<Unit>>(source.Attributes, stateName);
        V body = new ApplyValue<Unit, IrType<Unit>>(source.Attributes, new ApplyValue<Unit, IrType<Unit>>(Types.Function(element, source.Attributes), callback, state), item);
        body = new LambdaValue<Unit, IrType<Unit>>(Types.Function(source.Attributes, source.Attributes), new AsPattern<IrType<Unit>>(source.Attributes, new WildcardPattern<IrType<Unit>>(source.Attributes), stateName), body);
        body = new LambdaValue<Unit, IrType<Unit>>(Types.Function(element, body.Attributes), new AsPattern<IrType<Unit>>(element, new WildcardPattern<IrType<Unit>>(element), itemName), body);
        return Sdk("List", "foldl", source.Attributes, body, seed, subject);
    }
}
