namespace Example.Completion;public enum State:byte{None,Active=2,Next,All=Active|Next}public static class Rules{
 public static State Identity(State value)=>value;
 public static State Get()=>State.Active;
 public static int Code(State value)=>(int)value;
 public static State From(int value)=>(State)value;
 public static bool Equal(State a,State b)=>a==b;
}
