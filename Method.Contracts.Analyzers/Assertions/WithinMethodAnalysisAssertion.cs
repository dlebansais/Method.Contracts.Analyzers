namespace Contracts.Analyzers;

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Represents an analysis assertion that check whether there is an ancestor method.
/// </summary>
internal class WithinMethodAnalysisAssertion : IAnalysisAssertion
{
    /// <inheritdoc cref="IAnalysisAssertion.IsTrue(SyntaxNodeAnalysisContext)" />
    public bool IsTrue(SyntaxNodeAnalysisContext context)
    {
        AttributeArgumentSyntax AttributeArgument = (AttributeArgumentSyntax)context.Node;

        return AttributeArgument.FirstAncestorOrSelf<MethodDeclarationSyntax>() is not null;
    }
}
