using System;using System.Numerics;
namespace Example.Parity;
public class ClassData{public int Value{get;set;}}
public struct StructData{public int Value;}
public record RecordData(int Value);
public record struct MutableRecord(int Value);
public readonly struct ReadonlyData{public readonly int Value;}
public static class Rules
{
public static int ClassRead(ClassData data)=>data.Value;
public static int StructRead(StructData data)=>data.Value;
public static int RecordRead(RecordData data)=>data.Value;
public static int MutableRecordRead(MutableRecord data)=>data.Value;
public static int ReadonlyRead(ReadonlyData data)=>data.Value;
}
