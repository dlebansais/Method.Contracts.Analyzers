namespace Contracts.Analyzers;

using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Represents an analysis assertion that check whether there is an ancestor attribute.
/// </summary>
/// <typeparam name="T">The attribute type.</typeparam>
internal class WithinAttributeAnalysisAssertion<T> : IAnalysisAssertion
    where T : Attribute
{
    /// <summary>
    /// Gets the ancestor attribute if the assertion is true.
    /// </summary>
    public AttributeSyntax? AncestorAttribute { get; private set; }

    /// <inheritdoc />
    public bool IsTrue(SyntaxNodeAnalysisContext context)
    {
        AttributeArgumentSyntax AttributeArgument = (AttributeArgumentSyntax)context.Node;
        AttributeSyntax? Attribute = AttributeArgument.FirstAncestorOrSelf<AttributeSyntax>();

        if (!AnalyzerTools.IsExpectedAttribute<T>(context, Attribute))
            return false;

        AncestorAttribute = Attribute;
        return true;
    }
}
