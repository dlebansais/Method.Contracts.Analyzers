namespace Contracts.Analyzers.Test;

using System.Threading.Tasks;
using NUnit.Framework;
using VerifyTests;

[TestFixture]
public class TestNoGeneration
{
    [Test]
    public async Task TestSuccess()
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

        Assert.That(Result, Is.Not.Null);
    }
}
