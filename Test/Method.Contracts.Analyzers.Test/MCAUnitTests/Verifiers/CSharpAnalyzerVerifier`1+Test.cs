#pragma warning disable CA2201 // Do not raise reserved exception types

namespace Contracts.Analyzers.Test;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Contracts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using NuGet.Configuration;

internal static partial class CSharpAnalyzerVerifier<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    private class Test : CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
    {
        public Test()
        {
            SolutionTransforms.Add((solution, projectId) =>
            {
                CompilationOptions compilationOptions = solution.GetProject(projectId)!.CompilationOptions!;
                compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(compilationOptions.SpecificDiagnosticOptions.SetItems(CSharpVerifierHelper.NullableWarnings));
                compilationOptions = compilationOptions.WithPlatform(Platform.X64);
                solution = solution.WithProjectCompilationOptions(projectId, compilationOptions);
                string RuntimePath = GetRuntimePath();

#if DEBUG
                const string ContractAssembly = "Method.Contracts-Debug";
#else
                const string ContractAssembly = "Method.Contracts";
#endif

                ISettings settings = Settings.LoadDefaultSettings(null);
                string nugetPath = SettingsUtility.GetGlobalPackagesFolder(settings);
                Version AssemblyVersion = typeof(RequireNotNullAttribute).Assembly.GetName().Version!;
                string AssemblyVersionString = $"{AssemblyVersion.Major}.{AssemblyVersion.Minor}.{AssemblyVersion.Build}";
                string AssemblyPath = Path.Combine(nugetPath, ContractAssembly, AssemblyVersionString, "lib", "net481", "Method.Contracts.dll");

                List<MetadataReference> DefaultReferences =
                [
                    //MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(AssemblyPath),
                    MetadataReference.CreateFromFile(string.Format(CultureInfo.InvariantCulture, RuntimePath, "mscorlib")),
                    MetadataReference.CreateFromFile(string.Format(CultureInfo.InvariantCulture, RuntimePath, "System")),
                    MetadataReference.CreateFromFile(string.Format(CultureInfo.InvariantCulture, RuntimePath, "System.Core")),
                    MetadataReference.CreateFromFile(string.Format(CultureInfo.InvariantCulture, RuntimePath, "System.Xaml")),
                    MetadataReference.CreateFromFile(string.Format(CultureInfo.InvariantCulture, RuntimePath, "PresentationCore")),
                    MetadataReference.CreateFromFile(string.Format(CultureInfo.InvariantCulture, RuntimePath, "PresentationFramework")),
                    MetadataReference.CreateFromFile(string.Format(CultureInfo.InvariantCulture, RuntimePath, @"Facades\System.Runtime")),
                    MetadataReference.CreateFromFile(string.Format(CultureInfo.InvariantCulture, RuntimePath, @"Facades\System.Collections")),
                ];

                solution = solution.WithProjectMetadataReferences(projectId, DefaultReferences);

                CSharpParseOptions ParseOptions = (CSharpParseOptions?)solution.GetProject(projectId)!.ParseOptions!;
                ParseOptions = ParseOptions.WithLanguageVersion(Version);
                solution = solution.WithProjectParseOptions(projectId, ParseOptions);

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
            string[] Directories = System.IO.Directory.GetDirectories(runtimeDirectoryBase);
            List<string> DirectoryList = [.. Directories];
            DirectoryList.Sort(CompareIgnoreCase);

            return DirectoryList;
        }

        private static int CompareIgnoreCase(string s1, string s2) => string.Compare(s1, s2, StringComparison.OrdinalIgnoreCase);

        private static bool IsValidRuntimeDirectory(string folderPath)
        {
            string FolderName = System.IO.Path.GetFileName(folderPath);
            const string Prefix = "v";

            Contract.Assert(FolderName.StartsWith(Prefix, StringComparison.Ordinal));

            string[] Parts = FolderName.Substring(Prefix.Length).Split('.');
            foreach (string Part in Parts)
                if (!int.TryParse(Part, out _))
                    return false;

            return true;
        }
    }
}
