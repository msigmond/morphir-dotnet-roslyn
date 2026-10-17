namespace Sample.Business { public static class Rules { public static int Read(Shared.Customer c) => c.Age; } }
namespace Shared { public readonly record struct Customer(int Age, bool Active); internal readonly record struct Unused(bool Flag); }
