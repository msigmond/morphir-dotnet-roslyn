using System;namespace Example.Completion;public static class Rules{
 public static Tuple<int,bool> Make(int code,bool flag)=>Tuple.Create(code,flag);
 public static Tuple<int,bool> Construct(int code,bool flag)=>new Tuple<int,bool>(code,flag);
 public static int Code(Tuple<int,bool> pair)=>pair.Item1;
 public static bool Flag(Tuple<int,bool> pair)=>pair.Item2;
 public static Tuple<int,bool,string> Triple(int code,bool flag,string text)=>Tuple.Create(code,flag,text);
}
