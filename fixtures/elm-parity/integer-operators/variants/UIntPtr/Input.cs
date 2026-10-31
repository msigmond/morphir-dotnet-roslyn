using System;using System.Numerics;
namespace Example.Parity;

public static class Rules
{
public static nuint Add(nuint a,nuint b)=>a+b;
public static nuint Subtract(nuint a,nuint b)=>a-b;
public static nuint Multiply(nuint a,nuint b)=>a*b;
public static nuint Divide(nuint a,nuint b)=>a/b;
public static bool Equal(nuint a,nuint b)=>a==b;
public static bool NotEqual(nuint a,nuint b)=>a!=b;
public static bool Less(nuint a,nuint b)=>a<b;
public static bool LessEqual(nuint a,nuint b)=>a<=b;
public static bool Greater(nuint a,nuint b)=>a>b;
public static bool GreaterEqual(nuint a,nuint b)=>a>=b;
}
