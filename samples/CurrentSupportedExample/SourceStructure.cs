// Deliberate source-structure cases: nullable access and a written unreachable branch.
#pragma warning disable CS8629, CS0162
namespace Example.CurrentSupported;

public record SampleLoan(string SecurityId, decimal Quantity, decimal Price);
public sealed class SampleOperands { public int IntOne; public int IntTwo; }

public static partial class CurrentSupportedExample
{
    public static decimal MarketValue(SampleLoan loan) => loan.Quantity * loan.Price;
    public static int SimpleProduct(SampleOperands obj) => obj.IntOne * obj.IntTwo;
    public static decimal UnguardedDivision(decimal a, decimal b) => a / b;
    public static decimal FloatingDecimal(double value) => (decimal)value;
    public static int DirectNullableRead(int? value) => value.Value;
    public static int ExplicitStatements(SampleOperands input)
    {
        int result;
        if (false) result = 1;
        else result = 2;
        input.IntOne = result;
        return input.IntOne;
    }
}
