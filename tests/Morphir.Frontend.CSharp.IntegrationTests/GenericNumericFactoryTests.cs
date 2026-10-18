using System.Numerics;
using System.Text;
using Morphir.Tests;

namespace Morphir.Frontend.CSharp.IntegrationTests;

public partial class NumericFactoryTests
{
    [Fact]
    public void GenericNumericFactoriesHaveCompleteIndependentParity() => Support.Parity("fixtures/D10/expected/generic-factories.json", Support.Read("fixtures/D10/csharp/GenericNumericFactories.cs"), "Example", "Example.D10");

    private const string GenericNumericBodies = """
      public static TOutput Saturate<TInput,TOutput>(TInput value) where TInput:System.Numerics.INumberBase<TInput> where TOutput:System.Numerics.INumberBase<TOutput> => TOutput.CreateSaturating(value);
      public static TOutput Truncate<TInput,TOutput>(TInput value) where TInput:System.Numerics.INumberBase<TInput> where TOutput:System.Numerics.INumberBase<TOutput> => TOutput.CreateTruncating(value);
      public static TOutput Through<TInput,TOutput>(TInput value) where TInput:System.Numerics.INumberBase<TInput> where TOutput:System.Numerics.INumberBase<TOutput> => Saturate<TInput,TOutput>(value);
      public static TOutput ThroughReference<TInput,TOutput>(TInput value) where TInput:System.Numerics.INumberBase<TInput> where TOutput:System.Numerics.INumberBase<TOutput> {System.Func<TInput,TOutput> convert=TOutput.CreateTruncating<TInput>;return convert(value);}
      public static TOutput DecimalInteger<TOutput>(decimal value) where TOutput:System.Numerics.IBinaryInteger<TOutput> => decimal.ConvertToInteger<TOutput>(value);
      public static TInteger FloatInteger<TFloat,TInteger>(TFloat value) where TFloat:System.Numerics.IFloatingPoint<TFloat> where TInteger:System.Numerics.IBinaryInteger<TInteger> => TFloat.ConvertToInteger<TInteger>(value);
      public static long Checked<TInput>(TInput value) where TInput:System.Numerics.INumberBase<TInput> => long.CreateChecked(value);
      public static long Widen(int value)=>Checked(value);
      """;

    [Fact]
    public void GenericNumericFactoriesPassOnlyRequiredModeAndRepresentationWitnesses()
    {
        var code = new StringBuilder("namespace Example.D10;public static class Rules{" + GenericNumericBodies);
        for (var index = 0; index < Domains.Length; index++) foreach (var mode in new[] { "Saturate", "Truncate", "Through", "ThroughReference" })
            {
                var source = Domains[index]; var target = Domains[(index + 4) % Domains.Length];
                code.Append($"public static {target.Code} G{index}_{mode}({source.Code} value)=>{mode}<{source.Code},{target.Code}>(value);");
            }
        code.Append("public static byte DecimalByte(decimal value)=>DecimalInteger<byte>(value);");
        code.Append("public static int FloatInt(float value)=>FloatInteger<float,int>(value);");
        code.Append("public static System.Half Half(long value)=>Through<long,System.Half>(value);");
        code.Append("public static decimal Decimal(long value)=>Through<long,decimal>(value);");
        code.Append("public static System.Numerics.Complex Complex(long value)=>Through<long,System.Numerics.Complex>(value);");
        code.Append('}'); var text = code.ToString(); var library = Support.Success(Support.Convert(text, selectors: ["Example.D10"])); var rules = Support.Emit(text).GetType("Example.D10.Rules")!;
        for (var index = 0; index < Domains.Length; index++) foreach (var mode in new[] { "Saturate", "Truncate", "Through", "ThroughReference" }) foreach (var value in Inputs(Domains[index]))
                {
                    var name = $"G{index}_{mode}"; var expected = NativeInteger(rules.GetMethod(name)!.Invoke(null, [Domains[index].Native(value)])!);
                    Assert.Equal(expected, Result(Domains[(index + 4) % Domains.Length], Support.Call(library, "D10.Rules", name, Domains[index].Encode(value))));
                }
        foreach (var value in new[] { decimal.MinValue, -0.99m, 0m, 255.99m, 256m, decimal.MaxValue }) Assert.Equal((byte)rules.GetMethod("DecimalByte")!.Invoke(null, [value])!, NativeInteger(Support.Call(library, "D10.Rules", "DecimalByte", DecimalData(value))));
        foreach (var value in new[] { float.NaN, float.NegativeInfinity, -1.99f, -0f, 0f, float.MaxValue, float.PositiveInfinity }) Assert.Equal((int)rules.GetMethod("FloatInt")!.Invoke(null, [value])!, NativeInteger(Support.Call(library, "D10.Rules", "FloatInt", (double)value)));
        foreach (var value in new[] { long.MinValue, -1, 0, 1, long.MaxValue })
        {
            SameReal((double)(Half)rules.GetMethod("Half")!.Invoke(null, [value])!, Support.Call(library, "D10.Rules", "Half", Support.Integer(value)));
            SameDecimal((decimal)rules.GetMethod("Decimal")!.Invoke(null, [value])!, Support.Call(library, "D10.Rules", "Decimal", Support.Integer(value)));
            var expected = (Complex)rules.GetMethod("Complex")!.Invoke(null, [value])!; var actual = Assert.IsType<Dictionary<string, object>>(Support.Call(library, "D10.Rules", "Complex", Support.Integer(value))); SameReal(expected.Real, actual["real"]); SameReal(expected.Imaginary, actual["imaginary"]);
        }
        foreach (var value in new[] { int.MinValue, -1, 0, 1, int.MaxValue }) Assert.Equal(new BigInteger(value), Support.IntegerResult(Support.Call(library, "D10.Rules", "Widen", value)));
    }

    [Fact]
    public void AGenericCheckedFactoryCannotAcquireAnUnsafeConversionCallback()
    {
        foreach (var call in new[] { "return Checked(value);", "if(false)return Checked(value);return 0;", "if(false){System.Func<int,byte> convert=Checked<int>;return convert(value);}return 0;" })
        {
            var source = "namespace Example.D10;public static class Rules{public static byte Checked<T>(T value) where T:System.Numerics.INumberBase<T> =>byte.CreateChecked(value);public static byte Unsafe(int value){" + call + "}}";
            var rejected = Support.Convert(source, selectors: ["Example.D10"]); Assert.Null(rejected.Distribution); Assert.Contains(rejected.Diagnostics, diagnostic => diagnostic.Code == "MCS2610");
        }
    }

    [Fact]
    public void NumericWitnessScopesSurviveOwnedSourceResultContinuations()
    {
        const string code = """
        namespace Example.D10;
        public sealed class Product{public byte Value;public Product(byte value){Value=value;}}
        public static class Rules{
          public static Product Build<T>(T value) where T:System.Numerics.INumberBase<T> =>new Product(byte.CreateSaturating(value));
          public static Product Truncated<T>(T value) where T:System.Numerics.INumberBase<T> =>new Product(byte.CreateTruncating(value));
          public static Product Forward<T>(T value) where T:System.Numerics.INumberBase<T> =>Build(value);
          public static byte Run(long value){var a=Forward(value);var b=Truncated(value);if(a is null||b is null)return 0;a.Value=b.Value;return a.Value;}
          public static byte Saturated(long value){var a=Forward(value);if(a is null)return 0;return a.Value;}
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var value in new[] { long.MinValue, -257, -1, 0, 1, 255, 256, long.MaxValue }) foreach (var name in new[] { "Run", "Saturated" }) Assert.Equal(new BigInteger((byte)rules.GetMethod(name)!.Invoke(null, [value])!), NativeInteger(Support.Call(library, "D10.Rules", name, Support.Integer(value))));
    }
}
