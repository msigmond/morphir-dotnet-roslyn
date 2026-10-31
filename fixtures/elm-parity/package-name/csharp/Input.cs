namespace Morphir.CSharp.Business;public record Loan(int Value);public static class Rules{public static int F(Loan loan)=>loan.Value;}
