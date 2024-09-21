namespace Contracts.Analyzers.Test;

extern alias Analyzers;

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = CSharpAnalyzerVerifier<Analyzers.Contracts.Analyzers.MCA1014EnsureAttributeHasTooManyArguments>;

[TestClass]
public partial class MCA1014UnitTests
{
    [TestMethod]
    public async Task InvalidParameterNameWithDebugOnly_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    [Access(""public"", ""static"")]
    [Ensure(""text.Length > 0"", [|""text.Length > 0""|], DebugOnly = true)]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task NoDebugOnly_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    [Access(""public"", ""static"")]
    [Ensure(""text.Length > 0"", """")]
    private static void HelloFromVerified(string text1, string text2, out string textPlus)
    {
        textPlus = text1 + text2 + ""!"";
    }
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task InvalidLastExpressionWithDebugOnly_Diagnostic()
    {
        var DescriptorCS1016 = new DiagnosticDescriptor(
            "CS1016",
            "title",
            "Named attribute argument expected",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        var Expected = new DiagnosticResult(DescriptorCS1016);
        Expected = Expected.WithLocation("/0/Test0.cs", 9, 51);

        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    [Access(""public"", ""static"")]
    [Ensure(""text.Length > 0"", DebugOnly = false, [|""text.Length > 0""|])]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
", Expected).ConfigureAwait(false);
    }
}
