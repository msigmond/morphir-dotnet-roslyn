using System;using System.Linq;using System.Collections.Generic;
namespace Example.Parity;
public static class Rules
{
 public static IEnumerable<int> Filter(int[] values,int minimum)=>values.Where(value=>value>minimum);
 public static IEnumerable<int> Map(int[] values)=>values.Select(value=>value*2);
 public static int[] Chain(int[] values)=>values.Where(value=>value>0).Select(value=>value+1).ToArray();
 public static List<int> Copy(IEnumerable<int> values)=>values.ToList();
 public static IEnumerable<string> Callback(int[] values,Func<int,string> callback)=>values.Select(callback);
 public static IEnumerable<T> Generic<T>(IEnumerable<T> values)=>values.Select(value=>value);
 public static List<int> Find(List<int> values)=>values.FindAll(value=>value>0);
 public static List<int> Convert(List<int> values)=>values.ConvertAll(value=>value+1);
 public static IEnumerable<int> Named(int[] values)=>Enumerable.Select(selector:value=>value+1,source:values);
 public static IEnumerable<int?> Nullable(int[] values)=>values.Select(value=>(int?)value);
 public static IEnumerable<int> Method(int[] values)=>values.Select(Increment);
 public static int Increment(int value)=>value+1;
 public static List<string> ConvertCallback(List<int> values,Converter<int,string> callback)=>values.ConvertAll(callback);
 public static List<int> FindCallback(List<int> values,Predicate<int> callback)=>values.FindAll(callback);
}
