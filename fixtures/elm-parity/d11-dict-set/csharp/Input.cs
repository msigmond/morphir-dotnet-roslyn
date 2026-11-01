using System;using System.Collections.Generic;using System.Collections.Immutable;
namespace Example.Parity;
public static class Rules
{
 public static Dictionary<int,string> Dictionary(int key,string value)=>new Dictionary<int,string>{{key,value}};
 public static Dictionary<int,string> IndexInitializer(int key,string value)=>new Dictionary<int,string>{[key]=value};
 public static Dictionary<int,string> EmptyDictionary()=>new Dictionary<int,string>();
 public static string? Read(IReadOnlyDictionary<int,string> items,int key)=>items[key];
 public static bool ContainsKey(Dictionary<int,string> items,int key)=>items.ContainsKey(key);
 public static int Count(Dictionary<int,string> items)=>items.Count;
 public static IEnumerable<int> Keys(IReadOnlyDictionary<int,string> items)=>items.Keys;
 public static IEnumerable<string> Values(IReadOnlyDictionary<int,string> items)=>items.Values;
 public static HashSet<int> Set(int value)=>new HashSet<int>{value,2};
 public static HashSet<int> SetFromList(List<int> items)=>new HashSet<int>(items);
 public static ImmutableHashSet<int> Immutable(int value)=>ImmutableHashSet.Create(value,2);
 public static bool Contains(HashSet<int> items,int value)=>items.Contains(value);
 public static int SetCount(IReadOnlySet<int> items)=>items.Count;
 public static bool IsEmpty(ImmutableHashSet<int> items)=>items.IsEmpty;
 public static List<int> SetSpread(HashSet<int> items)=>[..items];
 public static ImmutableDictionary<int,string> EmptyImmutable()=>ImmutableDictionary<int,string>.Empty;
 public static V? ReadGeneric<K,V>(IReadOnlyDictionary<K,V> items,K key)=>items[key];
 public static bool ContainsGeneric<T>(HashSet<T> items,T value)=>items.Contains(value);
 public static Dictionary<K,V> Identity<K,V>(Dictionary<K,V> items)=>items;
}
