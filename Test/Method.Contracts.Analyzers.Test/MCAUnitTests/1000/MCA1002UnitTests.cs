﻿namespace Contracts.Analyzers.Test;

extern alias Analyzers;

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS = CSharpAnalyzerVerifier<Analyzers.Contracts.Analyzers.MCA1002VerifiedMethodMustBeWithinType>;

[TestFixture]
internal partial class MCA1002UnitTests
{
    [Test]
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

    [Test]
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

    [Test]
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

    [Test]
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

    [Test]
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

    [Test]
    public async Task NoWithin_Diagnostic()
    {
        DiagnosticDescriptor DescriptorCS0116 = new(
            "CS0116",
            "title",
            "A namespace cannot directly contain members such as fields, methods or statements",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        DiagnosticDescriptor DescriptorMCA1002 = new(
            Analyzers.Contracts.Analyzers.MCA1002VerifiedMethodMustBeWithinType.DiagnosticId,
            "title",
            "'FooVerified' must be within type",
            "description",
            DiagnosticSeverity.Warning,
            true
            );

        DiagnosticResult Expected1 = new(DescriptorCS0116);
        Expected1 = Expected1.WithLocation("/0/Test0.cs", 8, 13);

        DiagnosticResult Expected2 = new(DescriptorMCA1002);
        Expected2 = Expected2.WithLocation("/0/Test0.cs", 12, 1);

        DiagnosticResult Expected3 = new(DescriptorCS0116);
        Expected3 = Expected3.WithLocation("/0/Test0.cs", 13, 6);

        await VerifyCS.VerifyAnalyzerAsync(@"
namespace Contracts.TestSuite;

static void Main()
{
}

[Access(""public"")]
void FooVerified()
{
}
", Expected1, Expected2, Expected3).ConfigureAwait(false);
    }
}
