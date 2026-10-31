using System;using System.Numerics;
namespace Example.Parity;

public static class Rules
{
public static BigInteger Add(BigInteger a,BigInteger b)=>a+b;
public static BigInteger Subtract(BigInteger a,BigInteger b)=>a-b;
public static BigInteger Multiply(BigInteger a,BigInteger b)=>a*b;
public static BigInteger Divide(BigInteger a,BigInteger b)=>a/b;
public static bool Equal(BigInteger a,BigInteger b)=>a==b;
public static bool NotEqual(BigInteger a,BigInteger b)=>a!=b;
public static bool Less(BigInteger a,BigInteger b)=>a<b;
public static bool LessEqual(BigInteger a,BigInteger b)=>a<=b;
public static bool Greater(BigInteger a,BigInteger b)=>a>b;
public static bool GreaterEqual(BigInteger a,BigInteger b)=>a>=b;
}
