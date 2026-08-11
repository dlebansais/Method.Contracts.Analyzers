namespace Contracts.Analyzers;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Represents the results of an assertion.
/// </summary>
/// <typeparam name="TResult">The result type.</typeparam>
internal class AssertionResults<TResult>
{
    private readonly Dictionary<SyntaxNodeAnalysisContext, (TimeSpan, TResult)> Results = [];
    private static readonly Stopwatch RecordingWatch = Stopwatch.StartNew();

    /// <summary>
    /// Adds a result.
    /// </summary>
    /// <param name="context">The assertion context.</param>
    /// <param name="result">The result.</param>
    public void Add(SyntaxNodeAnalysisContext context, TResult result)
        => Results.Add(context, (RecordingWatch.Elapsed, result));

    /// <summary>
    /// Gets a result.
    /// </summary>
    /// <param name="context">The assertion context.</param>
    public TResult Get(SyntaxNodeAnalysisContext context)
    {
        Contract.Assert(Results.ContainsKey(context));
        (_, TResult result) = Results[context];

        CleanupResults();

        return result;
    }

    private void CleanupResults()
    {
        List<SyntaxNodeAnalysisContext> LostContexts = [];
        foreach (KeyValuePair<SyntaxNodeAnalysisContext, (TimeSpan, TResult)> Entry in Results)
        {
            (TimeSpan addedTime, _) = Entry.Value;
            if (RecordingWatch.Elapsed - addedTime >= TimeSpan.FromSeconds(1))
                LostContexts.Add(Entry.Key);
        }

        foreach (SyntaxNodeAnalysisContext context in LostContexts)
            _ = Results.Remove(context);
    }
}
