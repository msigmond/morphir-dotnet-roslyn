# D1 and D2: container-preserving preview

Historical preview; normative execution context: [master](../../Morphir_CSharp_Frontend_delivery_plan.md), [D1](../../Morphir_CSharp_Frontend_deliveries/D01-cli-record.md), [D2](../../Morphir_CSharp_Frontend_deliveries/D02-project-inputs.md).

The TASTy frontend maps a Scala object to a module with the object's name. A case class gets a module with the same name, containing its record alias and supported instance methods. See [TypeDefMorph.scala](https://github.com/msigmond/morphir-jvm/blob/3086f7ad501d13239b474e23baa3d7fc88baf881/morphir/frontend/src/morphir/codegen/tasty/TypeDefMorph.scala) and the [HelperCall Elm fixture](https://github.com/msigmond/morphir-jvm/blob/3086f7ad501d13239b474e23baa3d7fc88baf881/morphir/frontend/test-fixtures/elm/helper-call/src/Arithmetic/HelperCall.elm).

Keep the static-class/member grouping in C#. Define data types in their namespace/enclosing-type module so their full names match the source. The TASTy data-type placement repeats the type name and is not copied. Rules keeps its own module. A partial class retains one member container across all its source files.

For these examples, set the Morphir package name to `Example`. Elm source modules start with `Example`; IR module paths contain the remaining segments.

| C# declaration | Elm source module | Declaration in that module |
| --- | --- | --- |
| `Example.D1.Customer` | `Example.D1` | `Customer` record alias |
| `Example.D1.Rules.Eligible` | `Example.D1.Rules` | `eligible` function |
| `Example.D2.Business.Rules` | `Example.D2.Business.Rules` | Its handwritten and generated functions |
| `Example.D2.Shared.AgeRule` | `Example.D2.Shared.AgeRule` | Its functions |

## D1 — Immutable business record and rule through CLI

Select `Example.D1`. Customer remains the named type `Example.D1.Customer`. Rules remains the module `Example.D1.Rules`.

### C# — Customer.cs

```csharp
namespace Example.D1;

public readonly record struct Customer(int Age, bool Active);
```

### C# — Rules.cs

```csharp
namespace Example.D1;

public static class Rules
{
    public static bool Eligible(Customer customer) =>
        customer.Active && customer.Age >= 18;

    public static string EligibleLabel() => "eligible";
}
```

### Elm — Example/D1.elm

```elm
module Example.D1 exposing (Customer)

type alias Customer =
    { age : Int, active : Bool }
```

### Elm — Example/D1/Rules.elm

```elm
module Example.D1.Rules exposing (eligible, eligibleLabel)

import Example.D1 exposing (Customer)

eligible : Customer -> Bool
eligible customer =
    if customer.active then
        customer.age >= 18
    else
        False

eligibleLabel : String
eligibleLabel =
    "eligible"
```

Check: Customer is a type in the namespace module, so its full name is `Example.D1.Customer`. Eligible stays in the Rules module and imports that type. An active customer aged 18 is eligible; an inactive customer is not.

## D2 — Project inputs, generated source and container boundaries

Select `Example.D2.Business` and `Example.D2.Shared`. The referenced source project supplies AgeRule. The configured source generator supplies the second part of Rules.

This preview uses a public cross-module helper and a private helper within Rules. Both helpers are named Adult; their containers distinguish them. It does not silently expose the old internal helper. The revised plan retains Private IR access across preserved containers and uses independent complete Elm IR/codec fixtures when ordinary Elm source cannot express that visibility combination.

### C# — Business/Rules.cs

```csharp
namespace Example.D2.Business;

public static partial class Rules
{
    private static bool Adult(int age) =>
        Example.D2.Shared.AgeRule.Adult(age);

    public static bool Eligible(int age) => Adult(age);
}
```

### C# — Shared/AgeRule.cs

```csharp
namespace Example.D2.Shared;

public static class AgeRule
{
    public static bool Adult(int age) => age >= 18;
}
```

### C# — generated Business/Rules.Generated.cs

```csharp
namespace Example.D2.Business;

public static partial class Rules
{
    public static int GeneratedThreshold() => 18;
}
```

### Elm — Example/D2/Shared/AgeRule.elm

```elm
module Example.D2.Shared.AgeRule exposing (adult)

adult : Int -> Bool
adult age =
    age >= 18
```

### Elm — Example/D2/Business/Rules.elm

```elm
module Example.D2.Business.Rules exposing (eligible, generatedThreshold)

import Example.D2.Shared.AgeRule as AgeRule

adult : Int -> Bool
adult age =
    AgeRule.adult age

eligible : Int -> Bool
eligible age =
    adult age

generatedThreshold : Int
generatedThreshold =
    18
```

Check: Rules.Adult stays private in the Rules module. AgeRule.Adult is called through the AgeRule module. Both partial Rules files contribute to one module. The unused generated function remains present. Selecting only the business namespace causes an outside-source-dependency diagnostic; a project reference does not select the shared namespace.

## Verification

The C# source is unchanged by the type-path correction. The corrected Elm module paths are verified with the complete delivery-example checks. Complete C#/Elm IR parity still requires the new frontend.

## Integration status

The corrected type/member placement is applied throughout the delivery children. Data definitions retain their full source paths without an extra type-name segment; member modules retain their class identities. The delivery plan no longer merges distinct containers for nonpublic access. Complete-distribution codec fixtures cover the corresponding nonpublic access metadata and qualified references without changing source identity or promoting declarations.
