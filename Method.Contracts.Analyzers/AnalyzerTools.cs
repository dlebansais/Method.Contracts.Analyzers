﻿namespace Contracts.Analyzers;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Helper providing methods for analyzers.
/// </summary>
internal static class AnalyzerTools
{
    /// <summary>
    /// The minimum version of the language we care about.
    /// </summary>
    public const LanguageVersion MinimumVersionAnalyzed = LanguageVersion.CSharp4;

    // Define this symbol in unit tests to simulate an assertion failure.
    // This will test branches that can only execute in future versions of C#.
    private const string CoverageDirectivePrefix = "#define COVERAGE_A25BDFABDDF8402785EB75AD812DA952";

    /// <summary>
    /// Gets the help link for a diagnostic id.
    /// </summary>
    /// <param name="diagnosticId">The diagnostic id.</param>
    public static string GetHelpLink(string diagnosticId) => $"https://github.com/dlebansais/Method.Contracts.Analyzers/blob/master/doc/{diagnosticId}.md";

    /// <summary>
    /// Asserts that the analyzed node is of the expected type and satisfies requirements, then executes <paramref name="continueAction"/>.
    /// </summary>
    /// <typeparam name="T">The type of the analyzed node.</typeparam>
    /// <param name="context">The analyzer context.</param>
    /// <param name="minimumLanguageVersion">The minimum language version supporting the feature.</param>
    /// <param name="continueAction">The next analysis step.</param>
    /// <param name="analysisAssertions">A list of assertions.</param>
    public static void AssertSyntaxRequirements<T>(SyntaxNodeAnalysisContext context, LanguageVersion minimumLanguageVersion, Action<SyntaxNodeAnalysisContext, T, IAnalysisAssertion[]> continueAction, params IAnalysisAssertion[] analysisAssertions)
        where T : CSharpSyntaxNode
    {
        T ValidNode = (T)context.Node;

        if (IsFeatureSupportedInThisVersion(context, minimumLanguageVersion))
        {
            bool IsCoverageContext = IsCalledForCoverage(context);
            bool AreAllAssertionsTrue = analysisAssertions.TrueForAll(context);

            if (!IsCoverageContext && AreAllAssertionsTrue)
                continueAction(context, ValidNode, analysisAssertions);
        }
    }

    private static bool IsFeatureSupportedInThisVersion(SyntaxNodeAnalysisContext context, LanguageVersion minimumLanguageVersion)
    {
        CSharpParseOptions ParseOptions = (CSharpParseOptions)context.SemanticModel.SyntaxTree.Options;
        return ParseOptions.LanguageVersion >= minimumLanguageVersion;
    }

    private static bool IsCalledForCoverage(SyntaxNodeAnalysisContext context)
    {
        string? FirstDirectiveText = context.SemanticModel.SyntaxTree.GetRoot().GetFirstDirective()?.GetText().ToString();
        return FirstDirectiveText is not null && FirstDirectiveText.StartsWith(CoverageDirectivePrefix, StringComparison.Ordinal);
    }

    private static bool TrueForAll(this IAnalysisAssertion[] analysisAssertions, SyntaxNodeAnalysisContext context) => Array.TrueForAll(analysisAssertions, analysisAssertion => IsTrue(analysisAssertion, context));

    private static bool IsTrue(this IAnalysisAssertion analysisAssertion, SyntaxNodeAnalysisContext context) => analysisAssertion.IsTrue(context);

    /// <summary>
    /// Checks whether an attribute is of the expected type.
    /// </summary>
    /// <typeparam name="T">The expected attribute type.</typeparam>
    /// <param name="context">The context.</param>
    /// <param name="attribute">The attribute.</param>
    public static bool IsExpectedAttribute<T>(SyntaxNodeAnalysisContext context, AttributeSyntax? attribute)
        where T : Attribute => IsExpectedAttribute(context, typeof(T), attribute);

    /// <summary>
    /// Checks whether a type symbol is the expected type.
    /// </summary>
    /// <typeparam name="T">The expected attribute type.</typeparam>
    /// <param name="context">The context.</param>
    /// <param name="typeSymbol">The attribute.</param>
    public static bool IsExpectedAttribute<T>(SyntaxNodeAnalysisContext context, ITypeSymbol? typeSymbol)
        where T : Attribute => IsExpectedAttribute(context, typeof(T), typeSymbol);

    /// <summary>
    /// Checks whether an attribute is of the expected type.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="attributeType">The type.</param>
    /// <param name="attribute">The attribute.</param>
    public static bool IsExpectedAttribute(SyntaxNodeAnalysisContext context, Type attributeType, AttributeSyntax? attribute)
    {
        // There must be a parent attribute to any argument except in the most pathological cases.
        Contract.RequireNotNull(attribute, out AttributeSyntax Attribute);

        TypeInfo TypeInfo = context.SemanticModel.GetTypeInfo(Attribute);
        ITypeSymbol? TypeSymbol = TypeInfo.Type;

        return IsExpectedAttribute(context, attributeType, TypeSymbol);
    }

    private static bool IsExpectedAttribute(SyntaxNodeAnalysisContext context, Type attributeType, ITypeSymbol? typeSymbol)
    {
        ITypeSymbol? ExpectedTypeSymbol = context.Compilation.GetTypeByMetadataName(attributeType.FullName);

        return SymbolEqualityComparer.Default.Equals(typeSymbol, ExpectedTypeSymbol);
    }

    /// <summary>
    /// Returns a string with <paramref name="oldString"/> replaced with <paramref name="newString"/>.
    /// </summary>
    /// <param name="s">The string with substrings to replace.</param>
    /// <param name="oldString">The string to replace.</param>
    /// <param name="newString">The new string.</param>
    public static string Replace(string s, string oldString, string newString) =>
#if NETSTANDARD2_1_OR_GREATER
        s.Replace(oldString, newString, StringComparison.Ordinal);
#else
        s.Replace(oldString, newString);
#endif

    /// <summary>
    /// Checks whether a statement is a call to some method of the Contract class.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="statement">The statement to check.</param>
    /// <param name="methodName">The method name to check.</param>
    /// <param name="arguments">The method arguments upon return.</param>
    public static bool IsInvocationOfContract(SyntaxNodeAnalysisContext context, StatementSyntax statement, string methodName, out List<ArgumentSyntax> arguments)
    {
        if (statement is not ExpressionStatementSyntax ExpressionStatement)
        {
            Contract.Unused(out arguments);
            return false;
        }

        if (ExpressionStatement.Expression is not InvocationExpressionSyntax InvocationExpression)
        {
            Contract.Unused(out arguments);
            return false;
        }

        return IsInvocationOfContract(context, InvocationExpression, methodName, out arguments);
    }

    /// <summary>
    /// Checks whether an invocation expression is a call to some method of the Contract class.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="invocationExpression">The expression to check.</param>
    /// <param name="methodName">The method name to check.</param>
    /// <param name="arguments">The method arguments upon return.</param>
    public static bool IsInvocationOfContract(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocationExpression, string methodName, out List<ArgumentSyntax> arguments)
    {
        if (invocationExpression.Expression is not MemberAccessExpressionSyntax MemberAccessExpression)
        {
            Contract.Unused(out arguments);
            return false;
        }

        SymbolInfo ClassSymbolInfo = context.SemanticModel.GetSymbolInfo(MemberAccessExpression.Expression);
        if (ClassSymbolInfo.Symbol is not ITypeSymbol ClassSymbol)
        {
            Contract.Unused(out arguments);
            return false;
        }

        ITypeSymbol ContractTypeSymbol = Contract.AssertNotNull(context.Compilation.GetTypeByMetadataName(typeof(Contract).FullName));
        if (!SymbolEqualityComparer.Default.Equals(ClassSymbol, ContractTypeSymbol))
        {
            Contract.Unused(out arguments);
            return false;
        }

        SymbolInfo NameSymbolInfo = context.SemanticModel.GetSymbolInfo(MemberAccessExpression.Name);
        if (NameSymbolInfo.Symbol is not ISymbol NameSymbol)
        {
            Contract.Unused(out arguments);
            return false;
        }

        IEnumerable<ISymbol> MatchingMethodSymbols = ContractTypeSymbol.GetMembers().Where(member => member.Name == methodName);
        bool IsMethodSymbolMatching = MatchingMethodSymbols.Any(symbol => SymbolEqualityComparer.Default.Equals(NameSymbol.OriginalDefinition, symbol));
        if (!IsMethodSymbolMatching)
        {
            Contract.Unused(out arguments);
            return false;
        }

        arguments = [.. invocationExpression.ArgumentList.Arguments];
        return true;
    }

    /// <summary>
    /// Gets the list of statements a statement is part of.
    /// </summary>
    /// <param name="statement">The statement.</param>
    /// <param name="parentStatements">A list of other statements next to <paramref name="statement"/>.</param>
    /// <param name="parentStatement">The parent statement.</param>
    /// <returns><see langword="true"/> if there is a parent statement, otherwise, <see langword="false"/>.</returns>
    public static bool GetStatementParentList(StatementSyntax statement, out List<StatementSyntax> parentStatements, out StatementSyntax parentStatement)
    {
        StatementSyntax? Parent;

        if (statement.Parent is BlockSyntax Block)
        {
            parentStatements = [.. Block.Statements];

            Parent = Block.Parent switch
            {
                CatchClauseSyntax CatchClause => CatchClause.Parent as TryStatementSyntax,
                FinallyClauseSyntax FinallyClause => FinallyClause.Parent as TryStatementSyntax,
                _ => Block.Parent as StatementSyntax,
            };
        }
        else if (statement.Parent is SwitchSectionSyntax SwitchSection)
        {
            parentStatements = [.. SwitchSection.Statements];
            Parent = SwitchSection.Parent as SwitchStatementSyntax;
        }
        else if (statement.Parent is ElseClauseSyntax ElseClause)
        {
            parentStatements = [statement];
            Parent = ElseClause.Parent as IfStatementSyntax;
        }
        else
        {
            parentStatements = [statement];

            // If the parent is a statement we can continue, otherwise it's some method declaration.
            Parent = statement.Parent as StatementSyntax;
        }

        if (Parent is not null)
        {
            parentStatement = Parent;
            return true;
        }
        else
        {
            Contract.Unused(out parentStatement);
            return false;
        }
    }

    /// <summary>
    /// Gets all statements that can follow the provided statement until the end of the method or a return statement.
    /// </summary>
    /// <param name="statement">The statement.</param>
    public static List<StatementSyntax> FindSubsequentStatements(StatementSyntax statement)
    {
        List<StatementSyntax> RemainingStatements = [];
        StatementSyntax CurrentStatement = statement;

        while (true)
        {
            bool HasParentStatement = GetStatementParentList(CurrentStatement, out List<StatementSyntax> ParentStatements, out StatementSyntax ParentStatement);

            int StatementIndex = ParentStatements.IndexOf(CurrentStatement);
            Contract.Assert(StatementIndex >= 0);
            Contract.Assert(StatementIndex < ParentStatements.Count);

            bool HasReturn = AddSubsequentStatements(RemainingStatements, ParentStatements, StatementIndex + 1);

            if (!HasParentStatement || HasReturn)
                break;

            CurrentStatement = ParentStatement;
        }

        return RemainingStatements;
    }

    private static bool AddSubsequentStatements(List<StatementSyntax> remainingStatements, List<StatementSyntax> parentStatements, int startIndex)
    {
        for (int i = startIndex; i < parentStatements.Count; i++)
        {
            StatementSyntax NextStatement = parentStatements[i];

            if (NextStatement is ReturnStatementSyntax)
                return true;

            remainingStatements.Add(NextStatement);
        }

        return false;
    }
}
