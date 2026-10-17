namespace Example.D1;
public readonly record struct Customer(int Age, bool Active);
public static class Rules { public static Customer Update(Customer customer) => customer with { Age = 20 }; }
