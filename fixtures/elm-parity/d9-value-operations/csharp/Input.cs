namespace Example.Completion;public record Data(int X,int Y);public static class Rules{
 public static bool Equal(Data a,Data b)=>a==b;
 public static bool Different(Data a,Data b)=>a!=b;
 public static bool TupleEqual((int,bool) a,(int,bool) b)=>a==b;
 public static Data Update(Data data,int value)=>data with{X=value};
 public static Data Multiple(Data data,int x,int y)=>data with{Y=y,X=x};
 }
