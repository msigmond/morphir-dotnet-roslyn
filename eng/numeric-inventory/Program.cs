using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Morphir.Frontend.CSharp;
var compilation = SourceCompilation.Create([]);
foreach (var name in new[]{"System.Char","System.Byte","System.SByte","System.Int16","System.UInt16","System.Int32","System.UInt32","System.Int64","System.UInt64","System.IntPtr","System.UIntPtr","System.Single","System.Double","System.Half","System.Int128","System.UInt128","System.Numerics.BigInteger","System.Numerics.Complex"})
{
 var type=compilation.GetTypeByMetadataName(name)!;
 foreach(var member in type.GetMembers().Where(m=>m.DeclaredAccessibility==Accessibility.Public && m is not IMethodSymbol {MethodKind:MethodKind.PropertyGet or MethodKind.PropertySet}).OrderBy(m=>m.ToDisplayString(),StringComparer.Ordinal))
 Console.WriteLine(member.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
}
