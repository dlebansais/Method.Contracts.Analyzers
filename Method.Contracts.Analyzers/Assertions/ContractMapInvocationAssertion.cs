namespace Contracts.Analyzers;

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Represents an analysis assertion that checks if an invocation is a call to Contract.Map.
/// </summary>
internal class ContractMapInvocationAssertion : IAnalysisAssertion
{
    /// <summary>
    /// Gets the key argument in the call to Contract.Map.
    /// </summary>
    public ExpressionSyntax? KeyExpression { get; private set; }

    /// <summary>
    /// Gets the dictionary argument in the call to Contract.Map.
    /// </summary>
    public ExpressionSyntax? DictionaryExpression { get; private set; }

    /// <inheritdoc cref="IAnalysisAssertion.IsTrue(SyntaxNodeAnalysisContext)" />
    public bool IsTrue(SyntaxNodeAnalysisContext context)
    {
        InvocationExpressionSyntax InvocationExpression = (InvocationExpressionSyntax)context.Node;

        if (!AnalyzerTools.IsInvocationOfContract(context, InvocationExpression, nameof(Contract.Map), out List<ArgumentSyntax> Arguments))
            return false;

        // If NameSymbol is the right symbol, there are exactly two arguments.
        Contract.Assert(Arguments.Count == 2);
        ArgumentSyntax FirstArgument = Arguments[0];
        ArgumentSyntax SecondArgument = Arguments[1];

        KeyExpression = FirstArgument.Expression;
        DictionaryExpression = SecondArgument.Expression;

        return true;
    }
}
