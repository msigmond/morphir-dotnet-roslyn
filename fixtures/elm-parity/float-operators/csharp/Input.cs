using System;using System.Numerics;
namespace Example.Parity;

public static class Rules
{
public static double Add(double a,double b)=>a+b;
public static double Subtract(double a,double b)=>a-b;
public static double Multiply(double a,double b)=>a*b;
public static double Divide(double a,double b)=>a/b;
public static bool Less(double a,double b)=>a<b;
}
