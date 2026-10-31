using Microsoft.CodeAnalysis;
using Morphir.IR.Classic;
using IrPath = Morphir.IR.Classic.Path;

namespace Morphir.Frontend.CSharp;

internal sealed partial class SourceTranslation
{
    private static FQName? SdkConstant(ISymbol symbol)
    {
        var module = "Basics";
        var name = symbol.ContainingType?.ToDisplayString() == "System.Math" ? symbol.Name switch { "PI" => "pi", "E" => "e", _ => null } : null;
        if (symbol.ContainingType?.SpecialType == SpecialType.System_Decimal)
        { module = "Decimal"; name = symbol.Name switch { "Zero" => "zero", "One" => "one", "MinusOne" => "minusOne", _ => null }; }
        return name is null ? null : new(Types.Sdk, IrPath.From(module), Name.From(name));
    }
    private FQName? SdkMethod(IMethodSymbol method)
    {
        var owner = method.ContainingType.ToDisplayString();
        var decimalType = method.ContainingType.SpecialType == SpecialType.System_Decimal;
        string? name = null; var module = "Basics";
        if (decimalType)
        {
            module = "Decimal";
            name = (method.Name, method.Parameters.Length, method.IsStatic) switch
            {
                ("Add", 2, true) => "add", ("Subtract", 2, true) => "sub", ("Multiply", 2, true) => "mul",
                ("Negate", 1, true) => "negate", ("Abs", 1, true) => "abs", ("Round", 1, true) => "round", ("Truncate", 1, true) => "truncate",
                ("Equals", 2, true) => "eq", ("Equals", 1, false) when method.Parameters[0].Type.SpecialType == SpecialType.System_Decimal => "eq",
                ("ToString", 0, false) => "toString", _ => null
            };
        }
        else if (owner is "System.Math" or "System.MathF" || method.ContainingType.SpecialType is SpecialType.System_Int32 or SpecialType.System_Int64 or SpecialType.System_Single or SpecialType.System_Double)
        {
            name = (method.Name, method.Parameters.Length) switch
            {
                ("Abs", 1) when method.ReturnType.SpecialType != SpecialType.System_Decimal => "abs",
                ("Min", 2) when method.ReturnType.SpecialType != SpecialType.System_Decimal => "min",
                ("Max", 2) when method.ReturnType.SpecialType != SpecialType.System_Decimal => "max",
                ("Sin", 1) => "sin", ("Cos", 1) => "cos", ("Tan", 1) => "tan", ("Asin", 1) => "asin", ("Acos", 1) => "acos", ("Atan", 1) => "atan", ("Atan2", 2) => "atan2",
                ("Sqrt", 1) => "sqrt", ("Pow", 2) => "power", ("IsNaN", 1) => "isNaN", ("IsInfinity", 1) => "isInfinite", _ => null
            };
        }
        if (method.IsStatic && owner is "System.Math" or "System.MathF" && method.Name == "Abs" && method.ReturnType.SpecialType == SpecialType.System_Decimal)
        { module = "Decimal"; name = "abs"; }
        if (!method.IsStatic && method.Name == "ToString" && method.Parameters.Length == 0)
        {
            if (Shape(Type(method.ContainingType)) == Shape(Types.Int)) { module = "String"; name = "fromInt"; }
            else if (Shape(Type(method.ContainingType)) == Shape(Types.Float)) { module = "String"; name = "fromFloat"; }
            else if (method.ContainingType.SpecialType == SpecialType.System_Char) { module = "String"; name = "fromChar"; }
        }
        if (name is null) return null;
        var inputs = method.Parameters.Select(p => Type(p.Type)).ToList();
        if (!method.IsStatic) inputs.Insert(0, Type(method.ContainingType));
        var result = Type(method.ReturnType);
        bool All(IrType<Unit> type) => inputs.All(t => Shape(t) == Shape(type));
        var valid = module switch
        {
            "Decimal" => All(Types.Decimal) && Shape(result) == Shape(name is "eq" ? Types.Bool : name is "toString" ? Types.String : Types.Decimal),
            "String" => Shape(result) == Shape(Types.String),
            _ when name is "isNaN" or "isInfinite" => All(Types.Float) && Shape(result) == Shape(Types.Bool),
            _ when name is "sin" or "cos" or "tan" or "asin" or "acos" or "atan" or "atan2" or "sqrt" => All(Types.Float) && Shape(result) == Shape(Types.Float),
            _ => (Shape(result) == Shape(Types.Int) || Shape(result) == Shape(Types.Float)) && All(result)
        };
        return valid ? new(Types.Sdk, IrPath.From(module), Name.From(name)) : null;
    }
}
