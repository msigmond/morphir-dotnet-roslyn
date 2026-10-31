using System;namespace Example.Completion;
public class Cell{public int Value;public int Add(int x)=>Value+x;public int Read()=>Value;}
public static class Rules{
 public static Func<int,int> Bind(Cell cell)=>cell.Add;
 public static Func<int> Zero(Cell cell)=>cell.Read;
 public static int Call(Cell cell,int value)=>cell.Add(value);
 public static int Apply(Cell cell,int value){var callback=cell.Add;return callback(value);}
}
