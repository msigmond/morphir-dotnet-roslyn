namespace Example.D1;

public readonly record struct Customer(int Age, bool Active);

public static class Rules
{
    public static bool Eligible(Customer customer) => customer.Active && customer.Age >= 18;
    public static string EligibleLabel() => "eligible";
    public static Customer Create(int age, bool active) => new Customer(age, active);
    public static int Addition() => 20 + 22;
    public static int IdentityAddition(int value) => value + 0;
}
