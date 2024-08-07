﻿namespace Contracts.Analyzers;

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    private static ContractModel TransformContractAttributes(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        SyntaxNode TargetNode = context.TargetNode;

        Debug.Assert(TargetNode is MethodDeclarationSyntax, $"Expected MethodDeclarationSyntax, but got instead: '{TargetNode}'.");
        MethodDeclarationSyntax MethodDeclaration = (MethodDeclarationSyntax)TargetNode;

        ContractModel Model = GetModelWithoutContract(context, MethodDeclaration);
        Model = Model with { Attributes = GetModelContract(MethodDeclaration) };
        Model = Model with { Documentation = GetMethodDocumentation(Model, MethodDeclaration) };
        Model = Model with { GeneratedMethodDeclaration = GetGeneratedMethodDeclaration(Model, context, out bool IsAsync) };
        (string UsingsBeforeNamespace, string UsingsAfterNamespace) = GetUsings(context, IsAsync);
        Model = Model with { UsingsBeforeNamespace = UsingsBeforeNamespace, UsingsAfterNamespace = UsingsAfterNamespace, IsAsync = IsAsync };

        return Model;
    }

    private static ContractModel GetModelWithoutContract(GeneratorAttributeSyntaxContext context, MethodDeclarationSyntax methodDeclaration)
    {
        var ContainingClass = context.TargetSymbol.ContainingType;
        Debug.Assert(ContainingClass is not null);

        var ContainingNamespace = ContainingClass!.ContainingNamespace;
        Debug.Assert(ContainingNamespace is not null);

        string Namespace = ContainingNamespace!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
        string ClassName = ContainingClass!.Name;

        var ClassDeclaration = methodDeclaration.FirstAncestorOrSelf<ClassDeclarationSyntax>()!;
        Debug.Assert(ClassDeclaration is not null);

        string FullClassName = ClassName;

        if (ClassDeclaration!.TypeParameterList is TypeParameterListSyntax TypeParameterList)
        {
            FullClassName += TypeParameterList.ToString();

            string ConstraintClauses = ClassDeclaration!.ConstraintClauses.ToString();
            if (ConstraintClauses != string.Empty)
                FullClassName += " " + ConstraintClauses;
        }

        string SymbolName = context.TargetSymbol.Name;
        string VerifiedSuffix = Settings.VerifiedSuffix;

        Debug.Assert(GeneratorHelper.StringEndsWith(SymbolName, VerifiedSuffix));
        Debug.Assert(SymbolName.Length > VerifiedSuffix.Length);
        string ShortMethodName = SymbolName.Substring(0, SymbolName.Length - VerifiedSuffix.Length);

        return new ContractModel(
            Namespace: Namespace,
            UsingsBeforeNamespace: string.Empty,
            UsingsAfterNamespace: string.Empty,
            ClassName: ClassName,
            FullClassName: FullClassName,
            ShortMethodName: ShortMethodName,
            UniqueOverloadIdentifier: GetUniqueOverloadIdentifier(methodDeclaration),
            Documentation: string.Empty,
            Attributes: new List<AttributeModel>(),
            GeneratedMethodDeclaration: string.Empty,
            IsAsync: false);
    }

    private static string GetUniqueOverloadIdentifier(MethodDeclarationSyntax methodDeclaration)
    {
        ParameterListSyntax ParameterList = methodDeclaration.ParameterList;
        string Result = string.Empty;

        for (int i = 0; i < ParameterList.Parameters.Count; i++)
        {
            ParameterSyntax Parameter = ParameterList.Parameters[i];

            // Empirically, there is always a type even if the parameter is empty.
            Debug.Assert(Parameter.Type is TypeSyntax);
            TypeSyntax Type = Parameter.Type!;

            string TypeAsString = Type.ToString();
            uint HashCode = unchecked((uint)GeneratorHelper.GetStableHashCode(TypeAsString));
            Result += $"_{HashCode}";
        }

        return Result;
    }

    private static string GetMethodDocumentation(ContractModel model, MethodDeclarationSyntax methodDeclaration)
    {
        string Documentation = string.Empty;

        if (methodDeclaration.HasLeadingTrivia)
        {
            var LeadingTrivia = methodDeclaration.GetLeadingTrivia();

            List<SyntaxTrivia> SupportedTrivias = new();
            foreach (var trivia in LeadingTrivia)
                if (IsSupportedTrivia(trivia))
                    SupportedTrivias.Add(trivia);

            // Trim consecutive end of lines until there is only at most one at the beginning.
            while (CountStartingEndOfLineTrivias(SupportedTrivias) > 1)
                SupportedTrivias.RemoveAt(0);

            // Trim whitespace trivias at start.
            while (IsFirstTriviaWhitespace(SupportedTrivias))
                SupportedTrivias.RemoveAt(0);

            // Remove successive whitespace trivias.
            int i = 0;
            while (i + 1 < SupportedTrivias.Count)
                if (SupportedTrivias[i].IsKind(SyntaxKind.WhitespaceTrivia) && SupportedTrivias[i + 1].IsKind(SyntaxKind.WhitespaceTrivia))
                    SupportedTrivias.RemoveAt(i);
                else
                    i++;

            LeadingTrivia = SyntaxFactory.TriviaList(SupportedTrivias);

            foreach (var Trivia in LeadingTrivia)
                if (Trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
                {
                    Documentation = LeadingTrivia.ToString().Trim('\r').Trim('\n').TrimEnd(' ');
                    break;
                }
        }

        Dictionary<string, string> ParameterNameReplacementTable = GetParameterNameReplacementTable(model, methodDeclaration);
        foreach (KeyValuePair<string, string> Entry in ParameterNameReplacementTable)
        {
            string OldParameterName = $"<param name=\"{Entry.Key}\">";
            string NewParameterName = $"<param name=\"{Entry.Value}\">";
#if NETSTANDARD2_1_OR_GREATER
            Documentation = Documentation.Replace(OldParameterName, NewParameterName, StringComparison.Ordinal);
#else
            Documentation = Documentation.Replace(OldParameterName, NewParameterName);
#endif

            string OldParameterRef = $"<paramref name=\"{Entry.Key}\"/>";
            string NewParameterRef = $"<paramref name=\"{Entry.Value}\"/>";
#if NETSTANDARD2_1_OR_GREATER
            Documentation = Documentation.Replace(OldParameterRef, NewParameterRef, StringComparison.Ordinal);
#else
            Documentation = Documentation.Replace(OldParameterRef, NewParameterRef);
#endif
        }

        return Documentation;
    }

    private static bool IsSupportedTrivia(SyntaxTrivia trivia)
    {
        return trivia.IsKind(SyntaxKind.EndOfLineTrivia) ||
               trivia.IsKind(SyntaxKind.WhitespaceTrivia) ||
               trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
               trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia);
    }

    private static bool IsFirstTriviaWhitespace(IList<SyntaxTrivia> trivias)
    {
        if (trivias.Count == 0)
            return false;

        SyntaxTrivia FirstTrivia = trivias[0];

        if (!FirstTrivia.IsKind(SyntaxKind.WhitespaceTrivia))
            return false;

        return true;
    }

    private static int CountStartingEndOfLineTrivias(List<SyntaxTrivia> trivias)
    {
        int Count = 0;

        for (int i = 0; i < trivias.Count; i++)
        {
            SyntaxTrivia Trivia = trivias[i];

            if (Trivia.IsKind(SyntaxKind.EndOfLineTrivia))
                Count++;
            else if (!Trivia.IsKind(SyntaxKind.WhitespaceTrivia))
                break;
        }

        return Count;
    }

    private static Dictionary<string, string> GetParameterNameReplacementTable(ContractModel model, MethodDeclarationSyntax methodDeclaration)
    {
        Dictionary<string, string> Result = new();

        ParameterListSyntax ParameterList = methodDeclaration.ParameterList;
        SeparatedSyntaxList<ParameterSyntax> Parameters = ParameterList.Parameters;

        foreach (var Parameter in Parameters)
            if (IsParameterNameReplaced(model, Parameter, out string ParameterName, out string ReplacementName))
                Result.Add(ParameterName, ReplacementName);

        return Result;
    }

    private static bool IsParameterNameReplaced(ContractModel model, ParameterSyntax parameter, out string parameterName, out string replacementName)
    {
        parameterName = string.Empty;
        replacementName = string.Empty;

        foreach (AttributeModel Attribute in model.Attributes)
            if (AttributeHasTypeOrName(Attribute, out parameterName, out _, out replacementName) && parameterName == parameter.Identifier.Text && replacementName != string.Empty)
                return true;

        return false;
    }

    private static List<AttributeModel> GetModelContract(MethodDeclarationSyntax methodDeclaration)
    {
        List<AttributeModel> Result = new();
        List<AttributeSyntax> MethodAttributes = GeneratorHelper.GetMethodSupportedAttributes(methodDeclaration, SupportedAttributeNames);

        Dictionary<string, Func<MethodDeclarationSyntax, IReadOnlyList<AttributeArgumentSyntax>, List<AttributeArgumentModel>>> AttributeTransformTable = new()
        {
            { nameof(AccessAttribute), TransformAccessAttribute },
            { nameof(RequireNotNullAttribute), TransformRequireNotNullAttribute },
            { nameof(RequireAttribute), TransformRequireAttribute },
            { nameof(EnsureAttribute), TransformEnsureAttribute },
        };

        foreach (AttributeSyntax Attribute in MethodAttributes)
            if (Attribute.ArgumentList is AttributeArgumentListSyntax AttributeArgumentList)
            {
                string AttributeName = GeneratorHelper.ToAttributeName(Attribute);
                IReadOnlyList<AttributeArgumentSyntax> AttributeArguments = AttributeArgumentList.Arguments;

                Debug.Assert(AttributeTransformTable.ContainsKey(AttributeName));
                var AttributeTransform = AttributeTransformTable[AttributeName];
                List<AttributeArgumentModel> Arguments = AttributeTransform(methodDeclaration, AttributeArguments);

                AttributeModel Model = new(AttributeName, Arguments);

                Result.Add(Model);
            }

        return Result;
    }

    private static List<AttributeArgumentModel> TransformAccessAttribute(MethodDeclarationSyntax methodDeclaration, IReadOnlyList<AttributeArgumentSyntax> attributeArguments)
    {
        return TransformStringOnlyAttribute(methodDeclaration, attributeArguments);
    }

    private static List<AttributeArgumentModel> TransformRequireNotNullAttribute(MethodDeclarationSyntax methodDeclaration, IReadOnlyList<AttributeArgumentSyntax> attributeArguments)
    {
        if (IsRequireNotNullAttributeWithAlias(attributeArguments))
            return TransformRequireNotNullAttributeWithAlias(methodDeclaration, attributeArguments);
        else
            return TransformRequireNotNullAttributeNoAlias(methodDeclaration, attributeArguments);
    }

    private static List<AttributeArgumentModel> TransformRequireNotNullAttributeWithAlias(MethodDeclarationSyntax methodDeclaration, IReadOnlyList<AttributeArgumentSyntax> attributeArguments)
    {
        Debug.Assert(attributeArguments.Count > 0, "This was verified in IsRequireNotNullAttributeWithAlias().");
        AttributeArgumentSyntax FirstAttributeArgument = attributeArguments[0];

        Debug.Assert(FirstAttributeArgument.NameEquals is null, "This was verified in IsValidRequireNotNullAttributeWithAlias().");

        bool IsValidParameterName = IsStringOrNameofAttributeArgument(FirstAttributeArgument, out string ParameterName);
        Debug.Assert(IsValidParameterName, "This was verified in IsValidRequireNotNullAttributeWithAlias().");

        bool IsValidParameterType = GetParameterType(ParameterName, methodDeclaration, out _);
        Debug.Assert(IsValidParameterType, "This was verified in IsValidRequireNotNullAttributeWithAlias().");

        string Type = string.Empty;
        string Name = string.Empty;
        string AliasName = string.Empty;

        for (int i = 1; i < attributeArguments.Count; i++)
        {
            bool IsValidAttributeArgument = IsValidArgumentWithAlias(methodDeclaration, attributeArguments[i], ref Type, ref Name, ref AliasName);
            Debug.Assert(IsValidAttributeArgument, "This was verified in IsValidRequireNotNullAttributeWithAlias().");
        }

        Debug.Assert(Type != string.Empty || Name != string.Empty || AliasName != string.Empty, "This was verified in IsValidRequireNotNullAttributeWithAlias().");

        List<AttributeArgumentModel> Result = new() { new AttributeArgumentModel(Name: string.Empty, Value: ParameterName) };

        if (Type != string.Empty)
            Result.Add(new AttributeArgumentModel(Name: nameof(RequireNotNullAttribute.Type), Value: Type));

        if (Name != string.Empty)
            Result.Add(new AttributeArgumentModel(Name: nameof(RequireNotNullAttribute.Name), Value: Name));

        if (AliasName != string.Empty)
            Result.Add(new AttributeArgumentModel(Name: nameof(RequireNotNullAttribute.AliasName), Value: AliasName));

        Debug.Assert(Result.Count > 1);

        return Result;
    }

    private static List<AttributeArgumentModel> TransformRequireNotNullAttributeNoAlias(MethodDeclarationSyntax methodDeclaration, IReadOnlyList<AttributeArgumentSyntax> attributeArguments)
    {
        List<AttributeArgumentModel> Result = new();

        foreach (var AttributeArgument in attributeArguments)
        {
            Debug.Assert(AttributeArgument.NameEquals is null, "This was verified in IsValidRequireNotNullAttributeWithAlias().");

            bool IsValidParameterName = IsStringOrNameofAttributeArgument(AttributeArgument, out string ParameterName);
            Debug.Assert(IsValidParameterName, "This was verified in IsValidRequireNotNullAttributeWithAlias().");

            Result.Add(new AttributeArgumentModel(Name: string.Empty, Value: ParameterName));
        }

        return Result;
    }

    private static List<AttributeArgumentModel> TransformRequireAttribute(MethodDeclarationSyntax methodDeclaration, IReadOnlyList<AttributeArgumentSyntax> attributeArguments)
    {
        return TransformRequireOrEnsureAttribute(methodDeclaration, attributeArguments);
    }

    private static List<AttributeArgumentModel> TransformEnsureAttribute(MethodDeclarationSyntax methodDeclaration, IReadOnlyList<AttributeArgumentSyntax> attributeArguments)
    {
        return TransformRequireOrEnsureAttribute(methodDeclaration, attributeArguments);
    }

    private static List<AttributeArgumentModel> TransformRequireOrEnsureAttribute(MethodDeclarationSyntax methodDeclaration, IReadOnlyList<AttributeArgumentSyntax> attributeArguments)
    {
        if (IsRequireOrEnsureAttributeWithDebugOnly(attributeArguments))
            return TransformRequireOrEnsureAttributeWithDebugOnly(methodDeclaration, attributeArguments);
        else
            return TransformStringOnlyAttribute(methodDeclaration, attributeArguments);
    }

    private static List<AttributeArgumentModel> TransformRequireOrEnsureAttributeWithDebugOnly(MethodDeclarationSyntax methodDeclaration, IReadOnlyList<AttributeArgumentSyntax> attributeArguments)
    {
        Debug.Assert(attributeArguments.Count > 0, "This was verified in IsRequireOrEnsureAttributeWithDebugOnly().");
        AttributeArgumentSyntax FirstAttributeArgument = attributeArguments[0];

        Debug.Assert(FirstAttributeArgument.NameEquals is null, "This was verified in IsValidRequireOrEnsureAttributeWithDebugOnly().");

        bool IsValidParameterName = IsStringAttributeArgument(FirstAttributeArgument, out string Expression);
        Debug.Assert(IsValidParameterName, "This was verified in IsValidRequireOrEnsureAttributeWithDebugOnly().");

        bool? IsDebugOnly = null;

        for (int i = 1; i < attributeArguments.Count; i++)
        {
            bool IsValidAttributeArgument = IsValidDebugOnlyArgument(methodDeclaration, attributeArguments[i], ref IsDebugOnly);
            Debug.Assert(IsValidAttributeArgument, "This was verified in IsValidRequireOrEnsureAttributeWithDebugOnly().");
        }

        Debug.Assert(IsDebugOnly.HasValue, "This was verified in IsValidRequireNotNullAttributeWithAlias().");

        List<AttributeArgumentModel> Result = new()
        {
            new AttributeArgumentModel(Name: string.Empty, Value: Expression),
            new AttributeArgumentModel(Name: nameof(RequireAttribute.DebugOnly), Value: IsDebugOnly == true ? "true" : "false"),
        };

        return Result;
    }

    private static List<AttributeArgumentModel> TransformStringOnlyAttribute(MethodDeclarationSyntax methodDeclaration, IReadOnlyList<AttributeArgumentSyntax> attributeArguments)
    {
        bool IsValid = IsValidStringOnlyAttribute(methodDeclaration, attributeArguments, out List<string> ArgumentValues);
        Debug.Assert(IsValid);

        List<AttributeArgumentModel> Result = new();
        foreach (string ArgumentValue in ArgumentValues)
            Result.Add(new AttributeArgumentModel(Name: string.Empty, Value: ArgumentValue));

        return Result;
    }

    private static (string BeforeNamespaceDeclaration, string AfterNamespaceDeclaration) GetUsings(GeneratorAttributeSyntaxContext context, bool isAsync)
    {
        string RawBeforeNamespaceDeclaration = string.Empty;
        string RawAfterNamespaceDeclaration = string.Empty;

        SyntaxNode TargetNode = context.TargetNode;
        BaseNamespaceDeclarationSyntax? BaseNamespaceDeclaration = TargetNode.FirstAncestorOrSelf<BaseNamespaceDeclarationSyntax>();

        Debug.Assert(BaseNamespaceDeclaration is not null, "We know it's not null from KeepNodeForPipeline().");
        RawAfterNamespaceDeclaration = BaseNamespaceDeclaration!.Usings.ToFullString();

        if (BaseNamespaceDeclaration.Parent is CompilationUnitSyntax CompilationUnit)
            RawBeforeNamespaceDeclaration = CompilationUnit.Usings.ToFullString();

        return FixUsings(RawBeforeNamespaceDeclaration, RawAfterNamespaceDeclaration, isAsync);
    }

    private static (string BeforeNamespaceDeclaration, string AfterNamespaceDeclaration) FixUsings(string rawBeforeNamespaceDeclaration, string rawAfterNamespaceDeclaration, bool isAsync)
    {
        string BeforeNamespaceDeclaration = GeneratorHelper.SortUsings(rawBeforeNamespaceDeclaration);
        string AfterNamespaceDeclaration = GeneratorHelper.SortUsings(rawAfterNamespaceDeclaration);
        bool UseGlobal = GeneratorHelper.HasGlobalSystem(AfterNamespaceDeclaration);

        (BeforeNamespaceDeclaration, AfterNamespaceDeclaration) = AddMissingUsing(BeforeNamespaceDeclaration, AfterNamespaceDeclaration, "Contracts", isGlobal: false);
        (BeforeNamespaceDeclaration, AfterNamespaceDeclaration) = AddMissingUsing(BeforeNamespaceDeclaration, AfterNamespaceDeclaration, "System.CodeDom.Compiler", isGlobal: UseGlobal);

        if (isAsync)
            (BeforeNamespaceDeclaration, AfterNamespaceDeclaration) = AddMissingUsing(BeforeNamespaceDeclaration, AfterNamespaceDeclaration, "System.Threading.Tasks", isGlobal: UseGlobal);

        AfterNamespaceDeclaration = GeneratorHelper.SortUsings(AfterNamespaceDeclaration);

        return (BeforeNamespaceDeclaration, AfterNamespaceDeclaration);
    }

    private static (string BeforeNamespaceDeclaration, string AfterNamespaceDeclaration) AddMissingUsing(string beforeNamespaceDeclaration, string afterNamespaceDeclaration, string usingDirective, bool isGlobal)
    {
        string GlobalDirective = $"using global::{usingDirective};\n";
        string NonGlobalDirective = $"using {usingDirective};\n";
        bool IsDirectiveBeforeNamespace;
        bool IsDirectiveAfterNamespace;

#if NETSTANDARD2_1_OR_GREATER
        IsDirectiveBeforeNamespace = beforeNamespaceDeclaration.Contains(GlobalDirective, StringComparison.Ordinal) || beforeNamespaceDeclaration.Contains(NonGlobalDirective, StringComparison.Ordinal);
        IsDirectiveAfterNamespace = afterNamespaceDeclaration.Contains(GlobalDirective, StringComparison.Ordinal) || afterNamespaceDeclaration.Contains(NonGlobalDirective, StringComparison.Ordinal);
#else
        IsDirectiveBeforeNamespace = beforeNamespaceDeclaration.Contains(GlobalDirective) || beforeNamespaceDeclaration.Contains(NonGlobalDirective);
        IsDirectiveAfterNamespace = afterNamespaceDeclaration.Contains(GlobalDirective) || afterNamespaceDeclaration.Contains(NonGlobalDirective);
#endif

        if (!IsDirectiveBeforeNamespace && !IsDirectiveAfterNamespace)
            afterNamespaceDeclaration += isGlobal ? GlobalDirective : NonGlobalDirective;

        return (beforeNamespaceDeclaration, afterNamespaceDeclaration);
    }
}
