namespace Contracts.Analyzers;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

/// <summary>
/// Analyzer for rule MCA2001: Object must be initialized.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MCA2001ObjectMustBeInitialized : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for this rule.
    /// </summary>
    public const string DiagnosticId = "MCA2001";

    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(AnalyzerResources.MCA2001AnalyzerTitle), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(AnalyzerResources.MCA2001AnalyzerMessageFormat), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
    private static readonly LocalizableString Description = new LocalizableResourceString(nameof(AnalyzerResources.MCA2001AnalyzerDescription), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
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

        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ObjectCreationExpression, SyntaxKind.ImplicitObjectCreationExpression);
    }

    private void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        AnalyzerTools.AssertSyntaxRequirements<BaseObjectCreationExpressionSyntax>(
            context,
            LanguageVersion.CSharp7,
            AnalyzeVerifiedNode,
            new InitializerAnalysisAssertion());
    }

    private void AnalyzeVerifiedNode(SyntaxNodeAnalysisContext context, BaseObjectCreationExpressionSyntax objectCreationExpression, IAnalysisAssertion[] analysisAssertions)
    {
        Contract.Assert(analysisAssertions.Length == 1);
        InitializerAnalysisAssertion Assertion = Contract.AssertNotNull(analysisAssertions.First() as InitializerAnalysisAssertion);
        List<IMethodSymbol> InitializerMethodSymbols = Contract.AssertNotNull(Assertion.InitializerMethodSymbols);

        Contract.Assert(InitializerMethodSymbols.Count > 0);
        IMethodSymbol InitializerMethodSymbol = InitializerMethodSymbols.First();
        string InitializerName = InitializerMethodSymbol.Name;

        if (InitializerMethodSymbols.Count == 1)
        {
            if (GetCreatedObjectAndFollowUpStatements(context, objectCreationExpression, out ISymbol CreatedSymbol, out StatementSyntax nextStatement))
            {
                if (IsFollowUpStatementInitialization(context, nextStatement, CreatedSymbol, InitializerMethodSymbol))
                    return;
            }
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), InitializerName));
    }

    private static bool GetCreatedObjectAndFollowUpStatements(SyntaxNodeAnalysisContext context, BaseObjectCreationExpressionSyntax objectCreationExpression, out ISymbol createdSymbol, out StatementSyntax nextStatement)
    {
        if (objectCreationExpression.Parent is EqualsValueClauseSyntax EqualsValueClause)
        {
            if (EqualsValueClause.Parent is VariableDeclaratorSyntax VariableDeclarator)
            {
                return CheckVariableDeclarator(context, VariableDeclarator, out createdSymbol, out nextStatement);
            }
        }

        Contract.Unused(out createdSymbol);
        Contract.Unused(out nextStatement);
        return false;
    }

    private static bool CheckVariableDeclarator(SyntaxNodeAnalysisContext context, VariableDeclaratorSyntax variableDeclarator, out ISymbol createdSymbol, out StatementSyntax nextStatement)
    {
        ISymbol DeclaredSymbol = Contract.AssertNotNull(context.SemanticModel.GetDeclaredSymbol(variableDeclarator));
        VariableDeclarationSyntax VariableDeclaration = Contract.AssertNotNull(variableDeclarator.Parent as VariableDeclarationSyntax);
        LocalDeclarationStatementSyntax LocalDeclarationStatement = Contract.AssertNotNull(VariableDeclaration.Parent as LocalDeclarationStatementSyntax);

        if (LocalDeclarationStatement.Parent is BlockSyntax Block)
        {
            var Statements = Block.Statements.ToImmutableList();
            int DeclarationIndex = Statements.IndexOf(LocalDeclarationStatement);

            if (DeclarationIndex + 1 < Statements.Count)
            {
                createdSymbol = DeclaredSymbol;
                nextStatement = Statements[DeclarationIndex + 1];
                return true;
            }
        }

        if (LocalDeclarationStatement.Parent is GlobalStatementSyntax GlobalStatement)
        {
            CompilationUnitSyntax CompilationUnit = Contract.AssertNotNull(GlobalStatement.Parent as CompilationUnitSyntax);
            var Members = CompilationUnit.Members.ToImmutableList();
            int DeclarationIndex = Members.IndexOf(GlobalStatement);

            if (DeclarationIndex + 1 < Members.Count)
            {
                var NextMember = Members[DeclarationIndex + 1];
                if (NextMember is GlobalStatementSyntax NextGlobalStatement)
                {
                    createdSymbol = DeclaredSymbol;
                    nextStatement = NextGlobalStatement.Statement;
                    return true;
                }
            }
        }

        Contract.Unused(out createdSymbol);
        Contract.Unused(out nextStatement);
        return false;
    }

    private static bool IsFollowUpStatementInitialization(SyntaxNodeAnalysisContext context, StatementSyntax firstStatement, ISymbol createdSymbol, IMethodSymbol initializerMethodSymbol)
    {
        if (firstStatement is ExpressionStatementSyntax ExpressionStatement)
        {
            if (ExpressionStatement.Expression is InvocationExpressionSyntax InvocationExpression)
            {
                if (InvocationExpression.Expression is MemberAccessExpressionSyntax MemberAccessExpression)
                {
                    ISymbol? ExpressionSymbol = null;
                    IMethodSymbol? MethodSymbol = null;

                    if (MemberAccessExpression.Expression is IdentifierNameSyntax ObjectIdentifierName)
                    {
                        var IdentifierNameInfo = context.SemanticModel.GetSymbolInfo(ObjectIdentifierName);
                        if (IdentifierNameInfo.Symbol is ISymbol ObjectSymbol)
                        {
                            ExpressionSymbol = ObjectSymbol;
                        }
                    }

                    if (MemberAccessExpression.Name is IdentifierNameSyntax IdentifierName)
                    {
                        var IdentifierNameInfo = context.SemanticModel.GetSymbolInfo(IdentifierName);
                        if (IdentifierNameInfo.Symbol is IMethodSymbol CalledMethodNameSymbol)
                        {
                            MethodSymbol = CalledMethodNameSymbol;
                        }
                    }

                    if (ExpressionSymbol is not null &&
                        MethodSymbol is not null &&
                        SymbolEqualityComparer.Default.Equals(ExpressionSymbol, createdSymbol) &&
                        SymbolEqualityComparer.Default.Equals(MethodSymbol, initializerMethodSymbol))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }
}
