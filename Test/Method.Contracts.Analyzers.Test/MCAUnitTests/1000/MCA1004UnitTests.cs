namespace Contracts.Analyzers.Test;

extern alias Analyzers;

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS = CSharpAnalyzerVerifier<Analyzers.Contracts.Analyzers.MCA1004AttributeIsMissingArgument>;

[TestFixture]
internal partial class MCA1004UnitTests
{
    [Test]
    public async Task NoArgumentAccess_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    [[|Access|]]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task OneArgument_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(Prologs.Nullable, @"
internal partial class Program
{
    [Access(""public"")]
    private void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task OneArgumentNullable_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(Prologs.Nullable, @"
internal partial class Program
{
    [Access(""public"")]
    private void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task EmptyArgument_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    [[|Access()|]]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task UnsupportedAttribute_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    [Obsolete]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task NoArgumentRequireNotNull_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    [[|RequireNotNull|]]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task NoArgumentRequire_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    [[|Require|]]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task NoArgumentEnsure_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    [[|Ensure|]]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task NoArgumentOtherAccess_NoDiagnostic()
    {
        DiagnosticDescriptor DescriptorCS7036 = new(
            "CS7036",
            "title",
            "There is no argument given that corresponds to the required parameter 'value' of 'AccessAttribute.AccessAttribute(string)'",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        DiagnosticResult Expected = new(DescriptorCS7036);
        Expected = Expected.WithLocation("/0/Test0.cs", 15, 6);

        await VerifyCS.VerifyAnalyzerAsync(Prologs.NoContract, @"
namespace Test;

internal class AccessAttribute : Attribute
{
    public AccessAttribute(string value) { Value = value; }
    public string Value { get; set; }
}

internal partial class Program
{
    [Access]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
", Expected).ConfigureAwait(false);
    }
}
