namespace Contracts.Analyzers;

using System.Collections.Generic;
using System.Collections.Immutable;
using Contracts.Analyzers.Helper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Analyzer for rule MCA1019: Verified property is missing suffix.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MCA1019VerifiedPropertyIsMissingSuffix : PropertyDiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for this rule.
    /// </summary>
    public const string DiagnosticId = "MCA1019";

    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(AnalyzerResources.MCA1019AnalyzerTitle), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(AnalyzerResources.MCA1019AnalyzerMessageFormat), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
    private static readonly LocalizableString Description = new LocalizableResourceString(nameof(AnalyzerResources.MCA1019AnalyzerDescription), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
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
        new SimpleAnalysisAssertion(context => ContractGenerator.GetFirstSupportedAttribute(context, (PropertyDeclarationSyntax)context.Node) is not null),
    ];

    /// <inheritdoc />
    private protected override void AnalyzeVerifiedNode(SyntaxNodeAnalysisContext context, PropertyDeclarationSyntax propertyDeclaration, IAnalysisAssertion[] analysisAssertions)
    {
        GeneratorSettings Settings = ContractGenerator.ReadSettings(context.Options.AnalyzerConfigOptionsProvider, context.CancellationToken)[0];

        // The suffix can't be empty: if invalid in user settings, it's the default suffix.
        string VerifiedSuffix = Settings.VerifiedSuffix;
        Contract.Assert(VerifiedSuffix != string.Empty);

        // Only accept properties with the 'Verified' suffix in their name.
        // Do not accept properties that are the suffix and nothing else.
        string PropertyName = propertyDeclaration.Identifier.Text;
        if (GeneratorHelper.StringEndsWith(PropertyName, VerifiedSuffix))
            if (PropertyName != VerifiedSuffix)
                return;

        string Text = propertyDeclaration.Identifier.ValueText;

        context.ReportDiagnostic(Diagnostic.Create(Rule, propertyDeclaration.Identifier.GetLocation(), Text, VerifiedSuffix));
    }
}
