namespace Contracts.Analyzers;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Analyzer for rule MCA1008: RequireNotNull attribute uses invalid alias.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MCA1008RequireNotNullAttributeUsesInvalidAlias : InvalidUseOfAttributeDiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for this rule.
    /// </summary>
    public const string DiagnosticId = "MCA1008";

    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(AnalyzerResources.MCA1008AnalyzerTitle), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(AnalyzerResources.MCA1008AnalyzerMessageFormat), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
    private static readonly LocalizableString Description = new LocalizableResourceString(nameof(AnalyzerResources.MCA1008AnalyzerDescription), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
    private const string Category = "Usage";

    private static readonly DiagnosticDescriptor Rule = new(DiagnosticId,
                                                            Title,
                                                            MessageFormat,
                                                            Category,
                                                            DiagnosticSeverity.Error,
                                                            isEnabledByDefault: true,
                                                            description: Description,
                                                            AnalyzerTools.GetHelpLink(DiagnosticId));

    /// <summary>
    /// Gets the list of supported diagnostic.
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <inheritdoc />
    private protected override void AnalyzeVerifiedNode(SyntaxNodeAnalysisContext context, AttributeArgumentSyntax attributeArgument, IAnalysisAssertion[] analysisAssertions)
    {
        if (!IsAttributeValidForRule(attributeArgument, nameof(RequireNotNullAttribute.AliasName), out string ArgumentValue))
            return;

        string AliasName = ArgumentValue;

        // No diagnostic if the alias is a valid identifier.
        if (SyntaxFacts.IsValidIdentifier(AliasName))
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), AliasName));
    }
}
