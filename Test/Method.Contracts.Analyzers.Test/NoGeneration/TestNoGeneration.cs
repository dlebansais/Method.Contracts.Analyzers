namespace Contracts.Analyzers.Test;

using System.Threading.Tasks;
using NUnit.Framework;
using VerifyTests;

[TestFixture]
public class TestNoGeneration
{
    [Test]
    public async Task TestNoNamespace()
    {
        // The source code to test
        const string Source = @"
public class SimpleTest
{
    [Access(""public"")]
    public void FooVerified()
    {
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifiyNoGeneration.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(0).Items);
    }

    [Test]
    public async Task TestNoClass()
    {
        // The source code to test
        const string Source = @"
namespace Contracts.TestSuite;

[Access(""public"")]
public void FooVerified()
{
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifiyNoGeneration.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(0).Items);
    }

    [Test]
    public async Task TestNoMember()
    {
        // The source code to test
        const string Source = @"
namespace Contracts.TestSuite;

public class SimpleTest
{
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifiyNoGeneration.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(0).Items);
    }

    [Test]
    public async Task TestProperty()
    {
        // The source code to test
        const string Source = @"
namespace Contracts.TestSuite;

public class SimpleTest
{
    [Access(""public"")]
    public int Foo { get; set; }
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifiyNoGeneration.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(0).Items);
    }

    [Test]
    public async Task TestNotAVerifiedMethod1()
    {
        // The source code to test
        const string Source = @"
namespace Contracts.TestSuite;

public class SimpleTest
{
    [Access(""public"")]
    public void Foo()
    {
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifiyNoGeneration.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(0).Items);
    }

    [Test]
    public async Task TestNotAVerifiedMethod2()
    {
        // The source code to test
        const string Source = @"
namespace Contracts.TestSuite;

public class SimpleTest
{
    [Access(""public"")]
    public void Verified()
    {
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifiyNoGeneration.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(0).Items);
    }

    [Test]
    public async Task TestNoAttributeArguments()
    {
        // The source code to test
        const string Source = @"
namespace Contracts.TestSuite;

public class SimpleTest
{
    [Access]
    public void FooVerified()
    {
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifiyNoGeneration.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(0).Items);
    }

    [Test]
    public async Task TestEmptyAttributeArguments1()
    {
        // The source code to test
        const string Source = @"
namespace Contracts.TestSuite;

public class SimpleTest
{
    [Access()]
    public void FooVerified()
    {
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifiyNoGeneration.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(0).Items);
    }

    [Test]
    public async Task TestEmptyAttributeArguments2()
    {
        // The source code to test
        const string Source = @"
namespace Contracts.TestSuite;

public class SimpleTest
{
    [RequireNotNull()]
    public void FooVerified()
    {
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifiyNoGeneration.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(0).Items);
    }

    [Test]
    public async Task TestEmptyStringAttributeArgument()
    {
        // The source code to test
        const string Source = @"
namespace Contracts.TestSuite;

public class SimpleTest
{
    [RequireNotNull("""")]
    public void FooVerified()
    {
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifiyNoGeneration.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(0).Items);
    }

    [Test]
    public async Task TestInvalidAttributeArguments()
    {
        // The source code to test
        const string Source = @"
namespace Contracts.TestSuite;

public class SimpleTest
{
    private const int Arg = 0;

    [Access(""public"", Arg)]
    public void FooVerified()
    {
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifiyNoGeneration.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(0).Items);
    }

    [Test]
    public async Task TestInvalidAttributeArgumentNotNameof()
    {
        // The source code to test
        const string Source = @"
namespace Contracts.TestSuite;

public class SimpleTest
{
    [Access(arg(System.String))]
    public void FooVerified()
    {
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifiyNoGeneration.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(0).Items);
    }

    [Test]
    public async Task TestInvalidAttributeArgumentBadNameof1()
    {
        // The source code to test
        const string Source = @"
namespace Contracts.TestSuite;

public class SimpleTest
{
    [Access(nameof(System.String))]
    public void FooVerified()
    {
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifiyNoGeneration.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(0).Items);
    }

    [Test]
    public async Task TestInvalidAttributeArgumentBadNameof2()
    {
        // The source code to test
        const string Source = @"
namespace Contracts.TestSuite;

public class SimpleTest
{
    [Access(nameof(System.String)(System.String))]
    public void FooVerified()
    {
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifiyNoGeneration.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(0).Items);
    }

    [Test]
    public async Task TestInvalidAttributeArgumentBadNameof3()
    {
        // The source code to test
        const string Source = @"
namespace Contracts.TestSuite;

public class SimpleTest
{
    [Access(nameof(System.String, System.String))]
    public void FooVerified()
    {
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifiyNoGeneration.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(0).Items);
    }

    [Test]
    public async Task TestInvalidAttributeArgumentBadNameof4()
    {
        // The source code to test
        const string Source = @"
namespace Contracts.TestSuite;

public class SimpleTest
{
    [Access(nameof())]
    public void FooVerified()
    {
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifiyNoGeneration.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(0).Items);
    }

    [Test]
    public async Task TestMixOfValidAndInvalidAttributes()
    {
        // The source code to test
        const string Source = @"
namespace Contracts.TestSuite;

public class SimpleTest
{
    [Access(""public"")]
    [RequireNotNull("""")]
    public void FooVerified()
    {
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifiyNoGeneration.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(0).Items);
    }

    [Test]
    public async Task TestInvalidParameterName()
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
    [RequireNotNull(""badname"")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifiyNoGeneration.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(0).Items);
    }

    [Test]
    public async Task TestInvalidAliasName()
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
    [RequireNotNull(""text"", BadAlias = ""Foo"")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifiyNoGeneration.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(0).Items);
    }

    [Test]
    public async Task TestInvalidAlias1()
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
    [RequireNotNull(AliasName = ""Foo"", ""text"")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifiyNoGeneration.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(0).Items);
    }

    [Test]
    public async Task TestInvalidAlias2()
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
    [RequireNotNull(nameof(), AliasName = ""Foo"")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifiyNoGeneration.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(0).Items);
    }

    [Test]
    public async Task TestInvalidAlias3()
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
    [RequireNotNull(""foo"", AliasName = ""Foo"")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifiyNoGeneration.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(0).Items);
    }

    [Test]
    public async Task TestInvalidAlias4()
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
    [RequireNotNull(""text"", AliasName = ""Foo"", ""foo"")]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifiyNoGeneration.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(0).Items);
    }

    [Test]
    public async Task TestInvalidAlias5()
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
    [RequireNotNull(""text"", AliasName = nameof())]
    private static void HelloFromVerified(string text, out string textPlus)
    {
        textPlus = text + ""!"";
    }
}
";

        // Pass the source code to the helper and snapshot test the output.
        var Driver = TestHelper.GetDriver(Source);
        VerifyResult Result = await VerifiyNoGeneration.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(0).Items);
    }

    [Test]
    public async Task TestEnsureReleaseModeDebugOnlyTrue()
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
        var Driver = TestHelper.GetDriver(Source, setDebug: false);
        VerifyResult Result = await VerifiyNoGeneration.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(0).Items);
    }

    [Test]
    public async Task TestRequireReleaseModeDebugOnlyTrue()
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
        var Driver = TestHelper.GetDriver(Source, setDebug: false);
        VerifyResult Result = await VerifiyNoGeneration.Verify(Driver).ConfigureAwait(false);

        Assert.That(Result.Files, Has.Exactly(0).Items);
    }
}
