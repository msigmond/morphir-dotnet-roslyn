using System;using System.Numerics;
namespace Example.Parity;

public static class Rules
{
public static Int128 Add(Int128 a,Int128 b)=>a+b;
public static Int128 Subtract(Int128 a,Int128 b)=>a-b;
public static Int128 Multiply(Int128 a,Int128 b)=>a*b;
public static Int128 Divide(Int128 a,Int128 b)=>a/b;
public static bool Equal(Int128 a,Int128 b)=>a==b;
public static bool NotEqual(Int128 a,Int128 b)=>a!=b;
public static bool Less(Int128 a,Int128 b)=>a<b;
public static bool LessEqual(Int128 a,Int128 b)=>a<=b;
public static bool Greater(Int128 a,Int128 b)=>a>b;
public static bool GreaterEqual(Int128 a,Int128 b)=>a>=b;
}
