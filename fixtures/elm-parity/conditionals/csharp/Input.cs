using System;using System.Numerics;
namespace Example.Parity;

public static class Rules
{
public static int Choose(bool condition,int a,int b)=>condition?a:b;
public static int Constant(int a,int b)=>true?a:b;
}
