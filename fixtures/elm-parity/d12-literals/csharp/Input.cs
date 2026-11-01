namespace Example.Parity;
public enum State { None, Active }
public static class Rules
{
 public static int NegativeResult(int value)=>value switch{0=>-1,_=>0};
 public static int Partial(int value)=>value switch{0=>1};
 public static double NegativeFloat()=>-0.1;
 public static decimal NegativeDecimal()=>-0.1m;
 public static int GroupedNegative()=>-(1);
 public static int Negate(int value)=>-value;
 public static int Boundary(long value)=>value switch{9007199254740991L=>1,_=>0};
 public static int? Lift(int value)=>value switch{0=>1,_=>2};
 public static int NullableText(string? value)=>value switch{null=>0,"x"=>1,_=>2};
 public static State EnumResult(State value)=>value switch{State.None=>State.Active,_=>value};
 public static int Integer(int value)=>value switch{0=>1,_=>2};
 public static int Float(float value)=>value switch{0.1f=>1,_=>0};
 public static int Boolean(bool value)=>value switch{true=>1,false=>0};
 public static int Character(char value)=>value switch{'x'=>1,_=>0};
 public static int Text(string value)=>value switch{"x"=>1,_=>0};
 public static int Enum(State value)=>value switch{State.None=>1,State.Active=>2,_=>0};
 public static int Nullable(int? value)=>value switch{null=>0,0=>1,_=>2};
 public static bool IsMissing(int? value)=>value is null;
 public static bool IsPresent(int? value)=>value is not null;
}
