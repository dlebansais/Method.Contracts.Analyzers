namespace Contracts.Analyzers.Test;

extern alias Analyzers;

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = CSharpAnalyzerVerifier<Analyzers.Contracts.Analyzers.MCA1002VerifiedMethodMustBeWithinType>;

[TestClass]
public partial class MCA1002UnitTests
{
    [TestMethod]
    public async Task NoTypeWithin_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    [|[Access(""public"", ""static"")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }|]
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task WithinClass_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
namespace Test;
" + Prologs.Default, @"
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

    [TestMethod]
    public async Task WithinStruct_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
namespace Test;
" + Prologs.Default, @"
internal partial struct Program
{
    [Access(""public"", ""static"")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task WithinRecord_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
namespace Test;
" + Prologs.Default, @"
internal partial record Program
{
    [Access(""public"", ""static"")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task NoTypeWithinNullable_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(Prologs.Nullable, @"
internal partial class Program
{
    [|[Access(""public"", ""static"")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }|]
}
").ConfigureAwait(false);
    }
}
