namespace Contracts.Analyzers;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Analyzer for rule MCA1002: Verified method must be within type.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MCA1002VerifiedMethodMustBeWithinType : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for this rule.
    /// </summary>
    public const string DiagnosticId = "MCA1002";

    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(AnalyzerResources.MCA1002AnalyzerTitle), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(AnalyzerResources.MCA1002AnalyzerMessageFormat), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
    private static readonly LocalizableString Description = new LocalizableResourceString(nameof(AnalyzerResources.MCA1002AnalyzerDescription), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
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

        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.MethodDeclaration);
    }

    private void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        AnalyzerTools.AssertSyntaxRequirements<MethodDeclarationSyntax>(
            context,
            LanguageVersion.CSharp7,
            AnalyzeVerifiedNode,
            new SimpleAnalysisAssertion(context => !IsMethodWithinType((MethodDeclarationSyntax)context.Node)),
            new SimpleAnalysisAssertion(context => ContractGenerator.GetFirstSupportedAttribute(context, (MethodDeclarationSyntax)context.Node) is not null));
    }

    private static bool IsMethodWithinType(MethodDeclarationSyntax methodDeclaration)
    {
        return (methodDeclaration.FirstAncestorOrSelf<ClassDeclarationSyntax>() is not null ||
                methodDeclaration.FirstAncestorOrSelf<StructDeclarationSyntax>() is not null ||
                methodDeclaration.FirstAncestorOrSelf<RecordDeclarationSyntax>() is not null) &&
               methodDeclaration.FirstAncestorOrSelf<BaseNamespaceDeclarationSyntax>() is not null;
    }

    private void AnalyzeVerifiedNode(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration, IAnalysisAssertion[] analysisAssertions)
    {
        string Text = methodDeclaration.Identifier.ValueText;

        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), Text));
    }
}
