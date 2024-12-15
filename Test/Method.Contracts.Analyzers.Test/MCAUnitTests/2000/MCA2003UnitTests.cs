namespace Contracts.Analyzers.Test;

extern alias Analyzers;

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS = CSharpAnalyzerVerifier<Analyzers.Contracts.Analyzers.MCA2003InitializeWithAttributeNotAllowedInClassWithExplicitConstructors>;

[TestFixture]
internal partial class MCA2003UnitTests
{
    [Test]
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

    [Test]
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

    [Test]
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

    [Test]
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

    [Test]
    public async Task Struct_NoDiagnostic()
    {
        DiagnosticDescriptor DescriptorCS0592 = new(
            "CS0592",
            "title",
            "Attribute 'InitializeWith' is not valid on this declaration type. It is only valid on 'class, constructor' declarations.",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        DiagnosticResult Expected = new(DescriptorCS0592);
        Expected = Expected.WithLocation("/0/Test0.cs", Prologs.DefaultLineCount + 3, 2);

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

    [Test]
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

    [Test]
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
