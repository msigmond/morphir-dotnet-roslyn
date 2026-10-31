using System;namespace Example.Completion;public static class Rules{
 public static decimal FromInt(int value)=>new decimal(value);
 public static decimal FromFloat(double value)=>new decimal(value);
 public static decimal Round(decimal value)=>decimal.Round(value);
 public static decimal Truncate(decimal value)=>decimal.Truncate(value);
 public static decimal Abs(decimal value)=>Math.Abs(value);
 public static string Text(decimal value)=>value.ToString();
 public static bool Equal(decimal a,decimal b)=>a==b;
 public static bool Different(decimal a,decimal b)=>a!=b;
 public static bool Less(decimal a,decimal b)=>a<b;
 public static bool LessEqual(decimal a,decimal b)=>a<=b;
 public static bool Greater(decimal a,decimal b)=>a>b;
 public static bool GreaterEqual(decimal a,decimal b)=>a>=b;
 public static decimal Add(decimal a,decimal b)=>decimal.Add(a,b);
 public static decimal Subtract(decimal a,decimal b)=>decimal.Subtract(a,b);
 public static decimal Multiply(decimal a,decimal b)=>decimal.Multiply(a,b);
 public static decimal Negate(decimal x)=>decimal.Negate(x);
 public static decimal StaticAbs(decimal x)=>decimal.Abs(x);
 public static bool StaticEquals(decimal a,decimal b)=>decimal.Equals(a,b);
 public static bool InstanceEquals(decimal a,decimal b)=>a.Equals(b);
 }
