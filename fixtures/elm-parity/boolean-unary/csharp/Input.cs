using System;using System.Numerics;
namespace Example.Parity;

public static class Rules
{
public static bool And(bool a,bool b)=>a&&b;
public static bool Or(bool a,bool b)=>a||b;
public static bool Not(bool a)=>!a;
public static bool Xor(bool a,bool b)=>a^b;
public static int Negate(int a)=>-a;
public static int Positive(int a)=>+a;
public static double FloatNegate(double a)=>-a;
}
