using System;using System.Numerics;
namespace Example.Parity;

public static class Rules
{
public static int Add(int a,int b)=>a+b;
public static int Subtract(int a,int b)=>a-b;
public static int Multiply(int a,int b)=>a*b;
public static int Divide(int a,int b)=>a/b;
public static bool Equal(int a,int b)=>a==b;
public static bool NotEqual(int a,int b)=>a!=b;
public static bool Less(int a,int b)=>a<b;
public static bool LessEqual(int a,int b)=>a<=b;
public static bool Greater(int a,int b)=>a>b;
public static bool GreaterEqual(int a,int b)=>a>=b;
}
