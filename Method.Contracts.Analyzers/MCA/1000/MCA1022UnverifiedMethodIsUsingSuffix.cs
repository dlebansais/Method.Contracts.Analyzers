namespace Contracts.Analyzers;

using System.Collections.Generic;
using System.Collections.Immutable;
using Contracts.Analyzers.Helper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Analyzer for rule MCA1022: Unverified method is using suffix.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MCA1022UnverifiedMethodIsUsingSuffix : MethodDiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for this rule.
    /// </summary>
    public const string DiagnosticId = "MCA1022";

    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(AnalyzerResources.MCA1022AnalyzerTitle), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(AnalyzerResources.MCA1022AnalyzerMessageFormat), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
    private static readonly LocalizableString Description = new LocalizableResourceString(nameof(AnalyzerResources.MCA1022AnalyzerDescription), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
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
        new SimpleAnalysisAssertion(context => ContractGenerator.GetFirstSupportedAttribute(context, (MethodDeclarationSyntax)context.Node) is null),
    ];

    /// <inheritdoc />
    private protected override void AnalyzeVerifiedNode(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration, IAnalysisAssertion[] analysisAssertions)
    {
        GeneratorSettings Settings = ContractGenerator.ReadSettings(context.Options.AnalyzerConfigOptionsProvider, context.CancellationToken)[0];

        // The suffix can't be empty: if invalid in user settings, it's the default suffix.
        string VerifiedSuffix = Settings.VerifiedSuffix;
        Contract.Assert(VerifiedSuffix != string.Empty);

        // Only detect methods with the 'Verified' suffix in their name.
        string MethodName = methodDeclaration.Identifier.Text;
        if (!GeneratorHelper.StringEndsWith(MethodName, VerifiedSuffix))
            return;

        string Text = methodDeclaration.Identifier.ValueText;

        context.ReportDiagnostic(Diagnostic.Create(Rule, methodDeclaration.Identifier.GetLocation(), Text, VerifiedSuffix));
    }
}
