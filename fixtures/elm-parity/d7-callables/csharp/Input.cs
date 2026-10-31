using System;namespace Example.Completion;public static class Rules{
 public static int Double(int x)=>x*2;
 public static Func<int,int> Construct()=>new Func<int,int>(Double);
 public static Func<int,int> Reference()=>Double;
 public static int Invoke(Func<int,int> callback,int value)=>callback(value);
 public static int Value()=>1;
 public static Func<int> ZeroReference()=>Value;
 public static int ZeroInvoke(Func<int> callback)=>callback();
 public static int Capture(int a){int Local()=>a;return Local();}
 public static int Parameters(int a){int Local(int x)=>x+a;return Local(2);}
 public static int Recursive(int n){int Loop(int value){if(value<=0)return 0;return Loop(value-1);}return Loop(n);}
 }
