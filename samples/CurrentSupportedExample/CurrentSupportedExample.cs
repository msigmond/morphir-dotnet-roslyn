namespace Example.CurrentSupported;

public readonly record struct Customer(int Age, bool Active)
{
    public static int DataConstant() => 1;
}
internal readonly record struct Unused(bool Flag);

public static partial class CurrentSupportedExample
{
    public readonly record struct Nested(int Value);
    public static class Tools
    {
        public static bool Echo(bool flag) => flag;
    }
    public static int Echo(int value) => value;
    public static bool Echo(bool value) => value;
    public static bool Eligible(Customer customer) => customer.Active && customer.Age >= 18;
    public static bool ActiveOrAdult(Customer customer) => customer.Active || customer.Age > 17;
    public static bool Below(Customer customer) => customer.Age < 18;
    public static bool AtMost(Customer customer) => customer.Age <= 18;
    public static bool Exactly(Customer customer) => customer.Age == 18;
    public static bool Different(Customer customer) => customer.Age != 18;
    /// <summary>The eligibility label.</summary>
    public static string EligibleLabel() => "eligible";
    public static Customer Create(int age, bool active) => new Customer(age, active);
    public static Customer TargetTyped(int age, bool active) => new(age, active);
    public static int Addition() => 20 + 22;
    public static int IdentityAddition(int value) => (value) + (0);
    public static int IdentityCast(int @checked) => unchecked((int)@checked + 0);
    public static bool Always() => true;
    public static Customer Named() => new Customer(Active: true, Age: 18);
    public static int Negative() => -18;
    private static int Hidden() => 7;
}

public static partial class CurrentSupportedExample
{
    private static bool HiddenFlag() => false;
}

public static class CurrentSupportedExampleTools
{
    public static int Number() => 2;
}
