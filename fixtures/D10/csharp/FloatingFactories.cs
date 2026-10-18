using System;
using System.Numerics;

namespace Example.D10;

public static class Rules
{
    public static byte Small(double value) => byte.CreateSaturating(value);
    public static int Wrapped(float value) => float.ConvertToInteger<int>(value);
    public static Half Narrow(double value) => Half.CreateTruncating(value);
    public static Complex Extend(long value) => Complex.CreateChecked(value);
}
