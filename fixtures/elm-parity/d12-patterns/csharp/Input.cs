using System;using System.Collections.Generic;
namespace Example.Parity;
public static class Rules
{
 public static int NamedPair((int,int) value)=>value switch{(0,_) pair=>pair.Item2,_=>0};
 public static int NamedList(int[] value)=>value switch{[_,..] all=>all.Length,_=>0};
 public static T Bind<T>(T value)=>value switch{var bound=>bound};
 public static string Pair((int,string) value)=>value switch{(0,var text)=>text,(_,var other)=>other};
 public static int Triple((int,int,int) value)=>value switch{(0,var second,var third)=>second+third,_=>0};
 public static int ReferencePair(Tuple<int,bool> value)=>value switch{(var first,true)=>first,_=>0};
 public static int First(int[] value)=>value switch{[]=>0,[var first,..var rest]=>first};
 public static int Two(List<int> value)=>value switch{[var first,var second]=>first+second,_=>0};
 public static int Rest(int[] value)=>value switch{[_,..var rest]=>rest.Length,_=>0};
 public static int Prefix(int[] value)=>value switch{[0,..]=>1,_=>0};
 public static T[] Copy<T>(T[] value)=>value switch{[..var rest]=>rest};
}
