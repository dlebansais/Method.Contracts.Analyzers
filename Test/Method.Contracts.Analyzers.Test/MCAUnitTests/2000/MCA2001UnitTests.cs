namespace Contracts.Analyzers.Test;

extern alias Analyzers;

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS = CSharpAnalyzerVerifier<Analyzers.Contracts.Analyzers.MCA2001ObjectMustBeInitialized>;

[TestFixture]
internal partial class MCA2001UnitTests
{
    [Test]
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

    [Test]
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

    [Test]
    public async Task InitializerNotCalled3_Diagnostic()
    {
        DiagnosticDescriptor DescriptorCS8805 = new(
            "CS8805",
            "title",
            "Program using top-level statements must be an executable.",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        DiagnosticResult Expected = new(DescriptorCS8805);
        Expected = Expected.WithLocation("/0/Test0.cs", Prologs.DefaultLineCount + 3, 1);

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

    [Test]
    public async Task InitializerNotCalled4_Diagnostic()
    {
        DiagnosticDescriptor DescriptorCS8803 = new(
            "CS8803",
            "title",
            "Top-level statements must precede namespace and type declarations.",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        DiagnosticDescriptor DescriptorCS8805 = new(
            "CS8805",
            "title",
            "Program using top-level statements must be an executable.",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        DiagnosticResult Expected1 = new(DescriptorCS8803);
        Expected1 = Expected1.WithLocation("/0/Test0.cs", Prologs.DefaultLineCount + 15, 1);

        DiagnosticResult Expected2 = new(DescriptorCS8805);
        Expected2 = Expected2.WithLocation("/0/Test0.cs", Prologs.DefaultLineCount + 15, 1);

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

    [Test]
    public async Task InitializerNotCalled5_Diagnostic()
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
        test1.Initialize();
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task InitializerNotCalled6_Diagnostic()
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
        int[] test = new int[] { 0 };
        var test1 = [|new Test()|];
        var test2 = [|new Test()|];
        test[0] = 0;
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task InitializerNotCalled7_Diagnostic()
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

    public Test Foo { get; set; }
}

internal partial class Program
{
    private static void Main()
    {
        var test = [|new Test()|];
        test.Foo.Initialize();
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task InitializerNotCalled8_Diagnostic()
    {
        DiagnosticDescriptor DescriptorCS0103 = new(
            "CS0103",
            "title",
            "The name 'foo' does not exist in the current context",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        DiagnosticResult Expected = new(DescriptorCS0103);
        Expected = Expected.WithLocation("/0/Test0.cs", Prologs.DefaultLineCount + 22, 9);

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

    public Test Foo { get; set; }
}

internal partial class Program
{
    private static void Main()
    {
        var test = [|new Test()|];
        foo.Initialize();
    }
}
", Expected).ConfigureAwait(false);
    }

    [Test]
    public async Task InitializerNotCalled9_Diagnostic()
    {
        DiagnosticDescriptor DescriptorCS0311 = new(
            "CS0311",
            "title",
            "The type 'string' cannot be used as type parameter 'T' in the generic type or method 'Test.Initialize<T>()'. There is no implicit reference conversion from 'string' to 'Test'.",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        DiagnosticResult Expected = new(DescriptorCS0311);
        Expected = Expected.WithLocation("/0/Test0.cs", Prologs.DefaultLineCount + 21, 14);

        await VerifyCS.VerifyAnalyzerAsync(@"
internal class Test
{
    [InitializeWith(nameof(Initialize))]
    public Test()
    {
    }

    public void Initialize<T>()
        where T : Test
    {
    }
}

internal partial class Program
{
    private static void Main()
    {
        var test = [|new Test()|];
        test.Initialize<string>();
    }
}
", Expected).ConfigureAwait(false);
    }

    [Test]
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

    [Test]
    public async Task RecordInitializerNotCalled_Diagnostic()
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

internal partial class Program
{
    private static void Main()
    {
        var test = [|new Test()|];
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task RecordInitializerCalled_NoDiagnostic()
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

    [Test]
    public async Task UnknownClass_NoDiagnostic()
    {
        DiagnosticDescriptor DescriptorCS8754 = new(
            "CS8754",
            "title",
            "There is no target type for 'new()'",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        DiagnosticResult Expected = new(DescriptorCS8754);
        Expected = Expected.WithLocation("/0/Test0.cs", Prologs.DefaultLineCount + 7, 19);

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

    [Test]
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

    [Test]
    public async Task Interface_NoDiagnostic()
    {
        DiagnosticDescriptor DescriptorCS0144 = new(
            "CS0144",
            "title",
            "Cannot create an instance of the abstract type or interface 'ITest'",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        DiagnosticDescriptor DescriptorCS1061 = new(
            "CS1061",
            "title",
            "'ITest' does not contain a definition for 'Initialize' and no accessible extension method 'Initialize' accepting a first argument of type 'ITest' could be found (are you missing a using directive or an assembly reference?)",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        DiagnosticResult Expected1 = new(DescriptorCS0144);
        Expected1 = Expected1.WithLocation("/0/Test0.cs", Prologs.DefaultLineCount + 11, 20);

        DiagnosticResult Expected2 = new(DescriptorCS1061);
        Expected2 = Expected2.WithLocation("/0/Test0.cs", Prologs.DefaultLineCount + 12, 14);

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

    [Test]
    public async Task ConstructorNotFound_NoDiagnostic()
    {
        DiagnosticDescriptor DescriptorCS1729 = new(
            "CS1729",
            "title",
            "'Test' does not contain a constructor that takes 1 arguments",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        DiagnosticResult Expected = new(DescriptorCS1729);
        Expected = Expected.WithLocation("/0/Test0.cs", Prologs.DefaultLineCount + 19, 24);

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

    [Test]
    public async Task UnknownAttribute_NoDiagnostic()
    {
        DiagnosticDescriptor DescriptorCS0246 = new(
            "CS0246",
            "title",
            "The type or namespace name 'Foo' could not be found (are you missing a using directive or an assembly reference?)",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        DiagnosticDescriptor DescriptorCS0246Attribute = new(
            "CS0246",
            "title",
            "The type or namespace name 'FooAttribute' could not be found (are you missing a using directive or an assembly reference?)",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        DiagnosticDescriptor DescriptorCS1061 = new(
            "CS1061",
            "title",
            "'Test' does not contain a definition for 'Initialize' and no accessible extension method 'Initialize' accepting a first argument of type 'Test' could be found (are you missing a using directive or an assembly reference?)",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        DiagnosticResult Expected1 = new(DescriptorCS0246);
        Expected1 = Expected1.WithLocation("/0/Test0.cs", Prologs.DefaultLineCount + 5, 6);

        DiagnosticResult Expected2 = new(DescriptorCS0246Attribute);
        Expected2 = Expected2.WithLocation("/0/Test0.cs", Prologs.DefaultLineCount + 5, 6);

        DiagnosticResult Expected3 = new(DescriptorCS1061);
        Expected3 = Expected3.WithLocation("/0/Test0.cs", Prologs.DefaultLineCount + 16, 14);

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

    [Test]
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

    [Test]
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

    [Test]
    public async Task NotEnoughAttributeArgument_NoDiagnostic()
    {
        DiagnosticDescriptor DescriptorCS7036 = new(
            "CS7036",
            "title",
            "There is no argument given that corresponds to the required parameter 'methodName' of 'InitializeWithAttribute.InitializeWithAttribute(string)'",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        DiagnosticResult Expected = new(DescriptorCS7036);
        Expected = Expected.WithLocation("/0/Test0.cs", Prologs.DefaultLineCount + 5, 6);

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

    [Test]
    public async Task TooManyAttributeArguments_NoDiagnostic()
    {
        DiagnosticDescriptor DescriptorCS1729 = new(
            "CS1729",
            "title",
            "'InitializeWithAttribute' does not contain a constructor that takes 2 arguments",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        DiagnosticResult Expected = new(DescriptorCS1729);
        Expected = Expected.WithLocation("/0/Test0.cs", Prologs.DefaultLineCount + 5, 6);

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

    [Test]
    public async Task WrongAttributeArgument_NoDiagnostic()
    {
        DiagnosticDescriptor DescriptorCS1503 = new(
            "CS1503",
            "title",
            "Argument 1: cannot convert from 'int' to 'string'",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        DiagnosticResult Expected = new(DescriptorCS1503);
        Expected = Expected.WithLocation("/0/Test0.cs", Prologs.DefaultLineCount + 5, 21);

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

    [Test]
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

    [Test]
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

    [Test]
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

    [Test]
    public async Task CreatedObjectFieldInitialization_Diagnostic()
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

    [Test]
    public async Task ParameterDefaultValue_Diagnostic()
    {
        DiagnosticDescriptor DescriptorCS1736 = new(
            "CS1736",
            "title",
            "Default parameter value for 'test' must be a compile-time constant",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        DiagnosticResult Expected = new(DescriptorCS1736);
        Expected = Expected.WithLocation("/0/Test0.cs", Prologs.DefaultLineCount + 17, 42);

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

    [Test]
    public async Task GlobalStatementInitializerCalled_NoDiagnostic()
    {
        DiagnosticDescriptor DescriptorCS8805 = new(
            "CS8805",
            "title",
            "Program using top-level statements must be an executable.",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        DiagnosticResult Expected = new(DescriptorCS8805);
        Expected = Expected.WithLocation("/0/Test0.cs", Prologs.DefaultLineCount + 3, 1);

        await VerifyCS.VerifyAnalyzerAsync(@"
Test test = new Test();
test.Initialize();

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

    [Test]
    public async Task NotSimpleLeftExpression_Diagnostic()
    {
        DiagnosticDescriptor DescriptorCS1001 = new(
            "CS1001",
            "title",
            "Identifier expected",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        DiagnosticDescriptor DescriptorCS0103 = new(
            "CS0103",
            "title",
            "The name 'Initialize' does not exist in the current context",
            "description",
            DiagnosticSeverity.Error,
            true
            );

        DiagnosticResult Expected1 = new(DescriptorCS1001);
        Expected1 = Expected1.WithLocation("/0/Test0.cs", Prologs.DefaultLineCount + 20, 16);

        DiagnosticResult Expected2 = new(DescriptorCS0103);
        Expected2 = Expected2.WithLocation("/0/Test0.cs", Prologs.DefaultLineCount + 20, 17);

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
        (test).(Initialize)();
    }
}
", Expected1, Expected2).ConfigureAwait(false);
    }

    [Test]
    public async Task VariableDeclaredInLoop_Diagnostic()
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
        for (Test test = [|new Test()|];;)
        {
        }
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task FieldInitialization_NoDiagnostic()
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
    private static Test test;

    private static void Main()
    {
        test = new Test();
        test.Initialize();
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task FieldInitializationMissingInit_Diagnostic()
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
    private static Test test;

    private static void Main()
    {
        test = [|new Test()|];
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task ArrayInitialization_Diagnostic()
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
    private static Test[] test = new Test[1];

    private static void Main()
    {
        test[0] = [|new Test()|];
        test[0].Initialize();
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task PropertyInitialization_NoDiagnostic()
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
    private static Test test { get; set; }

    private static void Main()
    {
        test = new Test();
        test.Initialize();
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task PropertyInitializationMissingInit_Diagnostic()
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
    private static Test test { get; set; }

    private static void Main()
    {
        test = [|new Test()|];
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task AsyncInitializerCalled_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(Prologs.Default, @"
using System.Threading.Tasks;

internal class Test
{
    [InitializeWith(nameof(InitializeAsync))]
    public Test()
    {
    }

    public async Task InitializeAsync()
    {
        await Task.CompletedTask;
    }
}

internal partial class Program
{
    private static async Task Main()
    {
        var test = new Test();
        await test.InitializeAsync();
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task MultipleConstructors_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal class Test
{
    [InitializeWith(nameof(InitializeString))]
    public Test(string value)
    {
    }

    [InitializeWith(nameof(InitializeInt))]
    public Test(int value)
    {
    }

    public void InitializeString()
    {
    }

    public void InitializeInt()
    {
    }
}

internal partial class Program
{
    private static void Main()
    {
        var test1 = new Test(string.Empty);
        test1.InitializeString();

        var test2 = new Test(0);
        test2.InitializeInt();
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task MultipleConstructorsMixed_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
internal class Test
{
    [InitializeWith(nameof(InitializeString))]
    public Test(string value)
    {
    }

    [InitializeWith(nameof(InitializeInt))]
    public Test(int value)
    {
    }

    public void InitializeString()
    {
    }

    public void InitializeInt()
    {
    }
}

internal partial class Program
{
    private static void Main()
    {
        var test1 = [|new Test(string.Empty)|];
        test1.InitializeInt();

        var test2 = [|new Test(0)|];
        test2.InitializeString();
    }
}
").ConfigureAwait(false);
    }

    [Test]
    public async Task DefaultConstructorInitializerNotCalled_Diagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
[InitializeWith(nameof(Initialize))]
internal class Test
{
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

    [Test]
    public async Task DefaultConstructorInitializerCalled_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
[InitializeWith(nameof(Initialize))]
internal class Test
{
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
}
