namespace Contracts.Analyzers;

using System.Collections.Immutable;
using Contracts.Analyzers.Helper;
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

        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.AttributeArgument);
    }

    private void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        AnalyzerTools.AssertSyntaxRequirements<AttributeArgumentSyntax>(
            context,
            LanguageVersion.CSharp7,
            AnalyzeVerifiedNode,
            new SimpleAnalysisAssertion(context => IsRequireNotNullAttribute(((AttributeArgumentSyntax)context.Node).FirstAncestorOrSelf<AttributeSyntax>())),
            new SimpleAnalysisAssertion(context => ((AttributeArgumentSyntax)context.Node).FirstAncestorOrSelf<MethodDeclarationSyntax>() is not null));
    }

    private static bool IsRequireNotNullAttribute(AttributeSyntax? attribute)
    {
        // There must be a parent attribute to any argument except in the most pathological cases.
        Contract.RequireNotNull(attribute, out AttributeSyntax Attribute);
        return GeneratorHelper.ToAttributeName(Attribute) == nameof(RequireNotNullAttribute);
    }

    private void AnalyzeVerifiedNode(SyntaxNodeAnalysisContext context, AttributeArgumentSyntax attributeArgument, IAnalysisAssertion[] analysisAssertions)
    {
        // If we reached this step, there is a method declaration and an attribute.
        MethodDeclarationSyntax MethodDeclaration = Contract.AssertNotNull(attributeArgument.FirstAncestorOrSelf<MethodDeclarationSyntax>());
        AttributeSyntax Attribute = Contract.AssertNotNull(attributeArgument.FirstAncestorOrSelf<AttributeSyntax>());
        AttributeArgumentListSyntax ArgumentList = Contract.AssertNotNull(Attribute.ArgumentList);
        var AttributeArguments = ArgumentList.Arguments;

        // No diagnostic if the attribute has an alias, type or name, and this is not the first argument.
        if (ContractGenerator.IsRequireNotNullAttributeWithAliasTypeOrName(AttributeArguments))
        {
            int ArgumentIndex = AttributeArguments.IndexOf(attributeArgument);
            if (ArgumentIndex > 0)
                return;
        }

        // No diagnostic if the argument is a valid parameter name.
        AttributeValidityCheckResult CheckResult = ContractGenerator.IsValidRequireNotNullAttribute(MethodDeclaration, [attributeArgument]);
        if (CheckResult.Result == AttributeGeneration.Valid)
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), CheckResult.PositionOfFirstInvalidArgument));
    }
}
