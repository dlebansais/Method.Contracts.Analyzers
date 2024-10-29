namespace Contracts.Analyzers.Test;

extern alias Analyzers;

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = CSharpAnalyzerVerifier<Analyzers.Contracts.Analyzers.MCA1015SetParameterAsUnusedBeforeReturn>;

[TestClass]
public partial class MCA1015UnitTests
{
    [TestMethod]
    public async Task InvocationWithoutReturn_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    private static int Foo(int n, out string text)
    {
        [|Contract.Unused(out text)|];

        if (n > 0)
            return 0;
        else
            return -1;
    }
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task InvocationWithReturn_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    private static int Foo(int n, out string text)
    {
        if (n > 0)
        {
            text = ""Foo"";
            return 0;
        }
        else
        {
            Contract.Unused(out text);
            return -1;
        }
    }
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task NestedInvocation_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    private static int Foo(int n)
    {
        return Bar(n) + Bar(n + 1);
    }

    private static int Bar(int n)
    {
        return n;
    }
}
").ConfigureAwait(false);
    }
}
