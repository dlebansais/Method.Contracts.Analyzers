namespace Contracts.Analyzers.Test;

extern alias Analyzers;

using System.Threading.Tasks;
using NUnit.Framework;
using VerifyCS = CSharpAnalyzerVerifier<Analyzers.Contracts.Analyzers.MCA1016OnlyUseContractUnusedWithParameters>;

[TestFixture]
internal partial class MCA1016UnitTests
{
    [Test]
    public async Task LocalVariable_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    private static void Foo()
    {
        string Bar;
        [|Contract.Unused(out Bar)|];
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task ParameterVariable_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    private static void Foo(out string text)
    {
        Contract.Unused(out text);
    }
}
").ConfigureAwait(false);
    }
}
