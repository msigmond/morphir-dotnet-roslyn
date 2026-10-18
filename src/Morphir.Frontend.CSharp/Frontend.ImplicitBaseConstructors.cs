using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Morphir.IR.Classic;
using TypedValue = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        private readonly Dictionary<string, string> implicitBaseTargets = [];
        private IMethodSymbol? ImplicitBaseTarget(IMethodSymbol constructor)
        {
            if (constructor.MethodKind != MethodKind.Constructor || !constructor.IsImplicitlyDeclared && !PrimaryDataConstructor(constructor) || ConstructorInitializer(constructor) is not null || constructor.ContainingType.BaseType is not { } parent || !ValueInterfaceType(parent)) return null;
            var key = SymbolId(constructor);
            if (!implicitBaseTargets.TryGetValue(key, out var selected))
            {
                var declaration = constructor.OriginalDefinition.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax()).OfType<ClassDeclarationSyntax>().FirstOrDefault() ?? constructor.ContainingType.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax()).OfType<ClassDeclarationSyntax>().First();
                var model = Semantic(declaration.SyntaxTree);
                var zero = parent.InstanceConstructors.SingleOrDefault(target => target.Parameters.Length == 0 && model.Compilation.IsSymbolAccessibleWithin(target, constructor.ContainingType));
                if (zero is not null) selected = SymbolId(zero);
                else
                {
                    // Ask the pinned compiler to bind an equivalent explicit
                    // base() in a temporary tree. Only the selected declaration
                    // identity is retained; no cloned symbol enters lowering.
                    var annotation = new SyntaxAnnotation(); ClassDeclarationSyntax changed;
                    if (declaration.ParameterList is not null)
                    {
                        var entry = declaration.BaseList?.Types.SingleOrDefault(item => model.GetTypeInfo(item.Type).Type is { } type && SymbolId(type) == SymbolId(parent));
                        var syntax = entry?.Type ?? SyntaxFactory.ParseTypeName(constructor.OriginalDefinition.ContainingType.BaseType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                        var primary = SyntaxFactory.PrimaryConstructorBaseType(syntax, SyntaxFactory.ArgumentList()).WithAdditionalAnnotations(annotation);
                        changed = entry is null ? declaration.WithBaseList(SyntaxFactory.BaseList((declaration.BaseList?.Types ?? default(SeparatedSyntaxList<BaseTypeSyntax>)).Insert(0, primary))) : declaration.ReplaceNode(entry, primary);
                    }
                    else
                    {
                        var probe = SyntaxFactory.ConstructorDeclaration(declaration.Identifier).WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword))).WithInitializer(SyntaxFactory.ConstructorInitializer(SyntaxKind.BaseConstructorInitializer, SyntaxFactory.ArgumentList())).WithBody(SyntaxFactory.Block()).WithAdditionalAnnotations(annotation);
                        changed = declaration.AddMembers(probe);
                    }
                    var tree = declaration.SyntaxTree.WithRootAndOptions(declaration.SyntaxTree.GetRoot().ReplaceNode(declaration, changed), declaration.SyntaxTree.Options);
                    var copy = model.Compilation.ReplaceSyntaxTree(declaration.SyntaxTree, tree).GetSemanticModel(tree);
                    var operation = copy.GetOperation(tree.GetRoot().GetAnnotatedNodes(annotation).Single());
                    var invocation = operation is IInvocationOperation boundPrimary ? boundPrimary : operation is IConstructorBodyOperation { Initializer: IExpressionStatementOperation { Operation: IInvocationOperation initializer } } ? initializer : null;
                    selected = invocation is null ? Fail<string>("MCS3005", "Implicit base construction has no resolved source constructor target.", declaration.GetLocation()) : SymbolId(invocation.TargetMethod);
                }
                implicitBaseTargets[key] = selected;
            }
            return parent.InstanceConstructors.SingleOrDefault(target => SymbolId(target) == selected) ?? Fail<IMethodSymbol>("MCS3005", "The bound implicit base constructor is outside the original source graph.", Location(constructor.ContainingType));
        }
        private TypedValue ImplicitConstructorArgument(IParameterSymbol parameter, Location location)
        {
            if (parameter.IsParams && parameter.Type is IArrayTypeSymbol array) return Just(new ListValue<Unit, IrType<Unit>>(Types.List(Type(array.ElementType, location)), []));
            if (!parameter.HasExplicitDefaultValue) return Fail<TypedValue>("MCS3005", "An implicit base argument requires its actual optional/params mapping.", location);
            var value = parameter.ExplicitDefaultValue; var type = NullableElement(parameter.Type) ?? parameter.Type;
            if (value is null) return Default(parameter.Type, location);
            if (EnumType(type) is { } enumeration) return WrapEnum(enumeration, IntegerLiteral(IntegerConstant(value)!.Value, Integral(enumeration.EnumUnderlyingType)!, location), location);
            if (Integral(type) is { } integer) return IntegerLiteral(IntegerConstant(value)!.Value, integer, location);
            if (Floating(type) is not null) return FloatLiteral(System.Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture), location);
            if (DecimalType(type)) return DecimalConstant((decimal)value, location);
            return value switch { bool flag => Lit(Types.Bool, new BoolLiteral(flag)), string text => Lit(Types.String, new StringLiteral(text)), _ => Fail<TypedValue>("MCS3005", "The bound optional argument has no delivered constant representation.", location) };
        }
        private TypedValue BindImplicitBaseArguments(IMethodSymbol constructor, BindingEnvironment environment, Func<BindingEnvironment, TypedValue> continuation)
        {
            TypedValue Bind(int index, BindingEnvironment state)
            {
                if (index == constructor.Parameters.Length) return continuation(state);
                var parameter = constructor.Parameters[index]; var location = Location(parameter)!; var previous = bindings; bindings = state; TypedValue value;
                try { value = ImplicitConstructorArgument(parameter, location); } finally { bindings = previous; }
                return BindValue(parameter.OriginalDefinition, parameter.Type, value, state, next => Bind(index + 1, next), location);
            }
            return Bind(0, environment);
        }
    }
}
