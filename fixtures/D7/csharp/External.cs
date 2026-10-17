namespace Example.D7;
public static class Rules
{
    public static bool Eligible(int age) => Trusted.Rules.Meets(minimum: 18, age: age);
    public static bool DefaultEligible(int age) => Trusted.Rules.Meets(age);
    public static bool Order(int value) => Trusted.Rules.Meets(minimum: value, age: value = 2);
    public static bool Reference(int age)
    {
        System.Func<int, int, bool> f = Trusted.Rules.Meets;
        return f(age, 18);
    }
}
