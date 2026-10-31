using System.Text.Json.Nodes;
using Morphir.IR.Classic;
using Morphir.IR.Json;

namespace Morphir.Tests;

/// <summary>Legacy normalized comparison; supplemental evidence only. Feature acceptance requires complete JSON equality (docs/elm-parity.md).</summary>
internal static class ClassicParity
{
    public static JsonDifference? Compare(string expected, string actual, bool alignPackage)
    {
        var left = JsonNode.Parse(expected)!;
        var right = JsonNode.Parse(actual)!;
        if (alignPackage) Repackage(left, right["distribution"]![1]!.AsArray());
        Normalize(left); Normalize(right);
        return StructuralComparison.Compare(left.ToJsonString(), right.ToJsonString());
    }
    private static void Repackage(JsonNode document, JsonArray target)
    {
        var distribution = document["distribution"]!.AsArray();
        var original = distribution[1]!.DeepClone().AsArray();
        JsonArray Module(JsonArray path)
        {
            var full = original.Concat(path).Select(n => n!.DeepClone()).ToArray();
            if (full.Length < target.Count || !target.Select((n,i) => JsonNode.DeepEquals(n,full[i])).All(x=>x)) throw new InvalidOperationException("The requested package is not a prefix of the source owner.");
            return new JsonArray(full.Skip(target.Count).ToArray());
        }
        void Visit(JsonNode? node)
        {
            if (node is JsonArray array)
            {
                if (array.Count >= 3 && array[0] is JsonValue tag && tag.TryGetValue<string>(out var kind) && kind is "Reference" or "Constructor"
                    && array[2] is JsonArray { Count: 3 } fq && JsonNode.DeepEquals(fq[0],original))
                {
                    fq[1] = Module(fq[1]!.AsArray()); fq[0] = target.DeepClone();
                }
                foreach (var child in array.ToArray()) Visit(child);
            }
            else if (node is JsonObject obj) foreach (var child in obj.ToArray()) Visit(child.Value);
        }
        Visit(distribution[3]);
        foreach (var module in distribution[3]!["modules"]!.AsArray()) module![0] = Module(module[0]!.AsArray());
        distribution[1] = target.DeepClone();
    }
    private static string Key(JsonNode name) => string.Join("/",name.AsArray().Select(n=>n!.GetValue<string>()));
    private static void Normalize(JsonNode document)
    {
        foreach (var module in document["distribution"]![3]!["modules"]!.AsArray())
        {
            foreach (var value in module![1]!["value"]!["values"]!.AsArray())
            {
                int next = 0;
                void Body(JsonNode node, Dictionary<string, JsonNode> scope)
                {
                    var array = node.AsArray(); var kind = array[0]!.GetValue<string>();
                    switch (kind)
                    {
                        case "Variable":
                            if (scope.TryGetValue(Key(array[2]!),out var replacement)) array[2]=replacement.DeepClone();
                            break;
                        case "LetDefinition":
                            var name=Key(array[2]!);var binding=new JsonArray("binding",(next++).ToString());
                            var definition=array[3]!;
                            Body(definition["body"]!,scope);
                            // Morphir Elm retains inference variables in some local signatures even when the initializer is typed.
                            if (definition["inputTypes"]!.AsArray().Count==0 && definition["outputType"]![0]!.GetValue<string>()=="Variable") definition["outputType"]=definition["body"]![1]!.DeepClone();
                            array[2]=binding;
                            Body(array[4]!,new(scope){[name]=binding});
                            break;
                        case "Apply": Body(array[2]!,scope);Body(array[3]!,scope);break;
                        case "Field": Body(array[2]!,scope);break;
                        case "IfThenElse": Body(array[2]!,scope);Body(array[3]!,scope);Body(array[4]!,scope);break;
                        case "Literal": case "Reference": case "Constructor": break;
                        default: throw new InvalidOperationException("No parity normalization is defined for " + kind + ".");
                    }
                }
                Body(value![1]!["value"]!["value"]!["body"]!,[]);
            }
        }
    }
}
