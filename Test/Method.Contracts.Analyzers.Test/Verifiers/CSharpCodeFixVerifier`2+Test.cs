﻿#pragma warning disable CA2201 // Do not raise reserved exception types

namespace Contracts.Analyzers.Test;

using System;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

public static partial class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new()
{
    private class Test : CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>
    {
        public Test()
        {
            SolutionTransforms.Add((solution, projectId) =>
            {
                var CompilationOptions = solution.GetProject(projectId)?.CompilationOptions;
                CompilationOptions = CompilationOptions?.WithSpecificDiagnosticOptions(CompilationOptions.SpecificDiagnosticOptions.SetItems(CSharpVerifierHelper.NullableWarnings));
                solution = solution.WithProjectCompilationOptions(projectId, CompilationOptions ?? throw new NullReferenceException());

                if (Version != LanguageVersion.Default)
                {
                    CSharpParseOptions? ParseOptions = (CSharpParseOptions?)solution.GetProject(projectId)?.ParseOptions;
                    ParseOptions = ParseOptions?.WithLanguageVersion(Version);
                    solution = solution.WithProjectParseOptions(projectId, ParseOptions ?? throw new NullReferenceException());
                }

                return solution;
            });
        }

        public LanguageVersion Version { get; set; } = LanguageVersion.Default;
    }
}
