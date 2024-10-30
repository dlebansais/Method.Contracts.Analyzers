namespace Contracts.Analyzers.Test;

extern alias Analyzers;

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = CSharpAnalyzerVerifier<Analyzers.Contracts.Analyzers.MCA1016OnlyUseContractUnusedWithParameters>;

[TestClass]
public partial class MCA1016UnitTests
{
    [TestMethod]
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

    [TestMethod]
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
