namespace Contracts.Analyzers;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Analyzer for rule MCA1010: RequireNotNull attribute uses invalid name.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MCA1010RequireNotNullAttributeUsesInvalidName : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for this rule.
    /// </summary>
    public const string DiagnosticId = "MCA1010";

    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(AnalyzerResources.MCA1010AnalyzerTitle), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(AnalyzerResources.MCA1010AnalyzerMessageFormat), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
    private static readonly LocalizableString Description = new LocalizableResourceString(nameof(AnalyzerResources.MCA1010AnalyzerDescription), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
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
            new SimpleAnalysisAssertion(context => AnalyzerTools.IsExpectedAttribute<RequireNotNullAttribute>(((AttributeArgumentSyntax)context.Node).FirstAncestorOrSelf<AttributeSyntax>())),
            new SimpleAnalysisAssertion(context => ((AttributeArgumentSyntax)context.Node).FirstAncestorOrSelf<MethodDeclarationSyntax>() is not null));
    }

    private void AnalyzeVerifiedNode(SyntaxNodeAnalysisContext context, AttributeArgumentSyntax attributeArgument, IAnalysisAssertion[] analysisAssertions)
    {
        // If we reached this step, there is a method declaration and an attribute.
        MethodDeclarationSyntax MethodDeclaration = Contract.AssertNotNull(attributeArgument.FirstAncestorOrSelf<MethodDeclarationSyntax>());
        AttributeSyntax Attribute = Contract.AssertNotNull(attributeArgument.FirstAncestorOrSelf<AttributeSyntax>());
        AttributeArgumentListSyntax ArgumentList = Contract.AssertNotNull(Attribute.ArgumentList);
        var AttributeArguments = ArgumentList.Arguments;

        // No diagnostic if the attribute has no alias.
        if (!ContractGenerator.IsRequireNotNullAttributeWithAliasTypeOrName(AttributeArguments))
            return;

        // No diagnostic if the argument is a parameter name.
        if (attributeArgument.NameEquals is not NameEqualsSyntax NameEquals)
            return;

        string ArgumentName = NameEquals.Name.Identifier.Text;

        // No diagnostic if the argument is not the type.
        if (ArgumentName != nameof(RequireNotNullAttribute.Name))
            return;

        // No diagnostic if the argument is not a valid string or nameof.
        if (!ContractGenerator.IsStringOrNameofAttributeArgument(attributeArgument, out string ArgumentValue))
            return;

        string AliasName = ArgumentValue;

        // No diagnostic if the type is a valid identifier.
        if (SyntaxFacts.IsValidIdentifier(AliasName))
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), AliasName));
    }
}
