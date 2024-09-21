namespace Contracts.Analyzers.Test;

extern alias Analyzers;

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = CSharpAnalyzerVerifier<Analyzers.Contracts.Analyzers.MCA2001ObjectMustBeInitialized>;

[TestClass]
public partial class MCA2001UnitTests
{
    [TestMethod]
    public async Task InitializerNotCalled1_Diagnostic()
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

internal partial class Program
{
    private static void Main()
    {
        var test = [|new Test()|];
    }
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task InitializerNotCalled2_Diagnostic()
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

internal partial class Program
{
    private static void Main()
    {
        var test1 = [|new Test()|];
        var test2 = [|new Test()|];
    }
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task InitializerNotCalled3_Diagnostic()
    {
        var DescriptorCS8805 = new DiagnosticDescriptor(
            "CS8805",
            "title",
            "Program using top-level statements must be an executable.",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        var Expected = new DiagnosticResult(DescriptorCS8805);
        Expected = Expected.WithLocation("/0/Test0.cs", 6, 1);

        await VerifyCS.VerifyAnalyzerAsync(@"
Test test = [|new Test()|];

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
", Expected).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task InitializerNotCalled4_Diagnostic()
    {
        var DescriptorCS8803 = new DiagnosticDescriptor(
            "CS8803",
            "title",
            "Top-level statements must precede namespace and type declarations.",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        var DescriptorCS8805 = new DiagnosticDescriptor(
            "CS8805",
            "title",
            "Program using top-level statements must be an executable.",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        var Expected1 = new DiagnosticResult(DescriptorCS8803);
        Expected1 = Expected1.WithLocation("/0/Test0.cs", 18, 1);

        var Expected2 = new DiagnosticResult(DescriptorCS8805);
        Expected2 = Expected2.WithLocation("/0/Test0.cs", 18, 1);

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

Test test = [|new Test()|];
", Expected1, Expected2).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task InitializerCalled_NoDiagnostic()
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

internal partial class Program
{
    private static void Main()
    {
        var test = new Test();
        test.Initialize();
    }
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task UnknownClass_NoDiagnostic()
    {
        var DescriptorCS8754 = new DiagnosticDescriptor(
            "CS8754",
            "title",
            "There is no target type for 'new()'",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        var Expected = new DiagnosticResult(DescriptorCS8754);
        Expected = Expected.WithLocation("/0/Test0.cs", 10, 19);

        await VerifyCS.VerifyAnalyzerAsync(@"
internal partial class Program
{
    private static void Main()
    {
        var foo = new();
        foo.Initialize();
    }
}
", Expected).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Struct_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal struct Test
{
    [InitializeWith(nameof(Initialize))]
    public Test()
    {
    }

    public void Initialize()
    {
    }
}

internal partial class Program
{
    private static void Main()
    {
        var test = new Test();
        test.Initialize();
    }
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Interface_NoDiagnostic()
    {
        var DescriptorCS0144 = new DiagnosticDescriptor(
            "CS0144",
            "title",
            "Cannot create an instance of the abstract type or interface 'ITest'",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        var DescriptorCS1061 = new DiagnosticDescriptor(
            "CS1061",
            "title",
            "'ITest' does not contain a definition for 'Initialize' and no accessible extension method 'Initialize' accepting a first argument of type 'ITest' could be found (are you missing a using directive or an assembly reference?)",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        var Expected1 = new DiagnosticResult(DescriptorCS0144);
        Expected1 = Expected1.WithLocation("/0/Test0.cs", 14, 20);

        var Expected2 = new DiagnosticResult(DescriptorCS1061);
        Expected2 = Expected2.WithLocation("/0/Test0.cs", 15, 14);

        await VerifyCS.VerifyAnalyzerAsync(@"
internal interface ITest
{
}

internal partial class Program
{
    private static void Main()
    {
        var test = new ITest();
        test.Initialize();
    }
}
", Expected1, Expected2).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ConstructorNotFound_NoDiagnostic()
    {
        var DescriptorCS1729 = new DiagnosticDescriptor(
            "CS1729",
            "title",
            "'Test' does not contain a constructor that takes 1 arguments",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        var Expected = new DiagnosticResult(DescriptorCS1729);
        Expected = Expected.WithLocation("/0/Test0.cs", 22, 24);

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

internal partial class Program
{
    private static void Main()
    {
        var test = new Test(0);
        test.Initialize();
    }
}
", Expected).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task UnknownAttribute_NoDiagnostic()
    {
        var DescriptorCS0246 = new DiagnosticDescriptor(
            "CS0246",
            "title",
            "The type or namespace name 'Foo' could not be found (are you missing a using directive or an assembly reference?)",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        var DescriptorCS0246Attribute = new DiagnosticDescriptor(
            "CS0246",
            "title",
            "The type or namespace name 'FooAttribute' could not be found (are you missing a using directive or an assembly reference?)",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        var DescriptorCS1061 = new DiagnosticDescriptor(
            "CS1061",
            "title",
            "'Test' does not contain a definition for 'Initialize' and no accessible extension method 'Initialize' accepting a first argument of type 'Test' could be found (are you missing a using directive or an assembly reference?)",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        var Expected1 = new DiagnosticResult(DescriptorCS0246);
        Expected1 = Expected1.WithLocation("/0/Test0.cs", 8, 6);

        var Expected2 = new DiagnosticResult(DescriptorCS0246Attribute);
        Expected2 = Expected2.WithLocation("/0/Test0.cs", 8, 6);

        var Expected3 = new DiagnosticResult(DescriptorCS1061);
        Expected3 = Expected3.WithLocation("/0/Test0.cs", 19, 14);

        await VerifyCS.VerifyAnalyzerAsync(@"
internal class Test
{
    [Foo]
    public Test()
    {
    }
}

internal partial class Program
{
    private static void Main()
    {
        var test = new Test();
        test.Initialize();
    }
}
", Expected1, Expected2, Expected3).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task NotOurAttribute_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
namespace Test;

internal class InitializeWithAttribute : Attribute
{
    public InitializeWithAttribute(string value) { Value = value; }
    public string Value { get; set; }
}

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

internal partial class Program
{
    private static void Main()
    {
        var test = new Test();
        test.Initialize();
    }
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task InitializerDoesNotExist_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal class Test
{
    [InitializeWith(nameof(Foo))]
    public Test()
    {
    }

    public void Initialize()
    {
    }

    public int Foo { get; set; }
}

internal partial class Program
{
    private static void Main()
    {
        var test = new Test();
        test.Initialize();
    }
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task NotEnoughAttributeArgument_NoDiagnostic()
    {
        var DescriptorCS7036 = new DiagnosticDescriptor(
            "CS7036",
            "title",
            "There is no argument given that corresponds to the required parameter 'methodName' of 'InitializeWithAttribute.InitializeWithAttribute(string)'",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        var Expected = new DiagnosticResult(DescriptorCS7036);
        Expected = Expected.WithLocation("/0/Test0.cs", 8, 6);

        await VerifyCS.VerifyAnalyzerAsync(@"
internal class Test
{
    [InitializeWith()]
    public Test()
    {
    }

    public void Initialize()
    {
    }
}

internal partial class Program
{
    private static void Main()
    {
        var test = new Test();
        test.Initialize();
    }
}
", Expected).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TooManyAttributeArguments_NoDiagnostic()
    {
        var DescriptorCS1729 = new DiagnosticDescriptor(
            "CS1729",
            "title",
            "'InitializeWithAttribute' does not contain a constructor that takes 2 arguments",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        var Expected = new DiagnosticResult(DescriptorCS1729);
        Expected = Expected.WithLocation("/0/Test0.cs", 8, 6);

        await VerifyCS.VerifyAnalyzerAsync(@"
internal class Test
{
    [InitializeWith(nameof(Initialize), nameof(Initialize))]
    public Test()
    {
    }

    public void Initialize()
    {
    }
}

internal partial class Program
{
    private static void Main()
    {
        var test = new Test();
        test.Initialize();
    }
}
", Expected).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task WrongAttributeArgument_NoDiagnostic()
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

    public void Initialize()
    {
    }
}

internal partial class Program
{
    private static void Main()
    {
        var test = new Test();
        test.Initialize();
    }
}
", Expected).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task OtherMembers_NoDiagnostic()
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

    public void Initialize2()
    {
    }

    public string Foo { get; set; }
}

internal partial class Program
{
    private static void Main()
    {
        var test = new Test();
        test.Initialize();
    }
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task MultipleInitializerOverloads_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal class Test
{
    [InitializeWith(nameof(Initialize))]
    public Test()
    {
    }

    public void Initialize(string s)
    {
    }

    public void Initialize(int n)
    {
    }
}

internal partial class Program
{
    private static void Main()
    {
        var test = [|new Test()|];
        test.Initialize(0);
    }
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ObjectCreatedOutsideBlock_Diagnostic()
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

internal partial class Program
{
    private static void Main()
    {
        var test = Pass([|new Test()|]);
    }

    private static Test Pass(Test test)
    {
        return test;
    }
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FieldInitialization_Diagnostic()
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

internal class Foo
{
    public Test Field { get; set; }
}

internal partial class Program
{
    private static void Main()
    {
        var TestFoo = new Foo() { Field = [|new Test()|] };
    }
}
").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ParameterDefaultValue_Diagnostic()
    {
        var DescriptorCS1736 = new DiagnosticDescriptor(
            "CS1736",
            "title",
            "Default parameter value for 'test' must be a compile-time constant",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        var Expected = new DiagnosticResult(DescriptorCS1736);
        Expected = Expected.WithLocation("/0/Test0.cs", 20, 42);

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

internal partial class Program
{
    private static void Main(Test test = [|new Test()|])
    {
    }
}
", Expected).ConfigureAwait(false);
    }
}
