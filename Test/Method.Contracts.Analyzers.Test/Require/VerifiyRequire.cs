namespace Contracts.Analyzers.Test;

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using VerifyNUnit;
using VerifyTests;

internal static class VerifyRequire
{
    public static async Task<VerifyResult> Verify(GeneratorDriver driver)
    => await Verifier.Verify(driver);
}
