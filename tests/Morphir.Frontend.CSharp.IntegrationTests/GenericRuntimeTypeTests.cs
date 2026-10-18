using Morphir.Tests;

namespace Morphir.Frontend.CSharp.IntegrationTests;

[Trait("Delivery", "D10")]
public class GenericRuntimeTypeTests
{
    [Fact]
    public void UnresolvedRuntimeTypeEqualityCannotBeInventedAsAlwaysFalse()
    {
        const string prefix = "namespace Example.D10;public interface IValue<T>{}public readonly record struct Holder<U>(U Value):IValue<U>;public static class Rules{";
        foreach (var body in new[] { "public static bool Test<T>(IValue<T>? value)=>value is IValue<int>;", "public static bool Test<T>(IValue<T>? value)=>(value as IValue<int>) is not null;" })
        {
            var source = prefix + body + "}"; var native = Support.Emit(source); var holder = Activator.CreateInstance(native.GetType("Example.D10.Holder`1")!.MakeGenericType(typeof(int)), [5]);
            Assert.Equal(true, native.GetType("Example.D10.Rules")!.GetMethod("Test")!.MakeGenericMethod(typeof(int)).Invoke(null, [holder]));
            var result = Support.Convert(source, selectors: ["Example.D10"]); Assert.Null(result.Distribution); Assert.Contains(result.Diagnostics, item => item.Code == "MCS3103" && item.File == "Case.cs");
        }
    }
}
