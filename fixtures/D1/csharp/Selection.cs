namespace Sample.Business { public readonly record struct Customer(int Age, bool Active); public static class Rules { public static int Read(Customer c) => c.Age; } }
namespace Sample.Business.Tax { internal readonly record struct Rate(int Value); }
namespace Sample.BusinessExtra { public class Unsupported { public object Value = new(); } }
namespace Other { public class Ignored { } }
