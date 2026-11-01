using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Morphir.IR.Classic;
using IrPath = Morphir.IR.Classic.Path;
using V = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

internal sealed partial class SourceTranslation
{
    private void Emit(INamedTypeSymbol type)
    {
        currentType = type;
        var identity = identities[Id(type)];
        var owner = GetModule(identity.Module, type.ContainingType is null ? Access.Public : Visibility(type.ContainingType));
        var members = GetModule(Owner(SourceOwner(type)), Visibility(type));
        if (!type.IsStatic)
        {
            IrType<Unit> representation;
            if (type.TypeKind == TypeKind.Enum) representation = Type(type.EnumUnderlyingType!);
            else
            {
                var slots = type.GetMembers().Where(m => m is IFieldSymbol { IsStatic: false, IsImplicitlyDeclared: false } || m is IPropertySymbol { IsStatic: false, IsIndexer: false } p && StoredProperty(p)).ToArray();
                var fields = new List<Entry<Name, IrType<Unit>>>();
                foreach (var slot in slots)
                {
                    var t = slot is IFieldSymbol f ? f.Type : ((IPropertySymbol)slot).Type;
                    CheckArray(t, slot);
                    foreach (var prior in slots.Where(s => !SymbolEqualityComparer.Default.Equals(s, slot) && Name.From(s.Name).Key == Name.From(slot.Name).Key))
                        Error("MCS1004", $"Data member names collide: {slot.Name} and {prior.Name}.", Location(slot));
                    fields.Add(new(Name.From(slot.Name), Type(t, DeclaredType(slot), Location(slot))));
                }
                if (!type.IsRecord)
                    foreach(var parameter in type.InstanceConstructors.Where(c=>c.DeclaringSyntaxReferences.Any(r=>r.GetSyntax() is TypeDeclarationSyntax)).SelectMany(c=>c.Parameters))
                    {
                        var name=Name.From(parameter.Name);
                        if(fields.Any(f=>f.Key.Key==name.Key))Fail<object>("MCS1004","A primary field collides after normalization.",Location(parameter));
                        fields.Add(new(name,Type(parameter.Type,DeclaredType(parameter),Location(parameter))));
                    }
                representation = new RecordType<Unit>(default, fields);
            }
            owner.Types.Add(new(identity.Local, new(Visibility(type), new(Documentation(type), new AliasDefinition<Unit>(TypeParameters(type).Select(p => Name.From(p.Name)).ToArray(), representation)))));
        }
        foreach (var symbol in type.GetMembers())
        {
            if (symbol is IMethodSymbol method && !method.IsImplicitlyDeclared && identities.ContainsKey(Id(method))) Attempt(() => EmitMethod(method, members));
            if (symbol is IFieldSymbol field && !field.IsImplicitlyDeclared && identities.ContainsKey(Id(field))) Attempt(() => EmitField(field, members));
            if (symbol is IPropertySymbol { IsImplicitlyDeclared: false } property)
            {
                CheckArray(property.Type, property); Type(property.Type, DeclaredType(property), Location(property));
                foreach (var reference in property.DeclaringSyntaxReferences)
                    if (reference.GetSyntax() is PropertyDeclarationSyntax { Initializer: not null } syntax) Expression(syntax.Initializer.Value);
            }
            if (symbol is IFieldSymbol { IsImplicitlyDeclared: false } instance && !instance.IsStatic)
            {
                CheckArray(instance.Type, instance); Type(instance.Type, DeclaredType(instance), Location(instance));
                foreach (var reference in instance.DeclaringSyntaxReferences)
                    if (reference.GetSyntax() is VariableDeclaratorSyntax { Initializer: not null } syntax) Expression(syntax.Initializer.Value);
            }
        }
    }
    private static bool StoredProperty(IPropertySymbol property) => property.DeclaringSyntaxReferences.Any(r => r.GetSyntax() is ParameterSyntax || r.GetSyntax() is PropertyDeclarationSyntax { ExpressionBody: null, AccessorList: not null } p && p.AccessorList.Accessors.All(a => a.Body is null && a.ExpressionBody is null));
    private void EmitField(IFieldSymbol field, Module module)
    {
        var syntax = field.DeclaringSyntaxReferences.Single().GetSyntax();
        var initializer = syntax switch { VariableDeclaratorSyntax variable => variable.Initializer?.Value, EnumMemberDeclarationSyntax member => member.EqualsValue?.Value, _ => null };
        var type = Type(field.Type, DeclaredType(field), Location(field));
        V body = field.ContainingType.TypeKind == TypeKind.Enum ? DefaultConstant(field.ConstantValue, type) : initializer is null ? Source(type, field.ContainingType.TypeKind == TypeKind.Enum ? "EnumMember" : "Storage", [], ("name", field.Name)) : Expression(initializer);
        module.Values.Add(new(identities[Id(field)].Local, new(Visibility(field), new(Documentation(field), new([], type, body)))));
    }
    private void EmitMethod(IMethodSymbol method, Module module)
    {
        var syntax = method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
        if (syntax is null) return;
        if (method.Parameters.GroupBy(p => Name.From(p.Name).Key).Any(g => g.Count() > 1)) Fail<object>("MCS1004", "Parameter names collide after normalization.", Location(method));
        foreach (var parameter in method.Parameters)
        {
            CheckArray(parameter.Type, parameter);
            if (parameter.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is ParameterSyntax { Default: not null } p) Expression(p.Default.Value);
        }
        CheckArray(method.ReturnType, method);
        var output = method.MethodKind == MethodKind.Constructor ? Type(method.ContainingType) : Type(method.ReturnType, method.MethodKind == MethodKind.PropertyGet ? DeclaredType(method.AssociatedSymbol!) : DeclaredType(method), Location(method));
        currentOutput = output;
        inferredLocalTypes.Clear();
        currentReceiver = Receiver(method);
        var inputs = method.Parameters.Select(p =>
        {
            var type = Type(p.Type, DeclaredType(p), Location(p));
            return new Input<Unit, IrType<Unit>>(Name.From(p.Name), type, type);
        }).ToList();
        if (!method.IsStatic && method.MethodKind != MethodKind.Constructor)
        {
            var receiver = Type(method.ContainingType);
            inputs.Insert(0, new(currentReceiver, receiver, receiver));
        }
        V body = syntax switch
        {
            BaseMethodDeclarationSyntax { Body: not null } declaration => Statement(declaration.Body),
            BaseMethodDeclarationSyntax { ExpressionBody: not null } declaration => Expression(declaration.ExpressionBody.Expression),
            AccessorDeclarationSyntax { Body: not null } accessor => Statement(accessor.Body),
            AccessorDeclarationSyntax { ExpressionBody: not null } accessor => Expression(accessor.ExpressionBody.Expression),
            ArrowExpressionClauseSyntax arrow => Expression(arrow.Expression),
            TypeDeclarationSyntax => Source(output, "PrimaryConstructor", []),
            _ => Source(output, "Declaration", [], ("kind", syntax.Kind().ToString()))
        };
        if (syntax is ConstructorDeclarationSyntax { Initializer: not null } ctor)
            body = Source(output, "Constructor", [Source(Types.Unit, "ConstructorInitializer", ctor.Initializer.ArgumentList.Arguments.Select(Argument).ToArray(), ("kind", ctor.Initializer.ThisOrBaseKeyword.Text)), body]);
        module.Values.Add(new(identities[Id(method)].Local, new(Visibility(method), new(Documentation(method.AssociatedSymbol ?? method), new(inputs, output, body)))));
    }
    private static Name Receiver(IMethodSymbol method)
    {
        var used = method.Parameters.Select(p => Name.From(p.Name).Key).ToHashSet(); var name = Name.From("this"); int version = 0;
        while (used.Contains(name.Key)) name = Name.From("this" + ++version);
        return name;
    }
    private V Statement(StatementSyntax syntax) => syntax switch
    {
        BlockSyntax block => Source(currentOutput, "Block", block.Statements.Select(Statement).ToArray()),
        LocalDeclarationStatementSyntax local => Source(Types.Unit, "LocalDeclaration", [Variables(local.Declaration)], ("modifiers", local.Modifiers.ToString())),
        ReturnStatementSyntax statement => Source(currentOutput, "Return", statement.Expression is null ? [] : [Expression(statement.Expression)]),
        ExpressionStatementSyntax statement => Source(Types.Unit, "ExpressionStatement", [Expression(statement.Expression)]),
        EmptyStatementSyntax => Source(Types.Unit, "EmptyStatement", []),
        IfStatementSyntax statement => Source(Types.Unit, "If", statement.Else is null ? [Expression(statement.Condition), Statement(statement.Statement)] : [Expression(statement.Condition), Statement(statement.Statement), Statement(statement.Else.Statement)]),
        CheckedStatementSyntax statement => Source(Types.Unit, statement.Keyword.Text, [Statement(statement.Block)]),
        LocalFunctionStatementSyntax local => LocalFunction(local),
        _ => Fail<V>("MCS2403", $"Statement {syntax.Kind()} has no delivered source mapping.", syntax.GetLocation())
    };
    private V Variables(VariableDeclarationSyntax syntax)
    {
        var values = syntax.Variables.Select(variable =>
        {
            var symbol = Semantic(variable).GetDeclaredSymbol(variable) as ILocalSymbol;
            if (symbol is not null && ContainsArray(symbol.Type) && !paramsArrays.Contains(symbol.Type)) Fail<object>("MCS3101", "General array locals require D11.", variable.GetLocation());
            var initializer=variable.Initializer is null?null:Expression(variable.Initializer.Value);
            var type = syntax.Type.IsVar && initializer is not null ? initializer.Attributes : symbol is null ? Types.Unit : Type(symbol.Type, syntax.Type, variable.GetLocation());
            if(symbol is not null)inferredLocalTypes[symbol]=type;
            return Source(type, "VariableDeclaration", initializer is null ? [] : [initializer], ("name", variable.Identifier.Text), ("type", syntax.Type.ToString()));
        }).ToArray();
        return Source(Types.Unit, "Variables", values);
    }
    private V LocalFunction(LocalFunctionStatementSyntax syntax)
    {
        var method = (IMethodSymbol)Semantic(syntax).GetDeclaredSymbol(syntax)!;
        if (method.Arity != 0) genericLocalDefinitions[Id(method)] = method;
        var saved = currentOutput; currentOutput = Type(method.ReturnType, syntax.ReturnType);
        try
        {
            var parameters = syntax.ParameterList.Parameters.Select(p =>
            {
                var symbol = (IParameterSymbol)Semantic(p).GetDeclaredSymbol(p)!;
                CheckArray(symbol.Type, symbol);
                if(symbol.IsParams && symbol.Type is IArrayTypeSymbol)paramsArrays.Add(symbol.Type);
                return Source(Type(symbol.Type, p.Type), "Parameter", p.Default is null ? [] : [Expression(p.Default.Value)], ("name", p.Identifier.Text), ("modifiers", p.Modifiers.ToString()));
            }).ToList();
            parameters.Add(syntax.Body is not null ? Statement(syntax.Body) : syntax.ExpressionBody is not null ? Expression(syntax.ExpressionBody.Expression) : Source(currentOutput, "Declaration", []));
            return Source(currentOutput, "LocalFunction", parameters, ("name", syntax.Identifier.Text), ("symbol", Id(method)));
        }
        finally { currentOutput = saved; }
    }
}
