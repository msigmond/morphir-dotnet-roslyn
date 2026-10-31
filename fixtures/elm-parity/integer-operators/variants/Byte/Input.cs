using System;using System.Numerics;
namespace Example.Parity;

public static class Rules
{
public static int Add(byte a,byte b)=>a+b;
public static int Subtract(byte a,byte b)=>a-b;
public static int Multiply(byte a,byte b)=>a*b;
public static int Divide(byte a,byte b)=>a/b;
public static bool Equal(byte a,byte b)=>a==b;
public static bool NotEqual(byte a,byte b)=>a!=b;
public static bool Less(byte a,byte b)=>a<b;
public static bool LessEqual(byte a,byte b)=>a<=b;
public static bool Greater(byte a,byte b)=>a>b;
public static bool GreaterEqual(byte a,byte b)=>a>=b;
}
