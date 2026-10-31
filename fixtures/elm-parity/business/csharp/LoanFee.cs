namespace Sigmond.Buslog.Model;

public readonly record struct LoanFee(
    string SecurityId,
    int MarketValue,
    int DailyFee
);