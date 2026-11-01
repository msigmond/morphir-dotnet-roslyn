namespace Example.Parity;
public static class Rules
{
 public static int NullableList(int[]? value)=>value switch{null=>0,[]=>1,[var first,..]=>first};
 public static int Nested((int,string)[] value)=>value switch{[(0,var text),..]=>text.Length,_=>0};
 public static int NestedSwitch(int value,int other)=>value switch{0=>other switch{1=>2,_=>3},_=>4};
 public static int? MaybeValue(int? value)=>value switch{null=>null,0=>1,var other=>other};
}
