using Microsoft.CodeAnalysis;

namespace D2.Generator;

[Generator]
public sealed class ThresholdGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var inputs = context.AdditionalTextsProvider.Where(file => System.IO.Path.GetFileName(file.Path) == "threshold.txt").Collect();
        var property = context.AnalyzerConfigOptionsProvider.Select((options, _) => options.GlobalOptions.TryGetValue("build_property.Threshold", out var value) ? value : "");
        context.RegisterSourceOutput(inputs.Combine(property), (production, pair) =>
        {
            var text = pair.Left.SingleOrDefault()?.GetText(production.CancellationToken)?.ToString().Trim();
            if (!int.TryParse(text, out var threshold) || pair.Right != text)
            {
                production.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("D2GEN001", "Missing generator input", "threshold.txt must be an Int32 and match build_property.Threshold", "Inputs", DiagnosticSeverity.Error, true), Location.None));
                return;
            }
            if (threshold == -1) throw new InvalidOperationException("Generator failure fixture");
            production.AddSource("Rules.Generated.cs", $"namespace Example.CurrentSupported; public static partial class CurrentSupportedExample {{ public static int GeneratedThreshold() => {threshold}; }}");
        });
    }
}
