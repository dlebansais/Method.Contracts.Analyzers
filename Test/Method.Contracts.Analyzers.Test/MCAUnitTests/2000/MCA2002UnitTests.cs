namespace Contracts.Analyzers.Test;

extern alias Analyzers;

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = CSharpAnalyzerVerifier<Analyzers.Contracts.Analyzers.MCA2002InitializeWithAttributeArgumentMustBeValidMethodName>;

[TestClass]
public partial class MCA2002UnitTests
{
    [TestMethod]
    public async Task MethodNameDoesNotExist_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal class Test
{
    [InitializeWith([|""Initialize""|])]
    public Test()
    {
    }
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task MethodNameDoesExist_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal class Test
{
    [InitializeWith(nameof(Initialize))]
    public Test()
    {
    }

    public void Initialize()
    {
    }
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task InvalidArgumentType_NoDiagnostic()
    {
        var DescriptorCS1503 = new DiagnosticDescriptor(
            "CS1503",
            "title",
            "Argument 1: cannot convert from 'int' to 'string'",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        var Expected = new DiagnosticResult(DescriptorCS1503);
        Expected = Expected.WithLocation("/0/Test0.cs", 8, 21);

        await VerifyCS.VerifyAnalyzerAsync(@"
internal class Test
{
    [InitializeWith(0)]
    public Test()
    {
    }
}
", Expected).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task RecordMethodNameDoesNotExist_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal record Test
{
    [InitializeWith([|""Initialize""|])]
    public Test()
    {
    }
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task RecordMethodNameDoesExist_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal record Test
{
    [InitializeWith(nameof(Initialize))]
    public Test()
    {
    }

    public void Initialize()
    {
    }
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task StructMethodNameDoesNotExist_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal struct Test
{
    [InitializeWith(""Initialize"")]
    public Test()
    {
    }
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TooManyOverloads_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal class Test
{
    [InitializeWith([|""Initialize""|])]
    public Test()
    {
    }

    public void Initialize(string value)
    {
    }

    public void Initialize(int value)
    {
    }
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task NoConstructorMethodNameDoesNotExist_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
[InitializeWith([|""Initialize""|])]
internal class Test
{
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task NoConstructorMethodNameDoesExist_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
[InitializeWith(nameof(Initialize))]
internal class Test
{
    public void Initialize()
    {
    }
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task NoConstructorInvalidArgumentType_NoDiagnostic()
    {
        var DescriptorCS1503 = new DiagnosticDescriptor(
            "CS1503",
            "title",
            "Argument 1: cannot convert from 'int' to 'string'",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        var Expected = new DiagnosticResult(DescriptorCS1503);
        Expected = Expected.WithLocation("/0/Test0.cs", 6, 17);

        await VerifyCS.VerifyAnalyzerAsync(@"
[InitializeWith(0)]
internal class Test
{
}
", Expected).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task NoConstructorRecordMethodNameDoesNotExist_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
[InitializeWith([|""Initialize""|])]
internal record Test
{
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task NoConstructorRecordMethodNameDoesExist_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
[InitializeWith(nameof(Initialize))]
internal record Test
{
    public void Initialize()
    {
    }
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task NoConstructorStructMethodNameDoesNotExist_NoDiagnostic()
    {
        var DescriptorCS0592 = new DiagnosticDescriptor(
            "CS0592",
            "title",
            "Attribute 'InitializeWith' is not valid on this declaration type. It is only valid on 'class, constructor' declarations.",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        var Expected = new DiagnosticResult(DescriptorCS0592);
        Expected = Expected.WithLocation("/0/Test0.cs", 6, 2);

        await VerifyCS.VerifyAnalyzerAsync(@"
[InitializeWith(""Initialize"")]
internal struct Test
{
}
", Expected).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task NoConstructorTooManyOverloads_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
[InitializeWith([|""Initialize""|])]
internal class Test
{
    public void Initialize(string value)
    {
    }

    public void Initialize(int value)
    {
    }
}
").ConfigureAwait(false);
    }
}
