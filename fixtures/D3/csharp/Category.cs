namespace Example.D3;
public static class Rules
{
    public static int Category(int age, bool active)
    {
        int category = 0;
        if (active)
        {
            category = 1;
            if (age >= 18) category = 2;
        }
        return category;
    }
}
