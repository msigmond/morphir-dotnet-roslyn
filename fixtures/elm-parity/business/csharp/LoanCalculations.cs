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
        var marketVal = MarketValue(loan);
        var rate = loan.AnnualFeeRate;
        return marketVal * rate / 100 / 360;
    }

    public static LoanFee Calculate(Loan loan)
    {
        var marketVal = MarketValue(loan);
        var dailyF = DailyFee(loan);
        return new LoanFee(loan.SecurityId, marketVal, dailyF);
    }
}
