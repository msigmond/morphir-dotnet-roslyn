using System.Text.Json;

namespace Morphir.IR.Json;

public sealed record JsonDifference(string Path, string Expected, string Actual);
public static class StructuralComparison
{
    public static JsonDifference? Compare(string expected, string actual)
    {
        using var e = JsonDocument.Parse(expected);
        using var a = JsonDocument.Parse(actual);
        // Validate both complete trees before comparing, including unequal subtrees.
        Validate(e.RootElement, "$expected"); Validate(a.RootElement, "$actual");
        return Compare(e.RootElement, a.RootElement, "$");
    }
    private static void Validate(JsonElement e, string path)
    {
        if (e.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var p in e.EnumerateObject())
            {
                if (!names.Add(p.Name)) throw new JsonException($"Duplicate property at {path}.{p.Name}");
                Validate(p.Value, path + "." + p.Name);
            }
        }
        else if (e.ValueKind == JsonValueKind.Array)
        {
            int i = 0; foreach (var v in e.EnumerateArray()) Validate(v, $"{path}[{i++}]");
        }
    }
    private static JsonDifference? Compare(JsonElement e, JsonElement a, string p)
    {
        JsonDifference Diff(string expected, string actual) => new(p, expected, actual);
        if (e.ValueKind != a.ValueKind) return Diff(e.GetRawText(), a.GetRawText());
        switch (e.ValueKind)
        {
            case JsonValueKind.Object:
                var ed = e.EnumerateObject().ToDictionary(x => x.Name, x => x.Value, StringComparer.Ordinal);
                var ad = a.EnumerateObject().ToDictionary(x => x.Name, x => x.Value, StringComparer.Ordinal);
                foreach (var k in ed.Keys.Union(ad.Keys).Order(StringComparer.Ordinal))
                {
                    if (!ed.TryGetValue(k, out var ev)) return new(p + "." + k, "<missing>", ad[k].GetRawText());
                    if (!ad.TryGetValue(k, out var av)) return new(p + "." + k, ev.GetRawText(), "<missing>");
                    if (Compare(ev, av, p + "." + k) is { } d) return d;
                }
                return null;
            case JsonValueKind.Array:
                if (e.GetArrayLength() != a.GetArrayLength()) return Diff($"length {e.GetArrayLength()}", $"length {a.GetArrayLength()}");
                for (int i = 0; i < e.GetArrayLength(); i++) if (Compare(e[i], a[i], $"{p}[{i}]") is { } d) return d;
                return null;
            case JsonValueKind.String: return e.GetString() == a.GetString() ? null : Diff(e.GetRawText(), a.GetRawText());
            case JsonValueKind.Number: return JsonElement.DeepEquals(e, a) ? null : Diff(e.GetRawText(), a.GetRawText());
            default: return e.GetRawText() == a.GetRawText() ? null : Diff(e.GetRawText(), a.GetRawText());
        }
    }
}
