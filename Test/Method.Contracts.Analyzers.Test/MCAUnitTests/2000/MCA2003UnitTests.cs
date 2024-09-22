namespace Contracts.Analyzers.Test;

extern alias Analyzers;

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = CSharpAnalyzerVerifier<Analyzers.Contracts.Analyzers.MCA2003InitializeWithAttributeNotAllowedInClassWithExplicitConstructors>;

[TestClass]
public partial class MCA2003UnitTests
{
    [TestMethod]
    public async Task ClassHasExplicitConstructors_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
[[|InitializeWith(""Initialize"")|]]
internal class Test
{
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
    public async Task ClassHasDefaultConstructorOnly_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
[InitializeWith(""Initialize"")]
internal class Test
{
    public void Initialize()
    {
    }
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task RecordHasExplicitConstructors_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
[[|InitializeWith(""Initialize"")|]]
internal record Test
{
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
    public async Task RecordHasDefaultConstructorOnly_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
[InitializeWith(""Initialize"")]
internal record Test
{
    public void Initialize()
    {
    }
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Struct_NoDiagnostic()
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
    public Test()
    {
    }

    public void Initialize()
    {
    }
}
", Expected).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task OtherAttribute_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(Prologs.NoContract, @"
namespace Test;

internal class InitializeWithAttribute : Attribute
{
    public InitializeWithAttribute(string value) { Value = value; }
    public string Value { get; set; }
}

[InitializeWith(""Initialize"")]
internal class Test
{
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
    public async Task AttributeOnConstructor_NoDiagnostic()
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
}
