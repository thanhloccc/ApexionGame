using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ApexionGame.SourceGen.Tests;

/// <summary>
/// Runs the stats generators and analyzers over a real compilation.
/// </summary>
/// <remarks>
/// The point of this helper is that it does not stop at "the generator did not throw": it feeds the
/// emitted sources back into the compilation and asserts the result actually compiles against the
/// real ApexionGame.Entities.Stats assembly. That is what catches renamed members, dropped `ref`,
/// and stale ECS type names — none of which a smoke test would notice.
/// </remarks>
internal static class GeneratorTestHelper
{
    /// <summary>
    /// Compiles <paramref name="source"/>, runs every generator, and returns the run result plus
    /// the errors of the post-generation compilation.
    /// </summary>
    public static GeneratorRun Run(string source, params IIncrementalGenerator[] generators)
    {
        var compilation = CreateCompilation(source);

        var driver = CSharpGeneratorDriver
            .Create(generators.Select(g => g.AsSourceGenerator()).ToArray())
            .RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var driverDiagnostics);

        var errors = outputCompilation.GetDiagnostics()
            .Where(static d => d.Severity == DiagnosticSeverity.Error)
            .Where(static d => IgnoredInTestHost(d) == false)
            .ToImmutableArray();

        return new GeneratorRun(
              driver.GetRunResult().GeneratedTrees
            , driverDiagnostics
            , errors
        );
    }

    /// <summary>
    /// Runs the analyzers over <paramref name="source"/> without generating anything.
    /// </summary>
    public static ImmutableArray<Diagnostic> Analyze(string source, params DiagnosticAnalyzer[] analyzers)
    {
        var compilation = CreateCompilation(source);

        return compilation
            .WithAnalyzers(ImmutableArray.Create(analyzers))
            .GetAnalyzerDiagnosticsAsync()
            .GetAwaiter()
            .GetResult();
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        var references = new List<MetadataReference>();
        var trusted = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty;

        foreach (var path in trusted.Split(Path.PathSeparator))
        {
            if (path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                references.Add(MetadataReference.CreateFromFile(path));
            }
        }

        foreach (var path in UnityDllPaths.All)
        {
            references.Add(MetadataReference.CreateFromFile(path));
        }

        return CSharpCompilation.Create(
              "StatsTestAssembly"
            , new[] { CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp10)) }
            , references
            , new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true)
        );
    }

    /// <summary>
    /// The test host loads the modern BCL, which ships types that EncosyTower.Core polyfills for
    /// netstandard2.1. Unity never sees both, so the resulting ambiguity is an artifact of the host
    /// rather than a defect in the generated code.
    /// </summary>
    private static bool IgnoredInTestHost(Diagnostic diagnostic)
        => diagnostic.Id == "CS0433"
        && diagnostic.GetMessage().Contains("StackTraceHiddenAttribute", StringComparison.Ordinal);
}

internal readonly record struct GeneratorRun(
      ImmutableArray<SyntaxTree> GeneratedTrees
    , ImmutableArray<Diagnostic> DriverDiagnostics
    , ImmutableArray<Diagnostic> CompilationErrors
)
{
    public string AllGeneratedText
        => string.Join("\n", GeneratedTrees.Select(static t => t.GetText().ToString()));

    public string DescribeErrors()
        => CompilationErrors.Length == 0
            ? "<none>"
            : string.Join("\n", CompilationErrors.Select(static d => $"  {d.Id} {d.GetMessage()}"));
}
