using System.Numerics;
using Morphir.Tests;
namespace Morphir.Frontend.CSharp.IntegrationTests;

[Trait("Delivery", "D5")]
public class ComplexTests
{
    private static object Data(Complex value) => new Dictionary<string, object> { { "real", value.Real }, { "imaginary", value.Imaginary } };
    private static void Same(Complex expected, object actual)
    {
        var record = (Dictionary<string, object>)actual;
        foreach (var (name, value) in new[] { ("real", expected.Real), ("imaginary", expected.Imaginary) })
        {
            var result = (double)record[name];
            if (double.IsNaN(value)) Assert.True(double.IsNaN(result)); else Assert.Equal(BitConverter.DoubleToInt64Bits(value), BitConverter.DoubleToInt64Bits(result));
        }
    }
    [Fact]
    public void ComplexAndScalarOverloadsPreserveArithmeticOrderAndNonFiniteCases()
    {
        const string code = """
        using System.Numerics;
        namespace Example.D5;
        public static class Rules {
          public static Complex Add(Complex a,Complex b)=>a+b;
          public static Complex Subtract(Complex a,Complex b)=>a-b;
          public static Complex Multiply(Complex a,Complex b)=>a*b;
          public static Complex Divide(Complex a,Complex b)=>a/b;
          public static Complex ScalarAdd(Complex a,double b)=>a+b;
          public static Complex ScalarSubtract(Complex a,double b)=>a-b;
          public static Complex ScalarMultiply(Complex a,double b)=>a*b;
          public static Complex ScalarDivide(Complex a,double b)=>a/b;
          public static Complex LeftSubtract(double a,Complex b)=>a-b;
          public static Complex LeftDivide(double a,Complex b)=>a/b;
          public static Complex Negate(Complex a)=>-a;
          public static Complex Create(double a,double b)=>new Complex(a,b);
          public static Complex Converted(long value)=>(Complex)value;
          public static double Real(Complex value)=>value.Real;
          public static double Imaginary(Complex value)=>value.Imaginary;
          public static bool Equal(Complex a,Complex b)=>a==b;
          public static bool Different(Complex a,Complex b)=>a!=b;
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D5"])); var rules = Support.Emit(code).GetType("Example.D5.Rules")!;
        var values = new[] { Complex.Zero, new Complex(-0.0, -0.0), new Complex(1, 2), new Complex(-3, 4), new Complex(double.PositiveInfinity, 1), new Complex(1, double.NegativeInfinity), new Complex(double.NaN, 2), new Complex(double.MaxValue, 1) };
        foreach (var a in values)
        {
            Same(-a, Support.Call(lib, "D5.Rules", "Negate", Data(a))); Same(a, Support.Call(lib, "D5.Rules", "Create", a.Real, a.Imaginary));
            Assert.Equal(a.Real, (double)Support.Call(lib, "D5.Rules", "Real", Data(a))); Assert.Equal(a.Imaginary, (double)Support.Call(lib, "D5.Rules", "Imaginary", Data(a)));
            foreach (var b in values)
            {
                foreach (var method in new[] { "Add", "Subtract", "Multiply", "Divide" }) Same((Complex)rules.GetMethod(method)!.Invoke(null, [a, b])!, Support.Call(lib, "D5.Rules", method, Data(a), Data(b)));
                Assert.Equal(a == b, Support.Call(lib, "D5.Rules", "Equal", Data(a), Data(b))); Assert.Equal(a != b, Support.Call(lib, "D5.Rules", "Different", Data(a), Data(b)));
            }
            foreach (var scalar in new[] { double.NaN, double.PositiveInfinity, -0.0, 0.0, -3.0, 3.0 })
            {
                foreach (var method in new[] { "ScalarAdd", "ScalarSubtract", "ScalarMultiply", "ScalarDivide" }) Same((Complex)rules.GetMethod(method)!.Invoke(null, [a, scalar])!, Support.Call(lib, "D5.Rules", method, Data(a), scalar));
                foreach (var method in new[] { "LeftSubtract", "LeftDivide" }) Same((Complex)rules.GetMethod(method)!.Invoke(null, [scalar, a])!, Support.Call(lib, "D5.Rules", method, scalar, Data(a)));
            }
        }
        Same(new Complex((double)long.MaxValue, 0), Support.Call(lib, "D5.Rules", "Converted", Support.Integer(long.MaxValue)));
    }
}
