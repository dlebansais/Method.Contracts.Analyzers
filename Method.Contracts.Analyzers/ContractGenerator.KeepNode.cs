namespace Contracts.Analyzers;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Contracts.Analyzers.Helper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
        Contract.Assert(VerifiedSuffix != string.Empty);

        // Only accept methods with the 'Verified' suffix in their name.
        string MethodName = MethodDeclaration.Identifier.Text;
        if (!GeneratorHelper.StringEndsWith(MethodName, VerifiedSuffix))
            return false;

        // Do not accept methods that are the suffix and nothing else.
        if (MethodName == VerifiedSuffix)
            return false;

        // Ignore methods that are not in a class and a namespace.
        if ((MethodDeclaration.FirstAncestorOrSelf<ClassDeclarationSyntax>() is null &&
             MethodDeclaration.FirstAncestorOrSelf<StructDeclarationSyntax>() is null &&
             MethodDeclaration.FirstAncestorOrSelf<RecordDeclarationSyntax>() is null) ||
            MethodDeclaration.FirstAncestorOrSelf<BaseNamespaceDeclarationSyntax>() is null)
            return false;

        string? FirstAttributeName = GetFirstSupportedAttribute(MethodDeclaration);

        // One of these attributes has to be the first, and we only return true for this one.
        // This way, multiple calls with different T return true exactly once.
        if (FirstAttributeName is null || FirstAttributeName != typeof(T).Name)
            return false;

        return true;
    }

    /// <summary>
    /// Checks whether a method contains at least one attribute we support and returns its name.
    /// All attributes we support must be valid.
    /// </summary>
    /// <param name="methodDeclaration">The method declaration.</param>
    /// <returns><see langword="null"/> if any of the attributes we support is invalid, or none was found; Otherwise, the name of the first attribute.</returns>
    public static string? GetFirstSupportedAttribute(MethodDeclarationSyntax methodDeclaration)
    {
        Contract.RequireNotNull(methodDeclaration, out MethodDeclarationSyntax MethodDeclaration);

        // Get a list of all supported attributes for this method.
        List<AttributeSyntax> MethodAttributes = GeneratorHelper.GetMethodSupportedAttributes(MethodDeclaration, SupportedAttributeNames);
        List<string> AttributeNames = new();
        bool IsDebugGeneration = MethodDeclaration.SyntaxTree.Options.PreprocessorSymbolNames.Contains("DEBUG");

        foreach (AttributeSyntax Attribute in MethodAttributes)
            if (!IsValidAttribute(Attribute, MethodDeclaration, IsDebugGeneration, AttributeNames))
                return null;

        return AttributeNames.Count > 0 ? AttributeNames[0] : null;
    }

    private static bool IsValidAttribute(AttributeSyntax attribute, MethodDeclarationSyntax methodDeclaration, bool isDebugGeneration, List<string> attributeNames)
    {
        if (attribute.ArgumentList is AttributeArgumentListSyntax AttributeArgumentList)
        {
            string AttributeName = GeneratorHelper.ToAttributeName(attribute);
            IReadOnlyList<AttributeArgumentSyntax> AttributeArguments = AttributeArgumentList.Arguments;

            Dictionary<string, Func<MethodDeclarationSyntax, IReadOnlyList<AttributeArgumentSyntax>, AttributeValidityCheckResult>> ValidityVerifierTable = new()
            {
                { nameof(AccessAttribute), IsValidAccessAttribute },
                { nameof(RequireNotNullAttribute), IsValidRequireNotNullAttribute },
                { nameof(RequireAttribute), IsValidRequireAttribute },
                { nameof(EnsureAttribute), IsValidEnsureAttribute },
            };

            Contract.Assert(ValidityVerifierTable.ContainsKey(AttributeName));
            var ValidityVerifier = ValidityVerifierTable[AttributeName];
            AttributeValidityCheckResult CheckResult = ValidityVerifier(methodDeclaration, AttributeArguments);
            AttributeGeneration AttributeGeneration = CheckResult.Result;

            if (AttributeGeneration == AttributeGeneration.Invalid)
                return false;
            else if (AttributeGeneration == AttributeGeneration.Valid || (AttributeGeneration == AttributeGeneration.DebugOnly && isDebugGeneration))
                attributeNames.Add(AttributeName);
        }

        return true;
    }

    /// <summary>
    /// Checks whether a list of arguments to <see cref="AccessAttribute"/> are valid.
    /// </summary>
    /// <param name="methodDeclaration">The method with the attribute.</param>
    /// <param name="attributeArguments">The list of arguments.</param>
    public static AttributeValidityCheckResult IsValidAccessAttribute(MethodDeclarationSyntax methodDeclaration, IReadOnlyList<AttributeArgumentSyntax> attributeArguments)
    {
        if (!IsValidStringOnlyAttribute(attributeArguments, out Collection<string> ArgumentValues, out int PositionOfFirstInvalidArgument))
            return AttributeValidityCheckResult.Invalid(PositionOfFirstInvalidArgument);

        for (int i = 0; i < ArgumentValues.Count; i++)
            if (!IsValidModifier(ArgumentValues[i]))
                return AttributeValidityCheckResult.Invalid(i);

        return new AttributeValidityCheckResult(AttributeGeneration.Valid, ArgumentValues, -1);
    }

    /// <summary>
    /// Checks whether a list of arguments to <see cref="RequireNotNullAttribute"/> are valid.
    /// </summary>
    /// <param name="methodDeclaration">The method with the attribute.</param>
    /// <param name="attributeArguments">The list of arguments.</param>
    public static AttributeValidityCheckResult IsValidRequireNotNullAttribute(MethodDeclarationSyntax methodDeclaration, IReadOnlyList<AttributeArgumentSyntax> attributeArguments)
    {
        Contract.RequireNotNull(attributeArguments, out IReadOnlyList<AttributeArgumentSyntax> AttributeArguments);

        if (IsRequireNotNullAttributeWithAliasTypeOrName(AttributeArguments))
            return IsValidRequireNotNullAttributeWithAliasTypeOrName(methodDeclaration, AttributeArguments);
        else if (AttributeArguments.Count > 0)
            return IsValidRequireNotNullAttributeNoAlias(methodDeclaration, AttributeArguments);
        else
            return AttributeValidityCheckResult.Invalid(-1);
    }

    /// <summary>
    /// Checks whether arguments of an attribute include an alias, type or name.
    /// </summary>
    /// <param name="attributeArguments">The attribute arguments.</param>
    public static bool IsRequireNotNullAttributeWithAliasTypeOrName(IReadOnlyList<AttributeArgumentSyntax> attributeArguments)
    {
        Contract.RequireNotNull(attributeArguments, out IReadOnlyList<AttributeArgumentSyntax> AttributeArguments);

        return AttributeArguments.Count > 0 && AttributeArguments.Any(argument => !IsParameterName(argument));
    }

    /// <summary>
    /// Checks whether an argument is a parameter name.
    /// </summary>
    /// <param name="attributeArgument">The attribute argument.</param>
    public static bool IsParameterName(AttributeArgumentSyntax attributeArgument)
    {
        Contract.RequireNotNull(attributeArgument, out AttributeArgumentSyntax AttributeArguments);

        return AttributeArguments.NameEquals is null;
    }

    private static AttributeValidityCheckResult IsValidRequireNotNullAttributeWithAliasTypeOrName(MethodDeclarationSyntax methodDeclaration, IReadOnlyList<AttributeArgumentSyntax> attributeArguments)
    {
        // We reach this step only if IsRequireNotNullAttributeWithAlias() returned true.
        Contract.Assert(attributeArguments.Count > 0);
        AttributeArgumentSyntax FirstAttributeArgument = attributeArguments[0];

        if (FirstAttributeArgument.NameEquals is not null)
            return AttributeValidityCheckResult.Invalid(0);

        if (!IsStringOrNameofAttributeArgument(FirstAttributeArgument, out string ParameterName))
            return AttributeValidityCheckResult.Invalid(0);

        if (!GetParameterType(ParameterName, methodDeclaration, out _))
            return AttributeValidityCheckResult.Invalid(0);

        string Type = string.Empty;
        string Name = string.Empty;
        string AliasName = string.Empty;

        // The first argument has no name, there has to be a second argument because IsRequireNotNullAttributeWithAlias() returned true.
        Contract.Assert(attributeArguments.Count > 1);

        for (int i = 1; i < attributeArguments.Count; i++)
            if (!IsValidArgumentWithAlias(methodDeclaration, attributeArguments[i], ref Type, ref Name, ref AliasName))
                return AttributeValidityCheckResult.Invalid(0);

        // At this step there is at least one valid argument that is either Type, Name or AliasName.
        Contract.Assert(Type != string.Empty || Name != string.Empty || AliasName != string.Empty);

        return new AttributeValidityCheckResult(AttributeGeneration.Valid, [ParameterName], -1);
    }

    private static bool IsValidArgumentWithAlias(MethodDeclarationSyntax methodDeclaration, AttributeArgumentSyntax attributeArgument, ref string type, ref string name, ref string aliasName)
    {
        if (attributeArgument.NameEquals is not NameEqualsSyntax NameEquals)
            return false;

        string ArgumentName = NameEquals.Name.Identifier.Text;

        if (!IsStringOrNameofAttributeArgument(attributeArgument, out string ArgumentValue))
            return false;

        // Valid string or nameof attribute arguments are never empty.
        Contract.Assert(ArgumentValue != string.Empty);

        if (ArgumentName == nameof(RequireNotNullAttribute.Type))
        {
            type = ArgumentValue;

            if (!IsValidTypeName(ArgumentValue))
                return false;
        }
        else if (ArgumentName == nameof(RequireNotNullAttribute.Name))
        {
            name = ArgumentValue;

            if (!SyntaxFacts.IsValidIdentifier(ArgumentValue))
                return false;
        }
        else if (ArgumentName == nameof(RequireNotNullAttribute.AliasName))
        {
            aliasName = ArgumentValue;

            if (!SyntaxFacts.IsValidIdentifier(ArgumentValue))
                return false;
        }
        else
            return false;

        return true;
    }

    private static AttributeValidityCheckResult IsValidRequireNotNullAttributeNoAlias(MethodDeclarationSyntax methodDeclaration, IReadOnlyList<AttributeArgumentSyntax> attributeArguments)
    {
        Collection<string> ArgumentValues = new();

        for (int i = 0; i < attributeArguments.Count; i++)
        {
            AttributeArgumentSyntax AttributeArgument = attributeArguments[i];

            // If not null, we would be running IsValidRequireNotNullAttributeWithAlias().
            Contract.Assert(AttributeArgument.NameEquals is null);

            if (!IsStringOrNameofAttributeArgument(AttributeArgument, out string ArgumentValue))
                return AttributeValidityCheckResult.Invalid(i);

            if (!GetParameterType(ArgumentValue, methodDeclaration, out _))
                return AttributeValidityCheckResult.Invalid(i);

            ArgumentValues.Add(ArgumentValue);
        }

        return new AttributeValidityCheckResult(AttributeGeneration.Valid, ArgumentValues, -1);
    }

    /// <summary>
    /// Checks whether a list of arguments to <see cref="RequireAttribute"/> are valid.
    /// </summary>
    /// <param name="methodDeclaration">The method with the attribute.</param>
    /// <param name="attributeArguments">The list of arguments.</param>
    public static AttributeValidityCheckResult IsValidRequireAttribute(MethodDeclarationSyntax methodDeclaration, IReadOnlyList<AttributeArgumentSyntax> attributeArguments)
    {
        Contract.RequireNotNull(attributeArguments, out IReadOnlyList<AttributeArgumentSyntax> AttributeArguments);

        return IsValidRequireOrEnsureAttribute(methodDeclaration, AttributeArguments);
    }

    /// <summary>
    /// Checks whether a list of arguments to <see cref="EnsureAttribute"/> are valid.
    /// </summary>
    /// <param name="methodDeclaration">The method with the attribute.</param>
    /// <param name="attributeArguments">The list of arguments.</param>
    public static AttributeValidityCheckResult IsValidEnsureAttribute(MethodDeclarationSyntax methodDeclaration, IReadOnlyList<AttributeArgumentSyntax> attributeArguments)
    {
        Contract.RequireNotNull(attributeArguments, out IReadOnlyList<AttributeArgumentSyntax> AttributeArguments);

        return IsValidRequireOrEnsureAttribute(methodDeclaration, AttributeArguments);
    }

    private static AttributeValidityCheckResult IsValidRequireOrEnsureAttribute(MethodDeclarationSyntax methodDeclaration, IReadOnlyList<AttributeArgumentSyntax> attributeArguments)
    {
        if (IsRequireOrEnsureAttributeWithDebugOnly(attributeArguments))
            return IsValidRequireOrEnsureAttributeWithDebugOnly(methodDeclaration, attributeArguments);
        else if (attributeArguments.Count > 0)
            if (IsValidStringOnlyAttribute(attributeArguments, out Collection<string> ArgumentValues, out int PositionOfFirstInvalidArgument))
                return new AttributeValidityCheckResult(AttributeGeneration.Valid, ArgumentValues, -1);
            else
                return AttributeValidityCheckResult.Invalid(PositionOfFirstInvalidArgument);
        else
            return AttributeValidityCheckResult.Invalid(-1);
    }

    /// <summary>
    /// Checks whether arguments of an attribute include DebugOnly.
    /// </summary>
    /// <param name="attributeArguments">The attribute arguments.</param>
    public static bool IsRequireOrEnsureAttributeWithDebugOnly(IReadOnlyList<AttributeArgumentSyntax> attributeArguments)
    {
        Contract.RequireNotNull(attributeArguments, out IReadOnlyList<AttributeArgumentSyntax> AttributeArguments);

        return AttributeArguments.Count > 0 && AttributeArguments.Any(argument => !IsStringExpression(argument));
    }

    /// <summary>
    /// Checks whether an argument is an expression in a string.
    /// </summary>
    /// <param name="attributeArgument">The attribute argument.</param>
    public static bool IsStringExpression(AttributeArgumentSyntax attributeArgument)
    {
        Contract.RequireNotNull(attributeArgument, out AttributeArgumentSyntax AttributeArguments);

        return AttributeArguments.NameEquals is null;
    }

    private static AttributeValidityCheckResult IsValidRequireOrEnsureAttributeWithDebugOnly(MethodDeclarationSyntax methodDeclaration, IReadOnlyList<AttributeArgumentSyntax> attributeArguments)
    {
        // We reach this step only if IsRequireOrEnsureAttributeWithDebugOnly() returned true.
        Contract.Assert(attributeArguments.Count > 0);
        AttributeArgumentSyntax FirstAttributeArgument = attributeArguments[0];

        if (FirstAttributeArgument.NameEquals is not null)
            return AttributeValidityCheckResult.Invalid(0);

        if (!IsStringAttributeArgument(FirstAttributeArgument, out string ArgumentValue))
            return AttributeValidityCheckResult.Invalid(0);

        bool? IsDebugOnly = null;

        // The first argument has no name, there has to be a second argument because IsRequireOrEnsureAttributeWithDebugOnly() returned true.
        Contract.Assert(attributeArguments.Count > 1);

        for (int i = 1; i < attributeArguments.Count; i++)
            if (!IsValidDebugOnlyArgument(methodDeclaration, attributeArguments[i], ref IsDebugOnly))
                return AttributeValidityCheckResult.Invalid(0);

        // At this step the DebugOnly argument must have been processed.
        Contract.Assert(IsDebugOnly.HasValue);

        return new AttributeValidityCheckResult(IsDebugOnly == false ? AttributeGeneration.Valid : AttributeGeneration.DebugOnly, [ArgumentValue], -1);
    }

    private static bool IsValidDebugOnlyArgument(MethodDeclarationSyntax methodDeclaration, AttributeArgumentSyntax attributeArgument, ref bool? isDebugOnly)
    {
        if (attributeArgument.NameEquals is not NameEqualsSyntax NameEquals)
            return false;

        string ArgumentName = NameEquals.Name.Identifier.Text;

        if (!IsBoolAttributeArgument(attributeArgument, out bool ArgumentValue))
            return false;

        if (ArgumentName == nameof(RequireAttribute.DebugOnly))
            isDebugOnly = ArgumentValue;
        else
            return false;

        return true;
    }

    /// <summary>
    /// Checks whether arguments of an attribute are all strings.
    /// </summary>
    /// <param name="attributeArguments">The attribute arguments.</param>
    /// <param name="argumentValues">Argument values as strings if the method returns <see langword="true"/>.</param>
    /// <param name="positionOfFirstInvalidArgument">The 0-based position of the first invalid argument if the method returns <see langword="false"/>.</param>
    public static bool IsValidStringOnlyAttribute(IReadOnlyList<AttributeArgumentSyntax> attributeArguments, out Collection<string> argumentValues, out int positionOfFirstInvalidArgument)
    {
        Contract.RequireNotNull(attributeArguments, out IReadOnlyList<AttributeArgumentSyntax> AttributeArguments);

        argumentValues = new();
        positionOfFirstInvalidArgument = -1;

        if (AttributeArguments.Count == 0)
            return false;

        for (int i = 0; i < AttributeArguments.Count; i++)
        {
            var AttributeArgument = AttributeArguments[i];

            if (!IsStringAttributeArgument(AttributeArgument, out string ArgumentValue))
            {
                positionOfFirstInvalidArgument = i;
                return false;
            }

            argumentValues.Add(ArgumentValue);
        }

        return true;
    }

    /// <summary>
    /// Checks whether the value of an attribute argument is a string or a nameof.
    /// </summary>
    /// <param name="attributeArgument">The attribute argument.</param>
    /// <param name="argumentValue">The string value upon return.</param>
    public static bool IsStringOrNameofAttributeArgument(AttributeArgumentSyntax attributeArgument, out string argumentValue)
    {
        Contract.RequireNotNull(attributeArgument, out AttributeArgumentSyntax AttributeArgument);

        if (IsStringAttributeArgument(AttributeArgument, out argumentValue))
            return true;

        if (IsNameofAttributeArgument(AttributeArgument, out argumentValue))
            return true;

        argumentValue = string.Empty;
        return false;
    }

    /// <summary>
    /// Checks whether the value of an attribute argument is a string.
    /// </summary>
    /// <param name="attributeArgument">The attribute argument.</param>
    /// <param name="argumentValue">The string value upon return.</param>
    public static bool IsStringAttributeArgument(AttributeArgumentSyntax attributeArgument, out string argumentValue)
    {
        Contract.RequireNotNull(attributeArgument, out AttributeArgumentSyntax AttributeArgument);

        if (AttributeArgument.Expression is LiteralExpressionSyntax LiteralExpression &&
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

            // If there is exactly one argument, it cannot be empty, otherwise there would be no argument.
            Contract.Assert(ArgumentText != string.Empty);

            argumentValue = ArgumentText;
            return true;
        }

        argumentValue = string.Empty;
        return false;
    }

    private static bool IsBoolAttributeArgument(AttributeArgumentSyntax attributeArgument, out bool argumentValue)
    {
        if (attributeArgument.Expression is LiteralExpressionSyntax LiteralExpression)
        {
            SyntaxKind Kind = LiteralExpression.Kind();

            if (Kind == SyntaxKind.TrueLiteralExpression)
            {
                argumentValue = true;
                return true;
            }

            if (Kind == SyntaxKind.FalseLiteralExpression)
            {
                argumentValue = false;
                return true;
            }
        }

        argumentValue = false;
        return false;
    }

    /// <summary>
    /// Checks whether a modifier is valid.
    /// </summary>
    /// <param name="modifier">The modifier.</param>
    public static bool IsValidModifier(string modifier)
    {
        List<string> ValidModifiers = new()
        {
            "public",
            "private",
            "protected",
            "internal",
            "file",
            "static",
            "extern",
            "new",
            "virtual",
            "abstract",
            "sealed",
            "override",
            "readonly",
            "unsafe",
            "required",
            "volatile",
            "async",
        };

        return ValidModifiers.Contains(modifier);
    }

    /// <summary>
    /// Checks whether a type name is valid.
    /// </summary>
    /// <param name="typeName">The type name.</param>
    public static bool IsValidTypeName(string typeName)
    {
        Contract.RequireNotNull(typeName, out string TypeName);

        TypeName = AnalyzerTools.Replace(TypeName, "?", ";");
        TypeName = AnalyzerTools.Replace(TypeName, "[]", ";");
        TypeName = AnalyzerTools.Replace(TypeName, "<", ";");
        TypeName = AnalyzerTools.Replace(TypeName, ">", ";");
        TypeName = AnalyzerTools.Replace(TypeName, ".", ";");
        TypeName = AnalyzerTools.Replace(TypeName, "::", ";");
        TypeName = TypeName.Trim(';');

        string[] Identifiers = TypeName.Split(';');

        return Identifiers.ToList().TrueForAll(identifier => SyntaxFacts.IsValidIdentifier(identifier));
    }
}
