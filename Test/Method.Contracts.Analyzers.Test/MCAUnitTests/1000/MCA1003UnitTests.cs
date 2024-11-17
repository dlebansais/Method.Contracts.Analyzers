namespace Contracts.Analyzers.Test;

extern alias Analyzers;

using System.Threading.Tasks;
using NUnit.Framework;
using VerifyCS = CSharpAnalyzerVerifier<Analyzers.Contracts.Analyzers.MCA1003VerifiedMethodIsMissingSuffix>;

[TestFixture]
internal partial class MCA1003UnitTests
{
    [Test]
    public async Task FooPrefix_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    [Access(""public"", ""static"")]
    private static void [|HelloFromFoo|](string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task VerifiedPrefix_NoDiagnostic()
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
    public async Task FooPrefixNullable_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(Prologs.Nullable, @"
internal partial class Program
{
    [Access(""public"", ""static"")]
    private static void [|HelloFromFoo|](string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task OnlyPrefix_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    [Access(""public"", ""static"")]
    private static void [|Verified|](string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
").ConfigureAwait(false);
    }
}
