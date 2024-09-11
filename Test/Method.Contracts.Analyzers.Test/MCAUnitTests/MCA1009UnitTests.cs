namespace Contracts.Analyzers.Test;

extern alias Analyzers;

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = CSharpAnalyzerVerifier<Analyzers.Contracts.Analyzers.MCA1009RequireNotNullAttributeUsesInvalidType>;

[TestClass]
public partial class MCA1009UnitTests
{
    [TestMethod]
    public async Task InvalidType_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    [Access(""public"", ""static"")]
    [RequireNotNull(""text"", [|Type = ""@@""|])]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ValidType_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    [Access(""public"", ""static"")]
    [RequireNotNull(""text"", Type = ""foo"")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ParametersOnly_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    [Access(""public"", ""static"")]
    [RequireNotNull(""text"")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task WithAliasOnly_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    [Access(""public"", ""static"")]
    [RequireNotNull(""text"", AliasName = ""Text"")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task WithNameOnly_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    [Access(""public"", ""static"")]
    [RequireNotNull(""text"", Name = ""newText"")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task AliasNotValidStringOrNameof_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    [Access(""public"", ""static"")]
    [RequireNotNull(""text"", Type = """")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task InvalidAliasWithType_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    [Access(""public"", ""static"")]
    [RequireNotNull(""text"", [|Type = ""@@""|], AliasName = ""Text"")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task InvalidTypeWithName_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    [Access(""public"", ""static"")]
    [RequireNotNull(""text"", [|Type = ""@@""|], Name = ""newText"")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task InvalidTypeWithAliadAndName_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    [Access(""public"", ""static"")]
    [RequireNotNull(""text"", [|Type = ""@@""|], AliasName = ""Text"", Name = ""newText"")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
").ConfigureAwait(false);
    }
}
