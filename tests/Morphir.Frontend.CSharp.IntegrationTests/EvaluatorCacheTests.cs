using Morphir.Tests;

namespace Morphir.Frontend.CSharp.IntegrationTests;

[Trait("Delivery", "D10")]
public class EvaluatorCacheTests
{
    [Fact]
    public void RepeatedPureCallsPreserveUtf16DataLibraryIsolationAndIndependentResults()
    {
        const string code = """
        namespace Example.D10;
        public readonly record struct Box(string? Value);
        public static class Rules{public static string? Read(Box box)=>box.Value;public static Box Echo(Box box)=>box;}
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var other = Support.Success(Support.Convert(code.Replace("=>box.Value;", "=>\"other\";"), selectors: ["Example.D10"]));
        var native = Support.Emit(code); var box = native.GetType("Example.D10.Box")!; var read = native.GetType("Example.D10.Rules")!.GetMethod("Read")!;
        Dictionary<string, object> Data(string? value) => new() { { "value", Support.Maybe(value) } };
        foreach (var value in new string?[] { null, "", "plain", "\ud800", "\ud801", "\ufffd", "\ud800\udc00" })
        {
            var expected = read.Invoke(null, [Activator.CreateInstance(box, [value])]);
            foreach (var attempt in new[] { 0, 1 }) Assert.Equal(Support.Maybe(expected), Support.Call(library, "D10.Rules", "Read", Data(value)));
            Assert.Equal(Support.Maybe("other"), Support.Call(other, "D10.Rules", "Read", Data(value)));
        }
        var first = Assert.IsType<Dictionary<string, object>>(Support.Call(library, "D10.Rules", "Echo", Data("original"))); first["value"] = Support.Maybe("changed after evaluation");
        var second = Assert.IsType<Dictionary<string, object>>(Support.Call(library, "D10.Rules", "Echo", Data("original"))); Assert.Equal(Support.Maybe("original"), second["value"]);
        second["value"] = Support.Maybe("changed again"); var third = Assert.IsType<Dictionary<string, object>>(Support.Call(library, "D10.Rules", "Echo", Data("original"))); Assert.Equal(Support.Maybe("original"), third["value"]);
    }
}
