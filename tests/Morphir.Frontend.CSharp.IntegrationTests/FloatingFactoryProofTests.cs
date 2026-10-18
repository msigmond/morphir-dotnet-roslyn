using System.Globalization;
using System.Numerics;
using System.Text;
using Morphir.Tests;

namespace Morphir.Frontend.CSharp.IntegrationTests;

public partial class NumericFactoryTests
{
    [Fact]
    public void CheckedAndNativeFloatingFactoriesUseNaNAwareTruncatedBounds()
    {
        var targets = Domains.Where(domain => domain.Bits != 0).ToArray();
        var code = new StringBuilder("namespace Example.D10;public static class Rules{");
        for (var source = 0; source < FloatingDomains.Length; source++) for (var target = 0; target < targets.Length; target++) foreach (var native in new[] { false, true })
                {
                    var input = FloatingDomains[source].Code; var output = targets[target].Code; var domain = targets[target];
                    var lower = (domain.Bits < 53 ? (double)domain.Minimum - 1d : (double)domain.Minimum).ToString("R", CultureInfo.InvariantCulture);
                    var upper = Math.ScaleB(1d, domain.Signed ? domain.Bits - 1 : domain.Bits).ToString("R", CultureInfo.InvariantCulture);
                    var lowerOp = domain.Bits < 53 ? ">" : ">=";
                    var call = native ? $"{input}.ConvertToIntegerNative<{output}>(value)" : $"{output}.CreateChecked<{input}>(value)";
                    code.Append($"public static {output} B{source}_{target}_{native}({input} value){{if((double)value{lowerOp}{lower}d&&(double)value<{upper}d)return {call};return default;}}");
                }
        code.Append("public static byte Finite(double value){if(!double.IsFinite(value)||value<=-1d||value>=256d)return 0;return byte.CreateChecked(value);}");
        code.Append("public static byte NaNGuard(double value){if(double.IsNaN(value)||value<=-1d||value>=256d)return 0;return byte.CreateChecked(value);}");
        code.Append("public static System.Numerics.BigInteger FiniteParts(double value){if(double.IsNaN(value)||double.IsInfinity(value))return default;return System.Numerics.BigInteger.CreateChecked(value);}");
        code.Append('}'); var text = code.ToString(); var library = Support.Success(Support.Convert(text, selectors: ["Example.D10"])); var rules = Support.Emit(text).GetType("Example.D10.Rules")!;
        for (var source = 0; source < FloatingDomains.Length; source++) for (var target = 0; target < targets.Length; target++) foreach (var native in new[] { false, true })
                {
                    var name = $"B{source}_{target}_{native}";
                    var edges = RealEdges(targets[target].Bits, targets[target].Signed).Concat([-129d, -128.99d, -128d, -1d, Math.BitIncrement(-1d), -0.99d, 255d, 255.99d, 256d]);
                    foreach (var value in RealInputs(FloatingDomains[source], edges))
                    {
                        var expected = NativeInteger(rules.GetMethod(name)!.Invoke(null, [value.Native])!); var actual = Result(targets[target], Support.Call(library, "D10.Rules", name, value.Encoded));
                        Assert.True(expected == actual, $"{name}({value.Encoded:R}): expected {expected}, actual {actual}");
                    }
                }
        foreach (var name in new[] { "Finite", "NaNGuard", "FiniteParts" }) foreach (var value in RealEdges(8, false).Append(double.Epsilon))
            {
                var expected = NativeInteger(rules.GetMethod(name)!.Invoke(null, [value])!); var actual = Support.Call(library, "D10.Rules", name, value);
                Assert.Equal(expected, name == "FiniteParts" ? Support.IntegerResult(actual) : NativeInteger(actual));
            }
    }

    [Fact]
    public void FloatingProofsRejectNaNHolesStaleBindingsAndInsufficientBounds()
    {
        foreach (var body in new[] {
            "if(value<0d||value>255d)return 0;return byte.CreateChecked(value);",
            "if(!double.IsFinite(value))return 0;return byte.CreateChecked(value);",
            "if(value>=0d&&value<256d){value=double.NaN;return byte.CreateChecked(value);}return 0;",
            "if(value>=0d&&value<256d){value=256d;return byte.CreateChecked(value);}return 0;",
            "if(double.IsInfinity(value)||value<0d||value>255d)return 0;return byte.CreateChecked(value);",
            "if(value>=0d&&value<=256d)return byte.CreateChecked(value);return 0;",
            "if(false)return byte.CreateChecked(value);return 0;" })
        {
            var result = Support.Convert("namespace Example.D10;public static class Rules{public static byte Unsafe(double value){" + body + "}}", selectors: ["Example.D10"]);
            Assert.Null(result.Distribution); Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "MCS2610" && diagnostic.Line > 0 && diagnostic.Column > 0);
        }
    }

    [Fact]
    public void HalfNativeIntegerApisUseTheirManagedSaturatingFallback()
    {
        var targets = Domains.Where(domain => domain.Bits != 0).Append(new Domain("char", 16, false, value => (char)(ushort)value)).ToArray();
        var text = "namespace Example.D10;public static class Rules{" + string.Join("", targets.Select((target, index) => $"public static {target.Code} H{index}(System.Half value)=>System.Half.ConvertToIntegerNative<{target.Code}>(value);")) + "}";
        var library = Support.Success(Support.Convert(text, selectors: ["Example.D10"])); var rules = Support.Emit(text).GetType("Example.D10.Rules")!;
        for (var target = 0; target < targets.Length; target++) foreach (var value in RealInputs(FloatingDomains[0], RealEdges(targets[target].Bits, targets[target].Signed)))
            {
                var name = "H" + target; Assert.Equal(NativeInteger(rules.GetMethod(name)!.Invoke(null, [value.Native])!), Result(targets[target], Support.Call(library, "D10.Rules", name, value.Encoded)));
            }
    }

    [Fact]
    public void StoredFloatingPredicatesRetainSnapshotProofsAcrossLocalAndOwnedDataPaths()
    {
        const string code = """
        namespace Example.D10;
        public sealed class Box {public double Value;public Box(double value){Value=value;}}
        public static class Rules{
          public static byte Stored(double value){bool valid=value>=0d&&value<256d;if(valid)return byte.CreateChecked(value);return 0;}
          public static byte Finite(double value){bool finite=double.IsFinite(value);if(!finite||value<=-1d||value>=256d)return 0;return byte.CreateChecked(value);}
          public static byte Owned(double value){var box=new Box(value);var alias=box;bool valid=box.Value>=0d&&box.Value<256d;if(valid)return byte.CreateChecked(alias.Value);return 0;}
          public static byte Snapshot(double value){var box=new Box(value);var snapshot=box.Value;bool valid=snapshot>=0d&&snapshot<256d;box.Value=double.NaN;if(valid)return byte.CreateChecked(snapshot);return 0;}
          public static byte Constant(){var box=new Box(123.5d);return byte.CreateChecked(box.Value);}
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var name in new[] { "Stored", "Finite", "Owned", "Snapshot" }) foreach (var value in RealEdges(8, false).Concat([255.99d, 256d, double.Epsilon])) Assert.Equal((byte)rules.GetMethod(name)!.Invoke(null, [value])!, NativeInteger(Support.Call(library, "D10.Rules", name, value)));
        Assert.Equal(123, NativeInteger(Support.Call(library, "D10.Rules", "Constant")));
        foreach (var body in new[] {
            "bool valid=value>=0d&&value<256d;value=other;if(valid)return byte.CreateChecked(value);return 0;",
            "var box=new Box(value);bool valid=box.Value>=0d&&box.Value<256d;box.Value=other;if(valid)return byte.CreateChecked(box.Value);return 0;"})
        {
            var source = "namespace Example.D10;public sealed class Box{public double Value;public Box(double value){Value=value;}}public static class Rules{public static byte Unsafe(double value,double other){" + body + "}}";
            var rejected = Support.Convert(source, selectors: ["Example.D10"]); Assert.Null(rejected.Distribution); Assert.Contains(rejected.Diagnostics, diagnostic => diagnostic.Code == "MCS2610");
        }
    }
}
