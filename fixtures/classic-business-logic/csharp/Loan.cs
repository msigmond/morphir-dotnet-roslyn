namespace Sigmond.Buslog.Model;

public readonly record struct Loan(
    string SecurityId,
    int Quantity,
    int Price,
    int AnnualFeeRate
);