using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Morphir.IR.Classic;
using V = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;
using P = Morphir.IR.Classic.Pattern<Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;
using IrPath = Morphir.IR.Classic.Path;

namespace Morphir.Frontend.CSharp;

internal sealed partial class SourceTranslation
{
    private static bool ExactElmIntegerLiteral(long value)
    {
        using var integer = JsonDocument.Parse(JsonSerializer.Serialize(value));
        using var number = JsonDocument.Parse(JsonSerializer.Serialize((double)value));
        return JsonElement.DeepEquals(integer.RootElement, number.RootElement);
    }
    private V PatternConstant(ExpressionSyntax syntax)
    {
        if (syntax is LiteralExpressionSyntax literal && !literal.IsKind(SyntaxKind.DefaultLiteralExpression)) return Expression(syntax);
        if (Semantic(syntax).GetSymbolInfo(syntax).Symbol is IFieldSymbol { ContainingType.TypeKind: TypeKind.Enum, HasConstantValue: true } field
            && syntax is IdentifierNameSyntax or MemberAccessExpressionSyntax)
        {
            RequireSelected(field, syntax.GetLocation());
            return DefaultConstant(field.ConstantValue, Type(field.Type, location: syntax.GetLocation()));
        }
        return Fail<V>("MCS3102", "This constant pattern has no direct pinned Elm literal mapping.", syntax.GetLocation());
    }
    private V Pattern(PatternSyntax syntax)
    {
        V core = syntax switch
        {
            ConstantPatternSyntax constant => Source(Types.Unit, "ConstantPattern", [PatternConstant(constant.Expression)]),
            DiscardPatternSyntax => Source(Types.Unit, "DiscardPattern", []),
            VarPatternSyntax variable => PatternBinding(variable.Designation),
            ParenthesizedPatternSyntax parenthesized => Pattern(parenthesized.Pattern),
            UnaryPatternSyntax unary => Source(Types.Unit, "NotPattern", [Pattern(unary.Pattern)]),
            RecursivePatternSyntax { Type: null, PropertyPatternClause: null, PositionalPatternClause: { } positional } =>
                Source(Types.Unit, "TuplePattern", positional.Subpatterns.Select(p => Pattern(p.Pattern)).ToArray()),
            ListPatternSyntax list => Source(Types.Unit, "ListPattern", list.Patterns.Select(Pattern).ToArray()),
            SlicePatternSyntax slice => Source(Types.Unit, "SlicePattern", slice.Pattern is null ? [] : [Pattern(slice.Pattern)]),
            _ => Fail<V>("MCS3102", "This pattern has no approved direct Elm case mapping.", syntax.GetLocation())
        };
        var designation = syntax switch { RecursivePatternSyntax r => r.Designation, ListPatternSyntax l => l.Designation, _ => null };
        return designation is null ? core : Source(Types.Unit, "NamedPattern", [core, PatternBinding(designation)]);
    }
    private static V PatternBinding(VariableDesignationSyntax designation) => designation switch
    {
        SingleVariableDesignationSyntax name => Source(Types.Unit, "BindingPattern", [], ("name", name.Identifier.ValueText)),
        DiscardDesignationSyntax => Source(Types.Unit, "DiscardPattern", []),
        _ => Source(Types.Unit, "UnmappedPattern", [])
    };
    private V SwitchExpression(SwitchExpressionSyntax syntax, IrType<Unit> type) => Source(type, "Switch",
        new[] { Expression(syntax.GoverningExpression) }.Concat(syntax.Arms.Select(arm =>
            arm.WhenClause is not null ? Fail<V>("MCS3102", "Switch guards have no approved Elm mapping.", arm.GetLocation())
            : Source(type, "MatchCase", [Pattern(arm.Pattern), Expression(arm.Expression)], ("body", "expression")))).ToArray());
    private V SwitchStatement(SwitchStatementSyntax syntax) => Source(currentOutput, "Switch",
        new[] { Expression(syntax.Expression) }.Concat(syntax.Sections.Select(section =>
        {
            if (section.Labels.Count != 1) return Fail<V>("MCS3102", "Grouped switch labels have no approved mapping.", section.GetLocation());
            var pattern = section.Labels[0] switch
            {
                DefaultSwitchLabelSyntax => Source(Types.Unit, "DiscardPattern", []),
                CaseSwitchLabelSyntax label => Source(Types.Unit, "ConstantPattern", [PatternConstant(label.Value)]),
                CasePatternSwitchLabelSyntax { WhenClause: null } label => Pattern(label.Pattern),
                _ => Fail<V>("MCS3102", "This switch label has no approved Elm mapping.", section.GetLocation())
            };
            return Source(currentOutput, "MatchCase", [pattern, Source(currentOutput, "Block", section.Statements.Select(Statement).ToArray())], ("body", "statement"));
        })).ToArray());

    private V ClassicSwitch(CSharpOperation<Unit, IrType<Unit>> source, IrType<Unit> output)
    {
        var subject = ClassicValue(source.Children[0]);
        var cases = new List<MatchCase<Unit, IrType<Unit>>>();
        foreach (var entry in source.Children.Skip(1).Cast<CSharpOperation<Unit, IrType<Unit>>>())
        {
            var added = new List<string>();
            try
            {
                var pattern = ClassicPattern(entry.Children[0], subject.Attributes, added);
                var body = Property(entry, "body") == "statement" ? ClassicValue(entry.Children[1]) : ClassicExpected(entry.Children[1], output);
                if (Shape(body.Attributes) != Shape(output)) return ClassicFailure("Switch branches require their declared result type.");
                cases.Add(new(pattern, body));
            }
            finally { foreach (var name in added) { classicBindings.Remove(name); classicBindingTypes.Remove(name); } }
        }
        return new MatchValue<Unit, IrType<Unit>>(output, subject, cases);
    }
    private P ClassicPattern(V value, IrType<Unit> type, List<string> added)
    {
        if (value is not CSharpOperation<Unit, IrType<Unit>> source) return Fail<P>("MCS3102", "Missing source pattern.", classicLocation);
        if (source.Kind == "DiscardPattern") return new WildcardPattern<IrType<Unit>>(type);
        if (source.Kind == "BindingPattern")
        {
            var name = Name.From(Property(source, "name"));
            if (!classicBindings.Add(name.Key)) return Fail<P>("MCS1004", "Pattern names collide after normalization.", classicLocation);
            classicBindingTypes[name.Key] = type; added.Add(name.Key);
            return new AsPattern<IrType<Unit>>(type, new WildcardPattern<IrType<Unit>>(type), name);
        }
        if (source.Kind == "NamedPattern")
        {
            var pattern = ClassicPattern(source.Children[0], type, added);
            var binding = ClassicPattern(source.Children[1], type, added);
            return binding is AsPattern<IrType<Unit>> name ? new AsPattern<IrType<Unit>>(type, pattern, name.Name) : pattern;
        }
        if (source.Kind == "ConstantPattern" && source.Children[0] is CSharpOperation<Unit, IrType<Unit>> { Kind: "Null" })
            return MaybeElement(type) is not null ? new ConstructorPattern<IrType<Unit>>(type, new(Types.Sdk, IrPath.From("Maybe"), Name.From("Nothing")), [])
                : Fail<P>("MCS3102", "A null switch pattern requires an explicit nullable type.", classicLocation);
        if (MaybeElement(type) is { } inner)
            return new ConstructorPattern<IrType<Unit>>(type, new(Types.Sdk, IrPath.From("Maybe"), Name.From("Just")), [ClassicPattern(value, inner, added)]);
        if (source.Kind == "ConstantPattern")
        {
            var literal = source.Children[0];
            if (literal is LiteralValue<Unit, IrType<Unit>> { Literal: not DecimalLiteral and not UnmappedNumberLiteral } constant
                && Shape(ScalarShape(type)) == Shape(ScalarShape(constant.Attributes)))
            {
                if (constant.Literal is IntLiteral number && (number.Value < 0 || !ExactElmIntegerLiteral(number.Value)))
                    return Fail<P>("MCS3102", "This whole-number pattern has no exact pinned Elm representation.", classicLocation);
                return new LiteralPattern<IrType<Unit>>(type, constant.Literal);
            }
            return Fail<P>("MCS3102", "This pattern literal needs another declared-type mapping.", classicLocation);
        }
        if (source.Kind == "TuplePattern" && type is TupleType<Unit> tuple && tuple.Elements.Count == source.Children.Count)
            return new TuplePattern<IrType<Unit>>(type, source.Children.Select((p, i) => ClassicPattern(p, tuple.Elements[i], added)).ToArray());
        if (source.Kind == "ListPattern" && CollectionModule(type) == "List")
        {
            var element = ((ReferenceType<Unit>)type).Arguments[0];
            P List(int index)
            {
                if (index == source.Children.Count) return new EmptyListPattern<IrType<Unit>>(type);
                if (source.Children[index] is CSharpOperation<Unit, IrType<Unit>> { Kind: "SlicePattern" } slice)
                {
                    if (index != source.Children.Count - 1) return Fail<P>("MCS3102", "Only a final list slice has an approved head/tail mapping.", classicLocation);
                    return slice.Children.Count == 0 ? new WildcardPattern<IrType<Unit>>(type) : ClassicPattern(slice.Children[0], type, added);
                }
                return new HeadTailPattern<IrType<Unit>>(type, ClassicPattern(source.Children[index], element, added), List(index + 1));
            }
            return List(0);
        }
        return Fail<P>("MCS3102", "This pattern has no approved direct Elm representation.", classicLocation);
    }
}
