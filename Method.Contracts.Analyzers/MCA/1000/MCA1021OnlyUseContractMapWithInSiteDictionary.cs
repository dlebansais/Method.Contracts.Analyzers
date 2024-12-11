namespace Contracts.Analyzers;

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Analyzer for rule MCA1021: Only use Contract.Map with in-site dictionary.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MCA1021OnlyUseContractMapWithInSiteDictionary : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for this rule.
    /// </summary>
    public const string DiagnosticId = "MCA1021";

    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(AnalyzerResources.MCA1021AnalyzerTitle), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(AnalyzerResources.MCA1021AnalyzerMessageFormat), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
    private static readonly LocalizableString Description = new LocalizableResourceString(nameof(AnalyzerResources.MCA1021AnalyzerDescription), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
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

        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.InvocationExpression);
    }

    private void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        AnalyzerTools.AssertSyntaxRequirements<InvocationExpressionSyntax>(
            context,
            LanguageVersion.CSharp7,
            AnalyzeVerifiedNode,
            new ContractMapInvocationAssertion());
    }

    private void AnalyzeVerifiedNode(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax objectCreationExpression, IAnalysisAssertion[] analysisAssertions)
    {
        // If we reached this step, there is an argument name.
        Contract.Assert(analysisAssertions.Length == 1);
        ContractMapInvocationAssertion Assertion = Contract.AssertNotNull(analysisAssertions.First() as ContractMapInvocationAssertion);
        ExpressionSyntax DictionaryExpression = Contract.AssertNotNull(Assertion.DictionaryExpression);

        if (DictionaryExpression is ObjectCreationExpressionSyntax)
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, DictionaryExpression.GetLocation()));
    }
}
