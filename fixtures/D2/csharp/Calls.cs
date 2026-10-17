namespace Example.D2.Business
{
    public static partial class Rules
    {
        private static bool Adult(int age) => Example.D2.Shared.AgeRule.Adult(age);
        public static bool Eligible(int age) => Adult(age);
        public static int GeneratedThreshold() => 18;
    }
}
namespace Example.D2.Shared
{
    public static class AgeRule { public static bool Adult(int age) => age >= 18; }
}
