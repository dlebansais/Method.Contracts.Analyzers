namespace Contracts.Analyzers.Test;

extern alias Analyzers;

using System.Threading.Tasks;
using NUnit.Framework;
using VerifyCS = CSharpAnalyzerVerifier<Analyzers.Contracts.Analyzers.MCA1011RequireAttributeArgumentMustBeValid>;

[TestFixture]
internal partial class MCA1011UnitTests
{
    [Test]
    public async Task InvalidExpression_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    [Require([|""""|])]
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
    [Access(""public"", ""static"")]
    [Require(""text.Length > 0"")]
    private static void HelloFromVerified(string text, out string textPlus)
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
    [Access(""public"", ""static"")]
    [Require(""text.Length > 0"")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task MultipleArguments_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(Prologs.Nullable, @"
internal partial class Program
{
    [Access(""public"", ""static"")]
    [Require(""text1.Length > 0"", ""text2.Length > 0"")]
    private static void HelloFromVerified(string text1, string text2, out string textPlus)
    {
        textPlus = text1 + text2 + ""!"";
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task MultipleArgumentsOneBad_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(Prologs.Nullable, @"
internal partial class Program
{
    [Access(""public"", ""static"")]
    [Require(""text1.Length > 0"", [|""""|], ""text3.Length > 0"")]
    private static void HelloFromVerified(string text1, string text2, string text3, out string textPlus)
    {
        textPlus = text1 + text2 + text3 + ""!"";
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task NameofArgument_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(Prologs.Nullable, @"
internal partial class Program
{
    [Access(""public"", ""static"")]
    [Require([|nameof(text)|])]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task ArgumentWithDebugOnly_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(Prologs.Nullable, @"
internal partial class Program
{
    [Access(""public"", ""static"")]
    [Require(""text.Length > 0"", DebugOnly = false)]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task BadArgumentWithDebugOnly_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(Prologs.Nullable, @"
internal partial class Program
{
    [Access(""public"", ""static"")]
    [Require([|""""|], DebugOnly = false)]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task OtherAttribute_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(Prologs.NoContract, @"
namespace Test;

internal class RequireAttribute : Attribute
{
    public RequireAttribute(string value) { Value = value; }
    public string Value { get; set; }
}

internal partial class Program
{
    [Require("""")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
").ConfigureAwait(false);
    }
}
