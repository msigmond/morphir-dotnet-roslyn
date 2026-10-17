using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Morphir.IR.Classic;
using IrPath = Morphir.IR.Classic.Path;
using TypedValue = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        private readonly HashSet<string> pureFunctionOrigins = [];
        private readonly HashSet<string> pureFunctionMethods = [];
        private readonly HashSet<IOperation> provedDelegateCalls = [];
        private static INamedTypeSymbol? DelegateType(ITypeSymbol? type) => type is INamedTypeSymbol { TypeKind: TypeKind.Delegate } named && named.ContainingAssembly.Identity.Name == "System.Runtime" && named.ContainingAssembly.Identity.Version == new Version(10, 0, 0, 0) && named.ContainingNamespace.ToDisplayString() == "System" && named.OriginalDefinition.MetadataName.StartsWith("Func`", StringComparison.Ordinal) ? named : null;
        private static string FunctionOwner(FQName name) => name.Module.Key + "\u0002" + name.Local.Key;
        private IrType<Unit> FunctionSignature(INamedTypeSymbol type, Location? location)
        {
            var invoke = type.DelegateInvokeMethod!;
            if (invoke.Parameters.Any(parameter => parameter.RefKind != RefKind.None)) return Fail<IrType<Unit>>("MCS2803", "Function values require by-value parameters.", location);
            IrType<Unit> result = Type(invoke.ReturnType, location);
            if (invoke.Parameters.Length == 0) return Types.Function(Types.Unit, result);
            foreach (var parameter in invoke.Parameters.Reverse()) result = Types.Function(Type(parameter.Type, location), result);
            return result;
        }
        private bool KnownPureFunction(TypedValue value)
        {
            if (ValueKey(value) is { } key && pureFunctionOrigins.Contains(key)) return true;
            if (value is LambdaValue<Unit, IrType<Unit>>) return true;
            if (value is ConstructorValue<Unit, IrType<Unit>> { Name.Local.Key: "nothing" }) return true;
            if (value is VariableValue<Unit, IrType<Unit>> variable && boundValues.TryGetValue(variable.Name.Key, out var bound)) return KnownPureFunction(bound);
            if (value is LetValue<Unit, IrType<Unit>> let) return KnownPureFunction(let.Body);
            if (value is IfValue<Unit, IrType<Unit>> conditional) return KnownPureFunction(conditional.Then) && KnownPureFunction(conditional.Else);
            if (value is MatchValue<Unit, IrType<Unit>> match) return match.Cases.All(branch => KnownPureFunction(branch.Body));
            if (value is ApplyValue<Unit, IrType<Unit>> { Function: ConstructorValue<Unit, IrType<Unit>> { Name.Local.Key: "just" }, Argument: var present }) return KnownPureFunction(present);
            var root = value; while (root is ApplyValue<Unit, IrType<Unit>> apply) root = apply.Function;
            return root is ReferenceValue<Unit, IrType<Unit>> reference && reference.Name.Package.Key == package.Key && pureFunctionMethods.Contains(FunctionOwner(reference.Name));
        }
        private TypedValue InvokeDelegate(IInvocationOperation call)
        {
            var location = call.Syntax.GetLocation(); var function = DemandPresent(Lower(call.Instance!), location);
            if (!KnownPureFunction(function)) return Fail<TypedValue>("MCS2804", "Delegate invocation requires a resolved pure method reference; unknown callable effects need the D13 contract.", location);
            provedDelegateCalls.Add(call);
            if (call.Arguments.Length == 0) return new ApplyValue<Unit, IrType<Unit>>(((FunctionType<Unit>)function.Attributes).Result, function, new UnitValue<Unit, IrType<Unit>>(Types.Unit));
            foreach (var argument in call.Arguments.OrderBy(argument => argument.Parameter!.Ordinal)) function = new ApplyValue<Unit, IrType<Unit>>(((FunctionType<Unit>)function.Attributes).Result, function, Adapt(Lower(argument.Value), Type(argument.Parameter!.Type, location)));
            return function;
        }
        private TypedValue? LowerMethodReference(IOperation operation)
        {
            if (operation is not IDelegateCreationOperation creation) return null;
            if (creation.Target is not IMethodReferenceOperation reference) return Fail<TypedValue>("MCS2802", "Anonymous functions and captured closures require D13.", operation.Syntax.GetLocation());
            if (operandValues.TryGetValue(reference, out var evaluated)) return evaluated;
            return MethodReference(reference, DelegateType(creation.Type) ?? Fail<INamedTypeSymbol>("MCS2803", "Only exact standard Func value signatures are admitted in D7.", creation.Syntax.GetLocation()));
        }
        private TypedValue MethodReference(IMethodReferenceOperation reference, INamedTypeSymbol delegateType)
        {
            var target = reference.Method; var location = reference.Syntax.GetLocation();
            bool mapped = DecimalMethod(target) || FloatingPredicateMethod(target);
            FQName? fq = null;
            if (!mapped)
            {
                if (externalFunctions.ContainsKey(SymbolId(target))) fq = ExternalTarget(target, location);
                else if (!identities.TryGetValue(SymbolId(target), out fq)) return Fail<TypedValue>("MCS2202", "Method reference has no validated selected definition or explicit external contract.", location);
            }
            var output = mapped ? Type(target.ReturnType, location) : CallableOutput(target, location);
            if (target.Arity != 0 || target.Parameters.Any(parameter => parameter.RefKind != RefKind.None)) return Fail<TypedValue>("MCS2803", "Method references require resolved non-generic by-value signatures.", location);
            calls[currentMethod].Add((SymbolId(target), location));
            var signature = FunctionSignature(delegateType, location);
            IrType<Unit> targetSignature = output; foreach (var parameter in target.Parameters.Reverse()) targetSignature = Types.Function(Type(parameter.Type, location), targetSignature);
            TypedValue? receiver = null; Name? receiverName = null; TypedValue? receiverVariable = null;
            if (ReceiverRequired(target))
            {
                receiver = DemandPresent(Lower(reference.Instance!), location);
                receiverName = Fresh("methodReceiver"); receiverVariable = new VariableValue<Unit, IrType<Unit>>(receiver.Attributes, receiverName);
                targetSignature = Types.Function(receiver.Attributes, targetSignature);
            }
            TypedValue? body = mapped ? null : new ReferenceValue<Unit, IrType<Unit>>(targetSignature, fq!);
            if (!mapped && receiverVariable is not null) body = new ApplyValue<Unit, IrType<Unit>>(((FunctionType<Unit>)body!.Attributes).Result, body, receiverVariable);
            var parameters = new List<(Name Name, IrType<Unit> Type)>(); var invoke = delegateType.DelegateInvokeMethod!; var arguments = new List<TypedValue>();
            foreach (var parameter in invoke.Parameters)
            {
                var name = Fresh("methodArgument" + parameter.Ordinal); var type = Type(parameter.Type, location); parameters.Add((name, type)); var argument = new VariableValue<Unit, IrType<Unit>>(type, name); arguments.Add(argument);
                if (!mapped) body = new ApplyValue<Unit, IrType<Unit>>(((FunctionType<Unit>)body!.Attributes).Result, body, argument);
            }
            if (mapped)
            {
                if (DecimalMethod(target)) { if (receiverVariable is not null) arguments.Insert(0, receiverVariable); body = DecimalMethodValue(target, arguments, location); }
                else
                {
                    var argument = arguments[0]; var name = target.Name;
                    body = name == "IsNaN" ? SdkUnary("isNaN", Types.Float, Types.Bool, argument) : name == "IsInfinity" ? SdkUnary("isInfinite", Types.Float, Types.Bool, argument) : SdkUnary("not", Types.Bool, Types.Bool, SdkBinary("or", Types.Bool, Types.Bool, SdkUnary("isNaN", Types.Float, Types.Bool, argument), SdkUnary("isInfinite", Types.Float, Types.Bool, argument)));
                }
            }
            body = Adapt(body!, Type(invoke.ReturnType, location));
            if (parameters.Count == 0) body = new LambdaValue<Unit, IrType<Unit>>(signature, new UnitPattern<IrType<Unit>>(Types.Unit), body);
            else foreach (var parameter in parameters.AsEnumerable().Reverse()) body = new LambdaValue<Unit, IrType<Unit>>(Types.Function(parameter.Type, body.Attributes), new AsPattern<IrType<Unit>>(parameter.Type, new WildcardPattern<IrType<Unit>>(parameter.Type), parameter.Name), body);
            return receiver is null ? body : new LetValue<Unit, IrType<Unit>>(body.Attributes, receiverName!, new([], receiver.Attributes, receiver), body);
        }
    }
}
