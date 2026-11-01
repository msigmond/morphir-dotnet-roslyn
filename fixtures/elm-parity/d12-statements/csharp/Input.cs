namespace Example.Parity;
public static class Rules
{
 public static int Choose(int value){switch(value){case 0:return 1;default:return 2;}}
 public static int DefaultFirst(int value){switch(value){default:return 0;case 1:return 1;}}
 public static int Local(int value){switch(value){case 0:{int result=value+1;return result;}default:return value;}}
 public static int Bind(int value){switch(value){case var result:return result;}}
 public static int Nullable(int? value){switch(value){case null:return 0;case 1:return 1;default:return 2;}}
}
