using System;using System.Numerics;
namespace Example.Parity;

public static class Rules
{
public static int Add(sbyte a,sbyte b)=>a+b;
public static int Subtract(sbyte a,sbyte b)=>a-b;
public static int Multiply(sbyte a,sbyte b)=>a*b;
public static int Divide(sbyte a,sbyte b)=>a/b;
public static bool Equal(sbyte a,sbyte b)=>a==b;
public static bool NotEqual(sbyte a,sbyte b)=>a!=b;
public static bool Less(sbyte a,sbyte b)=>a<b;
public static bool LessEqual(sbyte a,sbyte b)=>a<=b;
public static bool Greater(sbyte a,sbyte b)=>a>b;
public static bool GreaterEqual(sbyte a,sbyte b)=>a>=b;
}
