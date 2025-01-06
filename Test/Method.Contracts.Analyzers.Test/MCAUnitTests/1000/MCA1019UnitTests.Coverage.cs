namespace Contracts.Analyzers.Test;

extern alias Analyzers;

using System.Threading.Tasks;
using NUnit.Framework;
using VerifyCS = CSharpAnalyzerVerifier<Analyzers.Contracts.Analyzers.MCA1019VerifiedPropertyIsMissingSuffix>;

[TestFixture]
internal partial class MCA1019UnitTests
{
    [Test]
    public async Task CoverageDirective_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
#define COVERAGE_A25BDFABDDF8402785EB75AD812DA952
" + Prologs.Default, @"
internal partial class Program
{
    [Access(""public"", ""static"")]
    private static int HelloFromFoo
    {
        get { return 0; }
    }
}
", Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp7).ConfigureAwait(false);
    }

    [Test]
    public async Task OldLanguageVersion_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(Prologs.Default, @"
internal partial class Program
{
    [Access(""public"", ""static"")]
    private static int HelloFromFoo
    {
        get { return 0; }
    }
}
", Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp6).ConfigureAwait(false);
    }
}
