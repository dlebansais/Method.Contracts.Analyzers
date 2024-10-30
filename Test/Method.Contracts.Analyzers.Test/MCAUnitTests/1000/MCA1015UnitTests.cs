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

    [TestMethod]
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

    [TestMethod]
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

    [TestMethod]
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

    [TestMethod]
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

    [TestMethod]
    public async Task UnknownClassSymbolInvocation_Diagnostic()
    {
        var DescriptorCS0103 = new DiagnosticDescriptor(
            "CS0103",
            "title",
            "The name 'Bar' does not exist in the current context",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        var Expected = new DiagnosticResult(DescriptorCS0103);
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

    [TestMethod]
    public async Task UnknownMethodSymbolInvocation_Diagnostic()
    {
        var DescriptorCS0117 = new DiagnosticDescriptor(
            "CS0117",
            "title",
            "'Contract' does not contain a definition for 'Bar'",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        var Expected = new DiagnosticResult(DescriptorCS0117);
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

    [TestMethod]
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

    [TestMethod]
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

    [TestMethod]
    public async Task InvalidInvocation1_NoDiagnostic()
    {
        var DescriptorCS7036 = new DiagnosticDescriptor(
            "CS7036",
            "title",
            "There is no argument given that corresponds to the required parameter 'result' of 'Contract.Unused<T>(out T)'",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        var Expected = new DiagnosticResult(DescriptorCS7036);
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

    [TestMethod]
    public async Task InvalidInvocation2_NoDiagnostic()
    {
        var DescriptorCS1620 = new DiagnosticDescriptor(
            "CS1620",
            "title",
            "Argument 1 must be passed with the 'out' keyword",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        var Expected = new DiagnosticResult(DescriptorCS1620);
        Expected = Expected.WithLocation("/0/Test0.cs", 18, 29);

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

    [TestMethod]
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

    [TestMethod]
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

    [TestMethod]
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

    [TestMethod]
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

    [TestMethod]
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

    [TestMethod]
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

    [TestMethod]
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

    [TestMethod]
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

    [TestMethod]
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
}
