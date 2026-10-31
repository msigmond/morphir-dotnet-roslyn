using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Morphir.IR.Classic;
using IrPath = Morphir.IR.Classic.Path;
using V = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;
using Definition = Morphir.IR.Classic.ValueDefinition<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

/// <summary>Syntax owns structure. Symbols supply identity and declared types. Flow facts do not change either.</summary>
internal sealed partial class SourceTranslation(CSharpCompilation compilation, FrontendSettings settings, IReadOnlyList<CSharpCompilation> projects)
{
    private sealed class Rejected : Exception;
    private sealed class Module(IrPath path, Access access)
    {
        internal IrPath Path { get; } = path;
        internal Access Access { get; set; } = access;
        internal List<Entry<Name, AccessControlled<Documented<TypeDefinition<Unit>>>>> Types { get; } = [];
        internal List<Entry<Name, AccessControlled<Documented<Definition>>>> Values { get; } = [];
    }
    private readonly CSharpCompilation[] compilations = projects.Prepend(compilation).Distinct().ToArray();
    private readonly IrPath package = IrPath.From(settings.Package ?? "");
    private readonly List<FrontendDiagnostic> diagnostics = [];
    private readonly Dictionary<string, FQName> identities = [];
    private readonly Dictionary<string, Module> modules = [];
    private readonly Dictionary<string, ISymbol> declared = [];
    private readonly Dictionary<string, ExternalFunctionContract> external = [];
    private readonly HashSet<ITypeSymbol> paramsArrays = new(SymbolEqualityComparer.Default);
    private INamedTypeSymbol? currentType;
    private IrType<Unit> currentOutput = Types.Unit;
    private Name currentReceiver = Name.From("this");
    private static readonly SymbolDisplayFormat Display = SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier | SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);
    private static string Id(ISymbol symbol) => symbol.ContainingAssembly?.Identity + "::" + (symbol.OriginalDefinition.GetDocumentationCommentId() ?? symbol.ToDisplayString(Display) + "@" + symbol.Locations.FirstOrDefault()?.SourceSpan.Start);
    private SemanticModel Semantic(SyntaxNode node) => compilations.Single(c => c.SyntaxTrees.Contains(node.SyntaxTree)).GetSemanticModel(node.SyntaxTree);
    private static Location? Location(ISymbol symbol) => symbol.Locations.FirstOrDefault(l => l.IsInSource);
    private static IEnumerable<INamedTypeSymbol> AllTypes(INamespaceOrTypeSymbol owner)
    {
        foreach (var symbol in owner.GetMembers())
        {
            if (symbol is INamespaceSymbol ns) foreach (var nested in AllTypes(ns)) yield return nested;
            if (symbol is INamedTypeSymbol type) { yield return type; foreach (var child in AllTypes(type)) yield return child; }
        }
    }
    private bool Selected(INamedTypeSymbol type) => settings.Namespaces.Any(ns => type.ContainingNamespace.ToDisplayString() == ns || type.ContainingNamespace.ToDisplayString().StartsWith(ns + ".", StringComparison.Ordinal));
    private static Access Visibility(ISymbol symbol)
    {
        for (ISymbol? parent = symbol; parent is not null && parent is not INamespaceSymbol; parent = parent.ContainingSymbol)
            if (parent.DeclaredAccessibility != Accessibility.Public) return Access.Private;
        return Access.Public;
    }
    private void Error(string code, string message, Location? location = null)
    {
        var span = (location ?? Microsoft.CodeAnalysis.Location.None).GetLineSpan();
        diagnostics.Add(new(code, message, span.Path, span.StartLinePosition.Line + 1, span.StartLinePosition.Character + 1));
    }
    private T Fail<T>(string code, string message, Location? location) { Error(code, message, location); throw new Rejected(); }
    private static void Attempt(Action action) { try { action(); } catch (Rejected) { } }
    private FrontendResult Failure() => new(null, diagnostics.Distinct().OrderBy(d => d.File, StringComparer.Ordinal).ThenBy(d => d.Line).ThenBy(d => d.Column).ThenBy(d => d.Code, StringComparer.Ordinal).ThenBy(d => d.Message, StringComparer.Ordinal).ToArray());
    private IrPath Owner(string owner)
    {
        var path = IrPath.From(owner);
        return path.Segments.Count >= package.Segments.Count && path.Segments.Take(package.Segments.Count).Select(n => n.Key).SequenceEqual(package.Segments.Select(n => n.Key)) ? new(path.Segments.Skip(package.Segments.Count).ToArray()) : path;
    }
    private Module GetModule(IrPath path, Access access)
    {
        if (!modules.TryGetValue(path.Key, out var module)) modules[path.Key] = module = new(path, access);
        if (access == Access.Private) module.Access = access;
        return module;
    }
    private static string Suffix(IMethodSymbol method) => "__" + method.Arity + "__" + string.Join("__", method.Parameters.Select(p => p.RefKind + "_" + p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
    private void Register(ISymbol symbol, string owner, string name)
    {
        var key = Id(symbol); if (identities.ContainsKey(key)) return;
        var identity = new FQName(package, Owner(owner), Name.From(name));
        foreach (var other in identities.Where(p => p.Value.Module.Key == identity.Module.Key && p.Value.Local.Key == identity.Local.Key && (declared[p.Key] is INamedTypeSymbol) == (symbol is INamedTypeSymbol)))
        {
            Error("MCS1004", $"Normalized identity collision: {symbol} and {declared[other.Key]}.", Location(symbol));
            Error("MCS1004", $"Other declaration colliding with {symbol}.", Location(declared[other.Key]));
        }
        identities[key] = identity; declared[key] = symbol;
    }
    private bool SourceSymbol(ISymbol symbol) => compilations.Any(c => c.Assembly.Identity.Equals(symbol.ContainingAssembly?.Identity));
    private void RequireSelected(ISymbol symbol, Location location)
    {
        if (symbol.ContainingType is { } owner && SourceSymbol(symbol) && !identities.ContainsKey(Id(owner.OriginalDefinition))) Fail<object>("MCS2201", $"Required source symbol {symbol} in namespace {owner.ContainingNamespace} is not selected; add that namespace explicitly.", location);
    }
    private static string Documentation(ISymbol symbol)
    {
        var xml = symbol.GetDocumentationCommentXml();
        return string.IsNullOrWhiteSpace(xml) ? "" : XElement.Parse(xml).Element("summary")?.Value.Trim() ?? xml.Trim();
    }
    public FrontendResult Run()
    {
        if (string.IsNullOrWhiteSpace(settings.Package) || package.Segments.Count == 0 || package.Segments.Any(n => n.Words.Count == 0)) Error("MCS1001", "An explicit package name is required.");
        if (settings.Namespaces.Count == 0 || settings.Namespaces.Any(ns => string.IsNullOrWhiteSpace(ns) || ns.Split('.').Any(s => !SyntaxFacts.IsValidIdentifier(s)))) Error("MCS1002", "Supply complete, case-sensitive namespace selectors.");
        foreach (var source in compilations)
        {
            if (source.Options.Platform is Platform.X86 or Platform.Arm or Platform.AnyCpu32BitPreferred) Error("MCS1003", "The fixed input baseline requires an x64-compatible compilation.");
            foreach (var tree in source.SyntaxTrees) if (tree.Options is not CSharpParseOptions { LanguageVersion: LanguageVersion.CSharp14 }) Error("MCS1005", "The fixed baseline is C# 14.", tree.GetRoot().GetLocation());
            foreach (var diagnostic in source.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error)) Error("MCS0001", diagnostic.Id + ": " + diagnostic.GetMessage(), diagnostic.Location);
        }
        if (diagnostics.Count != 0) return Failure();
        var types = compilations.SelectMany(c => AllTypes(c.Assembly.GlobalNamespace)).Where(Selected).Where(t => t.DeclaringSyntaxReferences.Length != 0).ToArray();
        if (types.Length == 0) Error("MCS1006", "No source declarations match the selected namespaces.");
        foreach (var type in types) Register(type, type.ContainingType?.ToDisplayString() ?? type.ContainingNamespace.ToDisplayString(), type.Name);
        foreach (var type in types)
        {
            foreach (var method in type.GetMembers().OfType<IMethodSymbol>().Where(m => !m.IsImplicitlyDeclared && m.MethodKind is MethodKind.Ordinary or MethodKind.Constructor or MethodKind.StaticConstructor or MethodKind.UserDefinedOperator or MethodKind.Conversion or MethodKind.PropertyGet or MethodKind.PropertySet))
            {
                string name = method.MethodKind switch { MethodKind.Constructor => "New", MethodKind.StaticConstructor => "StaticConstructor", MethodKind.PropertyGet => "Get" + method.AssociatedSymbol!.MetadataName, MethodKind.PropertySet => "Set" + method.AssociatedSymbol!.MetadataName, _ => method.Name };
                if (method.MethodKind != MethodKind.Ordinary || type.GetMembers(method.Name).OfType<IMethodSymbol>().Count(m => !m.IsImplicitlyDeclared && m.MethodKind == MethodKind.Ordinary) > 1) name += Suffix(method);
                Register(method, type.ToDisplayString(), name);
                foreach (var parameter in method.Parameters.Where(p => p.IsParams && p.Type is IArrayTypeSymbol)) paramsArrays.Add(parameter.Type);
            }
            foreach (var field in type.GetMembers().OfType<IFieldSymbol>().Where(f => !f.IsImplicitlyDeclared && (f.IsStatic || type.TypeKind == TypeKind.Enum))) Register(field, type.ToDisplayString(), field.Name);
        }
        if (diagnostics.Count != 0) return Failure();
        Attempt(ConfigureDependencies);
        foreach (var type in types) Attempt(() => Validate(type));
        if (diagnostics.Count != 0) return Failure();
        foreach (var type in types) Attempt(() => Emit(type));
        if (diagnostics.Count != 0) return Failure();
        var syntax = new List<SourceSyntax>();
        foreach (var tree in compilations.SelectMany(c => c.SyntaxTrees))
            foreach (var node in tree.GetRoot().DescendantNodes().OfType<BaseTypeDeclarationSyntax>().Where(n => !n.Ancestors().OfType<BaseTypeDeclarationSyntax>().Any()))
                if (Semantic(node).GetDeclaredSymbol(node) is INamedTypeSymbol type && Selected(type)) Attempt(() => syntax.Add(Syntax(node)));
        if (diagnostics.Count != 0) return Failure();
        var definitions = modules.Values.OrderBy(m => m.Path, IdentityOrder.Paths).Select(m => new Entry<IrPath, AccessControlled<ModuleDefinition<Unit, IrType<Unit>>>>(m.Path, new(m.Access, new(m.Types.OrderBy(t => t.Key.Key, StringComparer.Ordinal).ToArray(), m.Values.OrderBy(v => v.Key.Key, StringComparer.Ordinal).ToArray())))).ToArray();
        var library = new Library<Unit, IrType<Unit>>(package, (settings.Dependencies ?? []).Select(d => new Entry<IrPath, PackageSpecification<Unit>>(d.Package, d.Specification)).ToArray(), new(definitions)) { Source = new(syntax) };
        if (settings.OutputFormat == FrontendOutputFormat.Classic) Attempt(() => library = ProjectClassic(library));
        return diagnostics.Count != 0 ? Failure() : new(library, []);
    }
    private void Validate(INamedTypeSymbol type)
    {
        if (type.Arity != 0 || type.TypeKind is TypeKind.Interface or TypeKind.Delegate || type.BaseType is { SpecialType: not SpecialType.System_Object and not SpecialType.System_ValueType and not SpecialType.System_Enum }) Fail<object>("MCS3100", "Generic declarations, interfaces, and inheritance require D10.", Location(type));
        foreach (var syntax in type.DeclaringSyntaxReferences.Select(r => r.GetSyntax())) foreach (var node in syntax.DescendantNodesAndSelf())
        {
            if (node is TypeDeclarationSyntax { BaseList: not null }) Fail<object>("MCS3100", "Declared interfaces and base lists require D10.", node.GetLocation());
            if (node is PointerTypeSyntax or FunctionPointerTypeSyntax or StackAllocArrayCreationExpressionSyntax or ImplicitStackAllocArrayCreationExpressionSyntax || node is TypeDeclarationSyntax td && td.Modifiers.Any(SyntaxKind.RefKeyword)) Fail<object>("MCS2003", "Pointer, stack allocation, and ref-like forms have no source mapping.", node.GetLocation());
            if (node is AwaitExpressionSyntax || node is MethodDeclarationSyntax md && md.Modifiers.Any(SyntaxKind.AsyncKeyword)) Fail<object>("MCS2002", "Async structure has no delivered mapping.", node.GetLocation());
            if (node is AnonymousFunctionExpressionSyntax) Fail<object>("MCS3103", "Anonymous functions require D13.", node.GetLocation());
            if (node is ForStatementSyntax or ForEachStatementSyntax or ForEachVariableStatementSyntax or WhileStatementSyntax or DoStatementSyntax or GotoStatementSyntax or LabeledStatementSyntax or RefExpressionSyntax or RefTypeSyntax) Fail<object>("MCS3105", "Loops, labels, and ref forms require D15.", node.GetLocation());
            if (node is SwitchStatementSyntax or SwitchExpressionSyntax || node is PatternSyntax and not ConstantPatternSyntax and not UnaryPatternSyntax) Fail<object>("MCS3102", "This pattern or switch requires D12.", node.GetLocation());
            if (node is TryStatementSyntax or ThrowStatementSyntax or ThrowExpressionSyntax) Fail<object>("MCS3181", "Exception structure requires D18.1.", node.GetLocation());
            if (node is YieldStatementSyntax) Fail<object>("MCS3182", "Iterator structure requires D18.2.", node.GetLocation());
            if (node is QueryExpressionSyntax or UnsafeStatementSyntax or FixedStatementSyntax or LockStatementSyntax or UsingStatementSyntax or EventDeclarationSyntax or EventFieldDeclarationSyntax or DestructorDeclarationSyntax) Fail<object>("MCS2107", $"Source form {node.Kind()} has no delivered mapping.", node.GetLocation());
            if (node is MethodDeclarationSyntax { TypeParameterList: not null }) Fail<object>("MCS3100", "Generic method declarations require D10.", node.GetLocation());
            if (node is LocalFunctionStatementSyntax { TypeParameterList: not null }) Fail<object>("MCS3100", "Generic local declarations require D10.", node.GetLocation());
            if (node is ParameterSyntax parameter && parameter.Modifiers.Any(m => m.IsKind(SyntaxKind.RefKeyword) || m.IsKind(SyntaxKind.OutKeyword) || m.IsKind(SyntaxKind.InKeyword))) Fail<object>("MCS3105", "By-reference parameters require D15.", node.GetLocation());
        }
    }
    private void ConfigureDependencies()
    {
        var packages = new HashSet<string>();
        foreach (var dependency in settings.Dependencies ?? [])
        {
            if (dependency.Package.Segments.Count == 0 || dependency.Package.Key == package.Key || dependency.Package.Key == Types.Sdk.Key || !packages.Add(dependency.Package.Key)) Fail<object>("MCS2809", "Dependency package identities must be distinct.", null);
            if (dependency.Specification.Modules.GroupBy(m => m.Key.Key).Any(g => g.Count() != 1)
                || dependency.Specification.Modules.Any(m => m.Value.Values.GroupBy(v => v.Key.Key).Any(g => g.Count() != 1) || m.Value.Types.GroupBy(t => t.Key.Key).Any(g => g.Count() != 1)))
                Fail<object>("MCS2809", "Dependency declaration identities must be unique.", null);
            foreach (var contract in dependency.Functions)
            {
                var target = dependency.Specification.Modules.SingleOrDefault(m => m.Key.Key == contract.Target.Module.Key)?.Value.Values.SingleOrDefault(v => v.Key.Key == contract.Target.Local.Key)?.Value.Value;
                if (target is null || contract.Target.Package.Key != dependency.Package.Key || SourceSymbol(contract.Method) || !external.TryAdd(Id(contract.Method), contract)) Fail<object>("MCS2809", "Dependency mapping must resolve one exact metadata method and declared target.", Location(contract.Method));
                if (!contract.Method.IsStatic || target!.Inputs.Count != contract.Method.Parameters.Length
                    || !target.Inputs.Select(i => Shape(i.Value)).SequenceEqual(contract.Method.Parameters.Select(p => Shape(Type(p.Type, DeclaredType(p)))))
                    || Shape(target.Output) != Shape(Type(contract.Method.ReturnType)))
                    Fail<object>("MCS2809", "Dependency signature does not match the exact mapped static method.", Location(contract.Method));
            }
        }
    }
    private static string Shape(IrType<Unit> type) => type switch
    {
        UnitType<Unit> => "unit", ReferenceType<Unit> r => "ref:" + r.Name.Package.Key + "/" + r.Name.Module.Key + "/" + r.Name.Local.Key + "[" + string.Join(";", r.Arguments.Select(Shape)) + "]",
        TupleType<Unit> t => "tuple[" + string.Join(";", t.Elements.Select(Shape)) + "]", FunctionType<Unit> f => "function(" + Shape(f.Argument) + "," + Shape(f.Result) + ")",
        RecordType<Unit> r => "record[" + string.Join(";", r.Fields.Select(f => f.Key.Key + ":" + Shape(f.Value))) + "]", VariableType<Unit> v => "var:" + v.Name.Key,
        _ => throw new NotSupportedException(type.GetType().Name)
    };
    private static TypeSyntax? DeclaredType(ISymbol symbol) => symbol.DeclaringSyntaxReferences.Select(r => r.GetSyntax()).Select(node => node switch
    {
        ParameterSyntax p => p.Type, PropertyDeclarationSyntax p => p.Type, IndexerDeclarationSyntax i => i.Type,
        VariableDeclaratorSyntax v => (v.Parent as VariableDeclarationSyntax)?.Type, MethodDeclarationSyntax m => m.ReturnType,
        LocalFunctionStatementSyntax l => l.ReturnType, _ => null
    }).FirstOrDefault(t => t is not null);
    private IrType<Unit> Type(ITypeSymbol symbol, TypeSyntax? syntax = null, Location? location = null, bool annotations = true)
    {
        location ??= syntax?.GetLocation() ?? Location(symbol);
        if (syntax?.IsVar == true) annotations = false;
        if (syntax is NullableTypeSyntax nullable) return Types.Maybe(Type(symbol is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } n ? n.TypeArguments[0] : symbol.WithNullableAnnotation(NullableAnnotation.NotAnnotated), nullable.ElementType, location));
        if (symbol is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullableValue) return Types.Maybe(Type(nullableValue.TypeArguments[0], location: location));
        if (annotations && syntax is null && symbol.IsReferenceType && symbol.NullableAnnotation == NullableAnnotation.Annotated) return Types.Maybe(Type(symbol.WithNullableAnnotation(NullableAnnotation.NotAnnotated), location: location));
        if (symbol.IsRefLikeType || symbol is IPointerTypeSymbol or IFunctionPointerTypeSymbol) return Fail<IrType<Unit>>("MCS2003", "Ref-like and pointer types have no delivered source mapping.", location);
        if (symbol is IArrayTypeSymbol array) return new ReferenceType<Unit>(default, new(IrPath.From("Morphir.CSharp"), IrPath.From("Types"), Name.From("Array")), [Type(array.ElementType, (syntax as ArrayTypeSyntax)?.ElementType, location, annotations)]);
        if (symbol is ITypeParameterSymbol parameter) return new VariableType<Unit>(default, Name.From(parameter.Name));
        if (symbol is INamedTypeSymbol { IsTupleType: true } tuple) return new TupleType<Unit>(default, tuple.TupleElements.Select((element, i) => Type(element.Type, (syntax as TupleTypeSyntax)?.Elements[i].Type, location, annotations)).ToArray());
        if (symbol.SpecialType == SpecialType.System_Void) return Types.Unit;
        if (symbol.SpecialType == SpecialType.System_Boolean) return Types.Bool;
        if (symbol.SpecialType == SpecialType.System_String) return Types.String;
        if (symbol.SpecialType == SpecialType.System_Char) return Types.Reference("Char", "Char");
        if (symbol.SpecialType == SpecialType.System_Decimal) return Types.Decimal;
        if (symbol.SpecialType is SpecialType.System_Single or SpecialType.System_Double || symbol.ToDisplayString() == "System.Half") return Types.Float;
        if (symbol.SpecialType is SpecialType.System_Byte or SpecialType.System_SByte or SpecialType.System_Int16 or SpecialType.System_UInt16 or SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Int64 or SpecialType.System_UInt64 or SpecialType.System_IntPtr or SpecialType.System_UIntPtr || symbol.ToDisplayString() is "System.Int128" or "System.UInt128" or "System.Numerics.BigInteger") return Types.Int;
        if (symbol is not INamedTypeSymbol named) return Fail<IrType<Unit>>("MCS2104", $"Type {symbol} has no delivered source mapping.", location);
        if (SourceSymbol(named!))
        {
            if (!identities.TryGetValue(Id(named.OriginalDefinition), out var identity)) return Fail<IrType<Unit>>("MCS2201", $"Required source type {symbol} in namespace {symbol.ContainingNamespace} is not selected.", location);
            return new ReferenceType<Unit>(default, identity, named.TypeArguments.Select((t, i) => Type(t, GenericArguments(syntax).ElementAtOrDefault(i), location, annotations)).ToArray());
        }
        bool delivered = named.ToDisplayString() == "System.Numerics.Complex" || named.ContainingNamespace.ToDisplayString() == "System" && named.Name is "Func" or "Tuple" or "ValueTuple";
        if (!delivered) return Fail<IrType<Unit>>("MCS2104", $"Type {symbol} retains its later delivery gate.", location);
        return new ReferenceType<Unit>(default, new(IrPath.From("Morphir.CSharp"), IrPath.From(named.ContainingNamespace.ToDisplayString()), Name.From(named.MetadataName)), named.TypeArguments.Select((t, i) => Type(t, GenericArguments(syntax).ElementAtOrDefault(i), location, annotations)).ToArray());
    }
    private static IEnumerable<TypeSyntax> GenericArguments(TypeSyntax? syntax) => syntax switch
    {
        GenericNameSyntax generic => generic.TypeArgumentList.Arguments,
        QualifiedNameSyntax qualified => GenericArguments(qualified.Right),
        AliasQualifiedNameSyntax alias => GenericArguments(alias.Name),
        _ => []
    };
    private void CheckArray(ITypeSymbol type, ISymbol symbol)
    {
        if (ContainsArray(type) && !(symbol is IParameterSymbol { IsParams: true } && type is IArrayTypeSymbol array && !ContainsArray(array.ElementType))) Fail<object>("MCS3101", "General array declarations require D11; params inputs remain delivered.", Location(symbol));
    }
    private static bool ContainsArray(ITypeSymbol type) => type is IArrayTypeSymbol || type is INamedTypeSymbol named && named.TypeArguments.Any(ContainsArray);
    private static V Source(IrType<Unit> type, string kind, IReadOnlyList<V> children, params (string Key, string Value)[] properties) => new SourceValue<Unit, IrType<Unit>>(type, kind, properties.Select(p => new Entry<string, string>(p.Key, p.Value)).ToArray(), children);
    private SourceSyntax Syntax(SyntaxNode node)
    {
        var model = Semantic(node); ISymbol? symbol = null;
        if (node is MemberDeclarationSyntax or VariableDeclaratorSyntax or ParameterSyntax or LocalFunctionStatementSyntax or SingleVariableDesignationSyntax or TypeParameterSyntax) symbol = model.GetDeclaredSymbol(node);
        else if (node is ExpressionSyntax or ConstructorInitializerSyntax) symbol = model.GetSymbolInfo(node).Symbol;
        if (symbol is not null) RequireSelected(symbol, node.GetLocation());
        var type = node is ExpressionSyntax expression ? model.GetTypeInfo(expression).Type?.ToDisplayString(Display) : null;
        SourceLocation Position(SyntaxNodeOrToken item)
        {
            var span = item.GetLocation()!.GetLineSpan();
            return new(item.SyntaxTree!.FilePath, item.SpanStart, item.Span.Length, span.StartLinePosition.Line + 1, span.StartLinePosition.Character + 1);
        }
        return new(node.Kind().ToString(), null, symbol is null ? null : Id(symbol), type, Position(node), node.ChildNodesAndTokens().Select(child => child.IsNode ? Syntax(child.AsNode()!) : new SourceSyntax(child.Kind().ToString(), child.AsToken().Text, null, null, Position(child), [])).ToArray());
    }
}
