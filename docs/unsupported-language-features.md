# Unsupported language features

This guide applies to the D1–D13 and D11 follow-up profile on 2026-11-01.
Each C# block below is a separate rejected input.
Add `namespace Example.Unsupported;` before each block.
Select that namespace when you run the transpiler.

Save one block, with its namespace, as `Input.cs`. Run this command from the repository root after build:

```bash
dotnet run --project src/Morphir.Frontend.CSharp.Cli --no-build -- --package Example --namespace Example.Unsupported --source Input.cs --output artifacts/unsupported.json
```

Valid C# can still fail conversion. The diagnostic gives a source location.
The transpiler returns no IR for a rejected input. The CLI keeps the previous output file.
There are no runtime safety checks that can enable these forms.

Use the [current supported example](../samples/CurrentSupportedExample/README.md) for accepted code.
The [language catalog](catalogs/forms.json) records the full feature inventory and delivery owners.

## Changes to stored values

Do not assign a local again. Input-field writes and static-field writes are also rejected.
Increment, decrement, compound assignment, and `??=` are not supported.
An explicit data constructor can assign each field once. A record `with` update is supported.

```csharp
public static class Rules
{
    public static int Read(int value) { value += 1; return value; }
}
```

## Construction and property bodies

A data constructor must assign every field once. Omitted field values are rejected.
Constructor chaining, extra statements, instance initializers, and object initializers are also rejected.

```csharp
public class Cell
{
    public int Value;
    public Cell() { }
}
```

```csharp
public class Cell
{
    public int Value;
    public Cell(int value) { Value = value; }
}
public static class Rules
{
    public static Cell Create() => new Cell(1) { Value = 2 };
}
```

Property setter bodies and static constructor bodies are rejected.
Stored properties and supported getter bodies remain permitted.

```csharp
public class Cell
{
    public int Value { get => 1; set { } }
}
```

## Object identity and recursive data

Reference identity has no approved mapping. Recursive record aliases are also rejected.

```csharp
public class Cell { }
public static class Rules
{
    public static bool Same(Cell a, Cell b) => object.ReferenceEquals(a, b);
}
```

```csharp
public class Node { public Node? Next; }
```

## Nullable value extraction and nested results

`Nullable<T>.Value` is rejected. A prior null check does not enable it.
Coalesce with another nullable value and nested nullable results also lack a mapping.
Simple `??`, `?.`, null tests, and lifted arithmetic have supported forms.

```csharp
public static class Rules
{
    public static int Read(int? value) => value.Value;
}
```

```csharp
public static class Rules
{
    public static int? Choose(int? first, int? second) => first ?? second;
}
```

```csharp
public record Leaf(int Value);
public record Node(Leaf? Child);
public static class Rules
{
    public static int? Read(Node? node) => node?.Child?.Value;
}
```

## Numeric operations and conversions

Integer bit operations and shifts are rejected. Boolean `&`, `|`, and `^` are supported.

```csharp
public static class Rules
{
    public static int Shift(int value, int count) => value >> count;
}
```

A Decimal division needs a declared `decimal?` result. A plain `decimal` result is rejected.
Implicit conversion between different numeric forms also needs an approved mapping.
Supported explicit casts are listed in the [mapping guide](classic-output.md).

```csharp
public static class Rules
{
    public static decimal Divide(decimal amount, decimal count) => amount / count;
}
```

```csharp
public static class Rules
{
    public static decimal Convert(int value) => value;
}
```

Whole-number literals must remain exact in the pinned compiler JSON number representation.
The model also requires signed 64-bit literal storage.
An isolated UTF-16 surrogate cannot form an Elm Char.

```csharp
public static class Rules
{
    public static long Large() => 9007199254740993L;
}
```

## Arrays and collections

One-dimensional arrays and the approved List/Dict/Set carriers have supported forms.
Multidimensional arrays, Queue/Stack, capacity/comparer constructors, and mutations remain rejected.
Computed array dimensions also remain rejected. A List literal cannot retain that written operation.
An index read needs a declared nullable result. Already-nullable elements need another flattening mapping.
Expanded `params` arguments can form an Elm List. A params length read is supported; indexing is not.
Single-parameter filter/map callbacks, seeded Aggregate, and ToList/ToArray are supported on List carriers.
Indexed callbacks, unseeded Aggregate, result selectors, query syntax, and Set/Dict enumeration remain rejected.

```csharp
public static class Rules
{
    public static System.Collections.Generic.IEnumerable<int> Read(int[] values) =>
        System.Linq.Enumerable.Select(values, (value, index) => value + index);
}
```

```csharp
public static class Rules
{
    public static int Read(int[] values) => values[0];
}
```

```csharp
public static class Rules
{
    public static System.Collections.Generic.Queue<int> Create() => new();
}
```

```csharp
public static class Rules
{
    public static int Read(params int[] values) => values[0];
}
```

## Tuple limits

Empty CLR tuples and one-item CLR tuples are rejected.
Tuple field reads have a mapping only for pairs.

```csharp
public static class Rules
{
    public static System.Tuple<int> Read(System.Tuple<int> value) => value;
}
```

```csharp
public static class Rules
{
    public static int Read((int, int, int) value) => value.Item3;
}
```

## Patterns and switches

Returned switches support literal, enum, nullable, var, tuple, and list head/tail patterns.
Guards, relational/logical patterns, property patterns, runtime type tests, and grouped labels remain rejected.
Negative, Decimal, computed, and default-value patterns have no direct pinned compiler mapping.
Only a final list slice is supported. Plain-result indexing and nullable extraction keep their earlier limits.

```csharp
public static class Rules
{
    public static int Choose(int value) => value switch { > 0 => 1, _ => 2 };
}
```

```csharp
public record Cell(int Value);
public static class Rules
{
    public static bool Test(Cell cell) => cell is { Value: > 0 };
}
```

## Anonymous functions and delegate declarations

Custom delegate declarations, multicast delegates, async/ref callbacks, expression trees,
and mutation remain rejected. Lambdas, anonymous methods, lexical captures, `Func`, `Action`,
and the approved collection callbacks have supported forms.

```csharp
public static class Rules
{
    public static System.Func<int, int> Create(System.Func<int, int> first, System.Func<int, int> second) => first + second;
}
```

```csharp
public delegate int Read(int value);
```

## Loops and references to storage

Loops, labels, `goto`, and by-reference forms are not supported.
This includes `ref`, `out`, and `in` parameters, ref locals, and ref returns.

```csharp
public static class Rules
{
    public static int Read(int value)
    {
        while (value > 0) value--;
        return value;
    }
}
```

```csharp
public static class Rules
{
    public static int Read(ref int value) => value;
}
```

## Generic limits

Unconstrained generic data and methods have supported forms.
Constraints, variance, type-parameter construction, and generic default values are rejected.
An unused method type parameter has no Elm signature variable.

```csharp
public class Box<T> where T : class { }
```

```csharp
public static class Rules
{
    public static T Empty<T>() => default;
}
```

```csharp
public static class Rules
{
    public static int One<T>() => 1;
}
```

A generic local function needs a use that supplies its body type.
References only from other unused locals do not supply this context.
Conflicting body types are rejected by the pinned Elm compiler and the C# frontend.

```csharp
public static class Rules
{
    public static int Read(int value)
    {
        T Echo<T>(T item) => item;
        return value;
    }
}
```

```csharp
public static class Rules
{
    public static (int, string) Read(int number, string text)
    {
        T Echo<T>(T item) => item;
        return (Echo(number), Echo(text));
    }
}
```

## Interfaces, inheritance, and type tests

Interfaces, variance, inheritance, and abstract classes are rejected.
Abstract, virtual, and override members and `base` access are also rejected.
No dispatch body is generated.

```csharp
public interface IRule<out T> { T Read(); }
```

```csharp
public class Parent { public virtual int Read() => 1; }
public class Child : Parent { public override int Read() => base.Read(); }
```

Runtime type tests and casts to object, base, or interface types have no approved mapping.
Boxing and unboxing are not supported.

```csharp
public class Cell { }
public static class Rules
{
    public static bool Test(Cell cell) => cell is object;
}
```

## Name limits

Names must remain distinct after Morphir splits words and changes letter case.
The same type name at different arities is rejected. Type-parameter shadowing is also rejected.
Type parameters named `number`, `comparable`, `appendable`, or `compappend` imply unapproved Elm constraints.

```csharp
public record Foo_Bar(int Value);
public record FooBar(int Value);
```

```csharp
public record Box(int Value);
public record Box<T>(T Value);
```

```csharp
public static class Rules
{
    public static number Read<number>(number value) => value;
}
```

## Exceptions, iterators, and asynchronous code

`throw`, `try`, `catch`, filters, `finally`, and `yield` are not supported.
Asynchronous methods, `await`, and asynchronous iteration are also rejected.

```csharp
public static class Rules
{
    public static int Read() { throw new System.InvalidOperationException(); }
}
```

```csharp
public static class Rules
{
    public static System.Collections.Generic.IEnumerable<int> Read() { yield return 1; }
}
```

```csharp
public static class Rules
{
    public static async System.Threading.Tasks.Task<int> Read() =>
        await System.Threading.Tasks.Task.FromResult(1);
}
```

## Dynamic and unsafe code

Dynamic binding, pointers, function pointers, `stackalloc`, spans, and ref-like types are rejected.

```csharp
public static class Rules
{
    public static dynamic Read(dynamic value) => value.Name;
}
```

```csharp
public unsafe class Rules
{
    public static int Read(int* value) => *value;
}
```

## Other unmapped forms

Query syntax, anonymous objects, interpolated strings, events, and destructors are rejected.
Resource statements and lock statements are also rejected.

```csharp
public static class Rules
{
    public static string Read(int value) => $"Value: {value}";
}
```

```csharp
using System.Linq;
public static class Rules
{
    public static System.Collections.Generic.IEnumerable<int> Read(
        System.Collections.Generic.IEnumerable<int> values) =>
        from value in values select value;
}
```

```csharp
public static class Rules
{
    public static int Read()
    {
        using (var stream = new System.IO.MemoryStream()) { return 1; }
    }
}
```

## Framework API limits

A supported numeric type does not permit every member of that type.
Decimal precision, scale, rounding-mode APIs, and generic numeric conversion APIs have unmapped forms.
Date/time types, `Guid`, `Complex`, and unlisted framework types also lack a mapping.
See the [framework guide](catalogs/framework.md), [numeric members](catalogs/numeric-members.json),
and [Decimal members](catalogs/decimal-members.json) for each signature.
Array-taking BigInteger and Decimal constructors also remain rejected.

```csharp
public static class Rules
{
    public static decimal Round(decimal value) => decimal.Round(value, 2);
}
```

```csharp
public static class Rules
{
    public static int Convert(int value) => int.CreateChecked<int>(value);
}
```

## Evidence and future work

The [form cases](../fixtures/elm-parity/form-rejections.json)
and [member cases](../fixtures/elm-parity/member-rejections.json) contain executable rejection inputs.
The [D12 report](deliveries/D12-elm-parity.md) records the current acceptance checks.
The [delivery plan](../Morphir_CSharp_Frontend_delivery_plan.md) assigns later work to D14–D21.
A planned feature is not a supported feature. A new mapping needs paired C#/Elm tests with complete JSON equality.
All 45 C# blocks passed C#14 compiler binding and failed conversion when this guide was checked.
