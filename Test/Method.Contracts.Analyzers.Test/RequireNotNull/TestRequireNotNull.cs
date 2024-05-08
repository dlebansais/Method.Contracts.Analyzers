namespace Contracts.Analyzers.Test;

using System.Threading.Tasks;
using NUnit.Framework;
using VerifyTests;

[TestFixture]
public class TestRequireNotNull
{
    [Test]
    public async Task TestCommand()
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
    [RequireNotNull(""text"")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifyRequireNotNull.Verify(Driver).ConfigureAwait(false);

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

    [Access(""public"", ""static"")]
    [RequireNotNull(""text"")]
    private static string HelloFromVerified(string text)
    {
        return text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifyRequireNotNull.Verify(Driver).ConfigureAwait(false);

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

    [RequireNotNull(""text"")]
    [Access(""public"", ""static"")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifyRequireNotNull.Verify(Driver).ConfigureAwait(false);

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

    [RequireNotNull(""text"")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifyRequireNotNull.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestDefaultAsyncCommand()
    {
        // The source code to test
        const string Source = @"
namespace Contracts.TestSuite;

using System;
using System.Threading.Tasks;
using Contracts;

internal partial class Program
{
    public static void Main(string[] args)
    {
        Task.Run(async () => {
            await HelloFrom(""Hello, World"", out string Text);
            Console.WriteLine(Text);
        });
    }

    [RequireNotNull(""text"")]
    private static async Task HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
        return Task.CompletedTask;
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifyRequireNotNull.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestExplicitAsyncCommand()
    {
        // The source code to test
        const string Source = @"
namespace Contracts.TestSuite;

using System;
using System.Threading.Tasks;
using Contracts;

internal partial class Program
{
    public static void Main(string[] args)
    {
        Task.Run(async () => {
            await HelloFrom(""Hello, World"", out string Text);
            Console.WriteLine(Text);
        });
    }

    [Access(""public"", ""static"", ""async"")]
    [RequireNotNull(""text"")]
    private static async Task HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
        return Task.CompletedTask;
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifyRequireNotNull.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestDefaultAsyncQuery()
    {
        // The source code to test
        const string Source = @"
namespace Contracts.TestSuite;

using System;
using System.Threading.Tasks;
using Contracts;

internal partial class Program
{
    public static void Main(string[] args)
    {
        Task.Run(async () => {
            string Text = await HelloFrom(""Hello, World"");
            Console.WriteLine(Text);
        });
    }

    [RequireNotNull(""text"")]
    private async static Task<string> HelloFromVerified(string text)
    {
        return await Task.FromResult(text + ""!"");
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifyRequireNotNull.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestExplicitAsyncQuery()
    {
        // The source code to test
        const string Source = @"
namespace Contracts.TestSuite;

using System;
using System.Threading.Tasks;
using Contracts;

internal partial class Program
{
    public static void Main(string[] args)
    {
        Task.Run(async () => {
            string Text = await HelloFrom(""Hello, World"");
            Console.WriteLine(Text);
        });
    }

    [Access(""public"", ""static"", ""async"")]
    [RequireNotNull(""text"")]
    private async static Task<string> HelloFromVerified(string text)
    {
        return await Task.FromResult(text + ""!"");
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifyRequireNotNull.Verify(Driver).ConfigureAwait(false);

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

    [RequireNotNull(""text1"", ""text2"")]
    private static void HelloFromVerified(string text1, string text2, out string textPlus)
    {
        textPlus = text1 + text2 + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifyRequireNotNull.Verify(Driver).ConfigureAwait(false);

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

    [RequireNotNull(""text1"")]
    [RequireNotNull(""text2"")]
    private static void HelloFromVerified(string text1, string text2, out string textPlus)
    {
        textPlus = text1 + text2 + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifyRequireNotNull.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestUpperCaseParameter()
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
    [RequireNotNull(""Text"")]
    private static string HelloFromVerified(string Text)
    {
        return Text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifyRequireNotNull.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestNameof()
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

    [RequireNotNull(nameof(text))]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifyRequireNotNull.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }
}
