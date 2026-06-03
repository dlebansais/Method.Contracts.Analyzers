namespace Contracts.Analyzers;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Analyzer for rule MCA1001: Verified method must be private.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MCA1001VerifiedMethodMustBePrivate : MethodDiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for this rule.
    /// </summary>
    public const string DiagnosticId = "MCA1001";

    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(AnalyzerResources.MCA1001AnalyzerTitle), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(AnalyzerResources.MCA1001AnalyzerMessageFormat), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
    private static readonly LocalizableString Description = new LocalizableResourceString(nameof(AnalyzerResources.MCA1001AnalyzerDescription), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
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
    private protected override IEnumerable<IAnalysisAssertion> Assertions { get; } =
    [
        new SimpleAnalysisAssertion(context => !IsMethodPrivate((MethodDeclarationSyntax)context.Node)),
        new SimpleAnalysisAssertion(context => ContractGenerator.GetFirstSupportedAttribute(context, (MethodDeclarationSyntax)context.Node) is not null),
    ];

    private static bool IsMethodPrivate(MethodDeclarationSyntax methodDeclaration)
    {
        return methodDeclaration.Modifiers.All(modifier => !modifier.IsKind(SyntaxKind.ProtectedKeyword) &&
                                                           !modifier.IsKind(SyntaxKind.PublicKeyword) &&
                                                           !modifier.IsKind(SyntaxKind.InternalKeyword));
    }

    /// <inheritdoc />
    private protected override void AnalyzeVerifiedNode(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration, IAnalysisAssertion[] analysisAssertions)
    {
        string Text = methodDeclaration.Identifier.ValueText;

        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), Text));
    }
}
