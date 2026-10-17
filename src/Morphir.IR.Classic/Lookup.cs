namespace Morphir.IR.Classic;

public static class Lookup
{
    /// <summary>Definition lookup is private-inclusive; public exposure is a separate specification operation.</summary>
    public static ValueDefinition<TA, VA>? Value<TA, VA>(Library<TA, VA> library, FQName name)
    {
        if (library.Package.Key != name.Package.Key) return null;
        var module = library.Definition.Modules.FirstOrDefault(m => m.Key.Key == name.Module.Key);
        return module?.Value.Value.Values.FirstOrDefault(v => v.Key.Key == name.Local.Key)?.Value.Value.Value;
    }
}
