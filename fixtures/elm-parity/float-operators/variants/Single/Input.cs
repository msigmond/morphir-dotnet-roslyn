using System;using System.Numerics;
namespace Example.Parity;

public static class Rules
{
public static float Add(float a,float b)=>a+b;
public static float Subtract(float a,float b)=>a-b;
public static float Multiply(float a,float b)=>a*b;
public static float Divide(float a,float b)=>a/b;
public static bool Less(float a,float b)=>a<b;
}
