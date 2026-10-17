namespace Example.CurrentSupported;

public readonly record struct CallCustomer(int Age)
{
    public bool Minimum(int minimum = 18) => Age >= minimum;
    public bool Adult() => Minimum();
}
public static partial class CurrentSupportedExample
{
    private static bool CallMeets(int age, int minimum = 18) => age >= minimum;
    private static bool CallMeets(bool approval) => approval;
    private static int CallSelect(int first, int second) => first;
    private static int CallCount(params int[]? values) => values?.Length ?? -1;
    private static int CallFirst(params int[]? values)
    {
        if (values == null || values.Length == 0) return -1;
        return values[0];
    }
    private static bool CallAdultAge(int value) => value >= 18;
    public static bool CallEligible(int age) => CallMeets(minimum: 18, age: age);
    public static bool CallApproved(bool approval) => CallMeets(approval);
    public static bool CallDefaultEligible(int age) => CallMeets(age);
    public static int CallNamedOrder(int value) => CallSelect(second: value, first: value = 2);
    public static int CallEmpty() => CallCount();
    public static int CallMissing() => CallCount(null);
    public static int CallPacked(int value) => CallFirst(value, value = 2);
    public static bool CallInstance(CallCustomer value) => value.Adult();
    public static bool CallLocal(int age)
    {
        static bool Allows(int value, int minimum = 18) => value >= minimum;
        return Allows(value: age);
    }
    public static bool CallReference(int age)
    {
        System.Func<int, bool> check = CallAdultAge;
        return check(age);
    }
    public static bool CallSnapshot(int age)
    {
        var value = new CallCustomer(age);
        System.Func<bool> check = value.Adult;
        value = new CallCustomer(100);
        return check();
    }
}
