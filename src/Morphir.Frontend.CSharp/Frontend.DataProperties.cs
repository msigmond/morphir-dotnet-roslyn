using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Morphir.IR.Classic;
using TypedValue = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        private readonly Dictionary<string, IMethodSymbol> dataGetters = [];
        private static bool DataPropertyStorage(IPropertySymbol property) => !property.IsAbstract && property.ContainingType.TypeKind != TypeKind.Interface && (PositionalDataProperty(property) || AutoProperty(property) || property.ContainingType.GetMembers().OfType<IFieldSymbol>().Any(field => SymbolEqualityComparer.Default.Equals(field.AssociatedSymbol, property)));
        private static bool DataPropertyDeclaration(IPropertySymbol property) => property.RefKind == RefKind.None && property.GetAttributes().Length == 0 && property.DeclaringSyntaxReferences.All(reference => reference.GetSyntax() is PropertyDeclarationSyntax { AttributeLists.Count: 0 } or IndexerDeclarationSyntax { AttributeLists.Count: 0 } || PositionalDataProperty(property)) && (!property.IsStatic || !DataPropertyStorage(property));
        private static ISymbol DataStorageSymbol(ISymbol member) => member is IFieldSymbol { AssociatedSymbol: IPropertySymbol property } ? property : member;
        private void RegisterDataGetters(IEnumerable<INamedTypeSymbol> selected)
        {
            foreach (var type in selected.Where(type => DataType(type) || type.IsStatic || type.TypeKind == TypeKind.Class && ValueInterfaceType(type))) foreach (var property in type.GetMembers().OfType<IPropertySymbol>().Where(property => AccessorBody(property.GetMethod) && DataPropertyDeclaration(property)))
                    if (property.GetMethod is { } getter) { if (!identities.ContainsKey(SymbolId(getter))) { AddSymbol(getter, SourceOwnerName(type), "Get" + property.MetadataName + SignatureSuffix(getter)); RegisterLocalMethods(getter); } dataGetters[SymbolId(getter)] = getter; }
        }
        private static IOperation CallableOperation(SemanticModel model, SyntaxNode syntax)
        {
            var operation = model.GetOperation(syntax) ?? throw new InvalidOperationException("Resolved callable has no body operation.");
            while (operation.Parent is not null) operation = operation.Parent;
            return operation;
        }
        private static SyntaxNode CallableFlowSyntax(SyntaxNode syntax) => syntax switch
        {
            BaseMethodDeclarationSyntax method => (SyntaxNode?)method.Body ?? method.ExpressionBody!.Expression,
            LocalFunctionStatementSyntax local => (SyntaxNode?)local.Body ?? local.ExpressionBody!.Expression,
            AccessorDeclarationSyntax accessor => (SyntaxNode?)accessor.Body ?? accessor.ExpressionBody!.Expression,
            ArrowExpressionClauseSyntax arrow => arrow.Expression,
            _ => syntax
        };
        private bool DataGetter(IPropertyReferenceOperation operation) => operation.Property.GetMethod is { } method && dataGetters.ContainsKey(SymbolId(method));
        private TypedValue GetDataProperty(IPropertyReferenceOperation operation)
        {
            var method = operation.Property.GetMethod!; var location = operation.Syntax.GetLocation(); var output = CallableOutput(method, location);
            var arguments = operation.Arguments.OrderBy(argument => argument.Parameter!.Ordinal).Select(argument => Adapt(DataArgument(Lower(argument.Value), location), Type(argument.Parameter!.Type, location))).ToList();
            if (ReceiverRequired(method))
            {
                var receiver = InterfaceDispatchMethod(method) ? InterfaceView(operation.Instance!.Type!, method, Lower(operation.Instance), location) : Lower(operation.Instance!);
                arguments.Insert(0, DemandPresent(DataArgument(receiver, location), location));
            }
            arguments.InsertRange(0, DefaultWitnessArguments(method, location));
            BorrowUnknownDataResult(method.ReturnType, arguments); calls[currentMethod].Add((SymbolId(method), location));
            IrType<Unit> signature = output; foreach (var argument in arguments.AsEnumerable().Reverse()) signature = Types.Function(argument.Attributes, signature);
            TypedValue result = new ReferenceValue<Unit, IrType<Unit>>(signature, Identity(method)); foreach (var argument in arguments) { signature = ((FunctionType<Unit>)signature).Result; result = new ApplyValue<Unit, IrType<Unit>>(signature, result, argument); }
            return result;
        }
    }
}
