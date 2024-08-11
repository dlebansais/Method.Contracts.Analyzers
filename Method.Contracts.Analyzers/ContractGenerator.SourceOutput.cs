namespace Contracts.Analyzers;

using System;
using System.Collections.Immutable;
using System.Text;
using Contracts.Analyzers.Helper;
using Microsoft.CodeAnalysis;

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
                partial {{Model.DeclarationTokens}} {{Model.FullClassName}}
                {
                {{Model.Documentation}}{{Model.GeneratedMethodDeclaration}}
                }
                """;
#if NETSTANDARD2_1_OR_GREATER
            SourceText = SourceText.Replace("\r\n", "\n", StringComparison.Ordinal);
#else
            SourceText = SourceText.Replace("\r\n", "\n");
#endif

            context.AddSource($"{Model.ClassName}_{Model.ShortMethodName}{Model.UniqueOverloadIdentifier}.g.cs", Microsoft.CodeAnalysis.Text.SourceText.From(SourceText, Encoding.UTF8));
        }
    }
}
