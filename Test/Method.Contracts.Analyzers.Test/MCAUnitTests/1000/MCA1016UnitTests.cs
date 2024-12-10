namespace Contracts.Analyzers.Test;

extern alias Analyzers;

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS = CSharpAnalyzerVerifier<Analyzers.Contracts.Analyzers.MCA1016OnlyUseContractUnusedWithParameters>;

[TestFixture]
internal partial class MCA1016UnitTests
{
    [Test]
    public async Task LocalVariable_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    private static void Foo()
    {
        string Bar;
        [|Contract.Unused(out Bar)|];
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task ParameterVariable_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    private static void Foo(out string text)
    {
        Contract.Unused(out text);
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task NoArgument_NoDiagnostic()
    {
        DiagnosticDescriptor DescriptorCS0177 = new(
            "CS0177",
            "title",
            "The out parameter 'text' must be assigned to before control leaves the current method",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        DiagnosticResult Expected1 = new(DescriptorCS0177);
        Expected1 = Expected1.WithLocation("/0/Test0.cs", 8, 25);

        DiagnosticDescriptor DescriptorCS1501 = new(
            "CS1501",
            "title",
            "No overload for method 'Unused' takes 0 arguments",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        DiagnosticResult Expected2 = new(DescriptorCS1501);
        Expected2 = Expected2.WithLocation("/0/Test0.cs", 10, 18);

        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    private static void Foo(out string text)
    {
        Contract.Unused();
    }
}
", Expected1, Expected2).ConfigureAwait(false);
    }
}
