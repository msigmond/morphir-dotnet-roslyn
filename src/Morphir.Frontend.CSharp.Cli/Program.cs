using Morphir.Frontend.CSharp;
using Morphir.IR.Json;

try
{
    string? package = null, output = null;
    var selectors = new List<string>(); var sources = new List<string>();
    for (int i = 0; i < args.Length; i++)
    {
        var arg = args[i];
        if (arg == "--help")
        {
            Console.WriteLine("Usage: morphir-csharp --package Example --namespace Example.D1 [--namespace ...] --output morphir-ir.json --source Customer.cs [--source ...]");
            return 0;
        }
        if (++i >= args.Length) throw new ArgumentException($"Missing value for {arg}.");
        switch (arg)
        {
            case "--package" when package is null: package = args[i]; break;
            case "--output" when output is null: output = args[i]; break;
            case "--namespace": selectors.Add(args[i]); break;
            case "--source": sources.Add(args[i]); break;
            default: throw new ArgumentException($"Unknown or repeated option {arg}.");
        }
    }
    if (string.IsNullOrWhiteSpace(output) || sources.Count == 0) throw new ArgumentException("Explicit --output and --source inputs are required. Use --help.");
    var compilation = SourceCompilation.Create(sources.Distinct(StringComparer.Ordinal).Select(p => (p, File.ReadAllText(p))));
    var result = Frontend.Convert(compilation, new(package ?? "", selectors));
    foreach (var diagnostic in result.Diagnostics) Console.Error.WriteLine(diagnostic);
    if (!result.Success) return 1;
    Codec.WriteAtomic(output, result.Distribution!);
    return 0;
}
catch (Exception e) when (e is ArgumentException or IOException or UnauthorizedAccessException)
{
    Console.Error.WriteLine("MCS0002: " + e.Message);
    return 2;
}
