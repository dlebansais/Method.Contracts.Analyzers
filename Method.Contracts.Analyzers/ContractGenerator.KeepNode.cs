namespace Contracts.Analyzers;

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Contracts.Analyzers.Helper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Represents a code generator.
/// </summary>
public partial class ContractGenerator
{
    private static bool KeepNodeForPipeline<T>(SyntaxNode syntaxNode, CancellationToken cancellationToken)
        where T : Attribute
    {
        // Only accept methods.
        if (syntaxNode is not MethodDeclarationSyntax MethodDeclaration)
            return false;

        // The suffix can't be empty: if invalid in user settings, it's the default suffix.
        string VerifiedSuffix = Settings.VerifiedSuffix;
        Debug.Assert(VerifiedSuffix != string.Empty);

        // Only accept methods with the 'Verified' suffix in their name.
        string MethodName = MethodDeclaration.Identifier.Text;
        if (!GeneratorHelper.StringEndsWith(MethodName, VerifiedSuffix))
            return false;

        // Do not accept methods that are the suffix and nothing else.
        if (MethodName == VerifiedSuffix)
            return false;

        // Ignore methods that are not in a class and a namespace.
        if (MethodDeclaration.FirstAncestorOrSelf<ClassDeclarationSyntax>() is null ||
            MethodDeclaration.FirstAncestorOrSelf<BaseNamespaceDeclarationSyntax>() is null)
            return false;

        // Get a list of all supported attributes for this method.
        List<AttributeSyntax> MethodAttributes = GeneratorHelper.GetMethodSupportedAttributes(MethodDeclaration, SupportedAttributeNames);
        List<string> AttributeNames = new();
        bool IsDebugGeneration = MethodDeclaration.SyntaxTree.Options.PreprocessorSymbolNames.Contains("DEBUG");

        Dictionary<string, Func<MethodDeclarationSyntax, IReadOnlyList<AttributeArgumentSyntax>, bool>> ValidityVerifierTable = new()
        {
            { nameof(AccessAttribute), IsValidAccessAttribute },
            { nameof(RequireNotNullAttribute), IsValidRequireNotNullAttribute },
            { nameof(RequireAttribute), IsValidRequireAttribute },
            { nameof(EnsureAttribute), IsValidEnsureAttribute },
        };

        foreach (AttributeSyntax Attribute in MethodAttributes)
            if (Attribute.ArgumentList is AttributeArgumentListSyntax AttributeArgumentList)
            {
                string AttributeName = GeneratorHelper.ToAttributeName(Attribute);
                IReadOnlyList<AttributeArgumentSyntax> AttributeArguments = AttributeArgumentList.Arguments;

                Debug.Assert(ValidityVerifierTable.ContainsKey(AttributeName));
                var ValidityVerifier = ValidityVerifierTable[AttributeName];

                if (ValidityVerifier(MethodDeclaration, AttributeArguments))
                    AttributeNames.Add(AttributeName);
                else
                    return false;
            }

        // One of these attributes has to be the first, and we only return true for this one.
        // This way, multiple calls with different T return true exactly once.
        if (AttributeNames.Count == 0 || AttributeNames[0] != typeof(T).Name)
            return false;

        return true;
    }

    private static bool IsValidAccessAttribute(MethodDeclarationSyntax methodDeclaration, IReadOnlyList<AttributeArgumentSyntax> attributeArguments)
    {
        return IsValidStringOnlyAttribute(methodDeclaration, attributeArguments, out _);
    }

    private static bool IsValidRequireNotNullAttribute(MethodDeclarationSyntax methodDeclaration, IReadOnlyList<AttributeArgumentSyntax> attributeArguments)
    {
        if (IsRequireNotNullAttributeWithAlias(attributeArguments))
            return IsValidRequireNotNullAttributeWithAlias(methodDeclaration, attributeArguments);
        else if (attributeArguments.Count > 0)
            return IsValidRequireNotNullAttributeNoAlias(methodDeclaration, attributeArguments);
        else
            return false;
    }

    private static bool IsRequireNotNullAttributeWithAlias(IReadOnlyList<AttributeArgumentSyntax> arguments)
    {
        return arguments.Count > 0 && arguments.Any(argument => argument.NameEquals is not null);
    }

    private static bool IsValidRequireNotNullAttributeWithAlias(MethodDeclarationSyntax methodDeclaration, IReadOnlyList<AttributeArgumentSyntax> attributeArguments)
    {
        Debug.Assert(attributeArguments.Count > 0, "We reach this step only if IsRequireNotNullAttributeWithAlias() returned true.");
        AttributeArgumentSyntax FirstAttributeArgument = attributeArguments[0];

        if (FirstAttributeArgument.NameEquals is not null)
            return false;

        if (!IsStringOrNameofAttributeArgument(FirstAttributeArgument, out string ParameterName))
            return false;

        if (!GetParameterType(ParameterName, methodDeclaration, out _))
            return false;

        string Type = string.Empty;
        string Name = string.Empty;
        string AliasName = string.Empty;

        Debug.Assert(attributeArguments.Count > 1, "The first argument has no name, there has to be a second argument because IsRequireNotNullAttributeWithAlias() returned true.");

        for (int i = 1; i < attributeArguments.Count; i++)
            if (!IsValidArgumentWithAlias(methodDeclaration, attributeArguments[i], ref Type, ref Name, ref AliasName))
                return false;

        Debug.Assert(Type != string.Empty || Name != string.Empty || AliasName != string.Empty, "At this step there is at least one valid argument that is either Type, Name or AliasName.");

        return true;
    }

    private static bool IsValidArgumentWithAlias(MethodDeclarationSyntax methodDeclaration, AttributeArgumentSyntax attributeArgument, ref string type, ref string name, ref string aliasName)
    {
        if (attributeArgument.NameEquals is not NameEqualsSyntax NameEquals)
            return false;

        string ArgumentName = NameEquals.Name.Identifier.Text;

        if (!IsStringOrNameofAttributeArgument(attributeArgument, out string ArgumentValue))
            return false;

        Debug.Assert(ArgumentValue != string.Empty, "Valid string or nameof attribute arguments are never empty.");

        if (ArgumentName == nameof(RequireNotNullAttribute.Type))
            type = ArgumentValue;
        else if (ArgumentName == nameof(RequireNotNullAttribute.Name))
            name = ArgumentValue;
        else if (ArgumentName == nameof(RequireNotNullAttribute.AliasName))
            aliasName = ArgumentValue;
        else
            return false;

        return true;
    }

    private static bool IsValidRequireNotNullAttributeNoAlias(MethodDeclarationSyntax methodDeclaration, IReadOnlyList<AttributeArgumentSyntax> attributeArguments)
    {
        foreach (var AttributeArgument in attributeArguments)
        {
            Debug.Assert(AttributeArgument.NameEquals is null, "If not null, we would be running IsValidRequireNotNullAttributeWithAlias().");

            if (!IsStringOrNameofAttributeArgument(AttributeArgument, out string ArgumentValue))
                return false;

            if (!GetParameterType(ArgumentValue, methodDeclaration, out _))
                return false;
        }

        return true;
    }

    private static bool IsValidRequireAttribute(MethodDeclarationSyntax methodDeclaration, IReadOnlyList<AttributeArgumentSyntax> attributeArguments)
    {
        return IsValidStringOnlyAttribute(methodDeclaration, attributeArguments, out _);
    }

    private static bool IsValidEnsureAttribute(MethodDeclarationSyntax methodDeclaration, IReadOnlyList<AttributeArgumentSyntax> attributeArguments)
    {
        return IsValidStringOnlyAttribute(methodDeclaration, attributeArguments, out _);
    }

    private static bool IsValidStringOnlyAttribute(MethodDeclarationSyntax methodDeclaration, IReadOnlyList<AttributeArgumentSyntax> attributeArguments, out List<string> argumentValues)
    {
        argumentValues = new();

        if (attributeArguments.Count == 0)
            return false;

        foreach (var AttributeArgument in attributeArguments)
        {
            if (!IsStringOrNameofAttributeArgument(AttributeArgument, out string ArgumentValue))
                return false;

            argumentValues.Add(ArgumentValue);
        }

        return true;
    }

    private static bool IsStringOrNameofAttributeArgument(AttributeArgumentSyntax attributeArgument, out string argumentValue)
    {
        if (IsStringAttributeArgument(attributeArgument, out argumentValue))
            return true;

        if (IsNameofAttributeArgument(attributeArgument, out argumentValue))
            return true;

        argumentValue = string.Empty;
        return false;
    }

    private static bool IsStringAttributeArgument(AttributeArgumentSyntax attributeArgument, out string argumentValue)
    {
        if (attributeArgument.Expression is LiteralExpressionSyntax LiteralExpression &&
            LiteralExpression.Kind() == SyntaxKind.StringLiteralExpression)
        {
            string ArgumentText = LiteralExpression.Token.Text;
            ArgumentText = ArgumentText.Trim('"');
            if (ArgumentText != string.Empty)
            {
                argumentValue = ArgumentText;
                return true;
            }
        }

        argumentValue = string.Empty;
        return false;
    }

    private static bool IsNameofAttributeArgument(AttributeArgumentSyntax attributeArgument, out string argumentValue)
    {
        if (attributeArgument.Expression is InvocationExpressionSyntax InvocationExpression &&
            InvocationExpression.Expression is IdentifierNameSyntax IdentifierName &&
            IdentifierName.Identifier.Text == "nameof" &&
            InvocationExpression.ArgumentList.Arguments.Count == 1 &&
            InvocationExpression.ArgumentList.Arguments[0].Expression is IdentifierNameSyntax ExpressionIdentifierName)
        {
            string ArgumentText = ExpressionIdentifierName.Identifier.Text;
            Debug.Assert(ArgumentText != string.Empty, "If there is exactly one argument, it cannot be empty, otherwise there would be no argument.");

            argumentValue = ArgumentText;
            return true;
        }

        argumentValue = string.Empty;
        return false;
    }
}
