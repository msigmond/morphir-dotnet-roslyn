using System.Numerics;
using System.Text;
using Morphir.Tests;

namespace Morphir.Frontend.CSharp.IntegrationTests;

public partial class NumericFactoryTests
{
    private sealed record FloatingDomain(string Code, int Precision, Func<double, object> Native);
    private static readonly FloatingDomain[] FloatingDomains = [new("System.Half", 11, value => (Half)value), new("float", 24, value => (float)value), new("double", 53, value => value)];
    private static double NativeReal(object value) => value switch { Half number => (double)number, float number => number, double number => number, _ => throw new InvalidOperationException("Expected a floating value") };
    private static void SameReal(double expected, object value)
    {
        var actual = Assert.IsType<double>(value); if (double.IsNaN(expected)) Assert.True(double.IsNaN(actual)); else Assert.Equal(BitConverter.DoubleToInt64Bits(expected), BitConverter.DoubleToInt64Bits(actual));
    }
    private static IEnumerable<(object Native, double Encoded)> RealInputs(FloatingDomain source, IEnumerable<double> values) => values.Select(value => source.Native(value)).Select(value => (Native: value, Encoded: NativeReal(value))).DistinctBy(value => BitConverter.DoubleToInt64Bits(value.Encoded));
    private static IEnumerable<double> RealEdges(int bits, bool signed)
    {
        var boundary = Math.ScaleB(1d, signed ? bits - 1 : bits);
        return [double.NaN, double.NegativeInfinity, double.PositiveInfinity, double.MinValue, double.MaxValue, -1.99d, -1d, -0.99d, -0d, 0d, 0.99d, 1d, Math.BitDecrement(boundary), boundary, Math.BitIncrement(boundary), Math.BitDecrement(-boundary), -boundary, Math.BitIncrement(-boundary)];
    }

    [Fact]
    public void FloatingSaturatingTruncatingAndStandardIntegerApisMatchTargetBoundaries()
    {
        var targets = Domains.Where(domain => domain.Bits != 0).Append(new Domain("char", 16, false, value => (char)(ushort)value)).ToArray(); var code = new StringBuilder("namespace Example.D10;public static class Rules{");
        for (var source = 0; source < FloatingDomains.Length; source++) for (var target = 0; target < targets.Length; target++) foreach (var mode in new[] { "Saturating", "Truncating", "Integer" })
                {
                    if (targets[target].Code == "char" && mode != "Integer") continue;
                    var input = FloatingDomains[source].Code; var output = targets[target].Code; var call = mode == "Integer" ? $"{input}.ConvertToInteger<{output}>(value)" : $"{output}.Create{mode}<{input}>(value)";
                    code.Append($"public static {output} FI{source}_{target}_{mode}({input} value)=>{call};");
                }
        code.Append('}'); var text = code.ToString(); var library = Support.Success(Support.Convert(text, selectors: ["Example.D10"])); var rules = Support.Emit(text).GetType("Example.D10.Rules")!;
        for (var source = 0; source < FloatingDomains.Length; source++) for (var target = 0; target < targets.Length; target++) foreach (var mode in new[] { "Saturating", "Truncating", "Integer" })
                {
                    if (targets[target].Code == "char" && mode != "Integer") continue;
                    var name = $"FI{source}_{target}_{mode}";
                    foreach (var value in RealInputs(FloatingDomains[source], RealEdges(targets[target].Bits, targets[target].Signed)))
                    {
                        var expected = NativeInteger(rules.GetMethod(name)!.Invoke(null, [value.Native])!); var actual = Result(targets[target], Support.Call(library, "D10.Rules", name, value.Encoded));
                        Assert.True(expected == actual, $"{name} input={value.Encoded:R} bits={BitConverter.DoubleToInt64Bits(value.Encoded):X16}: expected {expected}, actual {actual}");
                    }
                }
    }

    private static IEnumerable<BigInteger> PrecisionIntegers(Domain source, int precision)
    {
        var values = new HashSet<BigInteger> { source.Minimum, source.Maximum, BigInteger.Zero, BigInteger.One, -BigInteger.One };
        foreach (var bit in new[] { precision, precision + 1 }) foreach (var offset in new[] { -3, -1, 0, 1, 3 }) { values.Add((BigInteger.One << bit) + offset); values.Add(-(BigInteger.One << bit) + offset); }
        if (source.Bits == 0) { values.Add(BigInteger.One << 1100); values.Add(-(BigInteger.One << 1100)); }
        return values.Where(value => source.Bits == 0 || value >= source.Minimum && value <= source.Maximum);
    }
    private static IEnumerable<(object Native, object Encoded)> OutputInputs(int source, int precision)
    {
        if (source < Domains.Length) return PrecisionIntegers(Domains[source], precision).Select(value => (Domains[source].Native(value), Domains[source].Encode(value)));
        source -= Domains.Length;
        if (source < FloatingDomains.Length) return RealInputs(FloatingDomains[source], [double.NaN, double.NegativeInfinity, double.PositiveInfinity, double.MinValue, double.MaxValue, -0d, 0d, 1d, -1d, 0.1d, 1.0004882812500002d, 65504d, 65520d, Math.ScaleB(1d, 24) + 1d, Math.ScaleB(1d, 53)]).Select(value => (value.Native, (object)value.Encoded));
        source -= FloatingDomains.Length;
        if (source == 0) return new[] { decimal.MinValue, decimal.MaxValue, -0.1m, 0m, new decimal(0, 0, 0, true, 8), 1.2300m, 0.1m }.Select(value => ((object)value, (object)DecimalData(value)));
        return new[] { double.NaN, double.NegativeInfinity, double.PositiveInfinity, -0d, 0d, 0.1d, 65520d, Math.ScaleB(1d, 128) }.SelectMany(real => new[] { 0d, 2d, double.NaN }.Select(imaginary => ((object)new Complex(real, imaginary), (object)new Dictionary<string, object> { { "real", real }, { "imaginary", imaginary } })));
    }

    [Fact]
    public void FloatingAndComplexOutputFactoriesRetainRoundingAndComponentSemantics()
    {
        var sources = Domains.Select(domain => domain.Code).Concat(FloatingDomains.Select(domain => domain.Code)).Concat(["decimal", "System.Numerics.Complex"]).ToArray(); var targets = FloatingDomains.Select(domain => domain.Code).Append("System.Numerics.Complex").ToArray();
        var code = new StringBuilder("namespace Example.D10;public static class Rules{");
        for (var source = 0; source < sources.Length; source++) for (var target = 0; target < targets.Length; target++) foreach (var mode in new[] { "Checked", "Saturating", "Truncating" })
                {
                    var guard = sources[source] == "System.Numerics.Complex" && target < FloatingDomains.Length && mode == "Checked" ? "if(value.Imaginary!=0d)return default;" : "";
                    code.Append($"public static {targets[target]} FO{source}_{target}_{mode}({sources[source]} value){{{guard}return {targets[target]}.Create{mode}<{sources[source]}>(value);}}");
                }
        code.Append('}'); var text = code.ToString(); var library = Support.Success(Support.Convert(text, selectors: ["Example.D10"])); var rules = Support.Emit(text).GetType("Example.D10.Rules")!;
        for (var source = 0; source < sources.Length; source++) for (var target = 0; target < targets.Length; target++) foreach (var mode in new[] { "Checked", "Saturating", "Truncating" })
                {
                    var name = $"FO{source}_{target}_{mode}"; var precision = target < FloatingDomains.Length ? FloatingDomains[target].Precision : 53;
                    foreach (var value in OutputInputs(source, precision))
                    {
                        var expected = rules.GetMethod(name)!.Invoke(null, [value.Native])!; var actual = Support.Call(library, "D10.Rules", name, value.Encoded);
                        if (expected is Complex complex) { var record = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object>>(actual); SameReal(complex.Real, record["real"]); SameReal(complex.Imaginary, record["imaginary"]); } else SameReal(NativeReal(expected), actual);
                    }
                }
    }

    [Fact]
    public void UnboundedAndNativeIntegerConversionsRequireTheirActualSafetyDomain()
    {
        var code = new StringBuilder("namespace Example.D10;public static class Rules{");
        for (var source = 0; source < FloatingDomains.Length; source++) foreach (var mode in new[] { "Checked", "Saturating", "Truncating", "Integer", "IntegerNative" })
            {
                var type = FloatingDomains[source].Code; var call = mode.StartsWith("Integer", StringComparison.Ordinal) ? $"{type}.ConvertTo{mode}<System.Numerics.BigInteger>(value)" : $"System.Numerics.BigInteger.Create{mode}<{type}>(value)";
                code.Append($"public static System.Numerics.BigInteger Big{source}_{mode}({type} value){{if(!{type}.IsFinite(value))return default;return {call};}}");
            }
        var targets = Domains.Append(new Domain("char", 16, false, value => (char)(ushort)value)).ToArray();
        for (var target = 0; target < targets.Length; target++) code.Append($"public static {targets[target].Code} Native{target}(double value){{if(value!=0d)return default;return double.ConvertToIntegerNative<{targets[target].Code}>(value);}}");
        code.Append("public static System.Numerics.BigInteger NaN()=>System.Numerics.BigInteger.CreateTruncating(double.NaN);}");
        var text = code.ToString(); var library = Support.Success(Support.Convert(text, selectors: ["Example.D10"])); var rules = Support.Emit(text).GetType("Example.D10.Rules")!;
        for (var source = 0; source < FloatingDomains.Length; source++) foreach (var mode in new[] { "Checked", "Saturating", "Truncating", "Integer", "IntegerNative" }) foreach (var value in RealInputs(FloatingDomains[source], RealEdges(128, true)))
                {
                    var name = $"Big{source}_{mode}"; Assert.Equal((BigInteger)rules.GetMethod(name)!.Invoke(null, [value.Native])!, Support.IntegerResult(Support.Call(library, "D10.Rules", name, value.Encoded)));
                }
        for (var target = 0; target < targets.Length; target++) foreach (var value in new[] { double.NaN, double.NegativeInfinity, -1d, -0d, 0d, 1d, double.PositiveInfinity }) Assert.Equal(NativeInteger(rules.GetMethod("Native" + target)!.Invoke(null, [value])!), Result(targets[target], Support.Call(library, "D10.Rules", "Native" + target, value)));
        Assert.Equal(BigInteger.Zero, Support.IntegerResult(Support.Call(library, "D10.Rules", "NaN")));
        foreach (var (body, diagnostic) in new[] { ("public static int Unsafe(double value)=>int.CreateChecked(value);", "MCS2610"), ("public static int Unsafe(double value)=>double.ConvertToIntegerNative<int>(value);", "MCS2612"), ("public static double Unsafe(System.Numerics.Complex value)=>double.CreateChecked(value);", "MCS2611") })
        {
            var rejected = Support.Convert("namespace Example.D10;public static class Rules{" + body + "}", selectors: ["Example.D10"]); Assert.Null(rejected.Distribution); Assert.Contains(rejected.Diagnostics, item => item.Code == diagnostic);
        }
    }

    [Fact]
    public void ComplexUnboundedFactoriesKeepTheirNaNFailureAndIgnoreFiniteImaginaryComponents()
    {
        foreach (var mode in new[] { "Saturating", "Truncating" })
        {
            var unsafeCode = "namespace Example.D10;public static class Rules{public static System.Numerics.BigInteger Unsafe()=>System.Numerics.BigInteger.Create" + mode + "(new System.Numerics.Complex(double.NaN,0d));}";
            var source = Support.Emit(unsafeCode).GetType("Example.D10.Rules")!; var thrown = Assert.Throws<System.Reflection.TargetInvocationException>(() => source.GetMethod("Unsafe")!.Invoke(null, null)); Assert.IsType<OverflowException>(thrown.InnerException);
            var rejected = Support.Convert(unsafeCode, selectors: ["Example.D10"]); Assert.Null(rejected.Distribution); Assert.Contains(rejected.Diagnostics, item => item.Code == "MCS2604");
            var finiteCode = "namespace Example.D10;public static class Rules{public static System.Numerics.BigInteger Finite()=>System.Numerics.BigInteger.Create" + mode + "(new System.Numerics.Complex(1.9d,2d));}";
            var library = Support.Success(Support.Convert(finiteCode, selectors: ["Example.D10"])); var expected = (BigInteger)Support.Emit(finiteCode).GetType("Example.D10.Rules")!.GetMethod("Finite")!.Invoke(null, null)!; Assert.Equal(expected, Support.IntegerResult(Support.Call(library, "D10.Rules", "Finite")));
        }
    }
}
