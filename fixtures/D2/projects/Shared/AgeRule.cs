namespace Example.D2.Shared;
#if INTERNAL_REFERENCE
internal
#else
public
#endif
static class AgeRule
{
    public static bool Adult(int age) => age >= 18;
}
internal readonly record struct Unused(bool Flag);
