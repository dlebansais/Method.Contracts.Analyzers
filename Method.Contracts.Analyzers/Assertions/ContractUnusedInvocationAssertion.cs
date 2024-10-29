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
    public string? ArgumentName { get; private set; }

    /// <summary>
    /// Gets the list of remaining statements if true.
    /// </summary>
    public List<StatementSyntax> RemainingStatements { get; } = new();

    /// <inheritdoc cref="IAnalysisAssertion.IsTrue(SyntaxNodeAnalysisContext)" />
    public bool IsTrue(SyntaxNodeAnalysisContext context)
    {
        InvocationExpressionSyntax InvocationExpression = (InvocationExpressionSyntax)context.Node;

        if (InvocationExpression.Parent is not ExpressionStatementSyntax ExpressionStatement)
            return false;

        if (!AnalyzerTools.IsInvocationOfContractUnused(context, ExpressionStatement, out string InvocationArgumentName))
            return false;

        SyntaxList<StatementSyntax> ParentList;

        if (ExpressionStatement.Parent is BlockSyntax Block)
            ParentList = Block.Statements;
        else if (ExpressionStatement.Parent is SwitchSectionSyntax SwitchSection)
            ParentList = SwitchSection.Statements;
        else
            return false;

        InvocationStatement = ExpressionStatement;
        ArgumentName = InvocationArgumentName;

        int StatementIndex = ParentList.IndexOf(ExpressionStatement);
        Contract.Assert(StatementIndex >= 0);
        Contract.Assert(StatementIndex < ParentList.Count);

        for (int i = StatementIndex + 1; i < ParentList.Count; i++)
            RemainingStatements.Add(ParentList[i]);

        return true;
    }
}
