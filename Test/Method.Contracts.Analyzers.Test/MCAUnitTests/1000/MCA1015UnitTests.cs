namespace Contracts.Analyzers.Test;

extern alias Analyzers;

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS = CSharpAnalyzerVerifier<Analyzers.Contracts.Analyzers.MCA1015SetParameterAsUnusedBeforeReturn>;

[TestFixture]
internal partial class MCA1015UnitTests
{
    [Test]
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

    [Test]
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

    [Test]
    public async Task FullNamespaceInvocation_NoDiagnostic()
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
            Contracts.Contract.Unused(out text);
            return -1;
        }
    }
}
").ConfigureAwait(false);
    }

    [Test]
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

    [Test]
    public async Task InvocationOfOtherMethod_NoDiagnostic()
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
            text = string.Empty;
            Contract.Assert(n <= 0);
            return -1;
        }
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task InvocationInElseClause_NoDiagnostic()
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
            Contract.Unused(out text);

        return -1;
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task NonInvocationBeforeReturn_Diagnostic()
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
            [|Contract.Unused(out text)|];
            n++;
            return -1;
        }
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task ComplexInvocation_Diagnostic()
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
            [|Contract.Unused(out text)|];
            Bar[0]();
            return -1;
        }
    }

    private static Action[] Bar;
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task UnknownClassSymbolInvocation_Diagnostic()
    {
        DiagnosticDescriptor DescriptorCS0103 = new(
            "CS0103",
            "title",
            "The name 'Bar' does not exist in the current context",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        DiagnosticResult Expected = new(DescriptorCS0103);
        Expected = Expected.WithLocation("/0/Test0.cs", 17, 13);

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
            Bar.Unused(out text);
            Contract.Unused(out text);
            return -1;
        }
    }
}
", Expected).ConfigureAwait(false);
    }

    [Test]
    public async Task UnknownMethodSymbolInvocation_Diagnostic()
    {
        DiagnosticDescriptor DescriptorCS0117 = new(
            "CS0117",
            "title",
            "'Contract' does not contain a definition for 'Bar'",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        DiagnosticResult Expected = new(DescriptorCS0117);
        Expected = Expected.WithLocation("/0/Test0.cs", 17, 22);

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
            Contract.Bar(out text);
            Contract.Unused(out text);
            return -1;
        }
    }
}
", Expected).ConfigureAwait(false);
    }

    [Test]
    public async Task OtherClassSymbol_NoDiagnostic()
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
            string.IsNullOrEmpty(string.Empty);
            Contract.Unused(out text);
            return -1;
        }
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task OtherMethodSymbol_NoDiagnostic()
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
            Contract.Assert(n <= 0);
            Contract.Unused(out text);
            return -1;
        }
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task InvalidInvocation1_NoDiagnostic()
    {
        DiagnosticDescriptor DescriptorCS1501 = new(
            "CS1501",
            "title",
            "No overload for method 'Unused' takes 0 arguments",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        DiagnosticResult Expected = new(DescriptorCS1501);
        Expected = Expected.WithLocation("/0/Test0.cs", 18, 22);

        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    private static int Foo(int n, out string text)
    {
        text = ""Foo"";

        if (n > 0)
        {
            return 0;
        }
        else
        {
            Contract.Unused();
            return -1;
        }
    }
}
", Expected).ConfigureAwait(false);
    }

    [Test]
    public async Task InvalidInvocation2_NoDiagnostic()
    {
        DiagnosticDescriptor DescriptorCS0453 = new(
            "CS0453",
            "title",
            "The type 'string' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'Contract.Unused<T>(out T, T?)'",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        DiagnosticResult Expected = new(DescriptorCS0453);
        Expected = Expected.WithLocation("/0/Test0.cs", 18, 22);

        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    private static int Foo(int n, out string text)
    {
        text = ""Foo"";

        if (n > 0)
        {
            return 0;
        }
        else
        {
            Contract.Unused(text);
            return -1;
        }
    }
}
", Expected).ConfigureAwait(false);
    }

    [Test]
    public async Task InvalidInvocation3_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    private static int Foo(int n, out string text)
    {
        text = ""Foo"";

        if (n > 0)
        {
            return 0;
        }
        else
        {
            Contract.Unused(out string Bar);
            return -1;
        }
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task InvocationWithoutReturnInSwitch_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    private static int Foo(int n, out string text)
    {
        switch (n)
        {
            case 0:
                [|Contract.Unused(out text)|];
                break;
            default:
                text = string.Empty;
                break;
        }

        return 0;
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task NestedInvocationWithoutReturn_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    private static void Foo(int n, out string text)
    {
        if (n > 0)
        {
            text = ""Foo"";
        }
        else
        {
            Contract.Unused(out text);
        }
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task NestedInvocationWithReturn_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    private static void Foo(int n, out string text)
    {
        if (n > 0)
        {
            text = ""Foo"";
        }
        else
        {
            Contract.Unused(out text);
            return;
        }
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task NestedInvocationWithSubsequentStatement1_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    private static void Foo(int n, out string text)
    {
        if (n > 0)
        {
            text = ""Foo"";
        }
        else
        {
            [|Contract.Unused(out text)|];
            n++;
        }
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task NestedInvocationWithSubsequentStatement2_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    private static void Foo(int n, out string text)
    {
        if (n > 0)
        {
            text = ""Foo"";
        }
        else
            [|Contract.Unused(out text)|];

        n++;
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task NestedInvocationWithSubsequentStatement3_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    private static void Foo(int n, out string text)
    {
        try
        {
            text = ""Foo"";
        }
        catch
        {
            [|Contract.Unused(out text)|];
        }

        n++;
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task NestedInvocationWithSubsequentStatement4_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    private static void Foo(int n, out string text)
    {
        try
        {
            text = ""Foo"";
        }
        catch
        {
        }
        finally
        {
            [|Contract.Unused(out text)|];
        }

        n++;
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task NestedInvocationWithSubsequentStatement5_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    private static void Foo(int n, out string text)
    {
        text = ""Foo"";
        lock (lockObject)
            [|Contract.Unused(out text)|];

        n++;
    }

    private static object lockObject;
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task InvocationClassOverride_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    private static int Foo(out string text)
    {
        Contract.Unused(out text);
        return -1;
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task InvocationStructOverride_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    private static int Foo(out int n)
    {
        Contract.Unused(out n);
        return -1;
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task InvocationNullableOverride_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    private static int Foo(out int? p)
    {
        Contract.Unused(out p);
        return -1;
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task InvocationMultipleOverride1_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    private static int Foo(out string text, out int n, out int? p)
    {
        Contract.Unused(out text);
        Contract.Unused(out n);
        Contract.Unused(out p);
        return -1;
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task InvocationMultipleOverride2_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    private static int Foo(out string text, out int n, out int? p)
    {
        Contract.Unused(out n);
        Contract.Unused(out p);
        Contract.Unused(out text);
        return -1;
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task InvocationMultipleOverride3_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    private static int Foo(out string text, out int n, out int? p)
    {
        Contract.Unused(out p);
        Contract.Unused(out text);
        Contract.Unused(out n);
        return -1;
    }
}
").ConfigureAwait(false);
    }
}
