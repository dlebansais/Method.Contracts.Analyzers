namespace Contracts.Analyzers;

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Contracts.Analyzers.Helper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Represents a code generator.
/// </summary>
public partial class ContractGenerator
{
    private static void OutputContractMethod(SourceProductionContext context, (GeneratorSettings Settings, ImmutableArray<ContractModel> Models) modelAndSettings)
    {
        string DisableWarnings = GeneratorHelper.AddPrefixAndSuffixIfNotEmpty(Settings.DisabledWarnings, "#pragma warning disable ", "\n");

        foreach (ContractModel Model in modelAndSettings.Models)
        {
            string SourceText = $$"""
                #nullable enable
                {{DisableWarnings}}{{Model.UsingsBeforeNamespace}}
                namespace {{Model.Namespace}};
                {{Model.UsingsAfterNamespace}}
                partial class {{Model.ClassName}}
                {
                {{Model.Documentation}}{{Model.GeneratedMethodDeclaration}}
                }
                """;
            SourceText = SourceText.Replace("\r\n", "\n");

            context.AddSource($"{Model.ClassName}_{Model.ShortMethodName}{Model.UniqueOverloadIdentifier}.g.cs", Microsoft.CodeAnalysis.Text.SourceText.From(SourceText, Encoding.UTF8));
        }
    }
}
