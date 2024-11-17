namespace Contracts.Analyzers.Test;

extern alias Analyzers;

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using VerifyCS = CSharpAnalyzerVerifier<Analyzers.Contracts.Analyzers.MCA1001VerifiedMethodMustBePrivate>;

[TestFixture]
internal partial class MCA1001UnitTests
{
    [Test]
    public async Task CoverageDirective_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
#define COVERAGE_A25BDFABDDF8402785EB75AD812DA952
" + Prologs.Nullable, @"
internal partial class Program
{
    [Access(""public"", ""static"")]
    protected static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task OldLanguageVersion_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(Prologs.Default, @"
internal partial class Program
{
    [Access(""public"", ""static"")]
    protected static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
", Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp6).ConfigureAwait(false);
    }
}
