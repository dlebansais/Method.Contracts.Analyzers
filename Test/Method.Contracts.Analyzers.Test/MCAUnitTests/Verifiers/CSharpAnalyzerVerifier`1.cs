#pragma warning disable CA1000 // Do not declare static members on generic types

namespace Contracts.Analyzers.Test;

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

internal static partial class CSharpAnalyzerVerifier<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    /// <inheritdoc cref="AnalyzerVerifier{TAnalyzer, TTest, TVerifier}.VerifyAnalyzerAsync(string, DiagnosticResult[])"/>
    public static async Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected) => await VerifyAnalyzerAsync(Prologs.Default, source, expected).ConfigureAwait(true);

    /// <inheritdoc cref="AnalyzerVerifier{TAnalyzer, TTest, TVerifier}.VerifyAnalyzerAsync(string, DiagnosticResult[])"/>
    public static async Task VerifyAnalyzerAsync(string prolog, string source, params DiagnosticResult[] expected) => await VerifyAnalyzerAsync(prolog, source, LanguageVersion.Default, expected).ConfigureAwait(true);

    /// <inheritdoc cref="AnalyzerVerifier{TAnalyzer, TTest, TVerifier}.VerifyAnalyzerAsync(string, DiagnosticResult[])"/>
    public static async Task VerifyAnalyzerAsync(string prolog, string source, LanguageVersion languageVersion = LanguageVersion.Default, params DiagnosticResult[] expected)
    {
        Test test = new();

        if (test.IsDiagnosticEnabledd && test.HasHelpLink)
            test = new() { TestCode = prolog + source, Version = languageVersion };

        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync(CancellationToken.None).ConfigureAwait(true);
    }
}
