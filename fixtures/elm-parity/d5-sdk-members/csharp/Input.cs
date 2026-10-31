using System;namespace Example.Completion;public static class Rules{
 public static int Abs(int value)=>Math.Abs(value);
 public static int Min(int a,int b)=>Math.Min(a,b);
 public static double Max(double a,double b)=>Math.Max(a,b);
 public static double Sqrt(double value)=>Math.Sqrt(value);
 public static double Sin(double value)=>Math.Sin(value);
 public static double Cos(double value)=>Math.Cos(value);
 public static double Atan2(double a,double b)=>Math.Atan2(a,b);
 public static double Power(double a,double b)=>Math.Pow(a,b);
 public static bool Nan(double value)=>double.IsNaN(value);
 public static bool Infinite(float value)=>float.IsInfinity(value);
 public static string Text(int value)=>value.ToString();
 public static int Length(string value)=>value.Length;
 public static double Tan(double x)=>Math.Tan(x);
 public static double Asin(double x)=>Math.Asin(x);
 public static double Acos(double x)=>Math.Acos(x);
 public static double Atan(double x)=>Math.Atan(x);
 public static float SingleSqrt(float x)=>MathF.Sqrt(x);
 public static long LongAbs(long x)=>long.Abs(x);
 public static int IntMin(int a,int b)=>int.Min(a,b);
 public static double DoubleMax(double a,double b)=>double.Max(a,b);
 public static float SingleAbs(float x)=>float.Abs(x);
 public static string FloatText(double x)=>x.ToString();
 public static string CharText(char x)=>x.ToString();
 }
