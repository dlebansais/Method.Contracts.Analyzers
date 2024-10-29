namespace Contracts.Analyzers;

using System;
using System.Linq;
using Contracts.Analyzers.Helper;
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
    public static string GetHelpLink(string diagnosticId)
    {
        return $"https://github.com/dlebansais/Method.Contracts.Analyzers/blob/master/doc/{diagnosticId}.md";
    }

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
        var ParseOptions = (CSharpParseOptions)context.SemanticModel.SyntaxTree.Options;
        return ParseOptions.LanguageVersion >= minimumLanguageVersion;
    }

    private static bool IsCalledForCoverage(SyntaxNodeAnalysisContext context)
    {
        string? FirstDirectiveText = context.SemanticModel.SyntaxTree.GetRoot().GetFirstDirective()?.GetText().ToString();
        return FirstDirectiveText is not null && FirstDirectiveText.StartsWith(CoverageDirectivePrefix, StringComparison.Ordinal);
    }

    private static bool TrueForAll(this IAnalysisAssertion[] analysisAssertions, SyntaxNodeAnalysisContext context)
    {
        return Array.TrueForAll(analysisAssertions, analysisAssertion => IsTrue(analysisAssertion, context));
    }

    private static bool IsTrue(this IAnalysisAssertion analysisAssertion, SyntaxNodeAnalysisContext context)
    {
        return analysisAssertion.IsTrue(context);
    }

    /// <summary>
    /// Checks whether an attribute is of the expected type.
    /// </summary>
    /// <typeparam name="T">The expected attribute type.</typeparam>
    /// <param name="context">The context.</param>
    /// <param name="attribute">The attribute.</param>
    public static bool IsExpectedAttribute<T>(SyntaxNodeAnalysisContext context, AttributeSyntax? attribute)
        where T : Attribute
    {
        return IsExpectedAttribute(context, typeof(T), attribute);
    }

    /// <summary>
    /// Checks whether a type symbol is the expected type.
    /// </summary>
    /// <typeparam name="T">The expected attribute type.</typeparam>
    /// <param name="context">The context.</param>
    /// <param name="typeSymbol">The attribute.</param>
    public static bool IsExpectedAttribute<T>(SyntaxNodeAnalysisContext context, ITypeSymbol? typeSymbol)
        where T : Attribute
    {
        return IsExpectedAttribute(context, typeof(T), typeSymbol);
    }

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

        var TypeInfo = context.SemanticModel.GetTypeInfo(Attribute);
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
    public static string Replace(string s, string oldString, string newString)
    {
#if NETSTANDARD2_1_OR_GREATER
        return s.Replace(oldString, newString, StringComparison.Ordinal);
#else
        return s.Replace(oldString, newString);
#endif
    }

    /// <summary>
    /// Checks whether a statement is a call to Contract.Unused().
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="statement">The statement to check.</param>
    /// <param name="invocationArgumentName">The name of the out argument upon return.</param>
    public static bool IsInvocationOfContractUnused(SyntaxNodeAnalysisContext context, StatementSyntax statement, out string invocationArgumentName)
    {
        if (statement is not ExpressionStatementSyntax ExpressionStatement)
        {
            Contract.Unused(out invocationArgumentName);
            return false;
        }

        if (ExpressionStatement.Expression is not InvocationExpressionSyntax InvocationExpression)
        {
            Contract.Unused(out invocationArgumentName);
            return false;
        }

        if (!IsInvocationOfContractUnused(context, InvocationExpression, out invocationArgumentName))
            return false;

        return true;
    }

    /// <summary>
    /// Checks whether an invocation expression is a call to Contract.Unused().
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="invocationExpression">The expression to check.</param>
    /// <param name="invocationArgumentName">The name of the out argument upon return.</param>
    public static bool IsInvocationOfContractUnused(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocationExpression, out string invocationArgumentName)
    {
        if (invocationExpression.Expression is not MemberAccessExpressionSyntax MemberAccessExpression)
        {
            Contract.Unused(out invocationArgumentName);
            return false;
        }

        if (MemberAccessExpression.Expression is not IdentifierNameSyntax IdentifierName)
        {
            Contract.Unused(out invocationArgumentName);
            return false;
        }

        SymbolInfo ClassSymbolInfo = context.SemanticModel.GetSymbolInfo(IdentifierName);
        if (ClassSymbolInfo.Symbol is not ITypeSymbol ClassSymbol)
        {
            Contract.Unused(out invocationArgumentName);
            return false;
        }

        ITypeSymbol ContractTypeSymbol = Contract.AssertNotNull(context.Compilation.GetTypeByMetadataName(typeof(Contract).FullName));
        if (!SymbolEqualityComparer.Default.Equals(ClassSymbol, ContractTypeSymbol))
        {
            Contract.Unused(out invocationArgumentName);
            return false;
        }

        SymbolInfo NameSymbolInfo = context.SemanticModel.GetSymbolInfo(MemberAccessExpression.Name);
        if (NameSymbolInfo.Symbol is not ISymbol NameSymbol)
        {
            Contract.Unused(out invocationArgumentName);
            return false;
        }

        ISymbol UnusedMethodSymbol = ContractTypeSymbol.GetMembers().First(member => member.Name == "Unused");
        if (!SymbolEqualityComparer.Default.Equals(ClassSymbol, ContractTypeSymbol))
        {
            Contract.Unused(out invocationArgumentName);
            return false;
        }

        if (invocationExpression.ArgumentList.Arguments.Count != 1)
        {
            Contract.Unused(out invocationArgumentName);
            return false;
        }

        ArgumentSyntax Argument = invocationExpression.ArgumentList.Arguments[0];

        if (!Argument.RefKindKeyword.IsKind(SyntaxKind.OutKeyword))
        {
            Contract.Unused(out invocationArgumentName);
            return false;
        }

        if (Argument.Expression is not IdentifierNameSyntax ArgumentIdentifierName)
        {
            Contract.Unused(out invocationArgumentName);
            return false;
        }

        invocationArgumentName = ArgumentIdentifierName.Identifier.Text;
        return true;
    }

    /// <summary>
    /// Checks whether a statement is the last one in a method.
    /// </summary>
    /// <param name="statement">The statement to check.</param>
    public static bool IsLastStementOfMethod(StatementSyntax statement)
    {
        return false;
    }
}
