namespace Contracts.Analyzers.Test;

extern alias Analyzers;

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS = CSharpAnalyzerVerifier<Analyzers.Contracts.Analyzers.MCA1020MissingDictionaryEntry>;

[TestFixture]
internal partial class MCA1020UnitTests
{
    [Test]
    public async Task ExtraEnumValue_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    private enum Color
    {
        Red,
        Green,
        Blue,
        White,
    }

    private static void Foo()
    {
        int Bar = [|Contract.Map(Color.Red, new System.Collections.Generic.Dictionary<Color, int>()
        {
            { Color.Red,   0xFF0000 },
            { Color.Green, 0x00FF00 },
            { Color.Blue,  0x0000FF },
        })|];
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task ValidTable_NoDiagnostic()
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
        Expected = Expected.WithLocation("/0/Test0.cs", Prologs.DefaultLineCount + 8, 28);

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
    public async Task IndirectTable_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    private enum Color
    {
        Red,
        Green,
        Blue,
        White,
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

    [Test]
    public async Task DictionaryWithoutArgument_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    private enum Color
    {
        Red,
        Green,
        Blue,
        White,
    }

    private static void Foo()
    {
        int Bar = [|Contract.Map(Color.Red, new System.Collections.Generic.Dictionary<Color, int>
        {
            { Color.Red,   0xFF0000 },
            { Color.Green, 0x00FF00 },
            { Color.Blue,  0x0000FF },
        })|];
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
        White,
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

    [Test]
    public async Task AsyncExtraEnumValue_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    private enum Color
    {
        Red,
        Green,
        Blue,
        White,
    }

    private static async Task Foo()
    {
        int Bar = await [|Contract.MapAsync(Color.Red, new System.Collections.Generic.Dictionary<Color, Func<Task<int>>>()
        {
            { Color.Red,   async () => await Task.Run(() => 0xFF0000) },
            { Color.Green, async () => await Task.Run(() => 0x00FF00) },
            { Color.Blue,  async () => await Task.Run(() => 0x0000FF) },
        })|];
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task AsyncValidTable_NoDiagnostic()
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

    private static async Task Foo()
    {
        int Bar = await Contract.MapAsync(Color.Red, new System.Collections.Generic.Dictionary<Color, Func<Task<int>>>()
        {
            { Color.Red,   async () => await Task.Run(() => 0xFF0000) },
            { Color.Green, async () => await Task.Run(() => 0x00FF00) },
            { Color.Blue,  async () => await Task.Run(() => 0x0000FF) },
        });
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task AsyncNoArgument_NoDiagnostic()
    {
        DiagnosticDescriptor DescriptorCS1501 = new(
            "CS1501",
            "title",
            "No overload for method 'MapAsync' takes 0 arguments",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        DiagnosticResult Expected = new(DescriptorCS1501);
        Expected = Expected.WithLocation("/0/Test0.cs", Prologs.DefaultLineCount + 8, 34);

        await VerifyCS.VerifyAnalyzerAsync(@"using System.Collections.Generic;

internal partial class Program
{
    private static async Task Foo()
    {
        int Bar = await Contract.MapAsync();
    }
}
", Expected).ConfigureAwait(false);
    }

    [Test]
    public async Task AsyncIndirectTable_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    private enum Color
    {
        Red,
        Green,
        Blue,
        White,
    }

    private static async Task Foo()
    {
        System.Collections.Generic.Dictionary<Color, Func<Task<int>>> Table = new()
        {
            { Color.Red,   async () => await Task.Run(() => 0xFF0000) },
            { Color.Green, async () => await Task.Run(() => 0x00FF00) },
            { Color.Blue,  async () => await Task.Run(() => 0x0000FF) },
        };

        int Bar = await Contract.MapAsync(Color.Red, Table);
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task AsyncDictionaryWithoutArgument_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    private enum Color
    {
        Red,
        Green,
        Blue,
        White,
    }

    private static async Task Foo()
    {
        int Bar = await [|Contract.MapAsync(Color.Red, new System.Collections.Generic.Dictionary<Color, Func<Task<int>>>
        {
            { Color.Red,   async () => await Task.Run(() => 0xFF0000) },
            { Color.Green, async () => await Task.Run(() => 0x00FF00) },
            { Color.Blue,  async () => await Task.Run(() => 0x0000FF) },
        })|];
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task AsyncDictionaryWithArgument_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    private enum Color
    {
        Red,
        Green,
        Blue,
        White,
    }

    private static async Task Foo()
    {
        System.Collections.Generic.Dictionary<Color, Func<Task<int>>> InitTable = new()
        {
        };

        int Bar = await Contract.MapAsync(Color.Red, new System.Collections.Generic.Dictionary<Color, Func<Task<int>>>(InitTable)
        {
            { Color.Red,   async () => await Task.Run(() => 0xFF0000) },
            { Color.Green, async () => await Task.Run(() => 0x00FF00) },
            { Color.Blue,  async () => await Task.Run(() => 0x0000FF) },
        });
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task AsyncDictionaryWithoutInitializer_NoDiagnostic()
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

    private static async Task Foo()
    {
        int Bar = await Contract.MapAsync(Color.Red, new System.Collections.Generic.Dictionary<Color, Func<Task<int>>>());
    }
}
").ConfigureAwait(false);
    }
}
