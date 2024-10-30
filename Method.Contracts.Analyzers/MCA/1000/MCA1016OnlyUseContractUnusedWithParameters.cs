namespace Contracts.Analyzers;

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Analyzer for rule MCA1016: Only use Contract.Unused with parameters.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MCA1016OnlyUseContractUnusedWithParameters : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for this rule.
    /// </summary>
    public const string DiagnosticId = "MCA1016";

    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(AnalyzerResources.MCA1016AnalyzerTitle), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(AnalyzerResources.MCA1016AnalyzerMessageFormat), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
    private static readonly LocalizableString Description = new LocalizableResourceString(nameof(AnalyzerResources.MCA1016AnalyzerDescription), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
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
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return [Rule]; } }

    /// <summary>
    /// Initializes the rule analyzer.
    /// </summary>
    /// <param name="context">The analysis context.</param>
    public override void Initialize(AnalysisContext context)
    {
        context = Contract.AssertNotNull(context);

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.InvocationExpression);
    }

    private void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        AnalyzerTools.AssertSyntaxRequirements<InvocationExpressionSyntax>(
            context,
            LanguageVersion.CSharp7,
            AnalyzeVerifiedNode,
            new ContractUnusedInvocationAssertion());
    }

    private void AnalyzeVerifiedNode(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax objectCreationExpression, IAnalysisAssertion[] analysisAssertions)
    {
        // If we reached this step, there is an argument name.
        Contract.Assert(analysisAssertions.Length == 1);
        ContractUnusedInvocationAssertion Assertion = Contract.AssertNotNull(analysisAssertions.First() as ContractUnusedInvocationAssertion);
        IdentifierNameSyntax ArgumentIdentifierName = Contract.AssertNotNull(Assertion.ArgumentIdentifierName);
        string ArgumentName = ArgumentIdentifierName.Identifier.Text;

        // No diagnostic if the argument is a parameter.
        SymbolInfo ParameterSymbolInfo = context.SemanticModel.GetSymbolInfo(ArgumentIdentifierName);
        if (ParameterSymbolInfo.Symbol is IParameterSymbol)
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), ArgumentName));
    }
}
