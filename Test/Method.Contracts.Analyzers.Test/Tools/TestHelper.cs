namespace Contracts.Analyzers.Test;

using System.Reflection;
using Contracts.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

public static class TestHelper
{
    public static GeneratorDriver GetDriver(string source)
    {
        // Parse the provided string into a C# syntax tree.
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

        // Create references for assemblies we require.
        PortableExecutableReference referenceBinder = MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location);
        PortableExecutableReference referenceContracts = MetadataReference.CreateFromFile(typeof(AccessAttribute).GetTypeInfo().Assembly.Location);

        // Create a Roslyn compilation for the syntax tree.
        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "compilation",
            syntaxTrees: new[] { syntaxTree },
            references: new[] { referenceBinder, referenceContracts },
            new CSharpCompilationOptions(OutputKind.ConsoleApplication, reportSuppressedDiagnostics: true, platform: Platform.X64, generalDiagnosticOption: ReportDiagnostic.Error, warningLevel: 4));


        // Create an instance of our EnumGenerator incremental source generator.
        var generator = new ContractGenerator();

        // The GeneratorDriver is used to run our generator against a compilation.
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        // Run the generation pass.
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

        return driver;
    }
}
