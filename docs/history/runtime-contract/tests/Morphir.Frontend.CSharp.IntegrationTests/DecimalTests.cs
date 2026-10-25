using System.Numerics;
using Morphir.Tests;

namespace Morphir.Frontend.CSharp.IntegrationTests;

[Trait("Delivery", "D6")]
public class DecimalTests
{
    [Fact]
    public void GuardedExactPriceRetainsDecimalCoefficientScaleSignAndSdkValue()
    {
        const string code = """
        namespace Example.D6;
        public static class Rules {
          public static decimal Price(int units){if(units<0 || units>1000)return 0m;return units*0.1m;}
          public static decimal Exact()=>0.1m;
          public static decimal Scaled()=>1.2300m;
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D6"]));
        var source = Support.Emit(code).GetType("Example.D6.Rules")!;
        foreach (var units in new[] { int.MinValue, -1, 0, 1, 3, 999, 1000, 1001, int.MaxValue })
        {
            var expected = (decimal)source.GetMethod("Price")!.Invoke(null, [units])!;
            Same(expected, Support.Call(lib, "D6.Rules", "Price", units));
        }
        Same(0.1m, Support.Call(lib, "D6.Rules", "Exact")); Same(1.2300m, Support.Call(lib, "D6.Rules", "Scaled"));
    }
    [Fact]
    public void DecimalMembersCastsConstructionAndCallSummariesPreserveSuccessfulBehavior()
    {
        const string code = """
        namespace Example.D6;
        public static class Rules {
          public static decimal Round(decimal a)=>decimal.Round(a,2);
          public static decimal Floor(decimal a)=>decimal.Floor(a);
          public static decimal Ceiling(decimal a)=>decimal.Ceiling(a);
          public static decimal Truncate(decimal a)=>decimal.Truncate(a);
          public static decimal Abs(decimal a)=>decimal.Abs(a);
          public static decimal Maximum(decimal a,decimal b)=>decimal.Max(a,b);
          public static decimal Minimum(decimal a,decimal b)=>decimal.Min(a,b);
          public static byte Scale(decimal a)=>a.Scale;
          public static System.Numerics.BigInteger Integer(decimal a)=>(System.Numerics.BigInteger)a;
          public static int Narrow(decimal a){if(a<0m || a>1000m)return 0;return (int)a;}
          private static decimal Tenth(int a){if(a<0 || a>1000)return 0m;return a*0.1m;}
          public static decimal Twice(int a)=>Tenth(a)*2m;
          public static decimal Parts(int lo,int mid,int hi,bool negative,byte scale){if(scale>28)return 0m;return new decimal(lo,mid,hi,negative,scale);}
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D6"])); var rules = Support.Emit(code).GetType("Example.D6.Rules")!;

        foreach (var a in new[] { decimal.MinValue, -2.555m, -0.00m, 0m, 1.2300m, 2.555m, 1000.1m, decimal.MaxValue })
        {
            foreach (var method in new[] { "Round", "Floor", "Ceiling", "Truncate", "Abs" }) Same((decimal)rules.GetMethod(method)!.Invoke(null, [a])!, Support.Call(lib, "D6.Rules", method, Data(a)));
            Assert.Equal((int)a.Scale, Support.Call(lib, "D6.Rules", "Scale", Data(a)));
            Assert.Equal((BigInteger)a, Support.IntegerResult(Support.Call(lib, "D6.Rules", "Integer", Data(a))));
            Assert.Equal(a < 0 || a > 1000 ? 0 : (int)a, Support.Call(lib, "D6.Rules", "Narrow", Data(a)));
        }
        foreach (var method in new[] { "Maximum", "Minimum" }) Same((decimal)rules.GetMethod(method)!.Invoke(null, [1.0m, 1.00m])!, Support.Call(lib, "D6.Rules", method, Data(1.0m), Data(1.00m)));
        foreach (var a in new[] { -1, 0, 3, 1000, 1001 }) Same((decimal)rules.GetMethod("Twice")!.Invoke(null, [a])!, Support.Call(lib, "D6.Rules", "Twice", a));
        foreach (var scale in new byte[] { 0, 2, 28, 29, 255 }) Same((decimal)rules.GetMethod("Parts")!.Invoke(null, [-1, -1, -1, true, scale])!, Support.Call(lib, "D6.Rules", "Parts", -1, -1, -1, true, (int)scale));
    }
    [Fact]
    public void CompleteIndependentSourceAndCumulativeDistributionsMatch()
    {
        Support.Parity("fixtures/D6/expected/decimal.json", Support.Read("fixtures/D6/csharp/Decimal.cs"), "Example", "Example.D6");
        var factory = typeof(Morphir.Frontend.CSharp.Frontend).Assembly.GetType("Morphir.Frontend.CSharp.DecimalLibrary")!.GetMethod("Modules", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var modules = (Morphir.IR.Classic.Entry<Morphir.IR.Classic.Path, Morphir.IR.Classic.AccessControlled<Morphir.IR.Classic.ModuleDefinition<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>>>[])factory.Invoke(null, [Morphir.IR.Classic.Path.From("Example")])!;
        var library = new Morphir.IR.Classic.Library<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>(Morphir.IR.Classic.Path.From("Example"), [], new(modules));
        Assert.Null(Morphir.IR.Json.StructuralComparison.Compare(Support.Read("src/Morphir.Frontend.CSharp/Compatibility/decimal.json"), Morphir.IR.Json.Codec.Encode(library)));
    }
    [Fact]
    public void GuardedArithmeticWideConversionsAndLocalUpdatesPreserveProofs()
    {
        const string code = """
        namespace Example.D6;
        public readonly record struct Amount(decimal Value,decimal? Optional);
        public static class Rules {
          public static decimal Add(decimal a){if(a<0m || a>100m)return 0m;var b=a+0.1m;b=b*2m;return b-0.2m;}
          public static decimal Divide(decimal a,decimal b){if(a<0m || a>1000m || b<1m || b>1000m)return 0m;return a/b;}
          public static decimal Remainder(decimal a,decimal b){if(b==0m)return 0m;return a%b;}
          public static decimal FromBig(System.Numerics.BigInteger a){if(a<(System.Numerics.BigInteger)decimal.MinValue || a>(System.Numerics.BigInteger)decimal.MaxValue)return 0m;return (decimal)a;}
          public static decimal From128(System.Int128 a){if(a<(System.Int128)decimal.MinValue || a>(System.Int128)decimal.MaxValue)return 0m;return (decimal)a;}
          public static byte Byte(decimal a){if(a<=-1m || a>=256m)return 0;return (byte)a;}
          public static decimal Round(decimal a,int digits){if(digits<0 || digits>28)return 0m;return decimal.Round(a,digits);}
          public static bool Equal(decimal a,decimal b)=>decimal.Equals(a,b);
          public static Amount Pack(decimal a,decimal? b)=>new(a,b);
          public static decimal Default()=>default;
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D6"])); var rules = Support.Emit(code).GetType("Example.D6.Rules")!;
        foreach (var a in new[] { decimal.MinValue, -1m, -0.9m, -0.00m, 0m, 0.1m, 1m, 99.999m, 100m, 255.9m, 256m, 1000m, decimal.MaxValue })
        {
            Same((decimal)rules.GetMethod("Add")!.Invoke(null, [a])!, Support.Call(lib, "D6.Rules", "Add", Data(a)));
            Assert.Equal((int)(byte)rules.GetMethod("Byte")!.Invoke(null, [a])!, Support.Call(lib, "D6.Rules", "Byte", Data(a)));
            foreach (var digits in new[] { -1, 0, 2, 28, 29 }) Same((decimal)rules.GetMethod("Round")!.Invoke(null, [a, digits])!, Support.Call(lib, "D6.Rules", "Round", Data(a), digits));
            foreach (var b in new[] { -1m, 0m, 0.1m, 1m, 3m, 1000m })
            {
                Same((decimal)rules.GetMethod("Divide")!.Invoke(null, [a, b])!, Support.Call(lib, "D6.Rules", "Divide", Data(a), Data(b)));
                Same((decimal)rules.GetMethod("Remainder")!.Invoke(null, [a, b])!, Support.Call(lib, "D6.Rules", "Remainder", Data(a), Data(b)));
                Assert.Equal(a == b, Support.Call(lib, "D6.Rules", "Equal", Data(a), Data(b)));
            }
        }
        var limit = (BigInteger)decimal.MaxValue;
        foreach (var a in new[] { -limit - 1, -limit, BigInteger.Zero, limit, limit + 1 })
        {
            var expected = a < -limit || a > limit ? 0m : (decimal)a;
            Same(expected, Support.Call(lib, "D6.Rules", "FromBig", Support.Integer(a))); Same(expected, Support.Call(lib, "D6.Rules", "From128", Support.Integer(a)));
        }
        var packed = (Dictionary<string, object>)Support.Call(lib, "D6.Rules", "Pack", Data(1.2300m), Support.Maybe(null)); Same(1.2300m, packed["value"]); Assert.Equal(Support.Maybe(null), packed["optional"]); Same(0m, Support.Call(lib, "D6.Rules", "Default"));
    }
    [Fact]
    public void CheckedCompilationRejectsCheckedConversionsAndRetainsIntrinsicDecimalArithmetic()
    {
        const string calculation = "namespace Example.D6;public static class Rules{public static decimal F(decimal a){if(a<0m || a>1000m)return 0m;return a*0.1m;}}";
        var library = Support.Success(Support.Convert(calculation, selectors: ["Example.D6"], check: true)); Same(0.3m, Support.Call(library, "D6.Rules", "F", Data(3m)));
        foreach (var code in new[]{
            "public static decimal F(int a){if(a<0 || a>1000)return 0m;return a*0.1m;}",
            "public static int F(decimal a){if(a<0m || a>100m)return 0;return (int)a;}"})
        {
            var result = Support.Convert("namespace Example.D6;public static class Rules{" + code + "}", selectors: ["Example.D6"], check: true);
            Assert.Null(result.Distribution); Assert.Contains(result.Diagnostics, d => d.Code == "MCS2004");
        }
    }
    private static object Data(decimal value)
    {
        var bits = decimal.GetBits(value); var coefficient = (BigInteger)(uint)bits[0] + ((BigInteger)(uint)bits[1] << 32) + ((BigInteger)(uint)bits[2] << 64);
        return new Dictionary<string, object> { { "coefficient", Support.Integer(coefficient) }, { "scale", (bits[3] >> 16) & 255 }, { "negative", bits[3] < 0 }, { "value", value } };
    }
    [Fact]
    public void NullableDecimalAndOutgoingFloatingConversionsRetainTheFullDomain()
    {
        const string code = """
        namespace Example.D6;
        public static class Rules {
          public static decimal? Negative(decimal? a)=>-a;
          public static decimal? Zero(decimal? a)=>a*0m;
          public static decimal? From(int? a)=>(decimal?)a;
          public static decimal Default(decimal? a)=>a.GetValueOrDefault();
          public static decimal Present(decimal? a){if(!a.HasValue)return 0m;return a.Value;}
          public static bool Equal(decimal? a,decimal? b)=>a==b;
          public static bool Less(decimal? a,decimal? b)=>a<b;
          public static double Double(decimal a)=>(double)a;
          public static float Single(decimal a)=>(float)a;
          public static System.Half Half(decimal a)=>(System.Half)a;
          public static System.Numerics.Complex Complex(decimal a)=>(System.Numerics.Complex)a;
          public static System.Numerics.BigInteger Construct(decimal a)=>new System.Numerics.BigInteger(a);
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D6"]));
        var random = new Random(0xD6); var values = new List<decimal> { decimal.MinValue, decimal.MaxValue, -0.00m, 0m, 0.1m, 1.2300m, 0.0000000000000000000000000001m, 65520m };
        for (var i = 0; i < 32; i++) values.Add(new decimal(random.Next(int.MinValue, int.MaxValue), random.Next(int.MinValue, int.MaxValue), random.Next(int.MinValue, int.MaxValue), i % 2 == 0, (byte)(i % 29)));
        void FloatSame(double expected, object actual) => Assert.Equal(BitConverter.DoubleToInt64Bits(expected), BitConverter.DoubleToInt64Bits((double)actual));
        foreach (var a in values)
        {
            var nullable = Support.Maybe(Data(a));
            Same(-a, ((Support.MaybeData)Support.Call(lib, "D6.Rules", "Negative", nullable)).Value!);
            Same(a * 0m, ((Support.MaybeData)Support.Call(lib, "D6.Rules", "Zero", nullable)).Value!);
            Same(a, Support.Call(lib, "D6.Rules", "Default", nullable)); Same(a, Support.Call(lib, "D6.Rules", "Present", nullable));
            FloatSame((double)a, Support.Call(lib, "D6.Rules", "Double", Data(a))); FloatSame((float)a, Support.Call(lib, "D6.Rules", "Single", Data(a))); FloatSame((double)(Half)a, Support.Call(lib, "D6.Rules", "Half", Data(a)));
            var complex = (Dictionary<string, object>)Support.Call(lib, "D6.Rules", "Complex", Data(a)); FloatSame(((Complex)a).Real, complex["real"]); FloatSame(0, complex["imaginary"]);
            Assert.Equal((BigInteger)a, Support.IntegerResult(Support.Call(lib, "D6.Rules", "Construct", Data(a))));
            foreach (var b in new decimal?[] { null, 0m, a })
            {
                Assert.Equal(a == b, Support.Call(lib, "D6.Rules", "Equal", nullable, Support.Maybe(b is null ? null : Data(b.Value))));
                Assert.Equal(a < b, Support.Call(lib, "D6.Rules", "Less", nullable, Support.Maybe(b is null ? null : Data(b.Value))));
            }
        }
        foreach (var method in new[] { "Negative", "Zero", "From" }) Assert.Equal(Support.Maybe(null), Support.Call(lib, "D6.Rules", method, Support.Maybe(null)));
        foreach (var method in new[] { "Default", "Present" }) Same(0m, Support.Call(lib, "D6.Rules", method, Support.Maybe(null)));
        foreach (var a in new[] { int.MinValue, 0, int.MaxValue }) Same(a, ((Support.MaybeData)Support.Call(lib, "D6.Rules", "From", Support.Maybe(a))).Value!);
    }
    private static void Same(decimal expected, object value)
    {
        var record = (Dictionary<string, object>)value; var bits = decimal.GetBits(expected);
        var coefficient = (BigInteger)(uint)bits[0] + ((BigInteger)(uint)bits[1] << 32) + ((BigInteger)(uint)bits[2] << 64);
        Assert.Equal(coefficient, Support.IntegerResult(record["coefficient"])); Assert.Equal((bits[3] >> 16) & 255, record["scale"]); Assert.Equal(bits[3] < 0, record["negative"]); Assert.Equal(expected, record["value"]);
    }
    [Theory]
    [InlineData("public static decimal F(decimal a,decimal b)=>a+b;", "MCS2702")]
    [InlineData("public static decimal F(decimal a,decimal b)=>a/b;", "MCS2701")]
    [InlineData("public static decimal F(decimal a,decimal b){if(b!=0m)return a/b;return 0m;}", "MCS2702")]
    [InlineData("public static decimal F(decimal a,decimal b){if(b!=0m){b=0m;return a/b;}return 0m;}", "MCS2701")]
    [InlineData("public static decimal F(decimal a){if(a<0m || a>100m)return 0m;a=decimal.MaxValue;return a+1m;}", "MCS2702")]
    [InlineData("public static decimal F(decimal a,int digits)=>decimal.Round(a,digits);", "MCS2706")]
    [InlineData("public static decimal F(byte scale)=>new decimal(1,0,0,false,scale);", "MCS2706")]
    [InlineData("public static decimal F(decimal a){if(false)return a+1m;return 0m;}", "MCS2702")]
    [InlineData("public static decimal F(decimal a){try{return a+1m;}catch{return 0m;}}", "MCS2403")]
    [InlineData("public static int F(decimal a)=>(int)a;", "MCS2703")]
    [InlineData("public static decimal F(System.Numerics.BigInteger a)=>(decimal)a;", "MCS2703")]
    [InlineData("public static decimal F(double a)=>(decimal)a;", "MCS2704")]
    [InlineData("public static decimal F()=>checked(0.1m+0.2m);", "MCS2001")]
    public void UnprovedDecimalFailureOrInexactConstructionFailsWholeConversion(string member, string diagnostic)
    {
        var result = Support.Convert("namespace Example.D6;public static class Rules{" + member + "}", selectors: ["Example.D6"]); Assert.Null(result.Distribution); Assert.Contains(result.Diagnostics, d => d.Code == diagnostic && d.File.Length > 0 && d.Line > 0 && d.Column > 0);
    }
}
