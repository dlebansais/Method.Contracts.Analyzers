namespace Contracts.Analyzers;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

/// <summary>
/// Represents an analysis assertion that checks if an invocation is a call to Contract.Unused.
/// </summary>
internal class ContractUnusedInvocationAssertion : IAnalysisAssertion
{
    /// <summary>
    /// Gets the statement containing the invocation.
    /// </summary>
    public StatementSyntax? InvocationStatement { get; private set; }

    /// <summary>
    /// Gets the argument in the call to Contract.Unused.
    /// </summary>
    public IdentifierNameSyntax? ArgumentIdentifierName { get; private set; }

    /// <inheritdoc cref="IAnalysisAssertion.IsTrue(SyntaxNodeAnalysisContext)" />
    public bool IsTrue(SyntaxNodeAnalysisContext context)
    {
        InvocationExpressionSyntax InvocationExpression = (InvocationExpressionSyntax)context.Node;

        if (InvocationExpression.Parent is not ExpressionStatementSyntax ExpressionStatement)
            return false;

        if (!AnalyzerTools.IsInvocationOfContractUnused(context, ExpressionStatement, out IdentifierNameSyntax IdentifierName))
            return false;

        InvocationStatement = ExpressionStatement;
        ArgumentIdentifierName = IdentifierName;

        return true;
    }
}
