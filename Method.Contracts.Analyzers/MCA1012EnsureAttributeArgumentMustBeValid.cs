﻿namespace Contracts.Analyzers;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Analyzer for rule MCA1012: Ensure attribute argument must be valid.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MCA1012EnsureAttributeArgumentMustBeValid : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for this rule.
    /// </summary>
    public const string DiagnosticId = "MCA1012";

    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(AnalyzerResources.MCA1012AnalyzerTitle), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(AnalyzerResources.MCA1012AnalyzerMessageFormat), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
    private static readonly LocalizableString Description = new LocalizableResourceString(nameof(AnalyzerResources.MCA1012AnalyzerDescription), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
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
            new SimpleAnalysisAssertion(context => AnalyzerTools.IsExpectedAttribute<EnsureAttribute>(((AttributeArgumentSyntax)context.Node).FirstAncestorOrSelf<AttributeSyntax>())),
            new SimpleAnalysisAssertion(context => ((AttributeArgumentSyntax)context.Node).FirstAncestorOrSelf<MethodDeclarationSyntax>() is not null));
    }

    private void AnalyzeVerifiedNode(SyntaxNodeAnalysisContext context, AttributeArgumentSyntax attributeArgument, IAnalysisAssertion[] analysisAssertions)
    {
        // If we reached this step, there is a method declaration and an attribute.
        MethodDeclarationSyntax MethodDeclaration = Contract.AssertNotNull(attributeArgument.FirstAncestorOrSelf<MethodDeclarationSyntax>());
        AttributeSyntax Attribute = Contract.AssertNotNull(attributeArgument.FirstAncestorOrSelf<AttributeSyntax>());
        AttributeArgumentListSyntax ArgumentList = Contract.AssertNotNull(Attribute.ArgumentList);
        var AttributeArguments = ArgumentList.Arguments;
        int ArgumentIndex = AttributeArguments.IndexOf(attributeArgument);

        // No diagnostic if the attribute has DebugOnly, and this is not the first argument.
        if (ContractGenerator.IsRequireOrEnsureAttributeWithDebugOnly(AttributeArguments) && ArgumentIndex > 0)
            return;

        AttributeValidityCheckResult CheckResult = ContractGenerator.IsValidEnsureAttribute(MethodDeclaration, AttributeArguments);

        // No diagnostic if the argument is a valid expression.
        if (CheckResult.Result == AttributeGeneration.Valid)
            return;

        // No diagnostic if the error is on another argument.
        if (CheckResult.PositionOfFirstInvalidArgument != ArgumentIndex)
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), ArgumentIndex));
    }
}
