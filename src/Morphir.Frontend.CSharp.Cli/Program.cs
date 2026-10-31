using Morphir.Frontend.CSharp;
using Morphir.Frontend.CSharp.ProjectSystem;
using Morphir.IR.Json;

try
{
    string? package = null, output = null, project = null, framework = null;
    string configuration = "Debug";
    var format = FrontendOutputFormat.Classic;
    bool formatSpecified = false;
    var selectors = new List<string>(); var sources = new List<string>();
    for (int i = 0; i < args.Length; i++)
    {
        var arg = args[i];
        if (arg == "--help")
        {
            Console.WriteLine("Usage: morphir-csharp --package Example --namespace Example.Business [--namespace ...] [--format classic|source] --output morphir-ir.json (--source File.cs [--source ...] | --project App.csproj --framework net10.0 [--configuration Debug])\nDefault output: standard Morphir Classic v3. Use --format source for the C# syntax extension.");
            return 0;
        }
        if (++i >= args.Length) throw new ArgumentException($"Missing value for {arg}.");
        switch (arg)
        {
            case "--package" when package is null: package = args[i]; break;
            case "--output" when output is null: output = args[i]; break;
            case "--project" when project is null: project = args[i]; break;
            case "--framework" when framework is null: framework = args[i]; break;
            case "--configuration": configuration = args[i]; break;
            case "--format" when !formatSpecified:
                format = args[i] switch { "classic" => FrontendOutputFormat.Classic, "source" => FrontendOutputFormat.Source, _ => throw new ArgumentException("--format must be classic or source.") };
                formatSpecified = true;
                break;
            case "--namespace": selectors.Add(args[i]); break;
            case "--source": sources.Add(args[i]); break;
            default: throw new ArgumentException($"Unknown or repeated option {arg}.");
        }
    }
    if (string.IsNullOrWhiteSpace(output) || (project is null) == (sources.Count == 0)) throw new ArgumentException("Supply --output and exactly one input mode: --source files or --project. Use --help.");
    FrontendResult result;
    if (project is not null)
    {
        if (framework is null) throw new ArgumentException("Project mode requires an explicit --framework net10.0.");
        var loaded = await ProjectLoader.LoadAsync(project, new(framework, configuration));
        if (!loaded.Success)
        {
            foreach (var diagnostic in loaded.Diagnostics) Console.Error.WriteLine(diagnostic);
            return 1;
        }
        result = Frontend.Convert(loaded.Compilation!, new(package ?? "", selectors, OutputFormat: format), loaded.SourceProjects);
    }
    else
    {
        if (framework is not null) throw new ArgumentException("--framework applies only to --project inputs.");
        var compilation = SourceCompilation.Create(sources.Distinct(StringComparer.Ordinal).Select(p => (p, File.ReadAllText(p))));
        result = Frontend.Convert(compilation, new(package ?? "", selectors, OutputFormat: format));
    }
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
