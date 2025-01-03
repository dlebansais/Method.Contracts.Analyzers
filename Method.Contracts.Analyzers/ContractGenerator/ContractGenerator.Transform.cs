﻿namespace Contracts.Analyzers;

using System;
using System.Collections.Generic;
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
    private static ContractModel TransformContractAttributes(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        SyntaxNode TargetNode = context.TargetNode;
        MemberDeclarationSyntax MemberDeclaration = Contract.AssertOfType<MemberDeclarationSyntax>(TargetNode);

        ContractModel Model = GetModelWithoutContract(context, MemberDeclaration);
        Model = Model with { Attributes = GetModelContract(MemberDeclaration) };
        Model = Model with { Documentation = GetMemberDocumentation(Model, MemberDeclaration) };
        Model = Model with { GeneratedMethodDeclaration = GetGeneratedMethodDeclaration(Model, context, out bool IsAsync) };
        Model = Model with { GeneratedPropertyDeclaration = GetGeneratedPropertyDeclaration(Model, context) };
        (string UsingsBeforeNamespace, string UsingsAfterNamespace) = GetUsings(context, IsAsync);
        Model = Model with
        {
            UsingsBeforeNamespace = UsingsBeforeNamespace,
            UsingsAfterNamespace = UsingsAfterNamespace,
            IsAsync = IsAsync,
        };

        return Model;
    }

    private static ContractModel GetModelWithoutContract(GeneratorAttributeSyntaxContext context, MemberDeclarationSyntax memberDeclaration)
    {
        INamedTypeSymbol ContainingClass = Contract.AssertNotNull(context.TargetSymbol.ContainingType);
        INamespaceSymbol ContainingNamespace = Contract.AssertNotNull(ContainingClass.ContainingNamespace);

        string Namespace = ContainingNamespace.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
        string ClassName = ContainingClass.Name;
        string? DeclarationTokens = null;
        string? FullClassName = null;

        if (memberDeclaration.FirstAncestorOrSelf<ClassDeclarationSyntax>() is ClassDeclarationSyntax ClassDeclaration)
        {
            DeclarationTokens = "class";
            FullClassName = ClassName;
            TypeParameterListSyntax? TypeParameterList = ClassDeclaration.TypeParameterList;

            if (TypeParameterList is not null)
            {
                FullClassName += TypeParameterList.ToString();

                string ConstraintClauses = ClassDeclaration.ConstraintClauses.ToString();
                if (ConstraintClauses != string.Empty)
                    FullClassName += " " + ConstraintClauses;
            }
        }

        if (memberDeclaration.FirstAncestorOrSelf<StructDeclarationSyntax>() is StructDeclarationSyntax StructDeclaration)
        {
            DeclarationTokens = "struct";
            FullClassName = ClassName;
            TypeParameterListSyntax? TypeParameterList = StructDeclaration.TypeParameterList;

            if (TypeParameterList is not null)
            {
                FullClassName += TypeParameterList.ToString();

                string ConstraintClauses = StructDeclaration.ConstraintClauses.ToString();
                if (ConstraintClauses != string.Empty)
                    FullClassName += " " + ConstraintClauses;
            }
        }

        if (memberDeclaration.FirstAncestorOrSelf<RecordDeclarationSyntax>() is RecordDeclarationSyntax RecordDeclaration)
        {
            DeclarationTokens = RecordDeclaration.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword) ? "record struct" : "record";
            FullClassName = ClassName;
            TypeParameterListSyntax? TypeParameterList = RecordDeclaration.TypeParameterList;

            if (TypeParameterList is not null)
            {
                FullClassName += TypeParameterList.ToString();

                string ConstraintClauses = RecordDeclaration.ConstraintClauses.ToString();
                if (ConstraintClauses != string.Empty)
                    FullClassName += " " + ConstraintClauses;
            }
        }

        string SymbolName = context.TargetSymbol.Name;
        string VerifiedSuffix = Settings.VerifiedSuffix;

        Contract.Assert(GeneratorHelper.StringEndsWith(SymbolName, VerifiedSuffix));
        Contract.Assert(SymbolName.Length > VerifiedSuffix.Length);
        string ShortName = SymbolName[..^VerifiedSuffix.Length];

        return new ContractModel(
            Namespace: Namespace,
            UsingsBeforeNamespace: string.Empty,
            UsingsAfterNamespace: string.Empty,
            ClassName: ClassName,
            DeclarationTokens: Contract.AssertNotNull(DeclarationTokens),
            FullClassName: Contract.AssertNotNull(FullClassName),
            ShortName: ShortName,
            UniqueOverloadIdentifier: GetUniqueOverloadIdentifier(memberDeclaration),
            Documentation: string.Empty,
            Attributes: [],
            GeneratedMethodDeclaration: string.Empty,
            GeneratedPropertyDeclaration: string.Empty,
            IsAsync: false);
    }

    private static string GetUniqueOverloadIdentifier(MemberDeclarationSyntax memberDeclaration) => memberDeclaration is MethodDeclarationSyntax MethodDeclaration ? GetUniqueOverloadIdentifier(MethodDeclaration) : "_get";

    private static string GetUniqueOverloadIdentifier(MethodDeclarationSyntax methodDeclaration)
    {
        ParameterListSyntax ParameterList = methodDeclaration.ParameterList;
        string Result = string.Empty;

        for (int i = 0; i < ParameterList.Parameters.Count; i++)
        {
            ParameterSyntax Parameter = ParameterList.Parameters[i];

            // Empirically, there is always a type even if the parameter is empty.
            TypeSyntax Type = Contract.AssertNotNull(Parameter.Type);

            string TypeAsString = Type.ToString();
            uint HashCode = unchecked((uint)GeneratorHelper.GetStableHashCode(TypeAsString));
            Result += $"_{HashCode}";
        }

        return Result;
    }

    private static string GetMemberDocumentation(ContractModel model, MemberDeclarationSyntax memberDeclaration)
    {
        string Documentation = string.Empty;

        if (memberDeclaration.HasLeadingTrivia)
        {
            SyntaxTriviaList LeadingTrivia = memberDeclaration.GetLeadingTrivia();

            List<SyntaxTrivia> SupportedTrivias = [];
            foreach (SyntaxTrivia trivia in LeadingTrivia)
                if (IsSupportedTrivia(trivia))
                    SupportedTrivias.Add(trivia);

            // Trim consecutive end of lines until there is only at most one at the beginning.
            bool HadEndOfLine = false;
            while (CountStartingEndOfLineTrivias(SupportedTrivias) > 1)
            {
                HadEndOfLine = true;
                SupportedTrivias.RemoveAt(0);
            }

            if (HadEndOfLine)
            {
                // Trim whitespace trivias at start.
                while (IsFirstTriviaWhitespace(SupportedTrivias))
                    SupportedTrivias.RemoveAt(0);
            }

            // Remove successive whitespace trivias.
            int i = 0;
            while (i + 1 < SupportedTrivias.Count)
                if (SupportedTrivias[i].IsKind(SyntaxKind.WhitespaceTrivia) && SupportedTrivias[i + 1].IsKind(SyntaxKind.WhitespaceTrivia))
                    SupportedTrivias.RemoveAt(i);
                else
                    i++;

            LeadingTrivia = SyntaxFactory.TriviaList(SupportedTrivias);

            foreach (SyntaxTrivia Trivia in LeadingTrivia)
                if (Trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
                {
                    Documentation = LeadingTrivia.ToFullString().Trim('\r').Trim('\n').TrimEnd(' ');
                    break;
                }
        }

        Dictionary<string, string> ParameterNameReplacementTable = memberDeclaration is MethodDeclarationSyntax MethodDeclaration
            ? GetParameterNameReplacementTable(model, MethodDeclaration)
            : [];

        foreach (KeyValuePair<string, string> Entry in ParameterNameReplacementTable)
        {
            string OldParameterName = $"<param name=\"{Entry.Key}\">";
            string NewParameterName = $"<param name=\"{Entry.Value}\">";
            Documentation = AnalyzerTools.Replace(Documentation, OldParameterName, NewParameterName);

            string OldParameterRef = $"<paramref name=\"{Entry.Key}\"/>";
            string NewParameterRef = $"<paramref name=\"{Entry.Value}\"/>";
            Documentation = AnalyzerTools.Replace(Documentation, OldParameterRef, NewParameterRef);
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

    private static bool IsFirstTriviaWhitespace(List<SyntaxTrivia> trivias)
    {
        // If we reach this method there is at least one end of line, therefore at least one trivia.
        Contract.Assert(trivias.Count > 0);

        SyntaxTrivia FirstTrivia = trivias.First();

        return FirstTrivia.IsKind(SyntaxKind.WhitespaceTrivia);
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
        Dictionary<string, string> Result = [];

        ParameterListSyntax ParameterList = methodDeclaration.ParameterList;
        SeparatedSyntaxList<ParameterSyntax> Parameters = ParameterList.Parameters;

        foreach (ParameterSyntax Parameter in Parameters)
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

    private static List<AttributeModel> GetModelContract(MemberDeclarationSyntax memberDeclaration)
    {
        List<AttributeModel> Result = [];
        List<AttributeSyntax> MemberAttributes = GeneratorHelper.GetMemberSupportedAttributes(context: null, memberDeclaration, SupportedAttributeTypes);

        Dictionary<string, Func<MemberDeclarationSyntax, IReadOnlyList<AttributeArgumentSyntax>, List<AttributeArgumentModel>>> AttributeTransformTable = new()
        {
            { nameof(AccessAttribute), TransformAccessAttribute },
            { nameof(RequireNotNullAttribute), TransformRequireNotNullAttribute },
            { nameof(RequireAttribute), TransformRequireAttribute },
            { nameof(EnsureAttribute), TransformEnsureAttribute },
        };

        foreach (AttributeSyntax Attribute in MemberAttributes)
            if (Attribute.ArgumentList is AttributeArgumentListSyntax AttributeArgumentList)
            {
                string AttributeName = GeneratorHelper.ToAttributeName(Attribute);
                IReadOnlyList<AttributeArgumentSyntax> AttributeArguments = AttributeArgumentList.Arguments;

                Contract.Assert(AttributeTransformTable.ContainsKey(AttributeName));
                Func<MemberDeclarationSyntax, IReadOnlyList<AttributeArgumentSyntax>, List<AttributeArgumentModel>> AttributeTransform = AttributeTransformTable[AttributeName];
                List<AttributeArgumentModel> Arguments = AttributeTransform(memberDeclaration, AttributeArguments);

                AttributeModel Model = new(AttributeName, Arguments);

                Result.Add(Model);
            }

        return Result;
    }

    private static List<AttributeArgumentModel> TransformAccessAttribute(MemberDeclarationSyntax memberDeclaration, IReadOnlyList<AttributeArgumentSyntax> attributeArguments) => TransformStringOnlyAttribute(attributeArguments);

    private static List<AttributeArgumentModel> TransformRequireNotNullAttribute(MemberDeclarationSyntax memberDeclaration, IReadOnlyList<AttributeArgumentSyntax> attributeArguments)
    {
        Contract.Assert(memberDeclaration is MethodDeclarationSyntax);
        MethodDeclarationSyntax MethodDeclarationSyntax = (MethodDeclarationSyntax)memberDeclaration;

        return IsRequireNotNullAttributeWithAliasTypeOrName(attributeArguments)
            ? TransformRequireNotNullAttributeWithAlias(MethodDeclarationSyntax, attributeArguments)
            : TransformRequireNotNullAttributeNoAlias(attributeArguments);
    }

    private static List<AttributeArgumentModel> TransformRequireNotNullAttributeWithAlias(MethodDeclarationSyntax methodDeclaration, IReadOnlyList<AttributeArgumentSyntax> attributeArguments)
    {
        // This was verified in IsRequireNotNullAttributeWithAlias().
        Contract.Assert(attributeArguments.Count > 0);
        AttributeArgumentSyntax FirstAttributeArgument = attributeArguments[0];

        // This was verified in IsValidRequireNotNullAttributeWithAlias().
        Contract.Assert(FirstAttributeArgument.NameEquals is null);

        bool IsValidParameterName = IsStringOrNameofAttributeArgument(FirstAttributeArgument, out string ParameterName);

        // This was verified in IsValidRequireNotNullAttributeWithAlias().
        Contract.Assert(IsValidParameterName);

        bool IsValidParameterType = GetParameterType(ParameterName, methodDeclaration, out _);

        // This was verified in IsValidRequireNotNullAttributeWithAlias().
        Contract.Assert(IsValidParameterType);

        string Type = string.Empty;
        string Name = string.Empty;
        string AliasName = string.Empty;

        for (int i = 1; i < attributeArguments.Count; i++)
        {
            bool IsValidAttributeArgument = IsValidArgumentWithAliasTypeOrName(attributeArguments[i], ref Type, ref Name, ref AliasName);

            // This was verified in IsValidRequireNotNullAttributeWithAlias().
            Contract.Assert(IsValidAttributeArgument);
        }

        // This was verified in IsValidRequireNotNullAttributeWithAlias().
        Contract.Assert(Type != string.Empty || Name != string.Empty || AliasName != string.Empty);

        List<AttributeArgumentModel> Result = [new AttributeArgumentModel(Name: string.Empty, Value: ParameterName)];

        if (Type != string.Empty)
            Result.Add(new AttributeArgumentModel(Name: nameof(RequireNotNullAttribute.Type), Value: Type));

        if (Name != string.Empty)
            Result.Add(new AttributeArgumentModel(Name: nameof(RequireNotNullAttribute.Name), Value: Name));

        if (AliasName != string.Empty)
            Result.Add(new AttributeArgumentModel(Name: nameof(RequireNotNullAttribute.AliasName), Value: AliasName));

        Contract.Assert(Result.Count > 1);

        return Result;
    }

    private static List<AttributeArgumentModel> TransformRequireNotNullAttributeNoAlias(IReadOnlyList<AttributeArgumentSyntax> attributeArguments)
    {
        List<AttributeArgumentModel> Result = [];

        foreach (AttributeArgumentSyntax AttributeArgument in attributeArguments)
        {
            // This was verified in IsValidRequireNotNullAttributeWithAlias().
            Contract.Assert(AttributeArgument.NameEquals is null);

            bool IsValidParameterName = IsStringOrNameofAttributeArgument(AttributeArgument, out string ParameterName);

            // This was verified in IsValidRequireNotNullAttributeWithAlias().
            Contract.Assert(IsValidParameterName);

            Result.Add(new AttributeArgumentModel(Name: string.Empty, Value: ParameterName));
        }

        return Result;
    }

    private static List<AttributeArgumentModel> TransformRequireAttribute(MemberDeclarationSyntax memberDeclaration, IReadOnlyList<AttributeArgumentSyntax> attributeArguments) => TransformRequireOrEnsureAttribute(attributeArguments);

    private static List<AttributeArgumentModel> TransformEnsureAttribute(MemberDeclarationSyntax memberDeclaration, IReadOnlyList<AttributeArgumentSyntax> attributeArguments) => TransformRequireOrEnsureAttribute(attributeArguments);

    private static List<AttributeArgumentModel> TransformRequireOrEnsureAttribute(IReadOnlyList<AttributeArgumentSyntax> attributeArguments)
    {
        return IsRequireOrEnsureAttributeWithDebugOnly(attributeArguments)
            ? TransformRequireOrEnsureAttributeWithDebugOnly(attributeArguments)
            : TransformStringOnlyAttribute(attributeArguments);
    }

    private static List<AttributeArgumentModel> TransformRequireOrEnsureAttributeWithDebugOnly(IReadOnlyList<AttributeArgumentSyntax> attributeArguments)
    {
        // This was verified in IsRequireOrEnsureAttributeWithDebugOnly().
        Contract.Assert(attributeArguments.Count > 0);
        AttributeArgumentSyntax FirstAttributeArgument = attributeArguments[0];

        // This was verified in IsValidRequireOrEnsureAttributeWithDebugOnly().
        Contract.Assert(FirstAttributeArgument.NameEquals is null);

        bool IsValidParameterName = IsStringAttributeArgument(FirstAttributeArgument, out string Expression);

        // This was verified in IsValidRequireOrEnsureAttributeWithDebugOnly().
        Contract.Assert(IsValidParameterName);

        bool? IsDebugOnly = null;

        for (int i = 1; i < attributeArguments.Count; i++)
        {
            bool IsValidAttributeArgument = IsValidDebugOnlyArgument(attributeArguments[i], ref IsDebugOnly);

            // This was verified in IsValidRequireOrEnsureAttributeWithDebugOnly().
            Contract.Assert(IsValidAttributeArgument);
        }

        // This was verified in IsValidRequireNotNullAttributeWithAlias().
        Contract.Assert(IsDebugOnly.HasValue);

        List<AttributeArgumentModel> Result =
        [
            new AttributeArgumentModel(Name: string.Empty, Value: Expression),
            new AttributeArgumentModel(Name: nameof(RequireAttribute.DebugOnly), Value: IsDebugOnly == true ? "true" : "false"),
        ];

        return Result;
    }

    private static List<AttributeArgumentModel> TransformStringOnlyAttribute(IReadOnlyList<AttributeArgumentSyntax> attributeArguments)
    {
        bool IsValid = IsValidStringOnlyAttribute(attributeArguments, out Collection<string> ArgumentValues, out _);
        Contract.Assert(IsValid);

        List<AttributeArgumentModel> Result = [];
        foreach (string ArgumentValue in ArgumentValues)
            Result.Add(new AttributeArgumentModel(Name: string.Empty, Value: ArgumentValue));

        return Result;
    }

    private static (string BeforeNamespaceDeclaration, string AfterNamespaceDeclaration) GetUsings(GeneratorAttributeSyntaxContext context, bool isAsync)
    {
        string RawBeforeNamespaceDeclaration = string.Empty;
        string RawAfterNamespaceDeclaration = string.Empty;

        // We know it's not null from KeepNodeForPipeline().
        BaseNamespaceDeclarationSyntax BaseNamespaceDeclaration = Contract.AssertNotNull(context.TargetNode.FirstAncestorOrSelf<BaseNamespaceDeclarationSyntax>());

        RawAfterNamespaceDeclaration = BaseNamespaceDeclaration.Usings.ToFullString();

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
