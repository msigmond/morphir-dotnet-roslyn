using Microsoft.CodeAnalysis;
using Morphir.IR.Classic;

namespace Morphir.Frontend.CSharp;

public static partial class Frontend
{
    private sealed partial class Translation
    {
        private readonly Dictionary<string, FQName> interfaceDefaultIdentities = [];
        private static bool InterfaceRequiresImplementation(IMethodSymbol method) => method.MethodKind != MethodKind.Constructor && !(method.MethodKind == MethodKind.PropertySet && method.ContainingType.TypeKind == TypeKind.Class && !method.IsAbstract && !method.IsVirtual && !method.IsOverride) && (method.DeclaredAccessibility != Accessibility.Private || method.IsAbstract && method.ExplicitInterfaceImplementations.Length != 0) && (!method.IsStatic || method.IsAbstract || method.IsVirtual);
        private bool InterfaceDispatchMethod(IMethodSymbol method) => ValueInterfaceType(method.ContainingType) && InterfaceRequiresImplementation(method);
        private string InterfaceDefaultKey(IMethodSymbol method) => InterfaceDispatchMethod(method) ? SymbolId(method) + "::default-body" : SymbolId(method);
        private FQName InterfaceDefaultIdentity(IMethodSymbol method)
        {
            if (!InterfaceDispatchMethod(method)) return Identity(method);
            var key = SymbolId(method); if (interfaceDefaultIdentities.TryGetValue(key, out var found)) return found;
            var original = Identity(method); var words = original.Local.Words.Concat(["default", "body"]).ToArray(); var name = new Name(words); var index = 0;
            while (identities.Values.Concat(interfaceDefaultIdentities.Values).Any(id => id.Module.Key == original.Module.Key && id.Local.Key == name.Key)) name = new Name(words.Append((++index).ToString(System.Globalization.CultureInfo.InvariantCulture)).ToArray());
            return interfaceDefaultIdentities[key] = original with { Local = name };
        }
        private IrType<Unit> InterfaceDefaultOutput(IMethodSymbol method, Location location) => InstantiateCallableOutput(method, methodOutputs[InterfaceDefaultKey(method)], location);
    }
}
