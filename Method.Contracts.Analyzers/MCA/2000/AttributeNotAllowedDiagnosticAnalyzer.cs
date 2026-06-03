namespace Contracts.Analyzers;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Analyzer for attributes not allowed.
/// </summary>
public abstract class AttributeNotAllowedDiagnosticAnalyzer : DiagnosticAnalyzer
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

        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.Attribute);
    }

    private void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        AnalyzerTools.AssertSyntaxRequirements<AttributeSyntax>(
            context,
            LanguageVersion.CSharp7,
            AnalyzeVerifiedNode,
            new SimpleAnalysisAssertion(context => IsClassAttribute(context, (AttributeSyntax)context.Node)));
    }

    private static bool IsClassAttribute(SyntaxNodeAnalysisContext context, AttributeSyntax attribute)
    {
        return AnalyzerTools.IsExpectedAttribute<InitializeWithAttribute>(context, attribute) &&
               attribute.FirstAncestorOrSelf<ConstructorDeclarationSyntax>() is null;
    }

    /// <summary>
    /// Analyzes the verified node.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="attribute">The attribute.</param>
    /// <param name="analysisAssertions">The analysis assertions.</param>
    private protected abstract void AnalyzeVerifiedNode(SyntaxNodeAnalysisContext context, AttributeSyntax attribute, IAnalysisAssertion[] analysisAssertions);
}
