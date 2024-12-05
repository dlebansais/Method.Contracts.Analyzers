namespace Contracts.Analyzers.Test;

extern alias Analyzers;

using System.Threading.Tasks;
using NUnit.Framework;
using VerifyCS = CSharpAnalyzerVerifier<Analyzers.Contracts.Analyzers.MCA2001ObjectMustBeInitialized>;

[TestFixture]
internal partial class MCA2001UnitTests
{
    [Test]
    public async Task CoverageDirective_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
#define COVERAGE_A25BDFABDDF8402785EB75AD812DA952
" + Prologs.Nullable, @"
internal class Test
{
    [InitializeWith(nameof(Initialize))]
    public Test()
    {
    }

    public void Initialize()
    {
    }
}

internal partial class Program
{
    private static void Main()
    {
        var test = new Test();
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task OldLanguageVersion_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(Prologs.Default, @"
internal class Test
{
    [InitializeWith(nameof(Initialize))]
    public Test()
    {
    }

    public void Initialize()
    {
    }
}

internal partial class Program
{
    private static void Main()
    {
        var test = new Test();
    }
}
", Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp6).ConfigureAwait(false);
    }
}
