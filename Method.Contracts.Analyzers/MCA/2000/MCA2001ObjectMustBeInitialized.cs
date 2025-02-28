﻿namespace Contracts.Analyzers;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

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
        // If we reached this step, there is an initializer method.
        Contract.Assert(analysisAssertions.Length == 1);
        InitializerAnalysisAssertion Assertion = Contract.AssertNotNull(analysisAssertions.First() as InitializerAnalysisAssertion);
        List<IMethodSymbol> InitializerMethodSymbols = Contract.AssertNotNull(Assertion.InitializerMethodSymbols);

        Contract.Assert(InitializerMethodSymbols.Count > 0);
        IMethodSymbol InitializerMethodSymbol = InitializerMethodSymbols[0];
        string InitializerName = InitializerMethodSymbol.Name;

        // Diagnostic if there isn't exactly one initializer.
        if (InitializerMethodSymbols.Count == 1)
        {
            // Diagnostic if we can't find the created instance or the next statement.
            if (GetCreatedObjectAndFollowUpStatements(context, objectCreationExpression, out ISymbol CreatedSymbol, out StatementSyntax nextStatement))
            {
                // No diagnostic if the next statement is a call to the initializer.
                if (IsFollowUpStatementInitialization(context, CreatedSymbol, nextStatement, InitializerMethodSymbol))
                    return;
            }
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), InitializerName));
    }

    private static bool GetCreatedObjectAndFollowUpStatements(SyntaxNodeAnalysisContext context, BaseObjectCreationExpressionSyntax objectCreationExpression, out ISymbol createdSymbol, out StatementSyntax nextStatement)
    {
        if (objectCreationExpression.Parent is EqualsValueClauseSyntax EqualsValueClause)
            if (EqualsValueClause.Parent is VariableDeclaratorSyntax VariableDeclarator)
                return CheckVariableDeclarator(context, VariableDeclarator, out createdSymbol, out nextStatement);

        if (objectCreationExpression.Parent is AssignmentExpressionSyntax AssignmentExpression)
            if (AssignmentExpression.Left is IdentifierNameSyntax IdentifierName)
                return CheckAssignmentExpression(context, IdentifierName, AssignmentExpression, out createdSymbol, out nextStatement);

        Contract.Unused(out createdSymbol);
        Contract.Unused(out nextStatement);
        return false;
    }

    private static bool CheckVariableDeclarator(SyntaxNodeAnalysisContext context, VariableDeclaratorSyntax variableDeclarator, out ISymbol createdSymbol, out StatementSyntax nextStatement)
    {
        ISymbol DeclaredSymbol = Contract.AssertNotNull(context.SemanticModel.GetDeclaredSymbol(variableDeclarator));
        VariableDeclarationSyntax VariableDeclaration = Contract.AssertNotNull(variableDeclarator.Parent as VariableDeclarationSyntax);

        if (VariableDeclaration.Parent is LocalDeclarationStatementSyntax LocalDeclarationStatement)
            if (CheckDestinationAndNextStatement(LocalDeclarationStatement, out nextStatement))
            {
                createdSymbol = DeclaredSymbol;
                return true;
            }

        Contract.Unused(out createdSymbol);
        Contract.Unused(out nextStatement);
        return false;
    }

    private static bool CheckAssignmentExpression(SyntaxNodeAnalysisContext context, IdentifierNameSyntax identifierName, AssignmentExpressionSyntax assignmentExpression, out ISymbol createdSymbol, out StatementSyntax nextStatement)
    {
        SymbolInfo AssignedSymbolInfo = context.SemanticModel.GetSymbolInfo(identifierName);
        ISymbol AssignedSymbol = Contract.AssertNotNull(AssignedSymbolInfo.Symbol);

        if (assignmentExpression.Parent is ExpressionStatementSyntax ExpressionStatement)
            if (CheckDestinationAndNextStatement(ExpressionStatement, out nextStatement))
            {
                createdSymbol = AssignedSymbol;
                return true;
            }

        Contract.Unused(out createdSymbol);
        Contract.Unused(out nextStatement);
        return false;
    }

    private static bool CheckDestinationAndNextStatement(StatementSyntax currentStatement, out StatementSyntax nextStatement)
    {
        if (currentStatement.Parent is BlockSyntax Block)
        {
            ImmutableList<StatementSyntax> Statements = [.. Block.Statements];
            int DeclarationIndex = Statements.IndexOf(currentStatement);

            if (DeclarationIndex + 1 < Statements.Count)
            {
                nextStatement = Statements[DeclarationIndex + 1];
                return true;
            }
        }

        if (currentStatement.Parent is GlobalStatementSyntax GlobalStatement)
        {
            CompilationUnitSyntax CompilationUnit = Contract.AssertNotNull(GlobalStatement.Parent as CompilationUnitSyntax);
            ImmutableList<MemberDeclarationSyntax> Members = [.. CompilationUnit.Members];
            int DeclarationIndex = Members.IndexOf(GlobalStatement);

            if (DeclarationIndex + 1 < Members.Count)
            {
                MemberDeclarationSyntax NextMember = Members[DeclarationIndex + 1];
                if (NextMember is GlobalStatementSyntax NextGlobalStatement)
                {
                    nextStatement = NextGlobalStatement.Statement;
                    return true;
                }
            }
        }

        Contract.Unused(out nextStatement);
        return false;
    }

    private static bool IsFollowUpStatementInitialization(SyntaxNodeAnalysisContext context, ISymbol createdSymbol, StatementSyntax firstStatement, IMethodSymbol initializerMethodSymbol)
    {
        Contract.RequireNotNull(createdSymbol, out ISymbol CreatedSymbol);
        Contract.RequireNotNull(firstStatement, out StatementSyntax FirstStatement);

        if (FirstStatement is ExpressionStatementSyntax ExpressionStatement)
        {
            ExpressionSyntax Expression = ExpressionStatement.Expression;

            if (Expression is AwaitExpressionSyntax AwaitExpression)
                Expression = AwaitExpression.Expression;

            if (Expression is InvocationExpressionSyntax InvocationExpression && InvocationExpression.Expression is MemberAccessExpressionSyntax MemberAccessExpression)
            {
                ISymbol? ExpressionSymbol = null;
                IMethodSymbol? MethodSymbol = null;

                if (MemberAccessExpression.Expression is IdentifierNameSyntax ObjectIdentifierName)
                {
                    SymbolInfo IdentifierNameInfo = context.SemanticModel.GetSymbolInfo(ObjectIdentifierName);
                    if (IdentifierNameInfo.Symbol is ISymbol ObjectSymbol)
                        ExpressionSymbol = ObjectSymbol;
                }

                if (MemberAccessExpression.Name is IdentifierNameSyntax IdentifierName)
                {
                    SymbolInfo IdentifierNameInfo = context.SemanticModel.GetSymbolInfo(IdentifierName);
                    if (IdentifierNameInfo.Symbol is IMethodSymbol CalledMethodNameSymbol)
                        MethodSymbol = CalledMethodNameSymbol;
                }

                if (ExpressionSymbol is not null &&
                    MethodSymbol is not null &&
                    SymbolEqualityComparer.Default.Equals(ExpressionSymbol, CreatedSymbol) &&
                    SymbolEqualityComparer.Default.Equals(MethodSymbol, initializerMethodSymbol))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
