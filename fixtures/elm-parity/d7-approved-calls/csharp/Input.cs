namespace Example.Completion;public static class Rules{
 public static int F(int x)=>x+1;
 public static long F(long x)=>x+2;
 public static int Pick(int a=2,int b=3)=>a*10+b;
 public static int First(params int[] values)=>values.Length;
 public static int Count(params int[] values)=>values.Length;
 public static int IntCall(int x)=>F(x);
 public static long LongCall(long x)=>F(x);
 public static int Named()=>Pick(b:4,a:5);
 public static int Default()=>Pick();
 public static int Partial()=>Pick(b:7);
 public static int Expanded()=>First(1,2,3);
 public static int Empty()=>Count();
 public static int Explicit()=>First(new int[]{4,5});
}
