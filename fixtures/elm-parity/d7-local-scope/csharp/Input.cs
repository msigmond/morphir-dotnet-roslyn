using System; namespace Example.Completion;
public static class Rules{
 public static void Empty(){}
 public static void Return(){return;}
 public static Action EmptyReference()=>Empty;
 public static void Invoke(Action callback)=>callback();
 public static int Forward(int x){return Local(x);int Local(int y)=>y+1;}
 public static bool Mutual(int n){bool Even(int x)=>x==0||Odd(x-1);bool Odd(int x)=>x!=0&&Even(x-1);return Even(n);}
 public static int Chain(int n){int First(int x)=>Second(x)+1;int Second(int x)=>x*2;return First(n);}
 public static int Independent(int n){int First(int x)=>x+1;int Second(int x)=>x*2;return First(Second(n));}
 public static int Interleaved(int n){int First(int x)=>Second(x)+1;int offset=2;int Second(int x)=>x+offset;return First(n);}
 public static int Keep(params Func<int>[] callbacks)=>1;
 public static int ListRecursion(){int Loop()=>Keep(Loop);return Loop();}
 public static int LocalParams(){int Size(params int[] items)=>items.Length;return Size(1,2);}
}
