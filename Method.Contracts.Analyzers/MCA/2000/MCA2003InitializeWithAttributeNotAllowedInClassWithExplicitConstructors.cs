﻿namespace Contracts.Analyzers;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Analyzer for rule MCA2003: InitializeWith attribute not allowed in class with explicit constructors.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MCA2003InitializeWithAttributeNotAllowedInClassWithExplicitConstructors : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for this rule.
    /// </summary>
    public const string DiagnosticId = "MCA2003";

    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(AnalyzerResources.MCA2003AnalyzerTitle), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(AnalyzerResources.MCA2003AnalyzerMessageFormat), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
    private static readonly LocalizableString Description = new LocalizableResourceString(nameof(AnalyzerResources.MCA2003AnalyzerDescription), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
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

    /// <summary>
    /// Initializes the rule analyzer.
    /// </summary>
    /// <param name="context">The analysis context.</param>
    public override void Initialize(AnalysisContext context)
    {
        context = Contract.AssertNotNull(context);

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.Attribute);
    }

    private void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        AnalyzerTools.AssertSyntaxRequirements<AttributeSyntax>(
            context,
            LanguageVersion.CSharp7,
            AnalyzeVerifiedNode,
            new SimpleAnalysisAssertion(context => IsClassAttribute(context, (AttributeSyntax)context.Node)));
    }

    private static bool IsClassAttribute(SyntaxNodeAnalysisContext context, AttributeSyntax attribute)
    {
        return AnalyzerTools.IsExpectedAttribute<InitializeWithAttribute>(context, attribute) &&
               attribute.FirstAncestorOrSelf<ConstructorDeclarationSyntax>() is null;
    }

    private void AnalyzeVerifiedNode(SyntaxNodeAnalysisContext context, AttributeSyntax attribute, IAnalysisAssertion[] analysisAssertions)
    {
        SyntaxList<MemberDeclarationSyntax> Members;
        string ClassOrRecordName;

        // No diagnostic if not a class or record.
        if (attribute.FirstAncestorOrSelf<ClassDeclarationSyntax>() is ClassDeclarationSyntax ClassDeclaration)
        {
            Members = ClassDeclaration.Members;
            ClassOrRecordName = ClassDeclaration.Identifier.Text;
        }
        else if (attribute.FirstAncestorOrSelf<RecordDeclarationSyntax>() is RecordDeclarationSyntax RecordDeclaration)
        {
            Members = RecordDeclaration.Members;
            ClassOrRecordName = RecordDeclaration.Identifier.Text;
        }
        else
        {
            return;
        }

        bool HasContructor = false;
        foreach (MemberDeclarationSyntax Member in Members)
            if (Member is ConstructorDeclarationSyntax)
                HasContructor = true;

        // No diagnostic if no explicit constructors.
        if (!HasContructor)
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), ClassOrRecordName));
    }
}
