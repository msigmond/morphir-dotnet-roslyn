using System.Globalization;
using System.Numerics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Morphir.IR.Classic;
using IrPath = Morphir.IR.Classic.Path;
using V = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

internal sealed partial class SourceTranslation
{
    private ITypeSymbol? ExpressionSymbolType(ExpressionSyntax syntax)
    {
        var model = Semantic(syntax); var symbol = model.GetSymbolInfo(syntax).Symbol;
        return symbol switch { IFieldSymbol field => field.Type, IPropertySymbol property => property.Type, IParameterSymbol parameter => parameter.Type, ILocalSymbol local => local.Type,
            IMethodSymbol { MethodKind: MethodKind.Constructor } constructor => constructor.ContainingType,
            IMethodSymbol method => syntax is InvocationExpressionSyntax ? method.ReturnType : model.GetTypeInfo(syntax).Type ?? method.ReturnType, _ => model.GetTypeInfo(syntax).Type ?? model.GetTypeInfo(syntax).ConvertedType };
    }
    private IrType<Unit> ExpressionType(ExpressionSyntax syntax)
    {
        var symbol = Semantic(syntax).GetSymbolInfo(syntax).Symbol;
        if(symbol is ILocalSymbol && inferredLocalTypes.TryGetValue(symbol,out var localType))return localType;
        if(syntax is TupleExpressionSyntax tuple)return new TupleType<Unit>(default,tuple.Arguments.Select(a=>ExpressionType(a.Expression)).ToArray());
        if(syntax is ConditionalExpressionSyntax conditional)
        {
            var left=ExpressionType(conditional.WhenTrue);var right=ExpressionType(conditional.WhenFalse);
            if(MaybeElement(left) is not null)return left;
            if(MaybeElement(right) is not null)return right;
            if(conditional.WhenTrue.IsKind(SyntaxKind.NullLiteralExpression))return Types.Maybe(right);
            if(conditional.WhenFalse.IsKind(SyntaxKind.NullLiteralExpression))return Types.Maybe(left);
        }
        var type = ExpressionSymbolType(syntax);
        if(syntax is ConditionalAccessExpressionSyntax && type is not null)
        {
            var result=Type(type,location:syntax.GetLocation(),annotations:false);
            return MaybeElement(result) is not null?result:Types.Maybe(result);
        }
        // A null token has no own source type. Do not adopt Roslyn's hidden object conversion for a null test.
        if (syntax.IsKind(SyntaxKind.NullLiteralExpression) && type?.SpecialType == SpecialType.System_Object)
        {
            var context = syntax.Parent switch
            {
                BinaryExpressionSyntax binary => ExpressionSymbolType(binary.Left == syntax ? binary.Right : binary.Left),
                ConstantPatternSyntax { Parent: IsPatternExpressionSyntax pattern } => ExpressionSymbolType(pattern.Expression),
                ConstantPatternSyntax { Parent: UnaryPatternSyntax { Parent: IsPatternExpressionSyntax pattern } } => ExpressionSymbolType(pattern.Expression),
                _ => null
            };
            return context is null ? currentOutput : Type(context, location: syntax.GetLocation());
        }
        var declaredType = symbol is IFieldSymbol or IPropertySymbol or IParameterSymbol or ILocalSymbol or IMethodSymbol ? DeclaredType(symbol) : syntax is CastExpressionSyntax cast ? cast.Type : null;
        return type is null ? syntax.IsKind(SyntaxKind.NullLiteralExpression) || syntax.IsKind(SyntaxKind.DefaultLiteralExpression) ? currentOutput : Types.Unit
            : Type(type, declaredType, syntax.GetLocation(), annotations: symbol is IFieldSymbol or IPropertySymbol or IParameterSymbol or ILocalSymbol or IMethodSymbol || syntax is CollectionExpressionSyntax or ArrayCreationExpressionSyntax or ImplicitArrayCreationExpressionSyntax or InitializerExpressionSyntax);
    }
    private V Expression(ExpressionSyntax syntax)
    {
        var model = Semantic(syntax); var symbol = model.GetSymbolInfo(syntax).Symbol;
        if (symbol is not null) RequireSelected(symbol, syntax.GetLocation());
        if (syntax is IdentifierNameSyntax or GenericNameSyntax && symbol is IMethodSymbol method) return MethodReference(method, null, syntax);
        var type = ExpressionType(syntax);
        switch (syntax)
        {
            case LiteralExpressionSyntax literal:
                if (literal.IsKind(SyntaxKind.NullLiteralExpression)) return Source(type, "Null", []);
                if (literal.IsKind(SyntaxKind.DefaultLiteralExpression)) return Source(type, "Default", [], ("type", "inferred"));
                Literal value = literal.Token.Value switch
                {
                    bool b => new BoolLiteral(b), string s => new StringLiteral(s),
                    char c when char.IsSurrogate(c) => Fail<Literal>("MCS4001", "An isolated UTF-16 surrogate has no Elm Char literal mapping.", syntax.GetLocation()),
                    char c => new CharLiteral(c.ToString()),
                    decimal d => new DecimalLiteral(d.ToString(CultureInfo.InvariantCulture)),
                    float or double => new FloatLiteral(double.Parse(literal.Token.Text.Replace("_", "", StringComparison.Ordinal).TrimEnd('f', 'F', 'd', 'D'), CultureInfo.InvariantCulture)),
                    ulong n when n > long.MaxValue => new UnmappedNumberLiteral(literal.Token.Text),
                    _ => new IntLiteral(Convert.ToInt64(literal.Token.Value, CultureInfo.InvariantCulture))
                };
                return new LiteralValue<Unit, IrType<Unit>>(type, value);
            case IdentifierNameSyntax name:
                return Identifier(name, symbol, type);
            case ThisExpressionSyntax:
                return new VariableValue<Unit, IrType<Unit>>(type, currentReceiver);
            case ParenthesizedExpressionSyntax parentheses:
                return Source(type, "Parenthesized", [Expression(parentheses.Expression)]);
            case BinaryExpressionSyntax binary:
                return Binary(binary, type);
            case PrefixUnaryExpressionSyntax unary:
                return Source(type, "Unary", [Expression(unary.Operand)], ("operator", unary.OperatorToken.Text), ("symbol", symbol is null ? "" : Id(symbol)));
            case PostfixUnaryExpressionSyntax unary when unary.IsKind(SyntaxKind.SuppressNullableWarningExpression):
                return Source(type, "NullForgiving", [Expression(unary.Operand)]);
            case PostfixUnaryExpressionSyntax:
                return Fail<V>("MCS3105", "Increment and decrement require D15.", syntax.GetLocation());
            case AssignmentExpressionSyntax assignment:
                if (assignment.Kind() is not (SyntaxKind.SimpleAssignmentExpression or SyntaxKind.CoalesceAssignmentExpression)) return Fail<V>("MCS3105", "Compound updates require D15.", syntax.GetLocation());
                return Source(type, "Assignment", [Expression(assignment.Left), Expression(assignment.Right)], ("operator", assignment.OperatorToken.Text));
            case MemberAccessExpressionSyntax member:
                return Member(member, symbol, type);
            case MemberBindingExpressionSyntax binding:
                return Source(type, "MemberBinding", [], ("member", binding.Name.ToString()), ("symbol", symbol is null ? "" : Id(symbol)));
            case InvocationExpressionSyntax invocation:
                return Invocation(invocation, type, symbol as IMethodSymbol);
            case ObjectCreationExpressionSyntax creation:
                if (symbol is IMethodSymbol collectionConstructor && CollectionModule(collectionConstructor.ContainingType) is not null)
                    return CollectionCreation(creation, collectionConstructor, type, creation.Initializer);
                return Source(type, "New", (symbol is IMethodSymbol ctor ? CallArguments(creation, ctor) : creation.ArgumentList?.Arguments.Select(Argument) ?? []).Concat(creation.Initializer is null ? [] : new[] { Expression(creation.Initializer) }).ToArray(), ("type", creation.Type.ToString()), ("symbol", symbol is null ? "" : Id(symbol)));
            case ImplicitObjectCreationExpressionSyntax creation:
                if (symbol is IMethodSymbol implicitCollectionConstructor && CollectionModule(implicitCollectionConstructor.ContainingType) is not null)
                    return CollectionCreation(creation, implicitCollectionConstructor, type, creation.Initializer);
                return Source(type, "New", (symbol is IMethodSymbol implicitCtor ? CallArguments(creation, implicitCtor) : creation.ArgumentList.Arguments.Select(Argument)).Concat(creation.Initializer is null ? [] : new[] { Expression(creation.Initializer) }).ToArray(), ("type", "inferred"), ("symbol", symbol is null ? "" : Id(symbol)));
            case InitializerExpressionSyntax initializer:
                return Source(type, "Initializer", initializer.Expressions.Select(Expression).ToArray(), ("kind", initializer.Kind().ToString()));
            case CastExpressionSyntax cast:
                return Source(type, "Cast", [Expression(cast.Expression)], ("type", cast.Type.ToString()), ("symbol", symbol is null ? "" : Id(symbol)));
            case ConditionalExpressionSyntax conditional:
                return new IfValue<Unit, IrType<Unit>>(type, Expression(conditional.Condition), Expression(conditional.WhenTrue), Expression(conditional.WhenFalse));
            case ConditionalAccessExpressionSyntax access:
                return Source(type, "ConditionalAccess", [Expression(access.Expression), Expression(access.WhenNotNull)]);
            case ElementAccessExpressionSyntax element:
                return Source(type, "Index", new[] { Expression(element.Expression) }.Concat(element.ArgumentList.Arguments.Select(Argument)).ToArray());
            case ElementBindingExpressionSyntax element:
                return Source(type, "IndexBinding", element.ArgumentList.Arguments.Select(Argument).ToArray());
            case TupleExpressionSyntax tuple:
                return new TupleValue<Unit, IrType<Unit>>(type, tuple.Arguments.Select(a => Expression(a.Expression)).ToArray());
            case DeclarationExpressionSyntax declaration:
                return Source(type, "DeclarationExpression", [Designation(declaration.Designation)], ("type", declaration.Type.ToString()));
            case DefaultExpressionSyntax defaultValue:
                return Source(type, "Default", [], ("type", defaultValue.Type.ToString()));
            case SizeOfExpressionSyntax size:
                return Source(type, "SizeOf", [], ("type", size.Type.ToString()));
            case CheckedExpressionSyntax check:
                return Source(type, check.Keyword.Text, [Expression(check.Expression)]);
            case SwitchExpressionSyntax match:
                return SwitchExpression(match, type);
            case IsPatternExpressionSyntax pattern:
                return Source(type, "IsPattern", [Expression(pattern.Expression), Pattern(pattern.Pattern)]);
            case WithExpressionSyntax update:
                return Source(type, "With", [Expression(update.Expression), Expression(update.Initializer)]);
            case CollectionExpressionSyntax collection:
                return CollectionExpression(collection, type);
            case ArrayCreationExpressionSyntax array:
                if (array.Type.RankSpecifiers.SelectMany(r => r.Sizes).Any(size => size is not OmittedArraySizeExpressionSyntax and not LiteralExpressionSyntax))
                    return Fail<V>("MCS3101", "A computed array dimension cannot be retained by the approved List mapping.", syntax.GetLocation());
                if (array.Initializer is null) return Fail<V>("MCS3101", "Implicit array element defaults have no approved Elm mapping.", syntax.GetLocation());
                return Source(type, "ArrayCreation", array.Initializer is null ? [] : [Expression(array.Initializer)], ("type", array.Type.ToString()));
            case ImplicitArrayCreationExpressionSyntax array:
                return Source(type, "ArrayCreation", [Expression(array.Initializer)], ("type", "inferred"));
            default:
                if (syntax.IsKind(SyntaxKind.FieldExpression)) return Source(type, "FieldKeyword", []);
                return Fail<V>("MCS2107", $"Expression {syntax.Kind()} has no delivered source mapping.", syntax.GetLocation());
        }
    }
    private V Identifier(IdentifierNameSyntax syntax, ISymbol? symbol, IrType<Unit> type)
    {
        if (syntax.Identifier.ValueText == "_" && symbol is IDiscardSymbol) return Source(type, "Discard", []);
        if(symbol is IParameterSymbol {ContainingSymbol:IMethodSymbol {MethodKind:MethodKind.Constructor} constructor} parameter && constructor.DeclaringSyntaxReferences.Any(r=>r.GetSyntax() is TypeDeclarationSyntax))
            return new FieldValue<Unit,IrType<Unit>>(type,new VariableValue<Unit,IrType<Unit>>(Type(currentType!),currentReceiver),Name.From(parameter.Name));
        if (symbol is IFieldSymbol { IsStatic: true } or IPropertySymbol { IsStatic: true }) return NamedReference(symbol, type);
        if (symbol is IPropertySymbol property && !StoredProperty(property))
            return Source(type, "MemberAccess", [new VariableValue<Unit, IrType<Unit>>(Type(currentType!), currentReceiver)], ("member", syntax.Identifier.Text), ("symbol", Id(property)));
        if (symbol is IFieldSymbol or IPropertySymbol)
            return new FieldValue<Unit, IrType<Unit>>(type, new VariableValue<Unit, IrType<Unit>>(Type(currentType!), currentReceiver), Name.From(symbol.Name));
        return new VariableValue<Unit, IrType<Unit>>(type, Name.From(syntax.Identifier.ValueText));
    }
    private V Member(MemberAccessExpressionSyntax syntax, ISymbol? symbol, IrType<Unit> type)
    {
        if (symbol is IMethodSymbol method)
            return MethodReference(method, method.IsStatic ? null : Expression(syntax.Expression), syntax);
        if (symbol is { IsStatic: true }) return NamedReference(symbol, type);
        var subject = Expression(syntax.Expression);
        if (symbol is IPropertySymbol && subject.Attributes is TupleType<Unit> && symbol.ContainingType?.Name == "Tuple" && symbol.Name.StartsWith("Item", StringComparison.Ordinal))
            return Source(type, "TupleField", [subject], ("index", (int.Parse(symbol.Name[4..], CultureInfo.InvariantCulture)-1).ToString(CultureInfo.InvariantCulture)));
        if (symbol is IPropertySymbol { Name: "Length", ContainingType.SpecialType: SpecialType.System_String } && Shape(subject.Attributes) == Shape(Types.String)) return Sdk("String", "length", Types.Int, subject);
        if (symbol is IPropertySymbol property && (!SourceSymbol(property) || !StoredProperty(property)))
            return Source(type, "MemberAccess", [subject], ("member", syntax.Name.ToString()), ("symbol", Id(property)));
        if (symbol is IFieldSymbol field && (field.ContainingType.IsTupleType || !SourceSymbol(field)))
        {
            if (field.ContainingType.IsTupleType)
            {
                var index = field.ContainingType.TupleElements.Select((f, i) => (Field: f, Index: i)).First(f => f.Field.Name == field.Name || f.Field.CorrespondingTupleField?.Name == field.Name).Index;
                return Source(type, "TupleField", [subject], ("index", index.ToString(CultureInfo.InvariantCulture)));
            }
            return Source(type, "MemberAccess", [subject], ("member", syntax.Name.ToString()), ("symbol", Id(field)));
        }
        return new FieldValue<Unit, IrType<Unit>>(type, subject, Name.From(syntax.Name.Identifier.ValueText));
    }
    private V NamedReference(ISymbol symbol, IrType<Unit> type)
    {
        if (symbol.IsStatic && symbol.Name == "Empty" && CollectionModule(type) is not null && CollectionModule(symbol.ContainingType) is not null) return EmptyCollection(type);
        if (identities.TryGetValue(Id(symbol), out var identity)) return new ReferenceValue<Unit, IrType<Unit>>(type, identity);
        if(symbol is IPropertySymbol {GetMethod:{} getter} && external.TryGetValue(Id(getter),out var contract))return new ReferenceValue<Unit,IrType<Unit>>(type,contract.Target);
        var constant = SdkConstant(symbol);
        if (constant is not null) return new ReferenceValue<Unit, IrType<Unit>>(type, constant);
        return Source(type, "NamedReference", [], ("name", symbol.ToDisplayString(Display)), ("symbol", Id(symbol)));
    }
    private V MethodReference(IMethodSymbol method, V? receiver, SyntaxNode syntax)
    {
        RequireSelected(method, syntax.GetLocation());
        // Signatures belong to the resolved symbol. No method body is evaluated here.
        var result = Type(method.ReturnType, GenericMember(method) ? DeclaredType(method) : null, syntax.GetLocation());
        IrType<Unit> signature = result;
        foreach (var parameter in method.Parameters.Reverse()) signature = Types.Function(Type(parameter.Type, DeclaredType(parameter), syntax.GetLocation()), signature);
        var identity = external.TryGetValue(Id(method), out var contract) ? contract.Target : identities.GetValueOrDefault(Id(method)) ?? SdkMethod(method);
        if (!method.IsStatic && (identity?.Package.Key == Types.Sdk.Key || SourceSymbol(method) && method.MethodKind != MethodKind.LocalFunction && GenericMember(method))) signature = Types.Function(Type(method.ContainingType), signature);
        V target = identity is null ? Source(signature, "NamedReference", [], ("name", method.ToDisplayString(Display)), ("symbol", Id(method))) : new ReferenceValue<Unit, IrType<Unit>>(signature, identity);
        if (method.MethodKind == MethodKind.LocalFunction)
        {
            var caller = syntax.Ancestors().OfType<LocalFunctionStatementSyntax>().FirstOrDefault();
            var owner = caller is null ? "" : Id((IMethodSymbol)Semantic(caller).GetDeclaredSymbol(caller)!);
            var key = Id(method);
            if (!localCallOwners.TryGetValue(key, out var owners)) localCallOwners[key] = owners = [];
            owners.Add(owner);
        }
        if (method.MethodKind == MethodKind.LocalFunction && method.Arity != 0)
        {
            var key = Id(method);
            var declaration = method.DeclaringSyntaxReferences.Single().GetSyntax();
            var definition = (IMethodSymbol)Semantic(declaration).GetDeclaredSymbol(declaration)!;
            genericLocalDefinitions[key] = definition;
            for (var i = 0; i < method.TypeArguments.Length; i++)
            {
                var parameter = TypeParameterKey(definition.TypeParameters[i]);
                if (!genericLocalArguments.TryGetValue(parameter, out var arguments)) genericLocalArguments[parameter] = arguments = [];
                arguments.Add(method.TypeArguments[i]);
            }
        }
        var converted = syntax is ExpressionSyntax expression ? Semantic(syntax).GetTypeInfo(expression).ConvertedType : null;
        var referenceType = converted is { TypeKind: TypeKind.Delegate } ? Type(converted) : signature;
        return Source(referenceType, "MethodReference", receiver is null ? [target] : [receiver, target], ("symbol", Id(method)), ("generic", GenericMember(method) ? "true" : ""));
    }
    private V Invocation(InvocationExpressionSyntax syntax, IrType<Unit> type, IMethodSymbol? method)
    {
        if (method is null) return Fail<V>("MCS2202", "The call has no resolved method symbol.", syntax.GetLocation());
        RequireSelected(method, syntax.GetLocation());
        if (method.MethodKind == MethodKind.DelegateInvoke)
            return Source(type, "DelegateInvocation", new[] { syntax.Expression is MemberBindingExpressionSyntax ? Source(Type(method.ContainingType),"ConditionalReceiver",[]) : Expression(syntax.Expression) }.Concat(CallArguments(syntax, method)).ToArray());
        if (CollectionFactory(syntax, method, type) is {} factory) return factory;
        if (CollectionInvocation(syntax, method, type) is {} collectionCall) return collectionCall;
        if (method.TypeArguments.Length != 0 && !SourceSymbol(method) && !(method.ContainingType.ContainingNamespace.ToDisplayString() == "System" && method.ContainingType.Name is "Tuple" or "ValueTuple")) return Fail<V>("MCS3100", "Generic metadata calls need an approved Elm mapping.", syntax.GetLocation());
        V? receiver = syntax.Expression is MemberAccessExpressionSyntax member && !method.IsStatic ? Expression(member.Expression) : syntax.Expression is MemberBindingExpressionSyntax && !method.IsStatic ? Source(Type(method.ContainingType),"ConditionalReceiver",[]) : null;
        if (receiver is null && !method.IsStatic && method.MethodKind!=MethodKind.LocalFunction && syntax.Expression is IdentifierNameSyntax) receiver = new VariableValue<Unit, IrType<Unit>>(Type(currentType!), currentReceiver);
        var arguments = CallArguments(syntax, method);
        if (type is TupleType<Unit> && method.IsStatic && method.Name == "Create" && method.ContainingType.ContainingNamespace.ToDisplayString() == "System" && method.ContainingType.Name is "Tuple" or "ValueTuple")
            return new TupleValue<Unit, IrType<Unit>>(type, arguments.Select(a => ((CSharpOperation<Unit, IrType<Unit>>)a).Children.Single()).ToArray());
        var target = MethodReference(method, receiver, syntax);
        return Source(type, "Invocation", new[] { target }.Concat(arguments).ToArray(), ("symbol", Id(method)),("decimalDivision",method.ContainingType.SpecialType==SpecialType.System_Decimal && method.Name=="Divide" && !external.ContainsKey(Id(method))?"true":""));
    }
    private IReadOnlyList<V> CallArguments(SyntaxNode syntax, IMethodSymbol method)
    {
        var operation = Semantic(syntax).GetOperation(syntax);
        var arguments = operation switch { IInvocationOperation invocation => invocation.Arguments.ToArray(), IObjectCreationOperation creation => creation.Arguments.ToArray(), _ => null };
        if (arguments is null) return syntax switch { ObjectCreationExpressionSyntax creation => creation.ArgumentList?.Arguments.Select(Argument).ToArray() ?? [], _ => Fail<IReadOnlyList<V>>("MCS2202", "No bound argument list.", syntax.GetLocation()) };
        var result = new List<V>();
        foreach (var parameter in method.Parameters)
        {
            var bound = arguments.Single(a => a.Parameter?.Ordinal == parameter.Ordinal);
            V value;
            if (bound.ArgumentKind == ArgumentKind.ParamArray && bound.Value is IArrayCreationOperation { Initializer: { } initializer })
                value = new ListValue<Unit, IrType<Unit>>(Type(parameter.Type), initializer.ElementValues.Select(e => Expression((ExpressionSyntax)e.Syntax)).ToArray());
            else if (bound.ArgumentKind == ArgumentKind.DefaultValue)
            {
                var written = parameter.DeclaringSyntaxReferences.Select(r => r.GetSyntax()).OfType<ParameterSyntax>().FirstOrDefault()?.Default?.Value;
                value = written is not null ? Expression(written) : DefaultConstant(parameter.ExplicitDefaultValue, Type(parameter.Type));
            }
            else value = bound.Syntax is ArgumentSyntax argument ? Argument(argument) is CSharpOperation<Unit, IrType<Unit>> a ? a.Children.Single() : throw new InvalidOperationException() : Expression((ExpressionSyntax)bound.Value.Syntax);
            result.Add(Source(Type(parameter.Type), "Argument", [value], ("name", "")));
        }
        return result;
    }
    private V DefaultConstant(object? value, IrType<Unit> type) => MaybeElement(type) is {} element && value is not null ? DefaultConstant(value,element) : value switch
    {
        null => Source(type, "Null", []),
        bool b => new LiteralValue<Unit, IrType<Unit>>(type, new BoolLiteral(b)),
        string s => new LiteralValue<Unit, IrType<Unit>>(type, new StringLiteral(s)),
        char c when !char.IsSurrogate(c) => new LiteralValue<Unit, IrType<Unit>>(type, new CharLiteral(c.ToString())),
        decimal d => new LiteralValue<Unit, IrType<Unit>>(type, new DecimalLiteral(d.ToString(CultureInfo.InvariantCulture))),
        float or double => new LiteralValue<Unit, IrType<Unit>>(type, new FloatLiteral(Convert.ToDouble(value, CultureInfo.InvariantCulture))),
        ulong n when n<=long.MaxValue => new LiteralValue<Unit, IrType<Unit>>(type,new IntLiteral((long)n)),
        byte or sbyte or short or ushort or int or uint or long => new LiteralValue<Unit, IrType<Unit>>(type, new IntLiteral(Convert.ToInt64(value, CultureInfo.InvariantCulture))),
        _ => ClassicFailure("This declared default has no scalar Elm literal mapping.")
    };
    private V Argument(ArgumentSyntax syntax)
    {
        if (!syntax.RefKindKeyword.IsKind(SyntaxKind.None)) return Fail<V>("MCS3105", "By-reference arguments require D15.", syntax.GetLocation());
        return Source(ExpressionType(syntax.Expression), "Argument", [Expression(syntax.Expression)], ("name", syntax.NameColon?.Name.Identifier.Text ?? ""), ("refKind", syntax.RefKindKeyword.Text));
    }
    private V Binary(BinaryExpressionSyntax syntax, IrType<Unit> type)
    {
        var model = Semantic(syntax); var op = model.GetSymbolInfo(syntax).Symbol as IMethodSymbol;
        var left = Expression(syntax.Left); var right = Expression(syntax.Right);
        var sourceType = ExpressionSymbolType(syntax.Left);
        string? name = syntax.Kind() switch
        {
            SyntaxKind.AddExpression => "add", SyntaxKind.SubtractExpression => "subtract", SyntaxKind.MultiplyExpression => "multiply",
            SyntaxKind.DivideExpression => sourceType?.SpecialType is SpecialType.System_Single or SpecialType.System_Double || sourceType?.ToDisplayString() == "System.Half" ? "divide" : "integerDivide",
            SyntaxKind.EqualsExpression => "equal", SyntaxKind.NotEqualsExpression => "notEqual", SyntaxKind.LessThanExpression => "lessThan",
            SyntaxKind.LessThanOrEqualExpression => "lessThanOrEqual", SyntaxKind.GreaterThanExpression => "greaterThan", SyntaxKind.GreaterThanOrEqualExpression => "greaterThanOrEqual", _ => null
        };
        bool primitive = sourceType?.SpecialType is SpecialType.System_Boolean or SpecialType.System_Char or SpecialType.System_SByte or SpecialType.System_Byte or SpecialType.System_Int16 or SpecialType.System_UInt16 or SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Int64 or SpecialType.System_UInt64 or SpecialType.System_IntPtr or SpecialType.System_UIntPtr or SpecialType.System_Single or SpecialType.System_Double;
        primitive |= sourceType?.ToDisplayString() == "System.Half";
        bool integer = sourceType?.ToDisplayString() is "System.Int128" or "System.UInt128" or "System.Numerics.BigInteger";
        var module = "Basics";
        if (sourceType?.SpecialType == SpecialType.System_Decimal && syntax.Kind() is SyntaxKind.AddExpression or SyntaxKind.SubtractExpression or SyntaxKind.MultiplyExpression)
        {
            module = "Decimal"; name = syntax.Kind() switch { SyntaxKind.AddExpression => "add", SyntaxKind.SubtractExpression => "sub", _ => "mul" }; primitive = true;
        }
        if (name is null || !(primitive || integer) || op is not null && SourceSymbol(op) || Shape(left.Attributes) != Shape(right.Attributes)
            || name is "add" or "sub" or "subtract" or "mul" or "multiply" or "divide" or "integerDivide" && Shape(left.Attributes) != Shape(type))
            return Source(type, "Binary", [left, right], ("operator", syntax.OperatorToken.Text), ("symbol", op is null ? "" : Id(op)));
        return Sdk(module, name, type, left, right);
    }
    private static V Designation(VariableDesignationSyntax syntax) => syntax switch
    {
        SingleVariableDesignationSyntax single => Source(Types.Unit, "Designation", [], ("name", single.Identifier.Text)),
        DiscardDesignationSyntax => Source(Types.Unit, "Discard", []),
        ParenthesizedVariableDesignationSyntax tuple => Source(Types.Unit, "DesignationTuple", tuple.Variables.Select(Designation).ToArray()),
        _ => throw new InvalidOperationException("Unknown compiler designation.")
    };
}
