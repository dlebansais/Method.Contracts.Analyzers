namespace Contracts.Analyzers;

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

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

        if (!AnalyzerTools.IsInvocationOfContract(context, InvocationExpression, nameof(Contract.Unused), out List<ArgumentSyntax> Arguments))
            return false;

        // If NameSymbol is the right symbol, there is exactly one argument and it's 'out' something.
        Contract.Assert(Arguments.Count == 1);
        ArgumentSyntax Argument = Arguments[0];
        Contract.Assert(Argument.RefKindKeyword.IsKind(SyntaxKind.OutKeyword));

        if (Argument.Expression is not IdentifierNameSyntax IdentifierName)
            return false;

        InvocationStatement = ExpressionStatement;
        ArgumentIdentifierName = IdentifierName;

        return true;
    }
}
