using System;using System.Numerics;
namespace Example.Parity;

public static class Rules
{
private static int Hidden(int x)=>x;
public static int Visible(int x)=>Hidden(x);
}
