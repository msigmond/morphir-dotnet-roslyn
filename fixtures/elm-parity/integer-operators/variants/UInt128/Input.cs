using System;using System.Numerics;
namespace Example.Parity;

public static class Rules
{
public static UInt128 Add(UInt128 a,UInt128 b)=>a+b;
public static UInt128 Subtract(UInt128 a,UInt128 b)=>a-b;
public static UInt128 Multiply(UInt128 a,UInt128 b)=>a*b;
public static UInt128 Divide(UInt128 a,UInt128 b)=>a/b;
public static bool Equal(UInt128 a,UInt128 b)=>a==b;
public static bool NotEqual(UInt128 a,UInt128 b)=>a!=b;
public static bool Less(UInt128 a,UInt128 b)=>a<b;
public static bool LessEqual(UInt128 a,UInt128 b)=>a<=b;
public static bool Greater(UInt128 a,UInt128 b)=>a>b;
public static bool GreaterEqual(UInt128 a,UInt128 b)=>a>=b;
}
