#pragma warning disable CA2201 // Do not raise reserved exception types

namespace Contracts.Analyzers.Test;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

public static partial class CSharpAnalyzerVerifier<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    private class Test : CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
    {
        public Test()
        {
            SolutionTransforms.Add((solution, projectId) =>
            {
                var compilationOptions = solution.GetProject(projectId)?.CompilationOptions;
                compilationOptions = compilationOptions?.WithSpecificDiagnosticOptions(compilationOptions.SpecificDiagnosticOptions.SetItems(CSharpVerifierHelper.NullableWarnings));
                compilationOptions = compilationOptions?.WithPlatform(Platform.X64);
                solution = solution.WithProjectCompilationOptions(projectId, compilationOptions ?? throw new NullReferenceException());

                string RuntimePath = GetRuntimePath();
                List<MetadataReference> DefaultReferences = new()
                {
                    //MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(AccessAttribute).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(string.Format(CultureInfo.InvariantCulture, RuntimePath, "mscorlib")),
                    MetadataReference.CreateFromFile(string.Format(CultureInfo.InvariantCulture, RuntimePath, "System")),
                    MetadataReference.CreateFromFile(string.Format(CultureInfo.InvariantCulture, RuntimePath, "System.Core")),
                    MetadataReference.CreateFromFile(string.Format(CultureInfo.InvariantCulture, RuntimePath, "System.Xaml")),
                    MetadataReference.CreateFromFile(string.Format(CultureInfo.InvariantCulture, RuntimePath, "PresentationCore")),
                    MetadataReference.CreateFromFile(string.Format(CultureInfo.InvariantCulture, RuntimePath, "PresentationFramework")),
                    MetadataReference.CreateFromFile(string.Format(CultureInfo.InvariantCulture, RuntimePath, @"Facades\System.Runtime")),
                };

                solution = solution.WithProjectMetadataReferences(projectId, DefaultReferences);

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

        private static string GetRuntimePath()
        {
            const string RuntimeDirectoryBase = @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework";
            string RuntimeDirectory = string.Empty;

            foreach (string FolderPath in GetRuntimeDirectories(RuntimeDirectoryBase))
                if (IsValidRuntimeDirectory(FolderPath))
                    RuntimeDirectory = FolderPath;

            string RuntimePath = RuntimeDirectory + @"\{0}.dll";

            return RuntimePath;
        }

        private static List<string> GetRuntimeDirectories(string runtimeDirectoryBase)
        {
            List<string> Directories = System.IO.Directory.GetDirectories(runtimeDirectoryBase).ToList();
            Directories.Sort(CompareIgnoreCase);

            return Directories;
        }

        private static int CompareIgnoreCase(string s1, string s2)
        {
            return string.Compare(s1, s2, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsValidRuntimeDirectory(string folderPath)
        {
            string FolderName = System.IO.Path.GetFileName(folderPath);
            const string Prefix = "v";

            Debug.Assert(FolderName.StartsWith(Prefix, StringComparison.Ordinal));

            string[] Parts = FolderName.Substring(Prefix.Length).Split('.');
            foreach (string Part in Parts)
                if (!int.TryParse(Part, out _))
                    return false;

            return true;
        }
    }
}
