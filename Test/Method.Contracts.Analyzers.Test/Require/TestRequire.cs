namespace Contracts.Analyzers.Test;

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using NUnit.Framework;
using VerifyTests;

[TestFixture]
internal class TestRequire
{
    [Test]
    public async Task TestMethodSuccess()
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
    [Require(""text.Length > 0"")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifyRequire.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestMethodAccessLast()
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

    [Require(""text.Length > 0"")]
    [Access(""public"", ""static"")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifyRequire.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestMethodDefaultAccess()
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

    [Require(""text.Length > 0"")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifyRequire.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestMethodMultipleArguments()
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
        HelloFrom(""Hello, "", ""World"", out string Text);
        Console.WriteLine(Text);
    }

    [Require(""text1.Length > 0"", ""text2.Length > 0"")]
    private static void HelloFromVerified(string text1, string text2, out string textPlus)
    {
        textPlus = text1 + text2 + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifyRequire.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestMethodMultipleAttributes()
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
        HelloFrom(""Hello, "", ""World"", out string Text);
        Console.WriteLine(Text);
    }

    [Require(""text1.Length > 0"")]
    [Require(""text2.Length > 0"")]
    private static void HelloFromVerified(string text1, string text2, out string textPlus)
    {
        textPlus = text1 + text2 + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifyRequire.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestMethodReleaseModeNoDebugOnly()
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

    [Require(""text.Length > 0"")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source, setDebug: false);
        VerifyResult Result = await VerifyRequire.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestMethodDebugModeNoDebugOnly()
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

    [Require(""text.Length > 0"")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source, setDebug: true);
        VerifyResult Result = await VerifyRequire.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestMethodReleaseModeDebugOnlyFalse()
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

    [Require(""text.Length > 0"", DebugOnly = false)]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source, setDebug: false);
        VerifyResult Result = await VerifyRequire.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestMethodDebugModeDebugOnlyFalse()
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

    [Require(""text.Length > 0"", DebugOnly = false)]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source, setDebug: true);
        VerifyResult Result = await VerifyRequire.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestMethodDebugModeDebugOnlyTrue()
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

    [Require(""text.Length > 0"", DebugOnly = true)]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source, setDebug: true);
        VerifyResult Result = await VerifyRequire.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestMethodReleaseModeDebugOnlyTrue()
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
    [Require(""text.Length > 0"", DebugOnly = true)]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source, setDebug: false);
        VerifyResult Result = await VerifyRequire.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestMethodReleaseModeDebugOnlyTrueNoGeneration()
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

    [Require(""text.Length > 0"", DebugOnly = true)]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source, setDebug: false);
        VerifyResult Result = await VerifyRequire.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(0).Items);
    }

    [Test]
    public async Task TestPropertySuccess()
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
        string Text = Foo;
        Console.WriteLine(Text);
    }

    [Access(""public"", ""static"")]
    [Require(""Value.Length > 0"")]
    private static string FooVerified
    {
        { get; set; }
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifyRequire.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestPropertyAccessLast()
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
        string Text = Foo;
        Console.WriteLine(Text);
    }

    [Require(""Value.Length > 0"")]
    [Access(""public"", ""static"")]
    private static string FooVerified
    {
        { get; set; }
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifyRequire.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestPropertyDefaultAccess()
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
        string Text = Foo;
        Console.WriteLine(Text);
    }

    [Require(""Value.Length > 0"")]
    private static string FooVerified
    {
        { get; set; }
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifyRequire.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestPropertyMultipleArguments()
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
        string Text = Foo;
        Console.WriteLine(Text);
    }

    [Require(""Value.Length > 0"", ""Value.Length > 1"")]
    private static string FooVerified
    {
        { get; set; }
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifyRequire.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestPropertyMultipleAttributes()
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
        string Text = Foo;
        Console.WriteLine(Text);
    }

    [Require(""Value.Length > 0"")]
    [Require(""Value.Length > 1"")]
    private static string FooVerified
    {
        { get; set; }
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifyRequire.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestPropertyReleaseModeNoDebugOnly()
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
        string Text = Foo;
        Console.WriteLine(Text);
    }

    [Require(""Value.Length > 0"")]
    private static string FooVerified
    {
        { get; set; }
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source, setDebug: false);
        VerifyResult Result = await VerifyRequire.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestPropertyDebugModeNoDebugOnly()
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
        string Text = Foo;
        Console.WriteLine(Text);
    }

    [Require(""Value.Length > 0"")]
    private static string FooVerified
    {
        { get; set; }
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source, setDebug: true);
        VerifyResult Result = await VerifyRequire.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestPropertyReleaseModeDebugOnlyFalse()
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
        string Text = Foo;
        Console.WriteLine(Text);
    }

    [Require(""Value.Length > 0"", DebugOnly = false)]
    private static string FooVerified
    {
        { get; set; }
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source, setDebug: false);
        VerifyResult Result = await VerifyRequire.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestPropertyDebugModeDebugOnlyFalse()
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
        string Text = Foo;
        Console.WriteLine(Text);
    }

    [Require(""Value.Length > 0"", DebugOnly = false)]
    private static string FooVerified
    {
        { get; set; }
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source, setDebug: true);
        VerifyResult Result = await VerifyRequire.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestPropertyDebugModeDebugOnlyTrue()
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
        string Text = Foo;
        Console.WriteLine(Text);
    }

    [Require(""Value.Length > 0"", DebugOnly = true)]
    private static string FooVerified
    {
        { get; set; }
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source, setDebug: true);
        VerifyResult Result = await VerifyRequire.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestPropertyReleaseModeDebugOnlyTrue()
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
        string Text = Foo;
        Console.WriteLine(Text);
    }

    [Access(""public"", ""static"")]
    [Require(""value.Length > 0"", DebugOnly = true)]
    private static string FooVerified
    {
        { get; set; }
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source, setDebug: false);
        VerifyResult Result = await VerifyRequire.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestPropertyReleaseModeDebugOnlyTrueNoGeneration()
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
        string Text = Foo;
        Console.WriteLine(Text);
    }

    [Require(""value.Length > 0"", DebugOnly = true)]
    private static string FooVerified
    {
        { get; set; }
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source, setDebug: false);
        VerifyResult Result = await VerifyRequire.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(0).Items);
    }

    [Test]
    public async Task TestPropertyWithEnsure()
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
        string Text = Foo;
        Console.WriteLine(Text);
    }

    [Require(""Value.Length > 0"")]
    [Ensure(""Result.Length > 0"")]
    private static string FooVerified
    {
        { get; set; }
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifyRequire.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestPropertyValue()
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
        int n = Foo;
        Console.WriteLine(n.ToString());
    }

    [Require(""value > 0"")]
    private static int FooVerified
    {
        { get; set; }
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifyRequire.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestPropertyNullableString()
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
        string Text = Foo;
        Console.WriteLine(Text);
    }

    [Require(""value is null || value.Length > 0"")]
    private static string? FooVerified
    {
        { get; set; }
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifyRequire.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestPropertyNonNullableString()
    {
        // The source code to test
        const string Source = @"
#nullable enable

namespace Contracts.TestSuite;

using System;
using Contracts;

internal partial class Program
{
    public static void Main(string[] args)
    {
        string Text = Foo;
        Console.WriteLine(Text);
    }

    [Require(""Value.Length > 0"")]
    private static string FooVerified
    {
        { get; set; }
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifyRequire.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestPropertyNullableDisabled()
    {
        // The source code to test
        const string Source = @"
#nullable disable

namespace Contracts.TestSuite;

using System;
using Contracts;

internal partial class Program
{
    public static void Main(string[] args)
    {
        string Text = Foo;
        Console.WriteLine(Text);
    }

    [Require(""value is not null && value.Length > 0"")]
    private static string FooVerified
    {
        { get; set; }
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifyRequire.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }
}
