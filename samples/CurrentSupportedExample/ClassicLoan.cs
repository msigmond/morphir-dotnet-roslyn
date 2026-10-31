namespace Example.CurrentSupported;

public readonly record struct ClassicLoan(string SecurityId, int Quantity, int Price, int AnnualFeeRate);
public readonly record struct ClassicLoanFee(string SecurityId, int MarketValue, int DailyFee);

public static class ClassicLoanCalculations
{
    public static int MarketValue(ClassicLoan loan)
    {
        return loan.Quantity * loan.Price;
    }

    public static int DailyFee(ClassicLoan loan)
    {
        var marketValue = MarketValue(loan);
        var rate = loan.AnnualFeeRate;
        return marketValue * rate / 100 / 360;
    }

    public static ClassicLoanFee Calculate(ClassicLoan loan)
    {
        var marketValue = MarketValue(loan);
        var dailyFee = DailyFee(loan);
        return new ClassicLoanFee(loan.SecurityId, marketValue, dailyFee);
    }
}
