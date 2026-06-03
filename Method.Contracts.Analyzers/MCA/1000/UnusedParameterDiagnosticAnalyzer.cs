namespace Contracts.Analyzers;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Analyzer for unused parameters.
/// </summary>
public abstract class UnusedParameterDiagnosticAnalyzer : DiagnosticAnalyzer
{
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
            new ContractUnusedInvocationAssertion());
    }

    /// <summary>
    /// Analyzes the verified node.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="objectCreationExpression">The expression no to analyze.</param>
    /// <param name="analysisAssertions">The analysis assertions.</param>
    private protected abstract void AnalyzeVerifiedNode(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax objectCreationExpression, IAnalysisAssertion[] analysisAssertions);
}
