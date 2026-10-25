using System.Text.Json;
using Morphir.IR.Classic;
using Morphir.IR.Json;
using Morphir.Tests;
using IrPath = Morphir.IR.Classic.Path;

namespace Morphir.IR.Json.Tests;

[Trait("Contract", "SourceStructure")]
public class SourceCodecTests
{
    [Fact]
    public void SourceOperationsCannotBeMislabelledAsStandardClassicV3()
    {
        var body = new SourceValue<Unit,IrType<Unit>>(Types.Int,"Return",[],[new LiteralValue<Unit,IrType<Unit>>(Types.Int,new IntLiteral(1))]);
        var definition = new ValueDefinition<Unit,IrType<Unit>>([],Types.Int,body);
        var value = new Entry<Name,AccessControlled<Documented<ValueDefinition<Unit,IrType<Unit>>>>>(Name.From("F"),new(Access.Public,new("",definition)));
        var module = new ModuleDefinition<Unit,IrType<Unit>>([],[value]);
        var library = new Library<Unit,IrType<Unit>>(IrPath.From("Example"),[],new([new(IrPath.From("Rules"),new(Access.Public,module))]));
        Assert.Throws<NotSupportedException>(()=>Codec.Encode(library));
        var source = library with{Source=new([])};
        using var json=JsonDocument.Parse(Codec.Encode(source));
        Assert.Equal("Morphir.CSharp.Source",json.RootElement.GetProperty("format").GetString());
        var encodedBody=json.RootElement.GetProperty("distribution")[3].GetProperty("modules")[0][1].GetProperty("value").GetProperty("values")[0][1].GetProperty("value").GetProperty("value").GetProperty("body");
        Assert.Equal("Source",encodedBody[0].GetString());Assert.Equal("Return",encodedBody[2].GetString());
        Assert.Equal("Literal",encodedBody[4][0][0].GetString());
    }
    [Fact]
    public void SourceNumbersAndCharactersHaveExplicitLiteralTags()
    {
        var character = new LiteralValue<Unit,IrType<Unit>>(Types.Reference("Char","Char"),new CharLiteral("λ"));
        var number = new LiteralValue<Unit,IrType<Unit>>(Types.Int,new SourceNumberLiteral("18446744073709551615UL"));
        using var c=JsonDocument.Parse(JsonSerializer.Serialize(Codec.Value(character,_=>new{},t=>Codec.Type(t,_=>new{}))));
        using var n=JsonDocument.Parse(JsonSerializer.Serialize(Codec.Value(number,_=>new{},t=>Codec.Type(t,_=>new{}))));
        Assert.Equal("CharLiteral",c.RootElement[2][0].GetString());Assert.Equal("λ",c.RootElement[2][1].GetString());
        Assert.Equal("SourceNumberLiteral",n.RootElement[2][0].GetString());Assert.Equal("18446744073709551615UL",n.RootElement[2][1].GetString());
    }
    [Fact]
    public void DeepWrittenGroupingIsNotLimitedByTheDefaultJsonDepth()
    {
        var expression=new string('(',80)+"1"+new string(')',80);
        var library=Support.Success(Support.Convert("namespace Example.D1;public static class Rules{public static int F()=>"+expression+";}"));
        Assert.Contains("Parenthesized",Codec.Encode(library),StringComparison.Ordinal);
    }
}
