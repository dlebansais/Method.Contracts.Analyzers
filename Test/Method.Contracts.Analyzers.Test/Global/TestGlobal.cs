namespace Contracts.Analyzers.Test;

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using NUnit.Framework;
using VerifyTests;

[TestFixture]
internal class TestGlobal
{
    [Test]
    public async Task TestNonGlobal()
    {
        // The source code to test
        const string Source = @"
namespace Contracts.TestSuite;

using System;
using Contracts;

internal partial class Program
{
    public static void Main(string[] args)
    {
        HelloFrom(""Hello, World"", out string Text);
        Console.WriteLine(Text);
    }

    [Access(""public"", ""static"")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifiyGlobal.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestWithGlobal()
    {
        // The source code to test
        const string Source = @"
namespace Contracts.TestSuite;

using global::System;
using Contracts;

internal partial class Program
{
    public static void Main(string[] args)
    {
        HelloFrom(""Hello, World"", out string Text);
        Console.WriteLine(Text);
    }

    [Access(""public"", ""static"")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifiyGlobal.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestWithGlobalThreading()
    {
        // The source code to test
        const string Source = @"
namespace Contracts.TestSuite;

using global::System.CodeDom.Compiler;
using global::System.Threading.Tasks;
using Contracts;

internal partial class Program
{
    public static void Main(string[] args)
    {
        HelloFrom(""Hello, World"", out string Text);
        Console.WriteLine(Text);
    }

    [Access(""public"", ""static"")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifiyGlobal.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestWithGlobalThreadingBeforeNamespace()
    {
        // The source code to test
        const string Source = @"
using global::System.CodeDom.Compiler;
using global::System.Threading.Tasks;
using Contracts;

namespace Contracts.TestSuite;

internal partial class Program
{
    public static void Main(string[] args)
    {
        HelloFrom(""Hello, World"", out string Text);
        Console.WriteLine(Text);
    }

    [Access(""public"", ""static"")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifiyGlobal.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestNonGlobalBeforeNamespace()
    {
        // The source code to test
        const string Source = @"
using System;
using Contracts;

namespace Contracts.TestSuite;

internal partial class Program
{
    public static void Main(string[] args)
    {
        HelloFrom(""Hello, World"", out string Text);
        Console.WriteLine(Text);
    }

    [Access(""public"", ""static"")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifiyGlobal.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }
}
