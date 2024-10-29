namespace Contracts.Analyzers.Test;

extern alias Analyzers;

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = CSharpAnalyzerVerifier<Analyzers.Contracts.Analyzers.MCA1015SetParameterAsUnusedBeforeReturn>;

public partial class MCA1015UnitTests
{
    [TestMethod]
    public async Task CoverageDirective_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
#define COVERAGE_A25BDFABDDF8402785EB75AD812DA952
" + Prologs.Nullable, @"
internal partial class Program
{
    private static int Foo(int n, out string text)
    {
        Contract.Unused(out text);

        if (n > 0)
            return 0;
        else
            return -1;
    }
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task OldLanguageVersion_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(Prologs.Default, @"
internal partial class Program
{
    private static int Foo(int n, out string text)
    {
        Contract.Unused(out text);

        if (n > 0)
            return 0;
        else
            return -1;
    }
}
", Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp6).ConfigureAwait(false);
    }
}
