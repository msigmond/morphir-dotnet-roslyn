namespace Example.Completion;public static class Rules{
 public static int First((int Code,bool Flag) pair)=>pair.Code;
 public static bool Second((int Code,bool Flag) pair)=>pair.Item2;
 public static int Deconstruct((int Code,bool Flag) pair){var (code,flag)=pair;return code;}
 public static int Discard((int Code,bool Flag) pair){var (code,_)=pair;return code;}
  public static int Triple((int,bool,int) pair){var(a,_,b)=pair;return a+b;}
 public static int Nested((int,(bool,int)) pair){var(a,(_,b))=pair;return a+b;}
}
