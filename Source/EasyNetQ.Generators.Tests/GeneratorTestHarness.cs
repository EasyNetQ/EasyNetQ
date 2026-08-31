using EasyNetQ.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace EasyNetQ.Generators.Tests;

/// <summary>
///     Runs <see cref="MessagingModuleGenerator" /> against an in-memory compilation that references the real
///     EasyNetQ assembly and exposes the generated output for assertions.
/// </summary>
public static class GeneratorTestHarness
{
    public static readonly MetadataReference[] DefaultReferences = BuildReferences();

    private static MetadataReference[] BuildReferences()
    {
        var trusted = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
        var references = trusted
            .Where(path =>
            {
                var file = Path.GetFileName(path);
                return file.StartsWith("System", StringComparison.Ordinal)
                       || file is "mscorlib.dll" or "netstandard.dll"
                       || file.StartsWith("Microsoft.Extensions", StringComparison.Ordinal);
            })
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToList();
        references.Add(MetadataReference.CreateFromFile(typeof(IBus).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.DependencyInjection.ServiceCollection).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions).Assembly.Location));
        return references.ToArray();
    }

    public static Result Run(string source, string assemblyName = "GeneratorTests", IEnumerable<MetadataReference>? extraReferences = null)
    {
        // One parse-options instance for the source tree AND the driver: interceptors need the feature flag, and
        // mixed feature sets make Roslyn reject the tree combination ("inconsistent syntax tree features")
        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest)
            .WithFeatures([new KeyValuePair<string, string>("InterceptorsNamespaces", "EasyNetQ.Generated")]);
        var compilation = CSharpCompilation.Create(
            assemblyName,
            [CSharpSyntaxTree.ParseText(source, parseOptions)],
            extraReferences is null ? DefaultReferences : [.. DefaultReferences, .. extraReferences],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable)
        );
        var driver = CSharpGeneratorDriver.Create(
            generators: [new MessagingModuleGenerator().AsSourceGenerator()],
            parseOptions: parseOptions
        );
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);

        return new Result(
            outputCompilation,
            generatorDiagnostics,
            outputCompilation.SyntaxTrees.Skip(1).Select(t => t.ToString()).ToArray()
        );
    }

    public sealed record Result(Compilation OutputCompilation, IReadOnlyList<Diagnostic> GeneratorDiagnostics, IReadOnlyList<string> GeneratedSources)
    {
        public string AllGenerated => string.Join("\n---\n", GeneratedSources);

        public IReadOnlyList<Diagnostic> CompilationErrors =>
            OutputCompilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
    }
}
