namespace Contracts.Analyzers;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Analyzer for rule MCA1015: Set parameter as unused before return.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MCA1015SetParameterAsUnusedBeforeReturn : UnusedParameterDiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for this rule.
    /// </summary>
    public const string DiagnosticId = "MCA1015";

    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(AnalyzerResources.MCA1015AnalyzerTitle), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(AnalyzerResources.MCA1015AnalyzerMessageFormat), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
    private static readonly LocalizableString Description = new LocalizableResourceString(nameof(AnalyzerResources.MCA1015AnalyzerDescription), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
    private const string Category = "Usage";

    private static readonly DiagnosticDescriptor Rule = new(DiagnosticId,
                                                            Title,
                                                            MessageFormat,
                                                            Category,
                                                            DiagnosticSeverity.Warning,
                                                            isEnabledByDefault: true,
                                                            description: Description,
                                                            AnalyzerTools.GetHelpLink(DiagnosticId));

    /// <summary>
    /// Gets the list of supported diagnostic.
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <inheritdoc />
    private protected override void AnalyzeVerifiedNode(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax objectCreationExpression, IAnalysisAssertion[] analysisAssertions)
    {
        // If we reached this step, there is an argument name.
        Contract.Assert(analysisAssertions.Length == 1);
        ContractUnusedInvocationAssertion Assertion = Contract.AssertNotNull(analysisAssertions[0] as ContractUnusedInvocationAssertion);
        IdentifierNameSyntax ArgumentIdentifierName = Contract.AssertNotNull(Assertion.ArgumentIdentifierName);
        StatementSyntax InvocationStatement = Contract.AssertNotNull(Assertion.InvocationStatement);
        string ArgumentName = ArgumentIdentifierName.Identifier.Text;

        List<StatementSyntax> RemainingStatements = AnalyzerTools.FindSubsequentStatements(InvocationStatement);
        bool IsFollowedByOtherStatement = RemainingStatements.Any(statement => !AnalyzerTools.IsInvocationOfContract(context, statement, nameof(Contract.Unused), out _));

        // No diagnostic if the statement is only followed by other invocations of Contract.Unused() or a return.
        if (!IsFollowedByOtherStatement)
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), ArgumentName));
    }
}
