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
    private readonly Dictionary<InvocationExpressionSyntax, (StatementSyntax, IdentifierNameSyntax)> InvocationTable = [];

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

        InvocationTable.Add(InvocationExpression, (ExpressionStatement, IdentifierName));
        return true;
    }

    /// <summary>
    /// Gets the statement and identifier name associated with a successful invocation expression analysis.
    /// </summary>
    /// <param name="invocationExpression">The invocation expression.</param>
    public (StatementSyntax ExpressionStatement, IdentifierNameSyntax IdentifierName) GetStatement(InvocationExpressionSyntax invocationExpression)
    {
        (StatementSyntax, IdentifierNameSyntax) Result = InvocationTable[invocationExpression];
        _ = InvocationTable.Remove(invocationExpression);

        return Result;
    }
}
