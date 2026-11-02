using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Morphir.IR.Classic;
using V = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

internal sealed partial class SourceTranslation
{
    private V AnonymousFunction(AnonymousFunctionExpressionSyntax syntax)
    {
        var method = ((IAnonymousFunctionOperation)Semantic(syntax).GetOperation(syntax)!).Symbol;
        var output = Type(method.ReturnType, location: syntax.GetLocation());
        var saved = currentOutput; currentOutput = output;
        try
        {
            var parameters = method.Parameters.Select(parameter =>
            {
                var declaration = parameter.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as ParameterSyntax;
                if (parameter.IsOptional || parameter.IsParams) return Fail<V>("MCS3103", "Optional and params callback parameters need another mapping.", syntax.GetLocation());
                return Source(Type(parameter.Type, declaration?.Type), "Parameter", [], ("name", parameter.Name));
            }).ToList();
            var body = syntax.Body is BlockSyntax block ? Statement(block) : Expression((ExpressionSyntax)syntax.Body);
            IrType<Unit> signature = output;
            foreach (var parameter in parameters.AsEnumerable().Reverse()) signature = Types.Function(parameter.Attributes, signature);
            parameters.Add(body);
            return Source(signature, "AnonymousFunction", parameters);
        }
        finally { currentOutput = saved; }
    }

    private V ClassicAnonymousFunction(CSharpOperation<Unit, IrType<Unit>> source)
    {
        var inputs = LocalFunctionInputs(source);
        if (inputs.Select(i => i.Name.Key).Distinct().Count() != inputs.Length || inputs.Any(i => classicBindings.Contains(i.Name.Key)))
            return ClassicFailure("Elm does not permit this callback parameter shadowing or name collision.");
        foreach (var input in inputs) { classicBindings.Add(input.Name.Key); classicBindingTypes.Add(input.Name.Key, input.Type); }
        try
        {
            var output = source.Attributes;
            foreach (var input in inputs) output = ((FunctionType<Unit>)output).Result;
            var body = source.Children[^1] is CSharpOperation<Unit, IrType<Unit>> { Kind: "Block" } block
                ? ClassicBlock(block) : ClassicExpected(source.Children[^1], output);
            foreach (var input in inputs.Reverse())
                body = new LambdaValue<Unit, IrType<Unit>>(Types.Function(input.Type, body.Attributes), new AsPattern<IrType<Unit>>(input.Type, new WildcardPattern<IrType<Unit>>(input.Type), input.Name), body);
            return body;
        }
        finally { foreach (var input in inputs) { classicBindings.Remove(input.Name.Key); classicBindingTypes.Remove(input.Name.Key); } }
    }
}
