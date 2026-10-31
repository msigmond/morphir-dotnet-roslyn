using System.Text.Json;
using Morphir.Tests;

namespace Morphir.Frontend.CSharp.Tests;

[Trait("Contract","ExactElmParityScope")]
public class MemberRejectionTests
{
    public static IEnumerable<object[]> Cases()
    {
        var cases=new List<object[]>();
        foreach(var file in new[]{"member-rejections","form-rejections"})
        {
            using var json=JsonDocument.Parse(Support.Read("fixtures/elm-parity/"+file+".json"));
            cases.AddRange(json.RootElement.GetProperty("cases").EnumerateArray().Select(c=>new object[]{file+":"+c.GetProperty("id").GetString()!,c.GetProperty("source").GetString()!,c.GetProperty("compilerBinding").GetBoolean()}));
        }
        return cases;
    }
    [Theory]
    [MemberData(nameof(Cases))]
    public void RequiredMemberRejectionsHaveSourceDiagnosticsAndNoDistribution(string id,string source,bool compilerBinding)
    {
        _=id;
        var result=Frontend.Convert(Support.Compile(source),new("Example",["Example.Inventory","Example.Rejections"]));
        Assert.False(result.Success);Assert.Null(result.Distribution);Assert.NotEmpty(result.Diagnostics);
        Assert.All(result.Diagnostics,d=>Assert.True(d.Line>0&&d.Column>0));
        if(compilerBinding)Assert.Contains(result.Diagnostics,d=>d.Code=="MCS0001");
        else Assert.DoesNotContain(result.Diagnostics,d=>d.Code=="MCS0001");
    }
}
