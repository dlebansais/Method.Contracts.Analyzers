namespace Contracts.Analyzers;

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Analyzer for rule MCA1018: Verified property must be within type.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MCA1018VerifiedPropertyMustBeWithinType : PropertyDiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for this rule.
    /// </summary>
    public const string DiagnosticId = "MCA1018";

    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(AnalyzerResources.MCA1018AnalyzerTitle), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(AnalyzerResources.MCA1018AnalyzerMessageFormat), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
    private static readonly LocalizableString Description = new LocalizableResourceString(nameof(AnalyzerResources.MCA1018AnalyzerDescription), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
    private const string Category = "Usage";

    private static readonly DiagnosticDescriptor Rule = new(DiagnosticId,
                                                            Title,
                                                            MessageFormat,
                                                            Category,
                                                            DiagnosticSeverity.Warning,
                                                            isEnabledByDefault: true,
                                                            description: Description,
                                                            AnalyzerTools.GetHelpLink(DiagnosticId));

    /// <summary>
    /// Gets the list of supported diagnostic.
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <inheritdoc />
    private protected override IEnumerable<IAnalysisAssertion> Assertions { get; } =
    [
        new SimpleAnalysisAssertion(context => !IsPropertyWithinType((PropertyDeclarationSyntax)context.Node)),
        new SimpleAnalysisAssertion(context => ContractGenerator.GetFirstSupportedAttribute(context, (PropertyDeclarationSyntax)context.Node) is not null),
    ];

    private static bool IsPropertyWithinType(PropertyDeclarationSyntax propertyDeclaration)
    {
        return (propertyDeclaration.FirstAncestorOrSelf<ClassDeclarationSyntax>() is not null ||
                propertyDeclaration.FirstAncestorOrSelf<StructDeclarationSyntax>() is not null ||
                propertyDeclaration.FirstAncestorOrSelf<RecordDeclarationSyntax>() is not null) &&
               propertyDeclaration.FirstAncestorOrSelf<BaseNamespaceDeclarationSyntax>() is not null;
    }

    /// <inheritdoc />
    private protected override void AnalyzeVerifiedNode(SyntaxNodeAnalysisContext context, PropertyDeclarationSyntax propertyDeclaration, IAnalysisAssertion[] analysisAssertions)
    {
        string Text = propertyDeclaration.Identifier.ValueText;

        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), Text));
    }
}
