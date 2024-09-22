namespace Contracts.Analyzers.Test;

extern alias Analyzers;

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = CSharpAnalyzerVerifier<Analyzers.Contracts.Analyzers.MCA1005AccessAttributeArgumentMustBeValidModifier>;

[TestClass]
public partial class MCA1005UnitTests
{
    [TestMethod]
    public async Task InvalidModifier_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    [Access([|""Foo""|])]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task OneArgument_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(Prologs.Nullable, @"
internal partial class Program
{
    [Access(""public"")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task OneArgumentNullable_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(Prologs.Nullable, @"
internal partial class Program
{
    [Access(""public"")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task InvalidArgument_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    [Access([|nameof(System.String)|])]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task AllValidModifiers_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(Prologs.Nullable, @"
internal partial class Program
{
    [Access(""public"", ""private"", ""protected"", ""internal"", ""file"", ""static"", ""extern"", ""new"", ""virtual"", ""abstract"", ""sealed"", ""override"", ""readonly"", ""unsafe"", ""required"", ""volatile"", ""async"")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task AttributeOnProperty_NoDiagnostic()
    {
        var DescriptorCS0592 = new DiagnosticDescriptor(
            "CS0592",
            "title",
            "Attribute 'Access' is not valid on this declaration type. It is only valid on 'method' declarations.",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        var Expected = new DiagnosticResult(DescriptorCS0592);
        Expected = Expected.WithLocation("/0/Test0.cs", 10, 6);

        await VerifyCS.VerifyAnalyzerAsync(Prologs.Nullable, @"
internal partial class Program
{
    [Access(""public"")]
    public int Foo { get; set; }
}
", Expected).ConfigureAwait(false);
    }
}
