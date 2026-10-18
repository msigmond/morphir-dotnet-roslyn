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
        private static EqualsValueClauseSyntax? DataInitializerSyntax(ISymbol symbol) => symbol.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax()).Select(syntax => syntax switch { VariableDeclaratorSyntax variable => variable.Initializer, PropertyDeclarationSyntax property => property.Initializer, _ => null }).FirstOrDefault(initializer => initializer is not null);
        private static bool HasDataInitializers(INamedTypeSymbol type) => type.GetMembers().Any(member => (member is IFieldSymbol { IsStatic: false } or IPropertySymbol { IsStatic: false }) && DataInitializerSyntax(member) is not null);
        private IEnumerable<(DataSlot Slot, IOperation Value)> ConstructorInitializers(IMethodSymbol constructor) => DataInitializers(ConstructorLayout(constructor.ContainingType)).Where(entry => SymbolId(entry.Slot.Symbol.ContainingType) == SymbolId(constructor.ContainingType));
        private IEnumerable<(DataSlot Slot, IOperation Value)> DataInitializers(DataLayout data)
        {
            var trees = compilations.SelectMany(compilation => compilation.SyntaxTrees).Distinct().Select((tree, index) => (tree, index)).ToDictionary(item => item.tree, item => item.index);
            foreach (var item in data.Slots.Select(slot => (Slot: slot, Syntax: DataInitializerSyntax(slot.Symbol))).Where(item => item.Syntax is not null).OrderBy(item => trees[item.Syntax!.SyntaxTree]).ThenBy(item => item.Syntax!.SpanStart))
            {
                var operation = Semantic(item.Syntax!.SyntaxTree).GetOperation(item.Syntax);
                yield return (item.Slot, operation switch { IFieldInitializerOperation field => field.Value, IPropertyInitializerOperation property => property.Value, _ => Fail<IOperation>("MCS3001", "Initializer requires its resolved field/property operation.", item.Syntax.GetLocation()) });
            }
        }
        private TypedValue RunDataInitializers(IMethodSymbol constructor, DataOrigin origin, BindingEnvironment environment, Func<BindingEnvironment, TypedValue> continuation)
        {
            var entries = ConstructorInitializers(constructor).ToArray();
            TypedValue Initialize(int index, BindingEnvironment state)
            {
                if (index == entries.Length) return continuation(state);
                var (slot, value) = entries[index]; var location = value.Syntax.GetLocation(); ValidateBlockOperations(value);
                return Expression(value, state, (result, next) => { WriteDataField(origin, slot.Symbol, Adapt(result, Type(slot.Type, location)), next, location); var body = Initialize(index + 1, next); ValidateUnreachedArithmetic(value); return body; });
            }
            return Initialize(0, environment);
        }
    }
}
