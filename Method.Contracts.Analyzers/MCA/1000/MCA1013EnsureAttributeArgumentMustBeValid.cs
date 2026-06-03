namespace Contracts.Analyzers;

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Analyzer for rule MCA1013: Ensure attribute argument must be valid.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MCA1013EnsureAttributeArgumentMustBeValid : AttributeDiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for this rule.
    /// </summary>
    public const string DiagnosticId = "MCA1013";

    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(AnalyzerResources.MCA1013AnalyzerTitle), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(AnalyzerResources.MCA1013AnalyzerMessageFormat), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
    private static readonly LocalizableString Description = new LocalizableResourceString(nameof(AnalyzerResources.MCA1013AnalyzerDescription), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
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
        new WithinAttributeAnalysisAssertion<EnsureAttribute>(),
        new WithinMethodAnalysisAssertion(),
    ];

    /// <inheritdoc />
    private protected override void AnalyzeVerifiedNode(SyntaxNodeAnalysisContext context, AttributeArgumentSyntax attributeArgument, IAnalysisAssertion[] analysisAssertions)
    {
        // If we reached this step, there is a method declaration and an attribute.
        Contract.Assert(analysisAssertions.Length == 2);
        WithinAttributeAnalysisAssertion<EnsureAttribute> FirstAssertion = Contract.AssertNotNull(analysisAssertions[0] as WithinAttributeAnalysisAssertion<EnsureAttribute>);
        AttributeSyntax Attribute = Contract.AssertNotNull(FirstAssertion.AncestorAttribute);
        WithinMethodAnalysisAssertion SecondAssertion = Contract.AssertNotNull(analysisAssertions[1] as WithinMethodAnalysisAssertion);
        MethodDeclarationSyntax MethodDeclaration = Contract.AssertNotNull(SecondAssertion.AncestorMethodDeclaration);

        AttributeArgumentListSyntax ArgumentList = Contract.AssertNotNull(Attribute.ArgumentList);
        SeparatedSyntaxList<AttributeArgumentSyntax> AttributeArguments = ArgumentList.Arguments;
        int ArgumentIndex = AttributeArguments.IndexOf(attributeArgument);

        AttributeValidityCheckResult CheckResult = ContractGenerator.IsValidEnsureAttribute(MethodDeclaration, AttributeArguments);

        // No diagnostic if the argument is a valid expression or if the error is on another argument.
        if (CheckResult.PositionOfFirstInvalidArgument != ArgumentIndex)
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), ArgumentIndex));
    }
}
