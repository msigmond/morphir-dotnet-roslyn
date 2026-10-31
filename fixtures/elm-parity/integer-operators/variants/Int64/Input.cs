using System;using System.Numerics;
namespace Example.Parity;

public static class Rules
{
public static long Add(long a,long b)=>a+b;
public static long Subtract(long a,long b)=>a-b;
public static long Multiply(long a,long b)=>a*b;
public static long Divide(long a,long b)=>a/b;
public static bool Equal(long a,long b)=>a==b;
public static bool NotEqual(long a,long b)=>a!=b;
public static bool Less(long a,long b)=>a<b;
public static bool LessEqual(long a,long b)=>a<=b;
public static bool Greater(long a,long b)=>a>b;
public static bool GreaterEqual(long a,long b)=>a>=b;
}
