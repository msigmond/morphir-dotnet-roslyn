namespace Example.Completion;public record Data(int Value){public int Read(int extra)=>Value+extra;public string Text=>"x";}public static class Rules{
 public static string? Null()=>null;
 public static string? Text()=>"x";
 public static int? Number()=>2;
 public static int? Branch(bool b)=>b?1:null;
 public static int Fallback(int? value)=>value??3;
 public static int? Multiply(int? a,int? b)=>a*b;
 public static bool Present(int? value)=>value.HasValue;
 public static bool Missing(Data? value)=>value==null;
 public static bool NotMissing(Data? value)=>value is not null;
 public static int? Read(Data? data)=>data?.Value;
 public static int Zero()=>default(int);
 public static bool False()=>default;
 public static int? Absent()=>default(int?);
 public static int? Echo(int? value)=>value;
 public static int? Call()=>Echo(2);
 public static int? Length(string? value)=>value?.Length??2;
 public static int? Invoke(Data? data,int extra)=>data?.Read(extra);
 public static int? Mixed(int? a,int b)=>a*b;
 public static int? Negate(int? value)=>-value;
 public static bool? Not(bool? value)=>!value;
 public static double DefaultFloat()=>default;
 public static decimal DefaultDecimal()=>default;
 public static char DefaultChar()=>default;
 public static string? DefaultReference()=>default;
 public static string? TextOf(Data? data)=>data?.Text;
 public static string? LocalReference(Data? data){var value=data?.Text;return value;}
 public static string? LocalBranch(bool flag,string value){var result=flag?null:value;return result;}
 public static (string?,int) LocalTuple(string? text){var pair=(text,1);return pair;}
 public static int? Plain(Data data)=>data?.Value;
 public static bool PlainMissing(Data data)=>data==null;
 public static bool PlainPattern(Data data)=>data is not null;
 public static string PlainFallback(string value)=>value??"x";
 public static string NullFallback(string value)=>null??value;
 public static int? ApplyOptional(System.Func<int,int>? callback,int value)=>callback?.Invoke(value);
}
