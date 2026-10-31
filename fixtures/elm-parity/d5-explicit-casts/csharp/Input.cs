namespace Example.Completion;public static class Rules{
 public static long Widen(int value)=>(long)value;
 public static int Narrow(long value)=>(int)value;
 public static double Float(int value)=>(double)value;
 public static int Integer(double value)=>(int)value;
 public static decimal Decimal(int value)=>(decimal)value;
 public static decimal FloatDecimal(double value)=>(decimal)value;
 public static double DecimalFloat(decimal value)=>(double)value;
 }
