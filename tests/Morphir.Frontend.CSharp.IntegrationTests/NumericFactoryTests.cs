using System.Globalization;
using System.Numerics;
using System.Text;
using Morphir.Tests;

namespace Morphir.Frontend.CSharp.IntegrationTests;

[Trait("Delivery", "D10")]
public partial class NumericFactoryTests
{
    [Fact]
    public void IntegralFactoriesHaveCompleteIndependentParity() => Support.Parity("fixtures/D10/expected/integer-factories.json", Support.Read("fixtures/D10/csharp/IntegerFactories.cs"), "Example", "Example.D10");

    [Fact]
    public void DecimalFactoriesHaveCompleteIndependentParity() => Support.Parity("fixtures/D10/expected/decimal-factories.json", Support.Read("fixtures/D10/csharp/DecimalFactories.cs"), "Example", "Example.D10");

    [Fact]
    public void FloatingFactoriesHaveCompleteIndependentParity() => Support.Parity("fixtures/D10/expected/floating-factories.json", Support.Read("fixtures/D10/csharp/FloatingFactories.cs"), "Example", "Example.D10");

    private sealed record Domain(string Code, int Bits, bool Signed, Func<BigInteger, object> Native)
    {
        public BigInteger Minimum => Bits == 0 ? -(BigInteger.One << 300) : Signed ? -(BigInteger.One << (Bits - 1)) : BigInteger.Zero;
        public BigInteger Maximum => Bits == 0 ? BigInteger.One << 300 : (BigInteger.One << (Signed ? Bits - 1 : Bits)) - 1;
        public object Encode(BigInteger value) => Bits == 0 || Bits > 32 ? Support.Integer(value) : (long)value;
    }
    private static readonly Domain[] Domains = [
      new("byte",8,false,value=>(byte)value),new("sbyte",8,true,value=>(sbyte)value),new("short",16,true,value=>(short)value),new("ushort",16,false,value=>(ushort)value),
      new("int",32,true,value=>(int)value),new("uint",32,false,value=>(uint)value),new("long",64,true,value=>(long)value),new("ulong",64,false,value=>(ulong)value),
      new("nint",64,true,value=>(nint)(long)value),new("nuint",64,false,value=>(nuint)(ulong)value),new("System.Int128",128,true,value=>(Int128)value),new("System.UInt128",128,false,value=>(UInt128)value),
      new("System.Numerics.BigInteger",0,true,value=>value)];
    private static IEnumerable<BigInteger> Inputs(Domain domain)
    {
        var values = new HashSet<BigInteger> { domain.Minimum, domain.Minimum + 1, domain.Maximum, domain.Maximum - 1, BigInteger.Zero, BigInteger.One, -BigInteger.One };
        foreach (var bits in new[] { 7, 8, 15, 16, 31, 32, 63, 64, 127, 128, 255 }) foreach (var delta in new[] { -1, 0, 1 }) { values.Add((BigInteger.One << bits) + delta); values.Add(-(BigInteger.One << bits) + delta); }
        return values.Where(value => value >= domain.Minimum && value <= domain.Maximum).Order();
    }
    private static BigInteger NativeInteger(object value) => value is char character ? character : BigInteger.Parse(Convert.ToString(value, CultureInfo.InvariantCulture)!, CultureInfo.InvariantCulture);
    private static BigInteger Result(Domain domain, object value) => domain.Bits == 0 || domain.Bits > 32 ? Support.IntegerResult(value) : NativeInteger(value);

    [Fact]
    public void IntegralSaturatingAndTruncatingFactoriesMatchThePinnedRuntimeAcrossAllDomains()
    {
        var sources = Domains.Append(new Domain("char", 16, false, value => (char)(ushort)value)).ToArray();
        var code = new StringBuilder("namespace Example.D10;public static class Rules{");
        for (var target = 0; target < Domains.Length; target++) for (var source = 0; source < sources.Length; source++) foreach (var mode in new[] { "Saturating", "Truncating" }) code.Append($"public static {Domains[target].Code} F{target}_{source}_{mode}({sources[source].Code} value)=>{Domains[target].Code}.Create{mode}<{sources[source].Code}>(value);");
        code.Append('}'); var text = code.ToString(); var library = Support.Success(Support.Convert(text, selectors: ["Example.D10"])); var rules = Support.Emit(text).GetType("Example.D10.Rules")!;
        for (var target = 0; target < Domains.Length; target++) for (var source = 0; source < sources.Length; source++) foreach (var mode in new[] { "Saturating", "Truncating" })
                {
                    var name = $"F{target}_{source}_{mode}"; var method = rules.GetMethod(name)!;
                    foreach (var value in Inputs(sources[source])) Assert.Equal(NativeInteger(method.Invoke(null, [sources[source].Native(value)])!), Result(Domains[target], Support.Call(library, "D10.Rules", name, sources[source].Encode(value))));
                }
    }

    [Fact]
    public void CheckedFactoriesRequireAndUseSourceBoundsIncludingUnreachableCalls()
    {
        var code = new StringBuilder("namespace Example.D10;public static class Rules{");
        for (var target = 0; target < Domains.Length; target++)
        {
            var type = Domains[target].Code; var guard = Domains[target].Bits == 0 ? "" : $"if(value<(System.Numerics.BigInteger){type}.MinValue||value>(System.Numerics.BigInteger){type}.MaxValue)return 0;";
            code.Append($"public static {type} F{target}(System.Numerics.BigInteger value){{{guard}return {type}.CreateChecked<System.Numerics.BigInteger>(value);}}");
        }
        code.Append('}'); var text = code.ToString(); var library = Support.Success(Support.Convert(text, selectors: ["Example.D10"])); var rules = Support.Emit(text).GetType("Example.D10.Rules")!;
        for (var target = 0; target < Domains.Length; target++) foreach (var value in Inputs(Domains[target]).Concat([Domains[target].Minimum - 1, Domains[target].Maximum + 1]))
            {
                var name = $"F{target}"; Assert.Equal(NativeInteger(rules.GetMethod(name)!.Invoke(null, [value])!), Result(Domains[target], Support.Call(library, "D10.Rules", name, Support.Integer(value))));
            }
        foreach (var body in new[] { "return byte.CreateChecked<long>(value);", "if(false)return byte.CreateChecked<long>(value);return 0;" })
        {
            var result = Support.Convert("namespace Example.D10;public static class Rules{public static byte Unsafe(long value){" + body + "}}", selectors: ["Example.D10"]); Assert.Null(result.Distribution); Assert.Contains(result.Diagnostics, item => item.Code == "MCS2610");
        }
    }

    [Fact]
    public void ConcreteFactoryMethodReferencesPreserveTheirExactConversionModes()
    {
        const string code = """
        namespace Example.D10;
        public static class Rules{
          public static byte Saturating(long value){System.Func<long,byte> convert=byte.CreateSaturating<long>;return convert(value);}
          public static byte Truncating(long value){System.Func<long,byte> convert=byte.CreateTruncating<long>;return convert(value);}
          public static long Widen(byte value){System.Func<byte,long> convert=long.CreateChecked<byte>;return convert(value);}
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var value in new[] { long.MinValue, -1, 0, 1, 127, 128, 255, 256, long.MaxValue }) foreach (var name in new[] { "Saturating", "Truncating" }) Assert.Equal(NativeInteger(rules.GetMethod(name)!.Invoke(null, [value])!), NativeInteger(Support.Call(library, "D10.Rules", name, Support.Integer(value))));
        foreach (var value in new byte[] { 0, 1, 127, 128, 255 }) Assert.Equal(NativeInteger(rules.GetMethod("Widen")!.Invoke(null, [value])!), Support.IntegerResult(Support.Call(library, "D10.Rules", "Widen", (int)value)));
    }

    private static Dictionary<string, object> DecimalData(decimal value)
    {
        var bits = decimal.GetBits(value); var coefficient = (BigInteger)(uint)bits[0] + ((BigInteger)(uint)bits[1] << 32) + ((BigInteger)(uint)bits[2] << 64);
        return new() { { "coefficient", Support.Integer(coefficient) }, { "scale", (bits[3] >> 16) & 255 }, { "negative", bits[3] < 0 }, { "value", value } };
    }
    private static void SameDecimal(decimal expected, object actual)
    {
        var wanted = DecimalData(expected); var record = Assert.IsType<Dictionary<string, object>>(actual);
        Assert.Equal(Support.IntegerResult(wanted["coefficient"]), Support.IntegerResult(record["coefficient"])); foreach (var field in new[] { "scale", "negative", "value" }) Assert.Equal(wanted[field], record[field]);
    }

    [Fact]
    public void DecimalIntegralFactoriesAndBothIntegerApisMatchAllIntegralDomains()
    {
        var targets = Domains.Append(new Domain("char", 16, false, value => (char)(ushort)value)).ToArray(); var code = new StringBuilder("namespace Example.D10;public static class Rules{");
        for (var target = 0; target < targets.Length; target++) foreach (var mode in new[] { "Saturating", "Truncating", "Integer", "IntegerNative" })
            {
                var type = targets[target].Code; var call = mode.StartsWith("Integer", StringComparison.Ordinal) ? $"decimal.ConvertTo{mode}<{type}>(value)" : $"{type}.Create{mode}<decimal>(value)";
                if (type == "char" && !mode.StartsWith("Integer", StringComparison.Ordinal)) continue;
                code.Append($"public static {type} F{target}_{mode}(decimal value)=>{call};");
            }
        code.Append('}'); var text = code.ToString(); var library = Support.Success(Support.Convert(text, selectors: ["Example.D10"])); var rules = Support.Emit(text).GetType("Example.D10.Rules")!;
        var values = new HashSet<decimal> { decimal.MinValue, decimal.MaxValue, -1.99m, -0.99m, 0m, 0.1m, 1.99m, 127.99m, 128m, 255.99m, 256m, new decimal(0, 0, 0, true, 8) };
        for (var target = 0; target < targets.Length; target++) foreach (var mode in new[] { "Saturating", "Truncating", "Integer", "IntegerNative" })
            {
                if (targets[target].Code == "char" && !mode.StartsWith("Integer", StringComparison.Ordinal)) continue;
                var samples = new HashSet<decimal>(values); var domain = targets[target];
                if (domain.Bits is > 0 and < 96) foreach (var limit in new[] { (decimal)domain.Minimum, (decimal)domain.Maximum }) foreach (var delta in new[] { -1m, -0.1m, 0m, 0.1m, 1m }) samples.Add(limit + delta);
                var name = $"F{target}_{mode}"; foreach (var value in samples) Assert.Equal(NativeInteger(rules.GetMethod(name)!.Invoke(null, [value])!), Result(targets[target], Support.Call(library, "D10.Rules", name, DecimalData(value))));
            }
    }

    [Fact]
    public void DecimalFactoriesRetainCoefficientScaleSignAndSourceSafetyProofs()
    {
        var code = new StringBuilder("namespace Example.D10;public static class Rules{");
        for (var source = 0; source < Domains.Length; source++) foreach (var mode in new[] { "Saturating", "Truncating" }) code.Append($"public static decimal F{source}_{mode}({Domains[source].Code} value)=>decimal.Create{mode}<{Domains[source].Code}>(value);");
        code.Append("public static decimal Checked(System.Numerics.BigInteger value){if(value<(System.Numerics.BigInteger)decimal.MinValue||value>(System.Numerics.BigInteger)decimal.MaxValue)return 0m;return decimal.CreateChecked<System.Numerics.BigInteger>(value);}public static decimal Price(byte value)=>decimal.CreateSaturating<byte>(value)+0.1m;public static byte Narrow(byte value)=>byte.CreateChecked<int>(int.CreateSaturating<byte>(value));");
        foreach (var mode in new[] { "Checked", "Saturating", "Truncating" }) code.Append($"public static decimal Identity{mode}(decimal value)=>decimal.Create{mode}<decimal>(value);");
        code.Append('}'); var text = code.ToString(); var library = Support.Success(Support.Convert(text, selectors: ["Example.D10"])); var rules = Support.Emit(text).GetType("Example.D10.Rules")!;
        for (var source = 0; source < Domains.Length; source++) foreach (var mode in new[] { "Saturating", "Truncating" }) foreach (var value in Inputs(Domains[source]).Concat(new[] { (BigInteger)decimal.MinValue - 1, (BigInteger)decimal.MinValue, (BigInteger)decimal.MinValue + 1, (BigInteger)decimal.MaxValue - 1, (BigInteger)decimal.MaxValue, (BigInteger)decimal.MaxValue + 1 }).Where(value => value >= Domains[source].Minimum && value <= Domains[source].Maximum).Distinct())
                {
                    var name = $"F{source}_{mode}"; SameDecimal((decimal)rules.GetMethod(name)!.Invoke(null, [Domains[source].Native(value)])!, Support.Call(library, "D10.Rules", name, Domains[source].Encode(value)));
                }
        foreach (var value in new[] { -(BigInteger.One << 128), -(BigInteger)decimal.MaxValue, BigInteger.Zero, (BigInteger)decimal.MaxValue, BigInteger.One << 128 }) SameDecimal((decimal)rules.GetMethod("Checked")!.Invoke(null, [value])!, Support.Call(library, "D10.Rules", "Checked", Support.Integer(value)));
        foreach (var value in new[] { 0.1m, 1.2300m, new decimal(0, 0, 0, true, 8), decimal.MaxValue, decimal.MinValue }) foreach (var mode in new[] { "Checked", "Saturating", "Truncating" }) SameDecimal(value, Support.Call(library, "D10.Rules", "Identity" + mode, DecimalData(value)));
        foreach (var value in new byte[] { 0, 1, 127, 128, 255 }) { SameDecimal(value + 0.1m, Support.Call(library, "D10.Rules", "Price", (int)value)); Assert.Equal((int)value, Support.Call(library, "D10.Rules", "Narrow", (int)value)); }
        var rejected = Support.Convert("namespace Example.D10;public static class Rules{public static decimal Unsafe(System.Numerics.BigInteger value)=>decimal.CreateChecked<System.Numerics.BigInteger>(value);}", selectors: ["Example.D10"]); Assert.Null(rejected.Distribution); Assert.Contains(rejected.Diagnostics, item => item.Code == "MCS2703");
    }
}
