namespace Contracts.Analyzers.Test;

extern alias Analyzers;

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = CSharpAnalyzerVerifier<Analyzers.Contracts.Analyzers.MCA1001VerifiedMethodMustBePrivate>;

[TestClass]
public partial class MCA1001UnitTests
{
    [TestMethod]
    public async Task Protected_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    [|[Access(""public"", ""static"")]
    protected static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }|]
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Private_NoDiagnostic()
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

    [TestMethod]
    public async Task ProtectedNullable_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(Prologs.Nullable, @"
internal partial class Program
{
    [|[Access(""public"", ""static"")]
    protected static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }|]
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Public_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    [|[Access(""public"", ""static"")]
    public static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }|]
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Internal_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    [|[Access(""public"", ""static"")]
    internal static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }|]
}
").ConfigureAwait(false);
    }
}
