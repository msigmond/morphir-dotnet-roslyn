using System;using System.Collections.Generic;using System.Collections.ObjectModel;using System.Collections.Immutable;
namespace Example.Parity;
public static class Rules
{
 public static List<int> UseSourceFactory(int value)=>System.Collections.Immutable.UserFactory.Create(value);
 public static int[] Sized(int first,int second)=>new int[2]{first,second};
 public static int[] Array(int first,int second)=>new int[]{first,second};
 public static int[] Inferred(int value)=>new[]{value};
 public static int[] Local(int value){int[] items={value};return items;}
 public static List<int> Literal(int value)=>new List<int>{value,2};
 public static IReadOnlyList<int> Expression(int value)=>[value,2];
 public static List<int> Spread(int value,int[] items)=>[value,..items,2];
 public static List<T> Generic<T>(T value,List<T> items)=>[value,..items];
 public static string?[] NullableElements(string? value)=>[value];
 public static int Length(int[] items)=>items.Length;
 public static int Count(List<int> items)=>items.Count;
 public static bool Contains(List<int> items,int value)=>items.Contains(value);
 public static List<int> Copy(int[] items)=>new List<int>(items);
 public static ReadOnlyCollection<int> ReadOnly(List<int> items)=>new ReadOnlyCollection<int>(items);
 public static ImmutableList<int> Immutable(int value)=>ImmutableList.Create(value,2);
 public static ImmutableArray<int> ImmutableArrayLiteral(int value)=>[value,2];
 public static ImmutableList<int> Empty()=>ImmutableList<int>.Empty;
 public static int[] EmptyArray()=>System.Array.Empty<int>();
 public static int? Read(int[] items,int index)=>items[index];
 public static int? ReadList(IReadOnlyList<int> items,int index)=>items[index];
 public static int? ReadParams(int index,params int[] items)=>items[index];
 public static int? NullableCount(List<int>? items)=>items?.Count;
 public static bool? NullableContains(List<int>? items,int value)=>items?.Contains(value);
 public static bool ContainsNullable(List<int?> items,int value)=>items.Contains(value);
 public static List<int>? Missing()=>null;
}
