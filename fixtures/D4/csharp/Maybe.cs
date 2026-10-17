namespace Example.D4;
public readonly record struct Customer(int? Age, string? Region);
public static class Rules
{
    public static bool Adult(Customer customer)
    {
        if (!customer.Age.HasValue) return false;
        return customer.Age.Value >= 18;
    }
    public static string RegionOrDefault(Customer customer) => customer.Region ?? "unknown";
}
