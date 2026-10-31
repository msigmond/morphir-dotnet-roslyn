using System.Text.Json;
using Microsoft.CodeAnalysis;
using Morphir.Frontend.CSharp;
var baseline=SourceCompilation.Create([]);
var types=new[]{"System.Char","System.Byte","System.SByte","System.Int16","System.UInt16","System.Int32","System.UInt32","System.Int64","System.UInt64","System.IntPtr","System.UIntPtr","System.Single","System.Double","System.Half","System.Int128","System.UInt128","System.Numerics.BigInteger","System.Numerics.Complex","System.Decimal"};
var symbols=types.SelectMany(n=>baseline.GetTypeByMetadataName(n)!.GetMembers()).Where(m=>m.DeclaredAccessibility==Accessibility.Public && m is not IMethodSymbol {MethodKind:MethodKind.PropertyGet or MethodKind.PropertySet}).ToDictionary(m=>m.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
var tokens=new Dictionary<string,string>{{"Addition","+"},{"Subtraction","-"},{"Multiply","*"},{"Division","/"},{"Modulus","%"},{"BitwiseAnd","&"},{"BitwiseOr","|"},{"ExclusiveOr","^"},{"LeftShift","<<"},{"RightShift",">>"},{"UnsignedRightShift",">>>"},{"Equality","=="},{"Inequality","!="},{"LessThan","<"},{"LessThanOrEqual","<="},{"GreaterThan",">"},{"GreaterThanOrEqual",">="},{"UnaryPlus","+"},{"UnaryNegation","-"},{"OnesComplement","~"},{"Increment","++"},{"Decrement","--"}};
var result=new List<object>();
foreach(var file in new[]{"numeric-members","decimal-members"})
{
 using var catalog=JsonDocument.Parse(File.ReadAllText("docs/catalogs/"+file+".json"));
 foreach(var row in catalog.RootElement.GetProperty("members").EnumerateArray().Where(r=>r.GetProperty("owner").GetString() is "D5" or "D6"))
 {
  var key=row.GetProperty("member").GetString()!;var symbol=symbols[key];
  var inputs=new List<(string Type,string Name)>();string expr;ITypeSymbol output;
  string Type(ITypeSymbol t)=>t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
  var owner=Type(symbol.ContainingType);
  if(symbol is IMethodSymbol method)
  {
   inputs.AddRange(method.Parameters.Select((p,i)=>(Type(p.Type),"p"+i)));output=method.MethodKind==MethodKind.Constructor?method.ContainingType:method.ReturnType;
   var argumentText=string.Join(",",inputs.Select(i=>i.Name));
   if(method.MethodKind==MethodKind.Constructor)expr="new "+owner+"("+argumentText+")";
   else if(method.MethodKind==MethodKind.Conversion)expr="("+Type(output)+")p0";
   else if(method.MethodKind==MethodKind.UserDefinedOperator)
   {
    var name=method.MetadataName[3..].Replace("Checked","");var token=tokens.GetValueOrDefault(name,"UNMAPPED");
    expr=inputs.Count==1?token+"p0":"p0 "+token+" p1";
   }
   else {if(!method.IsStatic)inputs.Insert(0,(owner,"receiver"));expr=(method.IsStatic?owner:"receiver")+"."+method.Name+"("+argumentText+")";}
   if(method.MetadataName.Contains("Checked",StringComparison.Ordinal))expr="checked("+expr+")";
  }
  else
  {
   output=symbol is IFieldSymbol field?field.Type:((IPropertySymbol)symbol).Type;
   if(!symbol.IsStatic)inputs.Add((owner,"receiver"));expr=(symbol.IsStatic?owner:"receiver")+"."+symbol.Name;
  }
  var member="public static "+Type(output)+" F("+string.Join(",",inputs.Select(i=>i.Type+" "+i.Name))+")=>"+expr+";";
  var code="namespace Example.Inventory;public static class Rules{"+member+"}";
  var compilation=SourceCompilation.Create([("Input.cs",code)]);var converted=Frontend.Convert(compilation,new("Example",["Example.Inventory"]));
  result.Add(new{file,key,member,expr,output=Type(output),inputs=inputs.Select(i=>new{type=i.Type,name=i.Name}),success=converted.Success,diagnostics=converted.Diagnostics});
 }
}
File.WriteAllText("artifacts/d1d9-inventory/results.json",JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine("Probed "+result.Count+" catalog entries; no IR output was used as reference.");
