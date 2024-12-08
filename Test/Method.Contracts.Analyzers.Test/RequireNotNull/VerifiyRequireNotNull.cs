﻿namespace Contracts.Analyzers.Test;

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using VerifyNUnit;
using VerifyTests;

internal static class VerifyRequireNotNull
{
    // Use verify to snapshot test the source generator output.
    public static async Task<VerifyResult> Verify(GeneratorDriver driver) => await Verifier.Verify(driver);
}
