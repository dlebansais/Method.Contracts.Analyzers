namespace Contracts.Analyzers;

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Analyzer for rule MCA1017: Verified property must be private.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MCA1017VerifiedPropertyMustBePrivate : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for this rule.
    /// </summary>
    public const string DiagnosticId = "MCA1017";

    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(AnalyzerResources.MCA1017AnalyzerTitle), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(AnalyzerResources.MCA1017AnalyzerMessageFormat), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
    private static readonly LocalizableString Description = new LocalizableResourceString(nameof(AnalyzerResources.MCA1017AnalyzerDescription), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
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

    /// <summary>
    /// Initializes the rule analyzer.
    /// </summary>
    /// <param name="context">The analysis context.</param>
    public override void Initialize(AnalysisContext context)
    {
        context = Contract.AssertNotNull(context);

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.PropertyDeclaration);
    }

    private void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        AnalyzerTools.AssertSyntaxRequirements<PropertyDeclarationSyntax>(
            context,
            LanguageVersion.CSharp7,
            AnalyzeVerifiedNode,
            new SimpleAnalysisAssertion(context => ((PropertyDeclarationSyntax)context.Node).Identifier.ValueText != string.Empty),
            new SimpleAnalysisAssertion(context => !IsPropertyPrivate((PropertyDeclarationSyntax)context.Node)),
            new SimpleAnalysisAssertion(context => ContractGenerator.GetFirstSupportedAttribute(context, (PropertyDeclarationSyntax)context.Node) is not null));
    }

    private static bool IsPropertyPrivate(PropertyDeclarationSyntax propertyDeclaration)
    {
        return propertyDeclaration.Modifiers.All(modifier => !modifier.IsKind(SyntaxKind.ProtectedKeyword) &&
                                                             !modifier.IsKind(SyntaxKind.PublicKeyword) &&
                                                             !modifier.IsKind(SyntaxKind.InternalKeyword));
    }

    private void AnalyzeVerifiedNode(SyntaxNodeAnalysisContext context, PropertyDeclarationSyntax propertyDeclaration, IAnalysisAssertion[] analysisAssertions)
    {
        string Text = propertyDeclaration.Identifier.ValueText;

        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), Text));
    }
}
