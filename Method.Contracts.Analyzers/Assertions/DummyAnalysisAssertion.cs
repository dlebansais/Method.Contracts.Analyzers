namespace Contracts.Analyzers;

using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Represents a dummy analysis assertion.
/// Used to create assertion results that must be eliminated some time in the future.
/// </summary>
internal class DummyAnalysisAssertion() : IAnalysisAssertion
{
    /// <summary>
    /// Gets the dummy object.
    /// </summary>
    public static AssertionResults<object> Dummy { get; } = new();

    /// <inheritdoc />
    public bool IsTrue(SyntaxNodeAnalysisContext context)
    {
        Dummy.Add(context, new());
        return true;
    }
}
