namespace Contracts.Analyzers.Test;

using System.Threading.Tasks;
using NUnit.Framework;

[TestFixture]
public class TestNoGeneration
{
    [Test]
    public async Task TestSuccess()
    {
        // The source code to test
        const string Source = @"
using Contracts.TestSuite;

public class SimpleTest
{
}
";

        // Pass the source code to our helper and snapshot test the output
        await TestHelper.Verify(Source).ConfigureAwait(false);
    }
}
