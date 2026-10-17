namespace Example.D7;
public readonly record struct Customer(int Age)
{
    public bool Minimum(int minimum = 18) => Age >= minimum;
    public bool Adult() => Minimum();
}
public static class Rules
{
    private static bool Meets(int age, int minimum = 18) => age >= minimum;
    private static bool Meets(bool approval) => approval;
    private static int Select(int first, int second) => first;
    private static int Count(params int[] values) => values?.Length ?? -1;
    private static int First(params int[] values)
    {
        if (values == null || values.Length == 0) return -1;
        return values[0];
    }
    private static bool AdultAge(int value) => value >= 18;
    public static bool Eligible(int age) => Meets(minimum: 18, age: age);
    public static bool Approved(bool approval) => Meets(approval);
    public static bool DefaultEligible(int age) => Meets(age);
    public static int NamedOrder(int value) => Select(second: value, first: value = 2);
    public static int Empty() => Count();
    public static int Missing() => Count(null);
    public static int Packed(int value) => First(value, value = 2);
    public static bool Instance(Customer value) => value.Adult();
    public static bool Local(int age)
    {
        static bool Allows(int value, int minimum = 18) => value >= minimum;
        return Allows(value: age);
    }
    public static bool Reference(int age)
    {
        System.Func<int, bool> check = AdultAge;
        return check(age);
    }
    public static bool Snapshot(int age)
    {
        var value = new Customer(age);
        System.Func<bool> check = value.Adult;
        value = new Customer(100);
        return check();
    }
}
