using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Morphir.Frontend.CSharp;
var compilation = SourceCompilation.Create([]);
foreach (var name in new[]{"System.Decimal"})
{
 var type=compilation.GetTypeByMetadataName(name)!;
 foreach(var member in type.GetMembers().Where(m=>m.DeclaredAccessibility==Accessibility.Public && m is not IMethodSymbol {MethodKind:MethodKind.PropertyGet or MethodKind.PropertySet}).OrderBy(m=>m.ToDisplayString(),StringComparer.Ordinal))
 Console.WriteLine(member.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
}
