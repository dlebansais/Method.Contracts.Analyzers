namespace Contracts.Analyzers;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Analyzer for invalid use of attribute.
/// </summary>
public abstract class InvalidUseOfAttributeDiagnosticAnalyzer : DiagnosticAnalyzer
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

        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.AttributeArgument);
    }

    private void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        AnalyzerTools.AssertSyntaxRequirements<AttributeArgumentSyntax>(
            context,
            LanguageVersion.CSharp7,
            AnalyzeVerifiedNode,
            new WithinAttributeAnalysisAssertion<RequireNotNullAttribute>());
    }

    /// <summary>
    /// Analyzes the verified node.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="syntax">The attribute syntax.</param>
    /// <param name="arg3">The assertions.</param>
    private protected abstract void AnalyzeVerifiedNode(SyntaxNodeAnalysisContext context, AttributeArgumentSyntax syntax, IAnalysisAssertion[] arg3);
}
