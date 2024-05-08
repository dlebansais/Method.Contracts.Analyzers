namespace Contracts.Analyzers.Test;

using System.Threading.Tasks;
using NUnit.Framework;
using VerifyTests;

[TestFixture]
public class TestAccess
{
    [Test]
    public async Task TestCommandOut()
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
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifiyAccess.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestCommandRef()
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
        string Text = ""Hello, World"";
        HelloFrom(ref Text);
        Console.WriteLine(Text);
    }

    [Access(""public"", ""static"")]
    private static void HelloFromVerified(ref string text)
    {
        text += ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifiyAccess.Verify(Driver).ConfigureAwait(false);

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
    private static string HelloFromVerified(string text)
    {
        return text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifiyAccess.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestQueryRef()
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
        string Copy = string.Empty;
        string Text = HelloFrom(""Hello, World"", ref Copy);
        Console.WriteLine(Text);
        Console.WriteLine(Copy);
    }

    [Access(""public"", ""static"")]
    private static string HelloFromVerified(string text, ref string copy)
    {
        copy = text;
        return text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifiyAccess.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestQueryOut()
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
        string Text = HelloFrom(""Hello, World"", out string Copy);
        Console.WriteLine(Text);
        Console.WriteLine(Copy);
    }

    [Access(""public"", ""static"")]
    private static string HelloFromVerified(string text, out string copy)
    {
        copy = text;
        return text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifiyAccess.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestExtraAttribute()
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
    [Obsolete]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifiyAccess.Verify(Driver).ConfigureAwait(false);

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
    private static string HelloFromVerified(string Text)
    {
        return Text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifiyAccess.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestMultipleMethods()
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
        string Text;

        HelloFrom(""Hello, World"", out Text);
        Console.WriteLine(Text);

        ByeByeFrom(""Bye bye, World"", out Text);
        Console.WriteLine(Text);
    }

    [Access(""public"", ""static"")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }

    [Access(""public"", ""static"")]
    private static void ByeByeFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifiyAccess.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(2).Items);
    }

    [Test]
    public async Task TestDoc()
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

    /// <summary>
    /// Test doc.
    /// </summary>
    /// <param name=""text"">Test parameter.</param>
    /// <returns>Test value.</returns>
    [Access(""public"", ""static"")]
    private static string HelloFromVerified(string text)
    {
        return text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifiyAccess.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }

    [Test]
    public async Task TestNoTrivia()
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
    }[Access(""public"", ""static"")]
    private static string HelloFromVerified(string text)
    {
        return text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifiyAccess.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(1).Items);
    }
}
