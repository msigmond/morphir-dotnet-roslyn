using System;using System.Numerics;
namespace Example.Parity;

public static class Rules
{
public static ulong Add(ulong a,ulong b)=>a+b;
public static ulong Subtract(ulong a,ulong b)=>a-b;
public static ulong Multiply(ulong a,ulong b)=>a*b;
public static ulong Divide(ulong a,ulong b)=>a/b;
public static bool Equal(ulong a,ulong b)=>a==b;
public static bool NotEqual(ulong a,ulong b)=>a!=b;
public static bool Less(ulong a,ulong b)=>a<b;
public static bool LessEqual(ulong a,ulong b)=>a<=b;
public static bool Greater(ulong a,ulong b)=>a>b;
public static bool GreaterEqual(ulong a,ulong b)=>a>=b;
}
