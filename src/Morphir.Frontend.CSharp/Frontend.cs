using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Morphir.IR.Classic;
using IrPath = Morphir.IR.Classic.Path;
using TypedValue = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;
using TypedDefinition = Morphir.IR.Classic.ValueDefinition<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

public sealed record FrontendSettings(string Package, IReadOnlyList<string> Namespaces);
public sealed record FrontendDiagnostic(string Code, string Message, string File, int Line, int Column)
{
    public override string ToString() => $"{File}({Line},{Column}): {Code}: {Message}";
}
public sealed record FrontendResult(Library<Unit, IrType<Unit>>? Distribution, IReadOnlyList<FrontendDiagnostic> Diagnostics)
{
    public bool Success => Distribution is not null && Diagnostics.Count == 0;
}

public static partial class Frontend
{
    public static FrontendResult Convert(CSharpCompilation compilation, FrontendSettings settings, IReadOnlyList<CSharpCompilation>? sourceProjects = null) =>
        new Translation(compilation, settings, sourceProjects ?? []).Run();

    private sealed class Rejected : Exception;
    private sealed partial class Translation(CSharpCompilation compilation, FrontendSettings settings, IReadOnlyList<CSharpCompilation> sourceProjects)
    {
        private readonly List<FrontendDiagnostic> diagnostics = [];
        private readonly Dictionary<ISymbol, FQName> symbols = new(SymbolEqualityComparer.Default);
        private readonly Dictionary<string, IParameterSymbol[]> records = [];
        private readonly Dictionary<string, FQName> identities = [];
        private readonly Dictionary<string, List<(string Target, Location Location)>> calls = [];
        private readonly CSharpCompilation[] compilations = sourceProjects.Prepend(compilation).Distinct().ToArray();
        private string currentMethod = "";
        private static string SymbolId(ISymbol symbol)
        {
            var format = SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.None);
            if (symbol is IMethodSymbol method)
                return SymbolId(method.ContainingType) + "::" + method.MetadataName + "(" + string.Join(",", method.Parameters.Select(p => p.RefKind + ":" + p.Type.ToDisplayString(format))) + ")";
            return symbol.ContainingAssembly.Identity + "::" + symbol.ToDisplayString(format);
        }
        private FQName Identity(ISymbol symbol) => identities[SymbolId(symbol)];
        private SemanticModel Semantic(SyntaxTree tree) => compilations.Single(c => c.SyntaxTrees.Contains(tree)).GetSemanticModel(tree);
        private FrontendResult Failure() => new(null, diagnostics.Distinct().OrderBy(d => d.File, StringComparer.Ordinal).ThenBy(d => d.Line).ThenBy(d => d.Column).ThenBy(d => d.Code, StringComparer.Ordinal).ThenBy(d => d.Message, StringComparer.Ordinal).ToArray());
        private readonly Dictionary<string, (IrPath Path, Access Access, List<Entry<Name, AccessControlled<Documented<AliasDefinition<Unit>>>>> Types, List<Entry<Name, AccessControlled<Documented<TypedDefinition>>>> Values)> modules = [];
        private IrPath package = IrPath.From(settings.Package ?? "");
        private static IEnumerable<INamedTypeSymbol> AllTypes(INamespaceOrTypeSymbol parent)
        {
            foreach (var member in parent.GetMembers())
            {
                if (member is INamespaceSymbol ns) foreach (var t in AllTypes(ns)) yield return t;
                if (member is INamedTypeSymbol type) { yield return type; foreach (var t in AllTypes(type)) yield return t; }
            }
        }
        private bool Selected(INamedTypeSymbol t)
        {
            var ns = t.ContainingNamespace.ToDisplayString();
            return settings.Namespaces.Any(s => ns == s || ns.StartsWith(s + ".", StringComparison.Ordinal));
        }
        private void Error(string code, string message, Location? location = null)
        {
            var span = (location ?? Microsoft.CodeAnalysis.Location.None).GetLineSpan();
            diagnostics.Add(new(code, message, span.Path ?? "", span.StartLinePosition.Line + 1, span.StartLinePosition.Character + 1));
        }
        private T Fail<T>(string code, string message, Location? location) { Error(code, message, location); throw new Rejected(); }
        private static Location? Location(ISymbol s) => s.Locations.FirstOrDefault(x => x.IsInSource);
        private static Access Visibility(ISymbol s)
        {
            for (ISymbol? p = s; p is not null && p is not INamespaceSymbol; p = p.ContainingSymbol)
                if (p.DeclaredAccessibility != Accessibility.Public) return Access.Private;
            return Access.Public;
        }
        private static string Documentation(ISymbol s)
        {
            var xml = s.GetDocumentationCommentXml();
            if (string.IsNullOrWhiteSpace(xml)) return "";
            return XElement.Parse(xml).Element("summary")?.Value.Trim() ?? xml.Trim();
        }
        private IrPath ModulePath(string owner)
        {
            var path = IrPath.From(owner);
            // Prefix removal follows normalized Morphir path segments, not character prefixes.
            if (path.Segments.Count >= package.Segments.Count && path.Segments.Take(package.Segments.Count).Select(n => n.Key).SequenceEqual(package.Segments.Select(n => n.Key)))
                return new(path.Segments.Skip(package.Segments.Count).ToArray());
            return path;
        }
        private void AddSymbol(ISymbol s, string owner, string local)
        {
            var id = new FQName(package, ModulePath(owner), Name.From(local));
            var collision = symbols.FirstOrDefault(kv => kv.Value.Module.Key == id.Module.Key && kv.Value.Local.Key == id.Local.Key && (kv.Key is INamedTypeSymbol) == (s is INamedTypeSymbol));
            if (collision.Key is not null && !SymbolEqualityComparer.Default.Equals(s, collision.Key))
            {
                Error("MCS1004", $"Normalized identity collision: {s} and {collision.Key}.", Location(s));
                Error("MCS1004", $"Other declaration colliding with {s}.", Location(collision.Key));
            }
            symbols[s] = id;
            identities[SymbolId(s)] = id;
        }
        private void EnsureModule(IrPath p, Access access = Access.Public)
        {
            if (!modules.TryGetValue(p.Key, out var existing)) modules[p.Key] = (p, access, [], []);
            else if (access == Access.Private && existing.Access != Access.Private) modules[p.Key] = (existing.Path, access, existing.Types, existing.Values);
        }
        public FrontendResult Run()
        {
            if (string.IsNullOrWhiteSpace(settings.Package) || package.Segments.Count == 0 || package.Segments.Any(n => n.Words.Count == 0)) Error("MCS1001", "An explicit nonempty package name is required.");
            if (settings.Namespaces.Count == 0 || settings.Namespaces.Any(n => string.IsNullOrWhiteSpace(n) || n.Split('.').Any(s => !SyntaxFacts.IsValidIdentifier(s)))) Error("MCS1002", "Supply complete, case-sensitive namespace selectors.");
            foreach (var source in compilations)
            {
                if (source.Options.Platform is Platform.X86 or Platform.Arm or Platform.AnyCpu32BitPreferred) Error("MCS1003", "D1 requires 64-bit native semantics; 32-bit targets are incompatible.");
                foreach (var tree in source.SyntaxTrees)
                    if (tree.Options is not CSharpParseOptions { LanguageVersion: LanguageVersion.CSharp14 }) Error("MCS1005", "The fixed baseline is C# 14.", tree.GetRoot().GetLocation());
                foreach (var d in source.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error)) Error("MCS0001", d.Id + ": " + d.GetMessage(), d.Location);
            }
            if (diagnostics.Count != 0) return Failure();
            var selected = compilations.SelectMany(c => AllTypes(c.Assembly.GlobalNamespace)).Where(Selected).ToArray();
            if (selected.Length == 0) Error("MCS1006", "No source declarations match the selected namespaces.");
            foreach (var type in selected)
            {
                if (type.DeclaringSyntaxReferences.Length == 0) continue;
                var owner = type.ContainingType?.ToDisplayString() ?? type.ContainingNamespace.ToDisplayString();
                AddSymbol(type, owner, type.Name);
                // A data definition belongs to its namespace/enclosing type; member containers stay distinct.
                foreach (var m in type.GetMembers().OfType<IMethodSymbol>().Where(m => !m.IsImplicitlyDeclared && m.MethodKind == MethodKind.Ordinary))
                {
                    var overloads = type.GetMembers(m.Name).OfType<IMethodSymbol>().Count(x => !x.IsImplicitlyDeclared && x.MethodKind == MethodKind.Ordinary);
                    var suffix = overloads > 1 ? "__" + m.Arity + "__" + string.Join("__", m.Parameters.Select(p => p.RefKind + "_" + p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.None)))) : "";
                    AddSymbol(m, type.ToDisplayString(), m.Name + suffix);
                }
            }
            if (diagnostics.Count != 0) return Failure();
            foreach (var type in selected) Try(() => Discover(type));
            if (diagnostics.Count != 0) return Failure();
            foreach (var method in selected.SelectMany(t => t.GetMembers().OfType<IMethodSymbol>()).Where(m => !m.IsImplicitlyDeclared && m.MethodKind == MethodKind.Ordinary)) sourceMethods[SymbolId(method)] = method;
            foreach (var type in selected) Try(() => Emit(type));
            RejectCycles();
            if (diagnostics.Count != 0) return Failure();
            var defs = modules.Values.OrderBy(m => m.Path, IdentityOrder.Paths).Select(m => new Entry<IrPath, AccessControlled<ModuleDefinition<Unit, IrType<Unit>>>>(m.Path,
                new(m.Access, new(m.Types.OrderBy(e => e.Key.Key, StringComparer.Ordinal).ToArray(), m.Values.OrderBy(e => e.Key.Key, StringComparer.Ordinal).ToArray())))).ToArray();
            return new(new(package, [], new(defs)), diagnostics);
        }
        private static void Try(Action a) { try { a(); } catch (Rejected) { } }
        private void Discover(INamedTypeSymbol type)
        {
            foreach (var syntax in type.DeclaringSyntaxReferences.Select(r => r.GetSyntax()))
            {
                // Checked syntax must be diagnosed even when unreachable or constant-safe.
                foreach (var node in syntax.DescendantNodesAndSelf())
                {
                    if (node.IsKind(SyntaxKind.CheckedExpression) || node.IsKind(SyntaxKind.CheckedStatement) || node is OperatorDeclarationSyntax op && !op.CheckedKeyword.IsKind(SyntaxKind.None) || node is ConversionOperatorDeclarationSyntax co && !co.CheckedKeyword.IsKind(SyntaxKind.None))
                        Error("MCS2001", "Explicit checked syntax is excluded.", node.GetLocation());
                    if (node is AwaitExpressionSyntax || node is AnonymousFunctionExpressionSyntax af && af.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword) || node is MethodDeclarationSyntax md && md.Modifiers.Any(SyntaxKind.AsyncKeyword))
                        Error("MCS2002", "Async/await is excluded.", node.GetLocation());
                    if (node is PointerTypeSyntax or FunctionPointerTypeSyntax or StackAllocArrayCreationExpressionSyntax or ImplicitStackAllocArrayCreationExpressionSyntax || node is TypeDeclarationSyntax td && td.Modifiers.Any(SyntaxKind.RefKeyword))
                        Error("MCS2003", "Pointers, stack allocation, and ref-like forms are excluded.", node.GetLocation());
                }
                if (syntax is not TypeDeclarationSyntax || ((TypeDeclarationSyntax)syntax).BaseList is not null || ((TypeDeclarationSyntax)syntax).AttributeLists.Count != 0 || type.Arity != 0)
                    Fail<object>("MCS2100", "D1 admits non-generic static containers and positional readonly record structs without base lists or attributes.", syntax.GetLocation());
                var decl = (TypeDeclarationSyntax)syntax;
                if (decl.Members.Any(m => m is not MethodDeclarationSyntax && m is not TypeDeclarationSyntax))
                    Fail<object>("MCS2101", "A selected member is outside D1; no declarations may be silently omitted.", decl.Members.First(m => m is not MethodDeclarationSyntax && m is not TypeDeclarationSyntax).GetLocation());
            }
            if (type.IsRefLikeType) Fail<object>("MCS2003", "Ref-like data is excluded.", Location(type));
            if (type.IsStatic && type.TypeKind == TypeKind.Class)
            {
                EnsureModule(ModulePath(type.ToDisplayString()), Visibility(type));
                return;
            }
            if (!type.IsRecord || type.TypeKind != TypeKind.Struct || !type.IsReadOnly)
                Fail<object>("MCS2102", "D1 data must be a positional readonly record struct.", Location(type));
            var recordSyntax = type.DeclaringSyntaxReferences.Select(r => r.GetSyntax()).OfType<RecordDeclarationSyntax>().SingleOrDefault(r => r.ParameterList is not null);
            if (recordSyntax?.ParameterList is null) Fail<object>("MCS2102", "A positional parameter list is required.", Location(type));
            var model = Semantic(recordSyntax!.SyntaxTree);
            var fields = recordSyntax.ParameterList!.Parameters.Select(p => (IParameterSymbol)model.GetDeclaredSymbol(p)!).ToArray();
            foreach (var p in fields)
            {
                if (!AdmittedDataSlot(p.Type) || p.RefKind != RefKind.None || p.HasExplicitDefaultValue || p.GetAttributes().Length != 0 || p.DeclaringSyntaxReferences.Any(r => ((ParameterSyntax)r.GetSyntax()).AttributeLists.Count != 0))
                    Fail<object>("MCS2103", "D1 positional data fields admit int/bool only, with no defaults, ref kinds, or attributes.", Location(p));
            }
            if (fields.GroupBy(p => Name.From(p.Name).Key).Any(g => g.Count() > 1)) Fail<object>("MCS1004", "Record field names collide after normalization.", Location(type));
            records[SymbolId(type)] = fields;
        }
        private IrType<Unit> Type(ITypeSymbol t, Location? loc, bool returnLiteralString = false)
        {
            if (t.IsRefLikeType || t is IPointerTypeSymbol or IFunctionPointerTypeSymbol) return Fail<IrType<Unit>>("MCS2003", "Ref-like/pointer type use is excluded.", loc);
            if (Floating(t) is not null) return Types.Float;
            if (ComplexType(t)) return ComplexIrType(loc);
            if (Integral(t) is { } integer) return IntegerType(integer, loc);
            if (t.SpecialType == SpecialType.System_Boolean) return Types.Bool;
            if (NullableElement(t) is { } element) return Types.Maybe(Type(element, loc));
            if (t.SpecialType == SpecialType.System_String) return returnLiteralString && t.NullableAnnotation != NullableAnnotation.Annotated ? Types.String : Types.Maybe(Types.String);
            if (t is INamedTypeSymbol n && records.ContainsKey(SymbolId(n))) return new ReferenceType<Unit>(default, Identity(n), []);
            if (t.Locations.Any(l => l.IsInSource) && !identities.ContainsKey(SymbolId(t)))
                return Fail<IrType<Unit>>("MCS2201", $"Required source symbol {t} in namespace {t.ContainingNamespace} is not selected; add that namespace explicitly.", loc);
            return Fail<IrType<Unit>>("MCS2104", $"Type {t} is outside the D1 data domain.", loc);
        }
        private void Emit(INamedTypeSymbol type)
        {
            if (records.TryGetValue(SymbolId(type), out var fields))
            {
                var id = Identity(type); EnsureModule(id.Module, type.ContainingType is null ? Access.Public : Visibility(type.ContainingType));
                var record = new RecordType<Unit>(default, fields.Select(p => new Entry<Name, IrType<Unit>>(Name.From(p.Name), Type(p.Type, Location(p)))).ToArray());
                modules[id.Module.Key].Types.Add(new(id.Local, new(Visibility(type), new(Documentation(type), new([], record)))));
            }
            foreach (var m in type.GetMembers().OfType<IMethodSymbol>().Where(m => !m.IsImplicitlyDeclared && m.MethodKind == MethodKind.Ordinary)) Try(() => EmitMethod(m));
        }
        private void EmitMethod(IMethodSymbol m)
        {
            var key = SymbolId(m);
            if (methodOutputs.ContainsKey(key)) return;
            if (!emittingMethods.Add(key)) Fail<object>("MCS2203", "Recursive calls require D14.", Location(m));
            var syntax = (MethodDeclarationSyntax)m.DeclaringSyntaxReferences.Single().GetSyntax();
            var operationRoot = Semantic(syntax.SyntaxTree).GetOperation(syntax)!;
            foreach (var call in Operations(operationRoot).OfType<IInvocationOperation>())
                if (sourceMethods.TryGetValue(SymbolId(call.TargetMethod), out var dependency)) EmitMethod(dependency);
            if (!m.IsStatic || m.Arity != 0 || m.IsAsync || (syntax.ExpressionBody is null && syntax.Body is null) || syntax.AttributeLists.Count != 0 || m.GetAttributes().Length != 0 || m.ReturnsByRef || m.ReturnsByRefReadonly || m.Parameters.Any(p => p.RefKind != RefKind.None || p.IsParams || p.HasExplicitDefaultValue || p.GetAttributes().Length != 0))
                Fail<object>("MCS2105", "Functions require static expression or block bodies and ordinary, non-generic by-value parameters.", Location(m));
            if (m.Parameters.GroupBy(p => Name.From(p.Name).Key).Any(g => g.Count() > 1)) Fail<object>("MCS1004", "Parameter names collide after normalization.", Location(m));
            var inputs = m.Parameters.Select(p => new Input<Unit, IrType<Unit>>(Name.From(p.Name), Type(p.Type, Location(p)), Type(p.Type, Location(p)))).ToArray();
            var output = Type(m.ReturnType, Location(m), true);
            var methodBody = (IMethodBodyOperation)Semantic(syntax.SyntaxTree).GetOperation(syntax)!;
            currentMethod = SymbolId(m);
            calls.TryAdd(currentMethod, []);
            PrepareBindings(m);
            currentReturnSlot = output;
            var body = methodBody.BlockBody is { } block
                ? LowerMethodBlock(methodBody, block, syntax)
                : Expression(((IReturnOperation)methodBody.ExpressionBody!.Operations.Single()).ReturnedValue!, Copy(bindings), (value, _) => ReturnValue(value));
            output = body.Attributes;
            methodOutputs[key] = output;
            emittingMethods.Remove(key);
            var id = Identity(m); EnsureModule(id.Module, Visibility(m.ContainingType));
            modules[id.Module.Key].Values.Add(new(id.Local, new(Visibility(m), new(Documentation(m), new(inputs, output, body)))));
        }
        private TypedValue Lit(IrType<Unit> t, Literal l) => new LiteralValue<Unit, IrType<Unit>>(t, l);
        private TypedValue Lower(IOperation op)
        {
            if (operandValues.TryGetValue(op, out var evaluated)) return evaluated;
            if (LowerFloating(op) is { } floating) return floating;
            if (LowerInteger(op) is { } numeric) return numeric;
            if (LowerPresence(op) is { } presence) return presence;
            var loc = op.Syntax.GetLocation();
            var t = op.Type is null ? Types.Bool : Type(op.Type, loc, true);
            switch (op)
            {
                case ILiteralOperation l when l.ConstantValue.HasValue:
                    return l.ConstantValue.Value switch
                    {
                        int i => Lit(t, new IntLiteral(i)),
                        bool b => Lit(t, new BoolLiteral(b)),
                        string s => Lit(t, new StringLiteral(s)),
                        _ => Fail<TypedValue>("MCS2301", "Only int, bool, and known-present string literals are admitted in D1.", loc)
                    };
                case IUnaryOperation u when u.OperatorKind == UnaryOperatorKind.Minus && u.Type?.SpecialType == SpecialType.System_Int32 && u.ConstantValue is { HasValue: true, Value: int i }:
                    if (u.IsChecked) return Fail<TypedValue>("MCS2004", "Checked semantic arithmetic is excluded.", loc);
                    return Lit(t, new IntLiteral(i));
                case IConversionOperation c when c.Operand is IDefaultValueOperation && SymbolEqualityComparer.Default.Equals(c.Type, c.Operand.Type):
                    return Lower(c.Operand);
                case IConversionOperation c when c.IsChecked:
                    return Fail<TypedValue>("MCS2004", "Checked semantic conversions are excluded.", loc);
                case IConversionOperation c when c.Conversion.IsIdentity:
                    return Lower(c.Operand);
                case IConversionOperation c when c.Operand is IObjectCreationOperation creation
                    && !c.Conversion.IsUserDefined && SymbolEqualityComparer.Default.Equals(c.Type, creation.Type):
                    // Roslyn wraps target-typed new in an object-creation conversion,
                    // distinct from CommonConversion.IsIdentity despite equal bound types.
                    return Lower(creation);
                case IParameterReferenceOperation p:
                    return ReadBinding(p.Parameter, loc);
                case ILocalReferenceOperation l:
                    return ReadBinding(l.Local, loc);
                case IConditionalOperation c when c.Type is not null && c.WhenFalse is not null && !c.IsRef:
                    return Decision(c.Condition, Copy(bindings), state => Expression(c.WhenTrue, state, (v, _) => v), state => Expression(c.WhenFalse, state, (v, _) => v));
                case IUnaryOperation u when u.OperatorKind == UnaryOperatorKind.Not && u.Type?.SpecialType == SpecialType.System_Boolean && u.OperatorMethod is null:
                    return new ApplyValue<Unit, IrType<Unit>>(Types.Bool, new ReferenceValue<Unit, IrType<Unit>>(Types.Function(Types.Bool, Types.Bool), new(Types.Sdk, IrPath.From("Basics"), Name.From("not"))), Lower(u.Operand));
                case IPropertyReferenceOperation p when p.Instance is not null && p.Property.ContainingType is { } owner && records.TryGetValue(SymbolId(owner), out var fields) && fields.Any(f => f.Name == p.Property.Name) && p.Arguments.Length == 0:
                    return new FieldValue<Unit, IrType<Unit>>(Type(p.Property.Type, loc), Lower(p.Instance), Name.From(p.Property.Name));
                case IObjectCreationOperation c when c.Type is INamedTypeSymbol n && records.TryGetValue(SymbolId(n), out var fs) && c.Initializer is null && c.Constructor is { } ctor && ctor.Parameters.Length == fs.Length && c.Arguments.Length == fs.Length && c.Arguments.All(a => a.ArgumentKind == ArgumentKind.Explicit):
                    // Pure D1 arguments permit sorting record entries; evaluation cannot change observations.
                    var values = c.Arguments.Select(a => new Entry<Name, TypedValue>(Name.From(fs[a.Parameter!.Ordinal].Name), Adapt(Lower(a.Value), Type(fs[a.Parameter!.Ordinal].Type, loc)))).OrderBy(e => e.Key.Key, StringComparer.Ordinal).ToArray();
                    return new RecordValue<Unit, IrType<Unit>>(t, values);
                case IInvocationOperation invocation:
                    return Invoke(invocation);
                case IBinaryOperation b:
                    if (b.OperatorMethod is not null || b.IsLifted) return Fail<TypedValue>("MCS2106", "User-defined/lifted operators are outside D1.", loc);
                    if (b.IsChecked && b.OperatorKind == BinaryOperatorKind.Add) return Fail<TypedValue>("MCS2004", "Checked semantic arithmetic is excluded.", loc);
                    var left = Lower(b.LeftOperand); var right = Lower(b.RightOperand);
                    if (b.OperatorKind == BinaryOperatorKind.ConditionalAnd) return new IfValue<Unit, IrType<Unit>>(t, left, right, Lit(Types.Bool, new BoolLiteral(false)));
                    if (b.OperatorKind == BinaryOperatorKind.ConditionalOr) return new IfValue<Unit, IrType<Unit>>(t, left, Lit(Types.Bool, new BoolLiteral(true)), right);
                    if (b.LeftOperand.Type?.SpecialType == SpecialType.System_Boolean && b.RightOperand.Type?.SpecialType == SpecialType.System_Boolean)
                    {
                        var booleanFunction = b.OperatorKind switch { BinaryOperatorKind.Equals => "equal", BinaryOperatorKind.NotEquals => "notEqual", BinaryOperatorKind.And => "and", BinaryOperatorKind.Or => "or", BinaryOperatorKind.ExclusiveOr => "xor", _ => null };
                        if (booleanFunction is null) return Fail<TypedValue>("MCS2106", "Unmapped Boolean operator.", loc);
                        return SdkBinary(booleanFunction, Types.Bool, Types.Bool, left, right);
                    }
                    if (b.LeftOperand.Type?.SpecialType != SpecialType.System_Int32 || b.RightOperand.Type?.SpecialType != SpecialType.System_Int32)
                        return Fail<TypedValue>("MCS2106", "D1 mapped binary operators require Int32 operands.", loc);
                    if (b.OperatorKind == BinaryOperatorKind.Add) provedArithmetic.Add(b);
                    if (b.OperatorKind == BinaryOperatorKind.Add && !SafeAddition(b)) return Fail<TypedValue>("MCS2302", "Int32 addition can overflow. D1 requires a constant-safe sum or a zero operand; wrap semantics arrive in D5.", loc);
                    var function = b.OperatorKind switch
                    {
                        BinaryOperatorKind.Add => "add",
                        BinaryOperatorKind.GreaterThanOrEqual => "greaterThanOrEqual",
                        BinaryOperatorKind.GreaterThan => "greaterThan",
                        BinaryOperatorKind.LessThanOrEqual => "lessThanOrEqual",
                        BinaryOperatorKind.LessThan => "lessThan",
                        BinaryOperatorKind.Equals => "equal",
                        BinaryOperatorKind.NotEquals => "notEqual",
                        _ => null
                    };
                    if (function is null) return Fail<TypedValue>("MCS2106", $"Binary operator {b.OperatorKind} is outside D1.", loc);
                    var arg = Types.Int;
                    var partialType = Types.Function(arg, t);
                    var reference = new ReferenceValue<Unit, IrType<Unit>>(Types.Function(arg, partialType), new(Types.Sdk, IrPath.From("Basics"), Name.From(function)));
                    return new ApplyValue<Unit, IrType<Unit>>(t, new ApplyValue<Unit, IrType<Unit>>(partialType, reference, left), right);
                default: return Fail<TypedValue>("MCS2107", $"Operation {op.Kind} is outside the delivered expression contract; no partial distribution was produced.", loc);
            }
        }
        private FQName ValidateInvocation(IInvocationOperation invocation)
        {
            if (FloatingPredicateCall(invocation)) return new(Types.Sdk, IrPath.From("Basics"), Name.From(invocation.TargetMethod.Name));
            if (NullableDefaultCall(invocation)) return MaybeName("withDefault");
            var target = invocation.TargetMethod;
            var loc = invocation.Syntax.GetLocation();
            if (!identities.TryGetValue(SymbolId(target), out var fq))
            {
                bool source = compilations.Any(c => c.Assembly.Identity.Equals(target.ContainingAssembly.Identity));
                return Fail<FQName>(source ? "MCS2201" : "MCS2202",
                    source ? $"Required source symbol {target} in namespace {target.ContainingNamespace} is not selected; add that namespace explicitly."
                           : $"External call {target} has no exact catalog mapping or configured Morphir specification; DLL bodies are not translated.", loc);
            }
            if (!target.IsStatic || target.Arity != 0 || target.Parameters.Any(p => p.RefKind != RefKind.None || p.IsParams || p.HasExplicitDefaultValue)
                || invocation.Arguments.Any(a => a.ArgumentKind != ArgumentKind.Explicit || a.Syntax is ArgumentSyntax { NameColon: not null })
                || !invocation.Arguments.Select(a => a.Parameter!.Ordinal).SequenceEqual(Enumerable.Range(0, target.Parameters.Length)))
                return Fail<FQName>("MCS2204", "D2 calls require exact static, non-generic positional by-value arguments; extended argument forms arrive in D7.", loc);
            calls[currentMethod].Add((SymbolId(target), loc));
            return fq;
        }
        private TypedValue Invoke(IInvocationOperation invocation)
        {
            var target = invocation.TargetMethod; var loc = invocation.Syntax.GetLocation();
            var fq = ValidateInvocation(invocation);
            IrType<Unit> functionType = methodOutputs[SymbolId(target)];
            foreach (var parameter in target.Parameters.Reverse()) functionType = Types.Function(Type(parameter.Type, loc), functionType);
            TypedValue result = new ReferenceValue<Unit, IrType<Unit>>(functionType, fq);
            foreach (var argument in invocation.Arguments)
            {
                var argumentValue = Adapt(Lower(argument.Value), Type(argument.Parameter!.Type, loc));
                functionType = ((FunctionType<Unit>)functionType).Result;
                result = new ApplyValue<Unit, IrType<Unit>>(functionType, result, argumentValue);
            }
            return result;
        }
        private void RejectCycles()
        {
            var state = new Dictionary<string, int>();
            void Visit(string method)
            {
                state[method] = 1;
                foreach (var edge in calls[method])
                {
                    if (state.GetValueOrDefault(edge.Target) == 1) Error("MCS2203", "Recursive calls require the D14 recursion proof contract.", edge.Location);
                    else if (!state.ContainsKey(edge.Target) && calls.ContainsKey(edge.Target)) Visit(edge.Target);
                }
                state[method] = 2;
            }
            foreach (var method in calls.Keys.Order(StringComparer.Ordinal)) if (!state.ContainsKey(method)) Visit(method);
        }
        private bool SafeAddition(IBinaryOperation b)
        {
            var l = Constant(b.LeftOperand); var r = Constant(b.RightOperand);
            if (l is int x && x == 0 || r is int y && y == 0) return true;
            return l is int a && r is int c && (long)a + c is >= int.MinValue and <= int.MaxValue;
        }

    }
}
