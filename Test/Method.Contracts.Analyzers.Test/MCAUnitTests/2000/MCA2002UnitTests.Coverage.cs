namespace Contracts.Analyzers.Test;

extern alias Analyzers;

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using VerifyCS = CSharpAnalyzerVerifier<Analyzers.Contracts.Analyzers.MCA2002InitializeWithAttributeArgumentMustBeValidMethodName>;

[TestFixture]
internal partial class MCA2002UnitTests
{
    [Test]
    public async Task CoverageDirective_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
#define COVERAGE_A25BDFABDDF8402785EB75AD812DA952
" + Prologs.Nullable, @"
internal class Test
{
    [InitializeWith(""Initialize"")]
    public Test()
    {
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
    [InitializeWith(""Initialize"")]
    public Test()
    {
    }
}
", Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp6).ConfigureAwait(false);
    }
}
