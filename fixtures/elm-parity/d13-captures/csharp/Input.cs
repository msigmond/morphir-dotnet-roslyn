using System;
namespace Example.Parity;
public static class Rules
{
 public static Func<int,bool> AtLeast(int minimum)=>age=>age>=minimum;
 public static Func<int,Func<int,int>> Nested(int offset)=>left=>right=>left+right+offset;
 public static int Local(int value,int offset){Func<int,int> add=item=>item+offset;return add(value);}
 public static Func<int,int> Block(int offset)=>value=>{int result=value+offset;return result*2;};
 public static int Pass(int value)=>Use(item=>item+1,value);
 public static int Use(Func<int,int> callback,int value)=>callback(value);
 public static Func<int,int> LocalFunction(int offset){int Add(int item)=>item+offset;return value=>Add(value);}
 public static int Natural(int value){var add=(int item)=>item+1;return add(value);}
 public static Func<int,int> Choice(bool flag)=>flag ? value=>value+1 : value=>value+2;
 public static Func<int,int> Branch(int minimum)=>value=>{if(value>minimum)return value;return minimum;};
}
