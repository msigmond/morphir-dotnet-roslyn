namespace Trusted;
public static class Rules
{
    public static bool Meets(int age, int minimum = 18) => age >= minimum;
}
