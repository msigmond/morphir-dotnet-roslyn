namespace Example.Completion;
public class Primary(int value){public int Read()=>value;}
public class Cell{public int Value;public Cell(int value){Value=value;}public int Double=>Value*2;public int Read()=>Double;}
public readonly struct Amount{public readonly int Value;public Amount(int value){Value=value;}public static Amount operator +(Amount a,Amount b)=>new Amount(a.Value+b.Value);}
public static class Rules{public static Primary Make(int x)=>new Primary(x);public static Cell Build(int x)=>new Cell(x);public static int Get(Cell cell)=>cell.Double;public static Amount Add(Amount a,Amount b)=>a+b;}
