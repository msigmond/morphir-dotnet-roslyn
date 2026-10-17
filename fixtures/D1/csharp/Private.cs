namespace Sample.Business;
internal readonly record struct Customer(int Age, bool Active);
internal readonly record struct Unused(bool Flag);
public static class Rules
{
    private static int Hidden() => 7;
    private static int Read(Customer customer) => customer.Age;
}
