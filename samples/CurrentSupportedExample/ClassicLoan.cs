namespace Example.CurrentSupported;

public readonly record struct ClassicLoan(string SecurityId, int Quantity, int Price, int AnnualFeeRate);
public readonly record struct ClassicLoanFee(string SecurityId, int MarketValue, int DailyFee);

public record ClassicBox<T>(T Value);

public static class ClassicLoanCalculations
{
    public static int ClassifyQuantity(int quantity) => quantity switch { 0 => 0, _ => 1 };
    public static int FirstOrZero(int[] items) => items switch { [] => 0, [var first, ..] => first };
    public static int LoanPresence(ClassicLoan? loan) => loan switch { null => 0, _ => 1 };
    public static int ClassifyState(ClassicState state)
    {
        switch (state) { case ClassicState.None: return 0; default: return 1; }
    }

    public static int[] CreateItems(int value) => [value, 2];
    public static int? ReadItem(System.Collections.Generic.IReadOnlyList<int> items, int index) => items[index];
    public static int ItemCount(int[] items) => items.Length;
    public static System.Collections.Generic.Dictionary<int, string> CreateLookup(int key, string value) => new() { [key] = value };
    public static string? ReadLookup(System.Collections.Generic.IReadOnlyDictionary<int, string> items, int key) => items[key];
    public static System.Collections.Generic.HashSet<int> CreateSet(int value) => [value, 2];

    public static T Identity<T>(T value) => value;
    public static ClassicBox<T> Wrap<T>(T value) => new(value);
    public static T Unwrap<T>(ClassicBox<T> box) => box.Value;
    public static int ReadWrapped(int value) => Unwrap(Wrap(value));

    public static int MarketValue(ClassicLoan loan)
    {
        return loan.Quantity * loan.Price;
    }

    public static int DailyFee(ClassicLoan loan)
    {
        var marketVal = MarketValue(loan);
        var rate = loan.AnnualFeeRate;
        return marketVal * rate / 100 / 360;
    }

    public static ClassicLoanFee Calculate(ClassicLoan loan)
    {
        var marketVal = MarketValue(loan);
        var dailyF = DailyFee(loan);
        return new ClassicLoanFee(loan.SecurityId, marketVal, dailyF);
    }
    public static int PositiveMarketValue(ClassicLoan loan)
    {
        if (loan.Quantity > 0) return MarketValue(loan);
        return 0;
    }

    public static ClassicLoan SetQuantity(ClassicLoan loan, int quantity) => loan with { Quantity = quantity };

    public static int ReadPair((int Quantity, int Price) pair)
    {
        var (quantity, price) = pair;
        return quantity * price;
    }

    public static int AddFee(int value, int fee)
    {
        int Add(int amount) => amount + fee;
        return Add(value);
    }

    public static decimal Rounded(decimal value) => decimal.Round(value);

    public static int Optional(int? value) => value ?? 3;
    public static int? NullableProduct(int? a, int? b) => a * b;
    public static decimal? DivideDecimals(decimal amount, decimal count) => amount / count;
    public static int SumDefaults(int a = 2, int b = 3) => a + b;
    public static int UseDefaults() => SumDefaults(b: 4);
    public static int Count(params int[] values) => values.Length;
    public static int UseParams() => Count(1, 2);
    public static int Choose(int value) => value;
    public static long Choose(long value) => value;
    public static int UseOverload(int value) => Choose(value);
    public static System.Tuple<int, bool> ReferencePair(int quantity, bool flag) => System.Tuple.Create(quantity, flag);
    public static ClassicState State() => ClassicState.Active;
    public static ClassicValue CreateValue(int value) => new ClassicValue(value);
}

public enum ClassicState { None, Active }
public class ClassicValue(int value)
{
    public int Read() => value;
}
