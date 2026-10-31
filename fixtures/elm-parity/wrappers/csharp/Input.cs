using System;using System.Numerics;
namespace Example.Parity;

public static class Rules
{
public static int Checked(int a)=>checked(a*2);
public static int Unchecked(int a)=>unchecked(a*2);
public static int Grouping(int a)=>(((a)));
public static string Suppress(string a)=>a!;
}
