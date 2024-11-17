namespace Contracts.Analyzers.Test;

extern alias Analyzers;

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS = CSharpAnalyzerVerifier<Analyzers.Contracts.Analyzers.MCA1014EnsureAttributeHasTooManyArguments>;

[TestFixture]
internal partial class MCA1014UnitTests
{
    [Test]
    public async Task InvalidParameterNameWithDebugOnly_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    [Ensure(""text.Length > 0"", [|""text.Length > 0""|], DebugOnly = true)]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
").ConfigureAwait(false);
    }

    [Test]
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

    [Test]
    public async Task InvalidLastExpressionWithDebugOnly_Diagnostic()
    {
        DiagnosticDescriptor DescriptorCS1016 = new(
            "CS1016",
            "title",
            "Named attribute argument expected",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        DiagnosticResult Expected = new(DescriptorCS1016);
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

    [Test]
    public async Task OtherAttribute_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(Prologs.NoContract, @"
namespace Test;

internal class EnsureAttribute : Attribute
{
    public EnsureAttribute(string value1, string value2) { Value1 = value1; Value2 = value2; }
    public string Value1 { get; set; }
    public string Value2 { get; set; }
    public bool DebugOnly { get; set; }
}

internal partial class Program
{
    [Ensure(""text.Length > 0"", ""text.Length > 0"", DebugOnly = true)]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
").ConfigureAwait(false);
    }
}
