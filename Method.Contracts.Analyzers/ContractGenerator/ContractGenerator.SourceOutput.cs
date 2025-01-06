namespace Contracts.Analyzers;

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
        // Stryker disable once String: this line is untestable.
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
                {{Model.Documentation}}{{Model.GeneratedMethodDeclaration}}{{Model.GeneratedPropertyDeclaration}}
                }
                """;

            // Stryker disable once String: this line has no effect when the environment is not Windows, but in Windows we do need it.
            SourceText = AnalyzerTools.Replace(SourceText, "\r\n", "\n");

            context.AddSource($"{Model.ClassName}_{Model.ShortName}{Model.UniqueOverloadIdentifier}.g.cs", Microsoft.CodeAnalysis.Text.SourceText.From(SourceText, Encoding.UTF8));
        }
    }
}
