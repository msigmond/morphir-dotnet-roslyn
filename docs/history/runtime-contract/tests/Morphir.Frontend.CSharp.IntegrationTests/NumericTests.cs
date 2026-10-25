using System.Numerics;
using Morphir.Frontend.CSharp;
using Morphir.IR.Json;
using Morphir.Tests;
namespace Morphir.Frontend.CSharp.IntegrationTests;

[Trait("Delivery", "D5")]
public class NumericTests
{
    [Fact]
    public void Int32UncheckedOperatorsMatchClrAndIncludeCompleteHelpers()
    {
        const string code = """
        namespace Example.D5;
        public static class Rules {
          public static int Add(int a,int b)=>unchecked(a+b);
          public static int Subtract(int a,int b)=>unchecked(a-b);
          public static int Multiply(int a,int b)=>unchecked(a*b);
          public static int Negate(int a)=>unchecked(-a);
          public static int Complement(int a)=>~a;
          public static int And(int a,int b)=>a&b;
          public static int Or(int a,int b)=>a|b;
          public static int Xor(int a,int b)=>a^b;
          public static int Left(int a,int b)=>a<<b;
          public static int Right(int a,int b)=>a>>b;
          public static int Logical(int a,int b)=>a>>>b;
          public static int Divide(int a)=>a/2;
          public static int Remainder(int a)=>a%2;
          public static byte Narrow(int a)=>unchecked((byte)a);
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D5"])); var rules = Support.Emit(code).GetType("Example.D5.Rules")!;
        foreach (var a in new[] { int.MinValue, int.MinValue + 1, -65536, -1, 0, 1, 65536, int.MaxValue })
        {
            foreach (var b in new[] { int.MinValue, -33, -1, 0, 1, 2, 31, 32, 33, int.MaxValue })
                foreach (var method in new[] { "Add", "Subtract", "Multiply", "And", "Or", "Xor", "Left", "Right", "Logical" })
                    Assert.Equal(rules.GetMethod(method)!.Invoke(null, [a, b]), Support.Call(lib, "D5.Rules", method, a, b));
            foreach (var method in new[] { "Negate", "Complement", "Divide", "Remainder" }) Assert.Equal(rules.GetMethod(method)!.Invoke(null, [a]), Support.Call(lib, "D5.Rules", method, a));
            Assert.Equal((int)unchecked((byte)a), Support.Call(lib, "D5.Rules", "Narrow", a));
        }
        var factory = typeof(Morphir.Frontend.CSharp.Frontend).Assembly.GetType("Morphir.Frontend.CSharp.NumericLibrary")!.GetMethod("Modules", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var modules = (Morphir.IR.Classic.Entry<Morphir.IR.Classic.Path, Morphir.IR.Classic.AccessControlled<Morphir.IR.Classic.ModuleDefinition<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>>>[])factory.Invoke(null, [Morphir.IR.Classic.Path.From("Example")])!;
        var productionModel = new Morphir.IR.Classic.Library<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>(Morphir.IR.Classic.Path.From("Example"), [], new(modules));
        Assert.Null(StructuralComparison.Compare(Support.Read("src/Morphir.Frontend.CSharp/Compatibility/numeric.json"), Codec.Encode(productionModel)));
        Assert.All(lib.Definition.Modules.Where(m => m.Key.Segments[0].Key == "compat").SelectMany(m => m.Value.Value.Values), v => Assert.Equal(Morphir.IR.Classic.Access.Private, v.Value.Access));
    }
    [Fact]
    public void WideLimbDataRoundTripsBeyondJavascriptPrecision()
    {
        const string code = """
        namespace Example.D5;
        public static class Rules {
          public static long Add(long a,long b)=>unchecked(a+b);
          public static ulong Multiply(ulong a,ulong b)=>unchecked(a*b);
          public static long Divide(long a)=>a/3;
          public static long Right(long a,int count)=>a>>count;
          public static long Logical(long a,int count)=>a>>>count;
          public static System.Int128 Add128(System.Int128 a,System.Int128 b)=>unchecked(a+b);
          public static System.UInt128 Multiply128(System.UInt128 a,System.UInt128 b)=>unchecked(a*b);
          public static System.Numerics.BigInteger Large(System.Numerics.BigInteger a,System.Numerics.BigInteger b)=>a*b+a;
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D5"]));
        var values = new BigInteger[] { long.MinValue, -9007199254740993L, -1, 0, 1, 9007199254740993L, long.MaxValue };
        foreach (var a in values) foreach (var b in values)
            {
                Assert.Equal(new BigInteger(unchecked((long)a + (long)b)), Support.IntegerResult(Support.Call(lib, "D5.Rules", "Add", Support.Integer(a), Support.Integer(b))));
                var ua = unchecked((ulong)(long)a); var ub = unchecked((ulong)(long)b);
                Assert.Equal(new BigInteger(unchecked(ua * ub)), Support.IntegerResult(Support.Call(lib, "D5.Rules", "Multiply", Support.Integer(ua), Support.Integer(ub))));
            }
        foreach (var a in values)
        {
            Assert.Equal(a / 3, Support.IntegerResult(Support.Call(lib, "D5.Rules", "Divide", Support.Integer(a))));
            foreach (var count in new[] { -1, 0, 1, 63, 64, 65 })
            {
                Assert.Equal(new BigInteger((long)a >> count), Support.IntegerResult(Support.Call(lib, "D5.Rules", "Right", Support.Integer(a), count)));
                Assert.Equal(new BigInteger((long)a >>> count), Support.IntegerResult(Support.Call(lib, "D5.Rules", "Logical", Support.Integer(a), count)));
            }
        }
        var max = (BigInteger.One << 127) - 1;
        Assert.Equal(-max - 1, Support.IntegerResult(Support.Call(lib, "D5.Rules", "Add128", Support.Integer(max), Support.Integer(1))));
        Assert.Equal((BigInteger.One << 128) - 2, Support.IntegerResult(Support.Call(lib, "D5.Rules", "Multiply128", Support.Integer((BigInteger.One << 128) - 1), Support.Integer(2))));
        var huge = (BigInteger.One << 256) + 1;
        Assert.Equal(huge * huge + huge, Support.IntegerResult(Support.Call(lib, "D5.Rules", "Large", Support.Integer(huge), Support.Integer(huge))));
    }
    [Fact]
    public void DominatingNumericGuardsProveDivisionAndBigIntegerNarrowing()
    {
        const string code = """
        namespace Example.D5;
        public static class Rules {
          public static int Divide(int a,int b) { if(b==0 || a==int.MinValue && b==-1) return 99; return a/b; }
          public static int Positive(int a,int b) { if(b>=1) return a/b; return 99; }
          public static int Narrow(System.Numerics.BigInteger value) { if(value<int.MinValue || value>int.MaxValue) return 99; return (int)value; }
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D5"])); var rules = Support.Emit(code).GetType("Example.D5.Rules")!;
        foreach (var a in new[] { int.MinValue, -1, 0, 1, int.MaxValue }) foreach (var b in new[] { int.MinValue, -1, 0, 1, int.MaxValue }) foreach (var method in new[] { "Divide", "Positive" })
                    Assert.Equal(rules.GetMethod(method)!.Invoke(null, [a, b]), Support.Call(lib, "D5.Rules", method, a, b));
        foreach (var value in new BigInteger[] { (BigInteger.One << 128), int.MinValue - 1L, int.MinValue, 0, int.MaxValue, int.MaxValue + 1L })
            Assert.Equal(rules.GetMethod("Narrow")!.Invoke(null, [value]), Support.Call(lib, "D5.Rules", "Narrow", Support.Integer(value)));
        foreach (var member in new[] { "public static int F(int a,int b)=>a/b;", "public static int F(int a)=>a/-1;", "public static int F(System.Numerics.BigInteger a)=>(int)a;", "public static int F(int a,int b){if(b!=0){b=0;return a/b;}return 0;}" })
        {
            var result = Support.Convert("namespace Example.D5;public static class Rules{" + member + "}", selectors: ["Example.D5"]);
            Assert.Null(result.Distribution); Assert.Contains(result.Diagnostics, d => d.Code.StartsWith("MCS260", StringComparison.Ordinal));
        }
    }
}
