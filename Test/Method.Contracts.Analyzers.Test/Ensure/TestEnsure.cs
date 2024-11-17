namespace Contracts.Analyzers.Test;

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using NUnit.Framework;
using VerifyTests;

[TestFixture]
internal class TestEnsure
{
    [Test]
    public async Task TestSuccess()
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
    [Ensure(""textPlus.Length > text.Length"")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifyEnsure.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestAccessLast()
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

    [Ensure(""textPlus.Length > text.Length"")]
    [Access(""public"", ""static"")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifyEnsure.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }
    [Test]
    public async Task TestDefaultAccess()
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

    [Ensure(""textPlus.Length > text.Length"")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifyEnsure.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestMultipleArguments()
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

    [Ensure(""textPlus.Length > text1.Length"", ""textPlus.Length > text2.Length"")]
    private static void HelloFromVerified(string text1, string text2, out string textPlus)
    {
        textPlus = text1 + text2 + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifyEnsure.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestMultipleAttributes()
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

    [Ensure(""textPlus.Length > text1.Length"")]
    [Ensure(""textPlus.Length > text2.Length"")]
    private static void HelloFromVerified(string text1, string text2, out string textPlus)
    {
        textPlus = text1 + text2 + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifyEnsure.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestQuery()
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
        string Text = HelloFrom(""Hello, World"");
        Console.WriteLine(Text);
    }

    [Ensure(""Result.Length > text.Length"")]
    private static string HelloFromVerified(string text)
    {
        return text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifyEnsure.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestReleaseModeNoDebugOnly()
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
        string Text = HelloFrom(""Hello, World"");
        Console.WriteLine(Text);
    }

    [Ensure(""Result.Length > text.Length"")]
    private static string HelloFromVerified(string text)
    {
        return text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source, setDebug: false);
        VerifyResult Result = await VerifyEnsure.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestDebugModeNoDebugOnly()
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
        string Text = HelloFrom(""Hello, World"");
        Console.WriteLine(Text);
    }

    [Ensure(""Result.Length > text.Length"")]
    private static string HelloFromVerified(string text)
    {
        return text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source, setDebug: true);
        VerifyResult Result = await VerifyEnsure.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestReleaseModeDebugOnlyFalse()
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
        string Text = HelloFrom(""Hello, World"");
        Console.WriteLine(Text);
    }

    [Ensure(""Result.Length > text.Length"", DebugOnly = false)]
    private static string HelloFromVerified(string text)
    {
        return text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source, setDebug: false);
        VerifyResult Result = await VerifyEnsure.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestDebugModeDebugOnlyFalse()
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
        string Text = HelloFrom(""Hello, World"");
        Console.WriteLine(Text);
    }

    [Ensure(""Result.Length > text.Length"", DebugOnly = false)]
    private static string HelloFromVerified(string text)
    {
        return text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source, setDebug: true);
        VerifyResult Result = await VerifyEnsure.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestDebugModeDebugOnlyTrue()
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
        string Text = HelloFrom(""Hello, World"");
        Console.WriteLine(Text);
    }

    [Ensure(""Result.Length > text.Length"", DebugOnly = true)]
    private static string HelloFromVerified(string text)
    {
        return text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source, setDebug: true);
        VerifyResult Result = await VerifyEnsure.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestReleaseModeDebugOnlyTrue()
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
        string Text = HelloFrom(""Hello, World"");
        Console.WriteLine(Text);
    }

    [Access(""public"", ""static"")]
    [Ensure(""Result.Length > text.Length"", DebugOnly = true)]
    private static string HelloFromVerified(string text)
    {
        return text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        GeneratorDriver Driver = TestHelper.GetDriver(Source, setDebug: false);
        VerifyResult Result = await VerifyEnsure.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }
}
