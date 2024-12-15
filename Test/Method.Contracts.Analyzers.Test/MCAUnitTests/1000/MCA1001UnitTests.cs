namespace Contracts.Analyzers.Test;

extern alias Analyzers;

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS = CSharpAnalyzerVerifier<Analyzers.Contracts.Analyzers.MCA1001VerifiedMethodMustBePrivate>;

[TestFixture]
internal partial class MCA1001UnitTests
{
    [Test]
    public async Task Protected_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    [|[Access(""public"", ""static"")]
    protected static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }|]
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task Private_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(Prologs.Nullable, @"
internal partial class Program
{
    [Access(""public"", ""static"")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task ProtectedNullable_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(Prologs.Nullable, @"
internal partial class Program
{
    [|[Access(""public"", ""static"")]
    protected static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }|]
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task Public_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    [|[Access(""public"", ""static"")]
    public static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }|]
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task Internal_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    [|[Access(""public"", ""static"")]
    internal static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }|]
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task DifferentAttribute_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(Prologs.NoContract, @"
namespace Test;

internal class AccessAttribute : Attribute
{
    public AccessAttribute(string value) { Value = value; }
    public string Value { get; set; }
}

internal partial class Program
{
    [Access(""public"")]
    protected void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task UndefinedAttribute_NoDiagnostic()
    {
        DiagnosticDescriptor DescriptorCS0116 = new(
            "CS0246",
            "title",
            "The type or namespace name 'Access' could not be found (are you missing a using directive or an assembly reference?)",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        DiagnosticResult Expected1 = new(DescriptorCS0116);
        Expected1 = Expected1.WithLocation("/0/Test0.cs", Prologs.NoContractLineCount + 5, 6);

        DiagnosticDescriptor DescriptorCS0246 = new(
            "CS0246",
            "title",
            "The type or namespace name 'AccessAttribute' could not be found (are you missing a using directive or an assembly reference?)",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        DiagnosticResult Expected2 = new(DescriptorCS0246);
        Expected2 = Expected2.WithLocation("/0/Test0.cs", Prologs.NoContractLineCount + 5, 6);

        await VerifyCS.VerifyAnalyzerAsync(Prologs.NoContract, @"
internal partial class Program
{
    [Access(""public"")]
    protected void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
", Expected1, Expected2).ConfigureAwait(false);
    }
}
