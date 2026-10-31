using System;using System.Numerics;
namespace Example.Parity;

public static class Rules
{
public static int Double(int x)=>x*2;
public static int Call(int x)=>Double(x);
public static int Optional(int x=2)=>x;
public static int Explicit()=>Optional(4);
public static int Recursive(int x)=>Recursive(x);
}
