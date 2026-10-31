using System;using System.Numerics;
namespace Example.Parity;
public record Cell(string? Value);
public static class Rules
{
public static Cell? Identity(Cell? cell)=>cell;
public static string? Read(Cell cell)=>cell.Value;
}
