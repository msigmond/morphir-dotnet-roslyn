using Sigmond.Buslog.Model;

namespace Sigmond.Buslog.Algo;

public static class LoanCalculations
{
    public static int MarketValue(Loan loan)
    {
        return loan.Quantity * loan.Price;
    }

    public static int DailyFee(Loan loan)
    {
        var marketValue = MarketValue(loan);
        var rate = loan.AnnualFeeRate;
        return marketValue * rate / 100 / 360;
    }

    public static LoanFee Calculate(Loan loan)
    {
        var marketValue = MarketValue(loan);
        var dailyFee = DailyFee(loan);
        return new LoanFee(loan.SecurityId, marketValue, dailyFee);
    }
}
