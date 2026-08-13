namespace Contracts.Analyzers;

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Represents an analysis assertion that checks if an invocation is a call to Contract.Map.
/// </summary>
internal class ContractMapInvocationAssertion : IAnalysisAssertion
{
    private readonly Dictionary<InvocationExpressionSyntax, (ExpressionSyntax, ExpressionSyntax)> ExpressionsTable = [];

    /// <inheritdoc cref="IAnalysisAssertion.IsTrue(SyntaxNodeAnalysisContext)" />
    public bool IsTrue(SyntaxNodeAnalysisContext context)
    {
        InvocationExpressionSyntax InvocationExpression = (InvocationExpressionSyntax)context.Node;

        if (!AnalyzerTools.IsInvocationOfContract(context, InvocationExpression, nameof(Contract.Map), out List<ArgumentSyntax> Arguments) &&
            !AnalyzerTools.IsInvocationOfContract(context, InvocationExpression, nameof(Contract.MapAsync), out Arguments))
        {
            return false;
        }

        // If NameSymbol is the right symbol, there are exactly two arguments.
        Contract.Assert(Arguments.Count == 2);
        ArgumentSyntax FirstArgument = Arguments[0];
        ArgumentSyntax SecondArgument = Arguments[1];

        ExpressionsTable.Add(InvocationExpression, (FirstArgument.Expression, SecondArgument.Expression));
        return true;
    }

    /// <summary>
    /// Gets the two argument expressions for an object creation expression that successfuly passed the analysis.
    /// </summary>
    /// <param name="invocationExpression">The invocation expression.</param>
    public (ExpressionSyntax FirstArgument, ExpressionSyntax SecondArgument) GetInvocationArgumentExpressions(InvocationExpressionSyntax invocationExpression)
    {
        (ExpressionSyntax, ExpressionSyntax) Result = ExpressionsTable[invocationExpression];
        _ = ExpressionsTable.Remove(invocationExpression);

        return Result;
    }
}
