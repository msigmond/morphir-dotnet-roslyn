using System;using System.Numerics;
namespace Example.Parity;

public static class Rules
{
public static (int Code,bool Flag) Identity((int Code,bool Flag) pair)=>pair;
public static (int Code,bool Flag) Create(int code,bool flag)=>(code,flag);
}
