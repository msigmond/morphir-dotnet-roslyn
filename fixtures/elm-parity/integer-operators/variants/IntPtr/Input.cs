using System;using System.Numerics;
namespace Example.Parity;

public static class Rules
{
public static nint Add(nint a,nint b)=>a+b;
public static nint Subtract(nint a,nint b)=>a-b;
public static nint Multiply(nint a,nint b)=>a*b;
public static nint Divide(nint a,nint b)=>a/b;
public static bool Equal(nint a,nint b)=>a==b;
public static bool NotEqual(nint a,nint b)=>a!=b;
public static bool Less(nint a,nint b)=>a<b;
public static bool LessEqual(nint a,nint b)=>a<=b;
public static bool Greater(nint a,nint b)=>a>b;
public static bool GreaterEqual(nint a,nint b)=>a>=b;
}
