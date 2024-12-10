namespace Contracts.Analyzers;

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Analyzer for rule MCA1020: Missing Dictionary Entry.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MCA1020MissingDictionaryEntry : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for this rule.
    /// </summary>
    public const string DiagnosticId = "MCA1020";

    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(AnalyzerResources.MCA1020AnalyzerTitle), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(AnalyzerResources.MCA1020AnalyzerMessageFormat), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
    private static readonly LocalizableString Description = new LocalizableResourceString(nameof(AnalyzerResources.MCA1020AnalyzerDescription), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
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
            new ContractMapInvocationAssertion());
    }

    private void AnalyzeVerifiedNode(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax objectCreationExpression, IAnalysisAssertion[] analysisAssertions)
    {
        // If we reached this step, there is an argument name.
        Contract.Assert(analysisAssertions.Length == 1);
        ContractMapInvocationAssertion Assertion = Contract.AssertNotNull(analysisAssertions.First() as ContractMapInvocationAssertion);
        ExpressionSyntax KeyExpression = Contract.AssertNotNull(Assertion.KeyExpression);
        ExpressionSyntax DictionaryExpression = Contract.AssertNotNull(Assertion.DictionaryExpression);

        SymbolInfo KeySymbolInfo = context.SemanticModel.GetSymbolInfo(KeyExpression);
        ISymbol KeySymbol = Contract.AssertNotNull(KeySymbolInfo.Symbol);
        INamedTypeSymbol KeyType = KeySymbol.ContainingType;
        int KeyCount = KeyType.MemberNames.Count();

        if (DictionaryExpression is not ObjectCreationExpressionSyntax ObjectCreationExpression)
            return;

        if (ObjectCreationExpression.ArgumentList is ArgumentListSyntax ArgumentList)
        {
            if (ArgumentList.Arguments.Count != 0)
                return;
        }

        if (ObjectCreationExpression.Initializer is not InitializerExpressionSyntax InitializerExpression)
            return;

        int EntryCount = InitializerExpression.Expressions.Count;
        if (KeyCount == EntryCount)
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation()));
    }
}
