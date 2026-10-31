namespace Example.Completion;public static class Rules{
 public static int Remainder(int value,int divisor)=>value%divisor;
 public static int Code(char value)=>(int)value;
 public static char Character(int value)=>(char)value;
 public static bool TextEqual(string a,string b)=>a==b;
 public static bool TextDifferent(string a,string b)=>a!=b;
 }
