using Morphir.Tests;
using Morphir.IR.Json;
namespace Morphir.Frontend.CSharp.IntegrationTests;

[Trait("Delivery", "D5")]
public class FloatingTests
{
    private static void Same(double expected, object actual)
    {
        var value = (double)actual;
        if (double.IsNaN(expected)) Assert.True(double.IsNaN(value));
        else Assert.Equal(BitConverter.DoubleToInt64Bits(expected), BitConverter.DoubleToInt64Bits(value));
    }
    [Fact]
    public void IeeeArithmeticAndNarrowingMatchClrIncludingNonFiniteAndSignedZero()
    {
        const string code = """
        namespace Example.D5;
        public static class Rules {
          public static double Add(double a,double b)=>a+b;
          public static double Subtract(double a,double b)=>a-b;
          public static double Multiply(double a,double b)=>a*b;
          public static double Divide(double a,double b)=>a/b;
          public static double Remainder(double a,double b)=>a%b;
          public static double Negate(double a)=>-a;
          public static float Single(double a)=>(float)a;
          public static System.Half Half(double a)=>(System.Half)a;
          public static float SingleAdd(float a,float b)=>a+b;
          public static System.Half HalfDivide(System.Half a,System.Half b)=>a/b;
          public static bool Equal(double a,double b)=>a==b;
          public static bool Greater(double a,double b)=>a>b;
          public static double NaN()=>double.NaN;
          public static double Infinity()=>double.PositiveInfinity;
          public static double Zero()=>-0.0;
          public static int Cast(double a)=>unchecked((int)a);
          public static byte Byte(double a)=>unchecked((byte)a);
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D5"])); var rules = Support.Emit(code).GetType("Example.D5.Rules")!;
        var values = new[] { double.NaN, double.NegativeInfinity, double.PositiveInfinity, -0.0, 0.0, -1.0, 1.0, double.Epsilon, -double.Epsilon, 16777217.0, 65520.0, Math.ScaleB(1, -25), Math.ScaleB(1, -150), double.MaxValue, -double.MaxValue };
        foreach (var a in values)
        {
            Same(-a, Support.Call(lib, "D5.Rules", "Negate", a));
            Same((float)a, Support.Call(lib, "D5.Rules", "Single", a)); Same((double)(Half)a, Support.Call(lib, "D5.Rules", "Half", a));
            Assert.Equal(unchecked((int)a), Support.Call(lib, "D5.Rules", "Cast", a)); Assert.Equal((int)unchecked((byte)a), Support.Call(lib, "D5.Rules", "Byte", a));
            foreach (var b in new[] { double.NaN, double.NegativeInfinity, double.PositiveInfinity, -0.0, 0.0, -3.0, 3.0 })
            {
                foreach (var method in new[] { "Add", "Subtract", "Multiply", "Divide", "Remainder" }) Same((double)rules.GetMethod(method)!.Invoke(null, [a, b])!, Support.Call(lib, "D5.Rules", method, a, b));
                Assert.Equal(a == b, Support.Call(lib, "D5.Rules", "Equal", a, b)); Assert.Equal(a > b, Support.Call(lib, "D5.Rules", "Greater", a, b));
                Same((float)a + (float)b, Support.Call(lib, "D5.Rules", "SingleAdd", (double)(float)a, (double)(float)b));
                Same((double)((Half)a / (Half)b), Support.Call(lib, "D5.Rules", "HalfDivide", (double)(Half)a, (double)(Half)b));
            }
        }
        Same(double.NaN, Support.Call(lib, "D5.Rules", "NaN")); Same(double.PositiveInfinity, Support.Call(lib, "D5.Rules", "Infinity")); Same(-0.0, Support.Call(lib, "D5.Rules", "Zero"));
        using var json = System.Text.Json.JsonDocument.Parse(Codec.Encode(lib)); Assert.DoesNotContain(":NaN", json.RootElement.GetRawText(), StringComparison.Ordinal);
    }
}
