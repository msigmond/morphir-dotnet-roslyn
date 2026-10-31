using System;using System.Numerics;
namespace Example.Parity;

public static class Rules
{
public static uint Add(uint a,uint b)=>a+b;
public static uint Subtract(uint a,uint b)=>a-b;
public static uint Multiply(uint a,uint b)=>a*b;
public static uint Divide(uint a,uint b)=>a/b;
public static bool Equal(uint a,uint b)=>a==b;
public static bool NotEqual(uint a,uint b)=>a!=b;
public static bool Less(uint a,uint b)=>a<b;
public static bool LessEqual(uint a,uint b)=>a<=b;
public static bool Greater(uint a,uint b)=>a>b;
public static bool GreaterEqual(uint a,uint b)=>a>=b;
}
