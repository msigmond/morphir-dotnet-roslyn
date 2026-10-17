namespace Example.CurrentSupported;

public static partial class CurrentSupportedExample
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
    public static int Early(int age, bool active)
    {
        if (!active) return 0;
        const int adult = 18;
        return age >= adult ? 2 : 1;
    }
    public static int Reassign(int value)
    {
        int zero = 0;
        value = value + zero;
        return value;
    }
    public static int Scoped(int value)
    {
        int copy = value;
        {
            const int extra = 0;
            copy = copy + extra;
        }
        return copy;
    }
    public static int InitLater(bool active)
    {
        int result;
        if (active) result = 1;
        else result = 2;
        return result;
    }
    public static bool Boolean(bool a, bool b) => !(a == b) & (a | b) ^ (a != b);
    public static string KnownLabel(bool active)
    {
        const string label = "eligible";
        if (active) return label;
        return "inactive";
    }
    public static int ConstantBranch()
    {
        if (true) return 3;
    }
    public static int ValueCopy(Customer customer)
    {
        var copy = customer;
        customer = new Customer(0, false);
        return copy.Age;
    }
    public static int AssignmentOrder(int value) { return value + (value = 0); }
    public static int NestedAssignment(int value, int other) { value = (other = 1) + 0; return other; }
    public static bool ShortAssignment(bool active, bool other) { bool result = active && (other = false); return other; }
    public static int ConditionalAssignment(bool active, int value) { return active ? (value = 1) : (value = 2); }
    public static int ArgumentAssignment(int value) { return FirstArgument(value, value = 0); }
    private static int FirstArgument(int first, int second) => first;
}
