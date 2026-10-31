namespace Example.Completion;public static class Rules{
 public static decimal? Divide(decimal amount,decimal count)=>amount/count;
 public static decimal? Block(decimal amount,decimal count){return amount/count;}
 public static decimal? Checked(decimal amount,decimal count)=>checked(amount/count);
 public static decimal? Cast(decimal amount,decimal count)=>(decimal?)(amount/count);
 public static decimal? StaticCall(decimal a,decimal b)=>decimal.Divide(a,b);
}
