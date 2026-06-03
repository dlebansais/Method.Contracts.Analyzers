namespace Contracts.Analyzers;

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Analyzer for rule MCA1005: Access attribute argument must be a valid modifier.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MCA1005AccessAttributeArgumentMustBeValidModifier : AttributeDiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for this rule.
    /// </summary>
    public const string DiagnosticId = "MCA1005";

    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(AnalyzerResources.MCA1005AnalyzerTitle), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(AnalyzerResources.MCA1005AnalyzerMessageFormat), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
    private static readonly LocalizableString Description = new LocalizableResourceString(nameof(AnalyzerResources.MCA1005AnalyzerDescription), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
    private const string Category = "Usage";

    private static readonly DiagnosticDescriptor Rule = new(DiagnosticId,
                                                            Title,
                                                            MessageFormat,
                                                            Category,
                                                            DiagnosticSeverity.Error,
                                                            isEnabledByDefault: true,
                                                            description: Description,
                                                            AnalyzerTools.GetHelpLink(DiagnosticId));

    /// <summary>
    /// Gets the list of supported diagnostic.
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <inheritdoc />
    private protected override IEnumerable<IAnalysisAssertion> Assertions { get; } =
    [
        new WithinAttributeAnalysisAssertion<AccessAttribute>(),
        new WithinMethodAnalysisAssertion(),
    ];

    /// <inheritdoc />
    private protected override void AnalyzeVerifiedNode(SyntaxNodeAnalysisContext context, AttributeArgumentSyntax attributeArgument, IAnalysisAssertion[] analysisAssertions)
    {
        // If we reached this step, there is a method declaration.
        Contract.Assert(analysisAssertions.Length == 2);
        WithinMethodAnalysisAssertion SecondAssertion = Contract.AssertNotNull(analysisAssertions[1] as WithinMethodAnalysisAssertion);
        MethodDeclarationSyntax MethodDeclaration = Contract.AssertNotNull(SecondAssertion.AncestorMethodDeclaration);

        // No diagnostic if the argument is a valid modifier.
        AttributeValidityCheckResult CheckResult = ContractGenerator.IsValidAccessAttribute(MethodDeclaration, [attributeArgument]);
        if (CheckResult.Result == AttributeGeneration.Valid)
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), CheckResult.PositionOfFirstInvalidArgument));
    }
}
