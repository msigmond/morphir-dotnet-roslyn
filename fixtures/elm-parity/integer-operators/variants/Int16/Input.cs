using System;using System.Numerics;
namespace Example.Parity;

public static class Rules
{
public static int Add(short a,short b)=>a+b;
public static int Subtract(short a,short b)=>a-b;
public static int Multiply(short a,short b)=>a*b;
public static int Divide(short a,short b)=>a/b;
public static bool Equal(short a,short b)=>a==b;
public static bool NotEqual(short a,short b)=>a!=b;
public static bool Less(short a,short b)=>a<b;
public static bool LessEqual(short a,short b)=>a<=b;
public static bool Greater(short a,short b)=>a>b;
public static bool GreaterEqual(short a,short b)=>a>=b;
}
