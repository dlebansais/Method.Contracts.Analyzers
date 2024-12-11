namespace Contracts.Analyzers.Test;

extern alias Analyzers;

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS = CSharpAnalyzerVerifier<Analyzers.Contracts.Analyzers.MCA1021OnlyUseContractMapWithInSiteDictionary>;

[TestFixture]
internal partial class MCA1021UnitTests
{
    [Test]
    public async Task IndirectTable_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
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

        int Bar = Contract.Map(Color.Red, [|Table|]);
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task InSiteDictionary_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
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
        int Bar = Contract.Map(Color.Red, new System.Collections.Generic.Dictionary<Color, int>()
        {
            { Color.Red,   0xFF0000 },
            { Color.Green, 0x00FF00 },
            { Color.Blue,  0x0000FF },
        });
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task NoArgument_NoDiagnostic()
    {
        DiagnosticDescriptor DescriptorCS1501 = new(
            "CS1501",
            "title",
            "No overload for method 'Map' takes 0 arguments",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        DiagnosticResult Expected = new(DescriptorCS1501);
        Expected = Expected.WithLocation("/0/Test0.cs", 11, 28);

        await VerifyCS.VerifyAnalyzerAsync(@"using System.Collections.Generic;

internal partial class Program
{
    private static void Foo()
    {
        int Bar = Contract.Map();
    }
}
", Expected).ConfigureAwait(false);
    }

    [Test]
    public async Task DictionaryWithoutArgument_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
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
        int Bar = Contract.Map(Color.Red, new System.Collections.Generic.Dictionary<Color, int>
        {
            { Color.Red,   0xFF0000 },
            { Color.Green, 0x00FF00 },
            { Color.Blue,  0x0000FF },
        });
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task DictionaryWithArgument_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
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
        System.Collections.Generic.Dictionary<Color, int> InitTable = new()
        {
        };

        int Bar = Contract.Map(Color.Red, new System.Collections.Generic.Dictionary<Color, int>(InitTable)
        {
            { Color.Red,   0xFF0000 },
            { Color.Green, 0x00FF00 },
            { Color.Blue,  0x0000FF },
        });
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task DictionaryWithoutInitializer_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
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
        int Bar = Contract.Map(Color.Red, new System.Collections.Generic.Dictionary<Color, int>());
    }
}
").ConfigureAwait(false);
    }
}
