namespace Contracts.Analyzers;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Analyzer for rule MCA1009: RequireNotNull attribute uses invalid type.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MCA1009RequireNotNullAttributeUsesInvalidType : InvalidUseOfAttributeDiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for this rule.
    /// </summary>
    public const string DiagnosticId = "MCA1009";

    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(AnalyzerResources.MCA1009AnalyzerTitle), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(AnalyzerResources.MCA1009AnalyzerMessageFormat), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
    private static readonly LocalizableString Description = new LocalizableResourceString(nameof(AnalyzerResources.MCA1009AnalyzerDescription), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
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
        if (!IsAttributeValidForRule(attributeArgument, nameof(RequireNotNullAttribute.Type), out string ArgumentValue))
            return;

        string TypeName = ArgumentValue;

        // No diagnostic if the type is a valid identifier.
        if (ContractGenerator.IsValidTypeName(TypeName))
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), TypeName));
    }
}
