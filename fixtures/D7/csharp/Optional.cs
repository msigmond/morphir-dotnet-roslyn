namespace Example.D7;
public static class Rules
{
    private static bool Meets(int age, int minimum = 18) => age >= minimum;
    private static bool Meets(bool approval) => approval;
    public static bool Eligible(int age) => Meets(minimum: 18, age: age);
    public static bool Approved(bool approval) => Meets(approval);
    public static bool DefaultEligible(int age) => Meets(age);
}
