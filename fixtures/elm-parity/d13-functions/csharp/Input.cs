using System;
namespace Example.Parity;
public static class Rules
{
 public static Func<int,int> Increment()=>value=>value+1;
 public static Func<int,int,int> Subtract()=> (left,right)=>left-right;
 public static Func<int,int> Explicit()=> (int value)=>value*2;
 public static Func<int,int> Anonymous()=>delegate(int value){return value+2;};
 public static Func<int,int> Construct()=>new Func<int,int>(value=>value+3);
 public static Func<int,int> Copy(Func<int,int> callback)=>new Func<int,int>(callback);
 public static Func<int,int> Parenthesized()=>new Func<int,int>(((int value)=>value+1));
 public static Func<int,int> Inferred()=>new(value=>value+1);
 public static int Apply(Func<int,int> callback,int value)=>callback(value);
 public static int Invoke(Func<int,int> callback,int value)=>callback.Invoke(value);
 public static Func<T,T> Identity<T>()=>value=>value;
 public static Func<int,int?> Nullable()=>value=>value;
 public static Action<int> Empty()=>value=>{};
 public static Func<int> Zero(int value)=>()=>value;
 public static void Run(Action<int> callback,int value)=>callback(value);
 public static Action<int> Forward(Action<int> callback)=>value=>callback(value);
 public static Action EmptyZero()=>()=>{};
 public static Func<int,int> Static()=>static value=>value+1;
}
