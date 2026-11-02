using System;using System.Linq;
namespace Example.Parity;
public static class Rules
{
 public static int Sum(int[] values,int seed)=>values.Aggregate(seed,(total,value)=>total+value);
 public static int Captured(int[] values,int seed,int extra)=>values.Aggregate(seed,(total,value)=>total+value+extra);
 public static string Callback(int[] values,string seed,Func<string,int,string> callback)=>values.Aggregate(seed,callback);
 public static int Method(int[] values,int seed)=>values.Aggregate(seed,Subtract);
 public static int Subtract(int total,int value)=>total-value;
 public static int Nested(int[] values,int seed)=>values.Where(value=>value>0).Aggregate(seed,(total,value)=>total+value);
 public static int State(int total,int value)=>total-value;
 public static int NamedState(int[] values,int seed)=>values.Aggregate(seed,State);
 public static int LocalName(int[] values,int seed)=>values.Aggregate(seed,(total,value)=>{int state=1;return total+value;});
 public static int Names(int[] values,int seed)=>values.Aggregate(seed,(state,item)=>state+item);
}
