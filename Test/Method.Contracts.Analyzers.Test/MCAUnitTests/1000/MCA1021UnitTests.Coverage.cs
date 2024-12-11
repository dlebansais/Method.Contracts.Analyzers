namespace Contracts.Analyzers.Test;

extern alias Analyzers;

using System.Threading.Tasks;
using NUnit.Framework;
using VerifyCS = CSharpAnalyzerVerifier<Analyzers.Contracts.Analyzers.MCA1021OnlyUseContractMapWithInSiteDictionary>;

[TestFixture]
internal partial class MCA1021UnitTests
{
    [Test]
    public async Task CoverageDirective_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
#define COVERAGE_A25BDFABDDF8402785EB75AD812DA952
" + Prologs.Nullable, @"
internal partial class Program
{
    private enum Color
    {
        Red,
        Green,
        Blue,
    }

    private static void Foo()
    {
        System.Collections.Generic.Dictionary<Color, int> Table = new()
        {
            { Color.Red,   0xFF0000 },
            { Color.Green, 0x00FF00 },
            { Color.Blue,  0x0000FF },
        };

        int Bar = Contract.Map(Color.Red, Table);
    }
}
").ConfigureAwait(false);
    }
}
