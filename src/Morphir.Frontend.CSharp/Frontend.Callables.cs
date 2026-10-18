using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Morphir.IR.Classic;
using IrPath = Morphir.IR.Classic.Path;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        private static string SignatureType(ITypeSymbol type) => type switch
        {
            IArrayTypeSymbol array => "ArrayRank" + array.Rank + "_" + (array.IsSZArray ? "Vector" : "NonVector") + "_Of_" + SignatureType(array.ElementType),
            ITypeParameterSymbol parameter => "Parameter_" + parameter.TypeParameterKind + "_" + SignatureParameterDepth(parameter) + "_" + parameter.Ordinal,
            INamedTypeSymbol { IsTupleType: true, TupleUnderlyingType: not null } tuple => SignatureType(tuple.TupleUnderlyingType),
            INamedTypeSymbol { Arity: > 0 } named => "Generic_" + (named.ContainingType is null ? named.ContainingNamespace.ToDisplayString() : SignatureType(named.ContainingType)) + "_" + named.MetadataName + "_Of_" + string.Join("_And_", named.TypeArguments.Select(SignatureType)),
            INamedTypeSymbol named => named.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.None)),
            _ => type.TypeKind + "_" + type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.None))
        };
        private static int SignatureParameterDepth(ITypeParameterSymbol parameter)
        {
            var depth = 0; for (var parent = parameter.ContainingType.ContainingType; parent is not null; parent = parent.ContainingType) depth++; return depth;
        }
        private static string SignatureSuffix(IMethodSymbol method) => "__" + method.Arity + "__" + string.Join("__", method.Parameters.Select(parameter => parameter.RefKind + "_" + SignatureType(parameter.Type)));
        private static bool SourceMethod(IMethodSymbol method) => method.MethodKind is MethodKind.Ordinary or MethodKind.ExplicitInterfaceImplementation;
        private static bool ReceiverRequired(IMethodSymbol method) => method.MethodKind is MethodKind.Ordinary or MethodKind.ExplicitInterfaceImplementation or MethodKind.PropertyGet or MethodKind.PropertySet && !method.IsStatic;
        private IEnumerable<IMethodSymbol> LocalMethods(IMethodSymbol method)
        {
            var syntax = method.DeclaringSyntaxReferences.Single().GetSyntax();
            return syntax.DescendantNodes().OfType<LocalFunctionStatementSyntax>().Select(node => (IMethodSymbol)Semantic(node.SyntaxTree).GetDeclaredSymbol(node)!);
        }
        private void RegisterLocalMethods(IMethodSymbol method)
        {
            foreach (var local in LocalMethods(method))
            {
                var parent = (IMethodSymbol)local.ContainingSymbol; var parentIdentity = Identity(parent);
                var declaration = (LocalFunctionStatementSyntax)local.DeclaringSyntaxReferences.Single().GetSyntax();
                var parentSyntax = parent.DeclaringSyntaxReferences.Single().GetSyntax();
                var blocks = parentSyntax.DescendantNodes().OfType<BlockSyntax>().Where(block => block.Ancestors().First(node => node is BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax or AccessorDeclarationSyntax or PropertyDeclarationSyntax or IndexerDeclarationSyntax).Span == parentSyntax.Span).ToArray();
                var scope = declaration.Ancestors().OfType<BlockSyntax>().First(); var index = Array.FindIndex(blocks, block => block.Span == scope.Span);
                var segments = parentIdentity.Module.Segments.Append(parentIdentity.Local);
                if (index > 0) segments = segments.Append(Name.From("scope" + index));
                AddSymbol(local, new FQName(package, new IrPath(segments.ToArray()), Name.From(local.Name + SignatureSuffix(local))));
            }
        }
        private ControlFlowGraph CallableGraph(IMethodSymbol method)
        {
            if (method.MethodKind == MethodKind.LocalFunction) return CallableGraph((IMethodSymbol)method.ContainingSymbol).GetLocalFunctionControlFlowGraph(method);
            var syntax = method.DeclaringSyntaxReferences.Single().GetSyntax();
            return CallableOperation(Semantic(syntax.SyntaxTree), syntax) switch { IBlockOperation block => ControlFlowGraph.Create(block), IConstructorBodyOperation constructor => ControlFlowGraph.Create(constructor), IMethodBodyOperation body => ControlFlowGraph.Create(body), _ => Fail<ControlFlowGraph>("MCS3001", "Callable graph requires a supported method/constructor body.", syntax.GetLocation()) };
        }
        private static IEnumerable<IOperation> LocalBodyOperations(IOperation root)
        {
            yield return root;
            foreach (var child in root.ChildOperations.Where(child => child is not ILocalFunctionOperation)) foreach (var operation in LocalBodyOperations(child)) yield return operation;
        }
        private void ValidateLocalCaptures(IMethodSymbol method, IBlockOperation body)
        {
            foreach (var operation in LocalBodyOperations(body))
            {
                var owner = operation switch { IParameterReferenceOperation parameter => parameter.Parameter.ContainingSymbol, ILocalReferenceOperation local => local.Local.ContainingSymbol, _ => null };
                if (owner is not null && !SymbolEqualityComparer.Default.Equals(owner, method) || operation is IInstanceReferenceOperation { ReferenceKind: InstanceReferenceKind.ContainingTypeInstance })
                    Fail<object>("MCS2802", "Captured state requires the D13 closure proof contract; D7 local functions must be noncapturing.", operation.Syntax.GetLocation());
            }
        }
    }
}
