using System;using System.Numerics;
namespace Example.Parity;

public static class Rules
{
public static int Add(ushort a,ushort b)=>a+b;
public static int Subtract(ushort a,ushort b)=>a-b;
public static int Multiply(ushort a,ushort b)=>a*b;
public static int Divide(ushort a,ushort b)=>a/b;
public static bool Equal(ushort a,ushort b)=>a==b;
public static bool NotEqual(ushort a,ushort b)=>a!=b;
public static bool Less(ushort a,ushort b)=>a<b;
public static bool LessEqual(ushort a,ushort b)=>a<=b;
public static bool Greater(ushort a,ushort b)=>a>b;
public static bool GreaterEqual(ushort a,ushort b)=>a>=b;
}
