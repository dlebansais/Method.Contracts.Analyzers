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

    /// <summary>
    /// Checks whether an attribute can be ignored for analysis.
    /// </summary>
    /// <param name="attributeArgument">The attribute argument to check.</param>
    /// <param name="requireNotNullAttribute">The expected type.</param>
    /// <param name="argumentValue">The value of the argument upon return.</param>
    private protected static bool IsAttributeValidForRule(AttributeArgumentSyntax attributeArgument, string requireNotNullAttribute, out string argumentValue)
    {
        argumentValue = string.Empty;

        // No diagnostic if the argument is a parameter name.
        if (attributeArgument.NameEquals is not NameEqualsSyntax NameEquals)
            return false;

        string ArgumentName = NameEquals.Name.Identifier.Text;

        // No diagnostic if the argument is not right.
        if (ArgumentName != requireNotNullAttribute)
            return false;

        // No diagnostic if the argument is not a valid string or nameof.
        return ContractGenerator.IsStringOrNameofAttributeArgument(attributeArgument, out argumentValue);
    }
}
