namespace Contracts.Analyzers.Test;

extern alias Analyzers;

using System.Threading.Tasks;
using NUnit.Framework;
using VerifyCS = CSharpAnalyzerVerifier<Analyzers.Contracts.Analyzers.MCA1019VerifiedPropertyIsMissingSuffix>;

[TestFixture]
internal partial class MCA1019UnitTests
{
    [Test]
    public async Task FooPrefix_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    [Access(""public"", ""static"")]
    private static int [|HelloFromFoo|]
    {
        get { return 0; }
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task VerifiedPrefix_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(Prologs.Nullable, @"
internal partial class Program
{
    [Access(""public"", ""static"")]
    private static int HelloFromVerified
    {
        get { return 0; }
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task FooPrefixNullable_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(Prologs.Nullable, @"
internal partial class Program
{
    [Access(""public"", ""static"")]
    private static int [|HelloFromFoo|]
    {
        get { return 0; }
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task OnlyPrefix_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    [Access(""public"", ""static"")]
    private static int [|Verified|]
    {
        get { return 0; }
    }
}
").ConfigureAwait(false);
    }
}
