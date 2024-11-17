namespace Contracts.Analyzers.Test;

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using VerifyNUnit;
using VerifyTests;

internal static class VerifyEnsure
{
    public static async Task<VerifyResult> Verify(GeneratorDriver driver)
    {
        // Use verify to snapshot test the source generator output.
        return await Verifier.Verify(driver);
    }
}
