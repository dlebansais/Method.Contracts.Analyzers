namespace Contracts.Analyzers;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Analyzer for rule MCA1006: RequireNotNull attribute argument must be a valid parameter name.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MCA1006RequireNotNullAttributeArgumentMustBeValidParameterName : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for this rule.
    /// </summary>
    public const string DiagnosticId = "MCA1006";

    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(AnalyzerResources.MCA1006AnalyzerTitle), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(AnalyzerResources.MCA1006AnalyzerMessageFormat), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
    private static readonly LocalizableString Description = new LocalizableResourceString(nameof(AnalyzerResources.MCA1006AnalyzerDescription), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
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

        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.AttributeArgument);
    }

    private void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        AnalyzerTools.AssertSyntaxRequirements<AttributeArgumentSyntax>(
            context,
            LanguageVersion.CSharp7,
            AnalyzeVerifiedNode,
            new WithinAttributeAnalysisAssertion<RequireNotNullAttribute>(),
            new WithinMethodAnalysisAssertion());
    }

    private void AnalyzeVerifiedNode(SyntaxNodeAnalysisContext context, AttributeArgumentSyntax attributeArgument, IAnalysisAssertion[] analysisAssertions)
    {
        // If we reached this step, there is a method declaration and an attribute.
        Contract.Assert(analysisAssertions.Length == 2);
        WithinAttributeAnalysisAssertion<RequireNotNullAttribute> FirstAssertion = Contract.AssertNotNull(analysisAssertions[0] as WithinAttributeAnalysisAssertion<RequireNotNullAttribute>);
        AttributeSyntax Attribute = Contract.AssertNotNull(FirstAssertion.AncestorAttribute);
        WithinMethodAnalysisAssertion SecondAssertion = Contract.AssertNotNull(analysisAssertions[1] as WithinMethodAnalysisAssertion);
        MethodDeclarationSyntax MethodDeclaration = Contract.AssertNotNull(SecondAssertion.AncestorMethodDeclaration);

        AttributeArgumentListSyntax ArgumentList = Contract.AssertNotNull(Attribute.ArgumentList);
        var AttributeArguments = ArgumentList.Arguments;
        int ArgumentIndex = AttributeArguments.IndexOf(attributeArgument);

        // No diagnostic if the attribute has an alias, type or name, and this is not the first argument.
        if (ContractGenerator.IsRequireNotNullAttributeWithAliasTypeOrName(AttributeArguments) && ArgumentIndex > 0)
            return;

        AttributeValidityCheckResult CheckResult = ContractGenerator.IsValidRequireNotNullAttribute(MethodDeclaration, AttributeArguments);

        // No diagnostic if the argument is a valid parameter name.
        if (CheckResult.Result == AttributeGeneration.Valid)
            return;

        // No diagnostic if the error is on another argument.
        if (CheckResult.PositionOfFirstInvalidArgument != ArgumentIndex)
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), ArgumentIndex));
    }
}
