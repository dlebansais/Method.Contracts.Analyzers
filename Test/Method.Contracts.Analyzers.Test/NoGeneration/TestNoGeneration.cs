namespace Contracts.Analyzers.Test;

using System.Threading.Tasks;
using NUnit.Framework;
using VerifyTests;

[TestFixture]
public class TestNoGeneration
{
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
    public async Task TestNotAVerifiedMethod()
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
}
