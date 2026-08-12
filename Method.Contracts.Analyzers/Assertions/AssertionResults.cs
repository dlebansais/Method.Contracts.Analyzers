namespace Contracts.Analyzers;

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Represents the results of an assertion.
/// </summary>
/// <typeparam name="TResult">The result type.</typeparam>
internal class AssertionResults<TResult>
{
    private readonly Dictionary<SyntaxNodeAnalysisContext, TResult> Results = [];

    /// <summary>
    /// Adds a result.
    /// </summary>
    /// <param name="context">The assertion context.</param>
    /// <param name="result">The result.</param>
    public void Add(SyntaxNodeAnalysisContext context, TResult result)
        => Results.Add(context, result);

    /// <summary>
    /// Gets a result.
    /// </summary>
    /// <param name="context">The assertion context.</param>
    public TResult Get(SyntaxNodeAnalysisContext context)
    {
        TResult result = Results[context];
        _ = Results.Remove(context);

        return result;
    }
}
