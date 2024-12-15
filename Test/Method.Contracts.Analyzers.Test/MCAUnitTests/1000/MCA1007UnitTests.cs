namespace Contracts.Analyzers.Test;

extern alias Analyzers;

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS = CSharpAnalyzerVerifier<Analyzers.Contracts.Analyzers.MCA1007RequireNotNullAttributeHasTooManyArguments>;

[TestFixture]
internal partial class MCA1007UnitTests
{
    [Test]
    public async Task InvalidParameterNameWithAlias_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    [RequireNotNull(""text"", [|""foo""|], AliasName = ""Text"")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task NoAlias_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    [Access(""public"", ""static"")]
    [RequireNotNull(""text1"", ""foo"")]
    private static void HelloFromVerified(string text1, string text2, out string textPlus)
    {
        textPlus = text1 + text2 + ""!"";
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task InvalidParameterNameWithType_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    [Access(""public"", ""static"")]
    [RequireNotNull(""text"", [|""foo""|], Type = ""string"")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task InvalidParameterNameWithName_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    [Access(""public"", ""static"")]
    [RequireNotNull(""text"", [|""foo""|], Name = ""newText"")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task InvalidParameterNameWithAliasTypeAndName_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    [Access(""public"", ""static"")]
    [RequireNotNull(""s"", [|""foo""|], Type = ""object"", Name = ""text"", AliasName = ""Foo"")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task InvalidLastParameterNameWithAlias_Diagnostic()
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
        Expected = Expected.WithLocation("/0/Test0.cs", Prologs.DefaultLineCount + 6, 49);

        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    [Access(""public"", ""static"")]
    [RequireNotNull(""text"", AliasName = ""Text"", [|""foo""|])]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
", Expected).ConfigureAwait(false);
    }

    [Test]
    public async Task InvalidLastParameterNameWithType_Diagnostic()
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
        Expected = Expected.WithLocation("/0/Test0.cs", Prologs.DefaultLineCount + 6, 46);

        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    [Access(""public"", ""static"")]
    [RequireNotNull(""text"", Type = ""string"", [|""foo""|])]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
", Expected).ConfigureAwait(false);
    }

    [Test]
    public async Task InvalidLastParameterNameWithName_Diagnostic()
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
        Expected = Expected.WithLocation("/0/Test0.cs", Prologs.DefaultLineCount + 6, 47);

        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    [Access(""public"", ""static"")]
    [RequireNotNull(""text"", Name = ""newText"", [|""foo""|])]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
", Expected).ConfigureAwait(false);
    }

    [Test]
    public async Task InvalidLastParameterNameWithAliasTypeAndName_Diagnostic()
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
        Expected = Expected.WithLocation("/0/Test0.cs", Prologs.DefaultLineCount + 6, 77);

        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    [Access(""public"", ""static"")]
    [RequireNotNull(""s"", Type = ""object"", Name = ""text"", AliasName = ""Foo"", [|""foo""|])]
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

internal class RequireNotNullAttribute : Attribute
{
    public RequireNotNullAttribute(string value1, string value2) { Value1 = value1; Value2 = value2; }
    public string Value1 { get; set; }
    public string Value2 { get; set; }
    public string AliasName { get; set; }
}

internal partial class Program
{
    [RequireNotNull(""text"", ""foo"", AliasName = ""Text"")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
").ConfigureAwait(false);
    }
}
