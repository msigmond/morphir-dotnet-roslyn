using System;using System.Numerics;
namespace Example.Parity;

public static class Rules
{
public static Half Add(Half a,Half b)=>a+b;
public static Half Subtract(Half a,Half b)=>a-b;
public static Half Multiply(Half a,Half b)=>a*b;
public static Half Divide(Half a,Half b)=>a/b;
public static bool Less(Half a,Half b)=>a<b;
}
