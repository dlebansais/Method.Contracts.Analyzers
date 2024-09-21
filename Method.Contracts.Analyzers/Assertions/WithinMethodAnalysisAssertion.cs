namespace Contracts.Analyzers;

using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Represents an analysis assertion that check whether there is an ancestor method.
/// </summary>
internal class WithinMethodAnalysisAssertion : IAnalysisAssertion
{
    /// <summary>
    /// Gets the ancestor method if the assertion is true.
    /// </summary>
    public MethodDeclarationSyntax? AncestorMethodDeclaration { get; private set; }

    /// <inheritdoc />
    public bool IsTrue(SyntaxNodeAnalysisContext context)
    {
        AttributeArgumentSyntax AttributeArgument = (AttributeArgumentSyntax)context.Node;

        if (AttributeArgument.FirstAncestorOrSelf<MethodDeclarationSyntax>() is not MethodDeclarationSyntax MethodDeclaration)
            return false;

        AncestorMethodDeclaration = MethodDeclaration;
        return true;
    }
}
